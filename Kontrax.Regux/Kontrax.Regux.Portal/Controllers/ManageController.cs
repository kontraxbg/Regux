using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Model.Account;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Service;
using Kontrax.Regux.Shared.Portal.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Controllers
{
    // През 2018-02 атрибутите ChangePassword и SkipChangePassword са заменени със стандартния ResetPassword процес. Виж повече в ChangePasswordAttribute.cs.
    //[SkipChangePassword]
    public class ManageController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            string userId = User.Identity.GetUserId();
            ApplicationUser user;
            using (ApplicationUserManager userManager = CreateUserManager())
            {
                user = await userManager.FindByIdAsync(User.Identity.GetUserId());
            }
            ManageViewModel model = new ManageViewModel
            {
                PersonName = user.PersonName,
                PhoneNumber = user.PhoneNumber
            };
            return await IndexViewAsync(model, userId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Index(ManageViewModel model)
        {
            string userId = User.Identity.GetUserId();
            return TryAsync(
                async () =>
                {
                    using (ApplicationUserManager userManager = CreateUserManager())
                    {
                        ApplicationUser user = await userManager.FindByIdAsync(userId);
                        user.PersonName = model.PersonName;
                        user.PhoneNumber = model.PhoneNumber;
                        await userManager.UpdateAsync(user);
                    }
                },
                () => RedirectToAction(nameof(Index)),
                () => IndexViewAsync(model, userId)
            );
        }

        private async Task<ActionResult> IndexViewAsync(ManageViewModel model, string userId)
        {
            model.CurrentUser = await User.GetPermissionsAsync();
            using (UserManagementService service = new UserManagementService())
            {
                model.View = await service.GetUserAsync(userId);
            }
            return View(model);
        }

        public ActionResult ChangePassword(bool? renew, string returnUrl)
        {
            ChangePasswordModel model = new ChangePasswordModel();
            if (renew ?? false)
            {
                Warning("Паролата трябва да бъде променена.");
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordModel model, string returnUrl)
        {
            _successMessage = "Паролата е променена успешно.";
            return await TryAsync(
                async () =>
                {
                    string userId = User.Identity.GetUserId();
                    using (ApplicationUserManager userManager = CreateUserManager())
                    {
                        DemandSuccess(await userManager.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword));

                        ApplicationUser user = await userManager.FindByIdAsync(userId);
                        using (ApplicationSignInManager signInManager = HttpContext.GetOwinContext().Get<ApplicationSignInManager>())
                        {
                            await signInManager.SignInAsync(user);
                        }

                        // idilov: Това защо е нужно?
                        DemandSuccess(await userManager.UpdateAsync(user));
                    }
                },
                () =>
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction(nameof(Index));
                },
                () => View(model)
            );
        }

        private static void DemandSuccess(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(Environment.NewLine, result.Errors));
            }
        }

        /* Няма такъв процес в този проект.
        public ActionResult SetPassword()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }
        */
    }
}
