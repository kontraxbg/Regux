using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Administration;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Model.UserManagement;
using Kontrax.Regux.Portal.Audit;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Service;
using Kontrax.Regux.Shared.Portal.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Controllers
{
    public class UserManagementController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index(int? admId, string filter, string level)
        {
            UsersViewModel model = new UsersViewModel
            {
                AdministrationId = admId,
                NameIdOrContact = filter,
                UserTypeCode = level
            };

            return await LoadUsersAsync(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(UsersViewModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index), new
                {
                    admId = model.AdministrationId,
                    filter = model.NameIdOrContact,
                    level = model.UserTypeCode
                });
            }

            // Грешка в параметрите за търсене. Последните данни се презареждат.
            return await LoadUsersAsync(model);
        }

        private async Task<ActionResult> LoadUsersAsync(UsersViewModel model)
        {
            model.CurrentUser = await User.GetPermissionsAsync();
            using (UserManagementService service = new UserManagementService())
            {
                await service.SearchUsersAsync(model);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Create(int? admId, string level)
        {
            UserCreateModel model = new UserCreateModel { SendSetPasswordEmail = true };
            model.NewWorkplace.AdministrationId = admId;
            model.NewWorkplace.AccessLevelCode = level;
            using (UserManagementService service = new UserManagementService())
            {
                await service.SetCreateModelViewDataAsync(model);
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserCreateModel model)
        {
            string userId = null;

            using (UserManagementService service = new UserManagementService())
            {
                return await TryAsync(
                    async () =>
                    {
                        UserPermissionsModel currentUser = await User.GetPermissionsAsync();
                        if (!currentUser.CanAddEmployees)
                        {
                            throw new Exception($"Потребител {currentUser.DisplayName} няма право да добавя потребители.");
                        }

                        ApplicationUser user = new ApplicationUser
                        {
                            UserName = model.UserName,
                            Email = model.UserName,
                            PersonName = model.PersonName,
                            // Потребителят е автоматично одобрен, тъй като се създава от администратор.
                            IsApproved = true,
                            // По подразбиране се изисква 2FA чрез еАвт.
                            TwoFactorEnabled = true
                        };

                        // Изпращането на e-mail за избор на парола не може да стане в service layer-а, затова се ползва user manager-ът.
                        using (ApplicationUserManager userManager = CreateUserManager())
                        {
                            DemandSuccess(await userManager.CreateAsync(user));
                            userId = user.Id;

                            WorkplaceCreateModel workplace = model.NewWorkplace;
                            if (workplace.AdministrationId.HasValue && !string.IsNullOrEmpty(workplace.AccessLevelCode))
                            {
                                await service.SetUserWorkplacesAsync(userId, workplace.ToEditModel(), currentUser, GetAuditModel());
                            }

                            // На новия потребител се изпраща e-mail с link за потвърждение на адреса и първоначално задаване на парола.
                            if (model.SendSetPasswordEmail)
                            {
                                // Името на администрацията и нивото на достъп се извличат по заобиколен начин - чрез попълване на UserViewModel.
                                UserViewModel userModel = await service.GetUserAsync(userId);
                                WorkplaceViewModel workplaceModel = userModel.Workplaces.First();

                                string code = await userManager.GeneratePasswordResetTokenAsync(userId);
                                string callbackUrl = Url.Action(nameof(AccountController.ResetPassword), "Account", new { userId, code }, protocol: Request.Url.Scheme);
                                string body = $"Получихте достъп до {ThisSystem.Name} като {workplaceModel.AccessLevelName.ToLower()} на {workplaceModel.AdministrationName}." +
                                    $"<br /><br />Моля щракнете <a href=\"{callbackUrl}\">тук</a>, за да докажете, че e-mail адресът {user.Email} е Ваш и да изберете парола за вход в системата.";
                                await userManager.SendEmailAsync(userId, ApplicationUserManager.ConfirmEmailAddressSubject, body);
                            }
                        }
                        _successMessage = "Потребителят е добавен успешно и следва да избере парола. При необходимост, тук може да направите допълнителни настройки.";
                    },
                    () => RedirectToAction(nameof(Edit), new { id = userId }),
                    async () =>
                    {
                        await service.SetCreateModelViewDataAsync(model);
                        return View(model);
                    }
                );
            }
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            UserEditModel model;
            using (UserManagementService service = new UserManagementService())
            {
                model = await service.GetUserForEditAsync(id);
                model.CurrentUser = await User.GetPermissionsAsync();
                return await EditViewAsync(model, service);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserEditModel model)
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            model.CurrentUser = currentUser;
            if (model.Workplaces == null)
            {
                model.Workplaces = new WorkplaceEditModel[0];
            }
            string userId = model.Id;

            using (UserManagementService service = new UserManagementService())
            {
                if (!string.IsNullOrEmpty(model.SendResetPasswordEmailAction))
                {
                    return await TryAsync(
                        async () =>
                        {
                            // Изпращането на e-mail за промяна на паролата не може да става в service layer-а, затова се ползва user manager-ът.
                            // Текущият потребител има право да изпраща такъв e-mail точно когато има право да редактира данните на избрания потребител.
                            await service.DemandPermission_UpdateUserAsync(userId, "разрешава нулиране на паролата на", currentUser);

                            using (ApplicationUserManager userManager = CreateUserManager())
                            {
                                ApplicationUser user = userManager.FindById(userId);
                                AuditModel auditModel = AuditUtil.CreateModelForUser(ControllerContext, AuditLevel.Form, AuditTypeCode.LoginError,
                                    $"Изпращане на e-mail за избор на нова парола до потребител \"{user.UserName}\".", user);
                                service.Log(auditModel);

                                IdentityResult result = await userManager.SendResetPasswordEmailAsync(user,
                                    (code) => Url.Action(nameof(AccountController.ResetPassword), "Account", new { userId, code }, protocol: Request.Url.Scheme));
                                if (!result.Succeeded)
                                {
                                    throw new Exception(string.Join(Environment.NewLine, result.Errors));
                                }
                            }
                            Success("E-mail-ът е изпратен успешно.");
                        },
                        () => EditViewAsync(model, service)
                    );
                }
                else
                {
                    return await TryAsync(
                        async () =>
                        {
                            await service.UpdateUserAsync(model, currentUser, GetAuditModel());

                            WorkplaceEditModel[] newWorkplace = model.NewWorkplace.ToEditModel();
                            await service.SetUserWorkplacesAsync(userId, model.Workplaces.Union(newWorkplace), currentUser, GetAuditModel());
                        },
                        () => RedirectToAction(nameof(Edit), new { id = userId }),
                        () => EditViewAsync(model, service)
                    );
                }
            }
        }

        private async Task<ActionResult> EditViewAsync(UserEditModel model, UserManagementService service)
        {
            await service.SetEditModelViewDataAsync(model);
            return View(model);
        }

        private static void DemandSuccess(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(Environment.NewLine, result.Errors));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> IssueCertificate(string id, int? administrationId)
        {
            _successMessage = "Сертификатът е издаден успешно.";
            return await TryAsync(
                async () =>
                {
                    if (!administrationId.HasValue)
                    {
                        throw new Exception("Не е избрана администрация издател на сертификата.");
                    }
                    X509Certificate2 rootCert;
                    using (AdministrationManagementService admService = new AdministrationManagementService())
                    {
                        rootCert = await admService.DemandCertificateWithPrivateKeyAsync(administrationId.Value, CertTypeCode.Root);
                    }

                    using (ApplicationUserManager userManager = CreateUserManager())
                    {
                        ApplicationUser user = await userManager.FindByIdAsync(id);
                        // Знакът + трябва да се escape-не, защото бил разделител за "multi-valued RDNs".
                        // http://bouncy-castle.1462172.n4.nabble.com/double-escaping-in-X509Name-td1467978.html
                        // Ако клас X509Name види escape-нат +, той записва името в кавички: CN="idilov+regux@gmail.com".
                        string commonName = (user.PersonName ?? user.UserName).Replace("+", "\\+");
                        string subjectName = $"CN={commonName}";
                        byte[] userCertificateData;
                        using (var certificateService = new CertificateService())
                        {
                            var userCertificate = certificateService.IssueCertificate(subjectName, rootCert, new[] { "server", "server.mydomain.com" });
                            userCertificateData = certificateService.WriteCertificate(userCertificate, "password");
                        }
                        user.Certificate = userCertificateData;
                        await userManager.UpdateAsync(user);
                    }
                },
                () => RedirectToAction(nameof(Edit), new { id }),
                () => RedirectToAction(nameof(Edit), new { id })
            );
        }

        [HttpGet]
        public async Task<FileContentResult> DownloadCertificate(string id)
        {
            byte[] bytes;
            using (UserManagementService service = new UserManagementService())
            {
                bytes = await service.GetUserCertificateAsync(id);
            }
            return File(bytes, "application/x-pkcs12", $"{id}.pfx");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Delete(string id)
        {
            _successMessage = "Потребителят е изтрит успешно.";

            return TryAsync(
                async () =>
                {
                    using (UserManagementService service = new UserManagementService())
                    {
                        await service.DeleteUserAsync(id, await User.GetPermissionsAsync(), GetAuditModel());
                    }
                },
                () => RedirectToAction(nameof(Index)),
                () => RedirectToAction(nameof(Edit), new { id })
            );
        }
    }
}
