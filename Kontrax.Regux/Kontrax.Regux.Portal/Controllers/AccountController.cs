using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Account;
using Kontrax.Regux.Model.Administration;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Portal.Attributes;
using Kontrax.Regux.Portal.Audit;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Service;
using Kontrax.Regux.Shared.Portal.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        public const string LoginFailedMessage = "Грешна парола или e-mail адрес.";
        private const string _youCanLoginMessage = "Може да влезете в системата.";

        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? (_userManager = CreateUserManager()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }
            }
            base.Dispose(disposing);
        }

        public ActionResult Login(string userName, string returnUrl)
        {
            return View(new LoginModel
            {
                UserName = userName,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        // Временно без ограничение, за да може потребителят да се сменя много пъти в минута - за тестови цели.
        //[AllowXRequestsEveryXSeconds(Name = "LogOn", ContentName = "TooManyRequests", Requests = 3, Seconds = 60)]
        public async Task<ActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userName = model.UserName;
            ApplicationUser user = await UserManager.FindByNameAsync(userName);
            string logErrorMessage =
                user == null ? $"не съществува потребител \"{userName}\"."
                : user.IsBanned ? $"потребител \"{userName}\" е блокиран."
                : !string.IsNullOrEmpty(user.Email) && !user.EmailConfirmed ? $"потребител \"{userName}\" не е потвърдил e-mail адреса си."
                : user.IsApproved != true ? $"потребител \"{userName}\" не е одобрен от администратор."
                : null;
            if (logErrorMessage != null)
            {
                // Видимото съобщение при липсващ, блокиран, с непотвърден e-mail или отказан потребител е същото,
                // като при грешна парола, за да не се разкриват подробности на евентуални злонамерени потребители.
                return LoginFailedView(model, LoginFailedMessage, logErrorMessage, user);
            }

            // Ако потребителят все още няма парола, а се опитва да влезе (очевидно със случайна парола),
            // той получава e-mail за първоначално задаване на парола.
            if (user.PasswordHash == null)
            {
                IdentityResult resetResult = await SendResetPasswordEmailAsync(user);
                if (resetResult.Succeeded)
                {
                    return RedirectToAction(nameof(ForgotPasswordConfirmation), new { noPassword = true });
                }
                string errorMessage = string.Join(Environment.NewLine, resetResult.Errors);
                return LoginFailedView(model, errorMessage, errorMessage, user);
            }

            SignInStatus result;
            try
            {
                using (ApplicationSignInManager signInManager = HttpContext.GetOwinContext().Get<ApplicationSignInManager>())
                {
                    result = await signInManager.PasswordSignInAsync(userName, model.Password);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Messages();
                return LoginFailedView(model, errorMessage, errorMessage, user);
            }

            switch (result)
            {
                case SignInStatus.Success:
                    // Нивото на логване е Minimal, за да не влиза правилната паролата в журнала.
                    AuditModel auditModel = AuditUtil.CreateModelForUser(ControllerContext, AuditLevel.Minimal, AuditTypeCode.LoginSuccess,
                        $"Вход: успешен вход с парола на потребител \"{userName}\".", user);
                    using (AuditService service = new AuditService())
                    {
                        service.Log(auditModel);
                    }
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return LoginFailedView(model, ApplicationUserManager.LockoutDisplayMessage, ApplicationUserManager.LockoutLogMessage(userName), user);
                case SignInStatus.RequiresVerification:
                    // TODO: Проверка на вида 2-factor authentication - еАвт или клиентски сертификат.
                    if (1.ToString() == "1")
                    {
                        return RedirectToAction(nameof(EAuthController.Login), "EAuth", new { returnUrl = model.ReturnUrl });
                    }
                    return RedirectToAction(nameof(CertificateController.Index), "Certificate", new { returnUrl = model.ReturnUrl });
                default:
                    return LoginFailedView(model, LoginFailedMessage, $"потребител \"{userName}\" е въвел грешна парола.", user);
            }
        }

        private ActionResult LoginFailedView(LoginModel model, string displayMessage, string logMessage, ApplicationUser user)
        {
            Danger(displayMessage);
            using (AuditService service = new AuditService())
            {
                LogLoginError(logMessage, user, service);
            }
            return View(model);
        }

        private void LogLoginError(string message, ApplicationUser user, BaseService service)
        {
            // TODO: Да не се допуска грешната парола да влиза в журнала.
            AuditModel auditModel = AuditUtil.CreateModelForUser(ControllerContext, AuditLevel.Form, AuditTypeCode.LoginError, $"Вход: {message}", user);
            service.Log(auditModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult LogOff(string returnUrl, bool twoFactor, bool twoFactorRemember)
        {
            List<string> authenticationTypes = new List<string> { DefaultAuthenticationTypes.ApplicationCookie };

            // Не е ясно какво променя. Засега в _LoginPartial.cshtml винги се подава true.
            // Неяснотата се заслива и от факта, че животът на това cookie е само 5 минути и целта на тези 5 минути не е ясна.
            if (twoFactor)
            {
                authenticationTypes.Add(DefaultAuthenticationTypes.TwoFactorCookie);
            }

            // Входът на второ ниво (еАвт или сертификат) се запомня за този browser, така че при следващ вход може да се ползва само парола.
            // Този флаг нулира И това cookie, освен останалите две (ApplicationCookie и TwoFactorCookie).
            if (twoFactorRemember)
            {
                authenticationTypes.Add(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
                EAuthController.ClearCachedEAuthResponse(Response);
            }

            HttpContext.GetOwinContext().Authentication.SignOut(authenticationTypes.ToArray());
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        #region Регистриране и потвърждение на e-mail-а

        public async Task<ActionResult> Register()
        {
            RegisterModel model = new RegisterModel
            {
                AccessLevelCode = AccessLevel.Employee
            };
            return await RegisterViewAsync(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowXRequestsEveryXSeconds(Name = "Register", ContentName = "TooManyRequests", Requests = 2, Seconds = 60)]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            if (!model.AdministrationId.HasValue && !model.PublicServiceProviderId.HasValue)
            {
                ModelState.AddModelError(nameof(model.AdministrationId), "Моля изберете администрация.");
                ModelState.AddModelError(nameof(model.PublicServiceProviderId), "Моля изберете доставчик на обществени услуги.");
            }
            if (model.PublicServiceProviderId == RegisterModel.NewPspId && string.IsNullOrEmpty(model.NewPspName))
            {
                ModelState.AddModelError(nameof(model.NewPspName), "Моля въведете наименованието на Вашата институция.");
            }

            if (ModelState.IsValid)
            {
                using (UserManagementService service = new UserManagementService())
                {
                    string userName = model.UserName;
                    ApplicationUser user = await UserManager.FindByNameAsync(userName);
                    if (user == null)
                    {
                        // Проверка дали въведеното потребителско име е валиден e-mail адрес.
                        string email;
                        try
                        {
                            new MailAddress(userName);
                            email = userName;
                        }
                        catch
                        {
                            email = null;
                        }

                        // Наиситна нов потребител (първи опит за регистриране).
                        // Потребителят се създава без парола. Паролата се задава с reset код след одобрение от администратор.
                        user = new ApplicationUser { UserName = userName, Email = email };
                        IdentityResult result = await UserManager.CreateAsync(user/*, model.Password*/);

                        if (result.Succeeded)
                        {
                            await service.RequestInitialWorkplaceAndQueueForApprovalAsync(user.Id, model, GetAuditModel());

                            bool noEmail = string.IsNullOrEmpty(email);
                            if (!noEmail)
                            {
                                bool emailSent = await SendConfirmationEmailAsync(user, service);
                                return RedirectToAction(nameof(RegisterConfirmation), new { email = emailSent ? email : null });
                            }
                            return RedirectToAction(nameof(RegisterConfirmation), new { noEmail });
                        }
                        ShowErrors(result);
                    }
                    else if (!user.IsBanned)
                    {
                        // Потребителят съществува и не е блокиран.
                        bool isQueued = await service.RequestInitialWorkplaceAndQueueForApprovalAsync(user.Id, model, GetAuditModel());

                        // Ако е необходимо се изпраща повторен e-mail за активиране.
                        bool emailSent = false;
                        bool noEmail = string.IsNullOrEmpty(user.Email);
                        if (!noEmail && !user.EmailConfirmed)
                        {
                            emailSent = await SendConfirmationEmailAsync(user, service);
                        }

                        if (isQueued)
                        {
                            return RedirectToAction(nameof(RegisterConfirmation), !noEmail ? new { email = emailSent ? user.Email : null } : (object)new { noEmail });
                        }

                        Success($"Заявлението за регистриране на {userName} вече е одобрено от администратор. {_youCanLoginMessage}");
                        return RedirectToAction(nameof(Login), new { userName });
                    }
                    else
                    {
                        // Блокиран потребител. Показва се екранът за прието заявление, но се логва причината за отказ.
                        LogLoginError($"Потребител \"{userName}\" е блокиран, затова не може да подава заявление за регистриране.", user, service);
                        return RedirectToAction(nameof(RegisterConfirmation));
                    }
                }
            }

            return await RegisterViewAsync(model);
        }

        [HttpGet]
        public ActionResult RegisterConfirmation(string email)
        {
            return View();
        }

        private async Task<ActionResult> RegisterViewAsync(RegisterModel model)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                model.Administrations = await service.GetAdministrationsAsync(false);

                // Нов доставчик на обществени услуги се регистрира, като от списъка се избере първият елемент.
                // Тогава се показват допълнителни полета за въвеждане на име и ЕИК на доставчика.
                model.PublicServiceProviders = new List<AdministrationIndexModel>() { new AdministrationIndexModel {
                    Id = RegisterModel.NewPspId,
                    Name = "ДРУГ ДОСТАВЧИК НА ОБЩЕСТВЕНИ УСЛУГИ"
                } };
                model.PublicServiceProviders.AddRange(await service.GetAdministrationsAsync(true));
            }
            using (UserManagementService service = new UserManagementService())
            {
                model.AccessLevels = await service.GetAccessLevelsAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code");
            }

            ApplicationUser user = await UserManager.FindByIdAsync(userId);
            string userName = user.UserName;
            string email = user.Email;
            bool? isApproved = user.IsApproved;
            EmailConfirmedModel model = new EmailConfirmedModel
            {
                IsSuccessful = true,
                IsApproved = isApproved,
                Email = email,
                UserName = userName
            };

            if (user == null || user.IsBanned)
            {
                // Блокиран потребител. Показва се екранът за успешно потвърждаване, но се логва причината за отказ.
                using (AuditService service = new AuditService())
                {
                    LogLoginError(user == null
                        ? $"Не съществува потребител с id {userId}, затова e-mail адресът не може да бъде потвърден."
                        : $"Потребител \"{userName}\" е блокиран, затова не може да потвърждава e-mail адреса си.",
                        user, service);
                }

                return View(model);
            }
            IdentityResult result = await UserManager.ConfirmEmailAsync(userId, code);

            if (result.Succeeded && isApproved == true && user.PasswordHash != null)  // Практически невъзможен случай.
            {
                Success($"Проверката на e-mail адрес {email} приключи успешно. Заявлението Ви вече е одобрено от администратор и сте избрали парола. {_youCanLoginMessage}");
                return RedirectToAction(nameof(Login), new { userName });
            }

            model.IsSuccessful = result.Succeeded;
            ShowErrors(result);
            return View(model);
        }

        #endregion

        #region Забравена парола и reset на паролата

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            ForgotPasswordModel model = new ForgotPasswordModel();
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            _successMessage = string.Empty;
            return TryAsync(
                async () =>
                {
                    string userName = model.UserName;
                    ApplicationUser user = await UserManager.FindByNameAsync(userName);
                    if (user != null && !user.IsBanned)
                    {
                        // Грешките в този клон не може да се логват, защото throw в TryAsync ще извърши rollback.
                        // TODO: Да се помисли дали някъде би било полезно логване извън транзкцията.
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            // Писмото за избор на нова парола се изпраща дори ако e-mail адресът на потребителя не е потвърден.
                            // Кодът за задаване на нова парола се използва като доказателство за собственост върху e-mail адреса и той се потвърждава.
                            IdentityResult result = await SendResetPasswordEmailAsync(user);
                            if (!result.Succeeded)
                            {
                                ThrowErrors(result);
                            }
                        }
                        else
                        {
                            throw new Exception($"За потребител \"{userName}\" не е въведен e-mail адрес. Не може да се изпрати инструкция за избор на нова парола.");
                        }
                    }
                    else
                    {
                        // Лог защо казваме на липсващия/блокирания потребител, че е изпратен e-mail, а всъщност не е.
                        using (AuditService service = new AuditService())
                        {
                            LogLoginError(user == null
                                ? $"Не съществува потребител \"{userName}\", затова няма на кого да се изпрати e-mail за избор на нова парола."
                                : $"Потребител \"{userName}\" е блокиран, затова няма да получи e-mail за избор на нова парола.",
                                user, service);
                        }
                    }
                },
                () => RedirectToAction(nameof(ForgotPasswordConfirmation)),
                () => View(model)
            );
        }

        private async Task<bool> SendConfirmationEmailAsync(ApplicationUser user, BaseService service)
        {
            LogLoginError($"Изпращане на писмо за потвърждение на e-mail адреса до потребител \"{user.UserName}\".", user, service);

            IdentityResult result = await UserManager.SendConfirmationEmailAsync(user,
                (code) => Url.Action(nameof(ConfirmEmail), null, new { userId = user.Id, code }, protocol: Request.Url.Scheme));
            ShowErrors(result);
            return result.Succeeded;
        }

        private async Task<IdentityResult> SendResetPasswordEmailAsync(ApplicationUser user)
        {
            using (AuditService service = new AuditService())
            {
                LogLoginError($"Изпращане на e-mail за избор на нова парола до потребител \"{user.UserName}\".", user, service);
            }

            return await UserManager.SendResetPasswordEmailAsync(user,
                (code) => Url.Action(nameof(ResetPassword), null, new { userId = user.Id, code }, protocol: Request.Url.Scheme));
        }

        [HttpGet]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> ResetPassword(string userId, string code)  // User manager–ът разчита на тези имена на параметри.
        {
            ApplicationUser user = await UserManager.FindByIdAsync(userId);
            if (user == null || user.IsBanned)
            {
                return ResetPassword_RedirectBannedUserToLogin(userId, user);
            }
            ResetPasswordModel model = new ResetPasswordModel
            {
                UserId = userId,
                Code = code,
                HasOldPassword = user.PasswordHash != null
            };
            return ResetPasswordView(model, user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordModel model)
        {
            string userId = model.UserId;
            ApplicationUser user = await UserManager.FindByIdAsync(userId);
            if (user == null || user.IsBanned)
            {
                return ResetPassword_RedirectBannedUserToLogin(userId, user);
            }
            if (ModelState.IsValid)
            {
                IdentityResult result = await UserManager.ResetPasswordAsync(userId, model.Code, model.Password);
                if (result.Succeeded)
                {
                    // Ако потребителят е пропуснал да потвърди e-mail-а си след първоначалната регистрация, кодът за задаване
                    // на нова парола се използва като доказателство за собственост върху e-mail адреса и той се потвърждава.
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        result = await UserManager.UpdateAsync(user);
                        ShowErrors(result);
                    }
                    Success($"Паролата за {user.PersonName ?? user.UserName} е зададена успешно. {_youCanLoginMessage}");
                    return RedirectToAction(nameof(Login), new { userName = user.UserName });
                }
                ShowErrors(result);
            }
            return ResetPasswordView(model, user);
        }

        private ActionResult ResetPassword_RedirectBannedUserToLogin(string userId, ApplicationUser user)
        {
            // Блокиран потребител се насочва към страницата за вход, но се логва причината изборът на парола да бъде отказан.
            using (AuditService service = new AuditService())  // Методът Log е в BaseService, така че няма значение кой service точно се създава.
            {
                LogLoginError(user == null
                    ? $"Не съществува потребител с id {userId}. Не може да започне процес по избор на парола."
                    : $"Потребител \"{user.UserName}\" е блокиран, затова не може да избира нова парола.",
                    user, service);
            }

            return RedirectToAction(nameof(Login));
        }

        private ActionResult ResetPasswordView(ResetPasswordModel model, ApplicationUser user)
        {
            model.DisplayName = user != null ? user.PersonName ?? user.UserName : "?";
            return View(model);
        }

        #endregion

        #region Helpers

        private void ThrowErrors(IdentityResult result)
        {
            if (result.Errors != null && result.Errors.Any())
            {
                throw new Exception(string.Join(Environment.NewLine, result.Errors));
            }
        }

        private void ShowErrors(IdentityResult result)
        {
            if (result.Errors != null && result.Errors.Any())
            {
                Danger(string.Join(Environment.NewLine, result.Errors));
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        #endregion
    }
}
