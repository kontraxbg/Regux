using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Kontrax.Regux.Model.UserManagement;
using Kontrax.Regux.Service;
using Kontrax.Regux.Portal.Models;
using Kontrax.Regux.Model;
using System.Collections.Generic;

namespace Kontrax.Regux.Portal.Controllers
{
    public class UserManagementController : BaseController
    {
        public string RoleCode { get; private set; }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            UsersViewModel model = new UsersViewModel
            {
                CurrentUser = await User.GetPermissionsAsync()
            };

            await LoadUsersAsync(model);
            return await IndexViewAsync(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(UsersViewModel model)
        {
            model.CurrentUser = await User.GetPermissionsAsync();

            return await TryAsync(false,
                () => LoadUsersAsync(model),
                () => IndexViewAsync(model),
                () => IndexViewAsync(model)
            );
        }

        private static async Task LoadUsersAsync(UsersViewModel model)
        {
            using (UserManagementService service = new UserManagementService())
            {
                model.Users = await service.SearchUsersAsync(model.AdministrationId, model.LocalRoleCode);
            }
        }

        private async Task<ActionResult> IndexViewAsync(UsersViewModel model)
        {
            using (AdministrationService service = new AdministrationService())
            {
                model.Administrations = await service.GetAdministrationsAsync(false);
            }
            // Въпреки че зареждането на данните и IndexViewAsync биха могли да споделят един UserManagementService,
            // той не може да се dispose-ва с using или трябва да се ползва await TryAsync. Така е по-стабилно.
            using (UserManagementService service = new UserManagementService())
            {
                model.LocalRoles = await service.GetLocalRolesAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            UserCreateModel model = new UserCreateModel();
            await PrepareModel(model);
            return await CreateViewAsync(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserCreateModel model)
        {
            await PrepareModel(model);

            return await TryAsync(true,
                async () =>
                {
                    using (UserManagementService service = new UserManagementService())
                    {
                        service.DemandPermission_AddEmployee(model.CurrentUser);

                        ApplicationUser user = new ApplicationUser
                        {
                            UserName = model.UserName,
                            PersonName = model.PersonName,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            // Паролата трябва да се смени при първи вход в системата
                            ChangePassword = true
                        };

                        // Създаването на потребител с парола не може да стане в service layer-а, затова се ползва user manager-ът.
                        using (ApplicationUserManager userManager = CreateUserManager())
                        {
                            DemandSuccess(await userManager.CreateAsync(user, model.NewPassword));
                        }

                        await service.SetGlobalAdminAsync(user.Id, model.IsGlobalAdmin, model.CurrentUser);

                        if (model.NewLocalRole.AdministrationId.HasValue && !string.IsNullOrEmpty(model.NewLocalRole.LocalRoleCode))
                        {
                            await service.SetUserLocalRolesAsync(user.Id, model.NewLocalRole.ToEditModel(), model.CurrentUser);
                        }
                    }
                },
                () => RedirectToAction(nameof(Index)),
                () => CreateViewAsync(model)
            );
        }

        private async Task<ActionResult> CreateViewAsync(UserCreateModel model)
        {
            // Въпреки че записването и CreateViewAsync биха могли да споделят един UserManagementService,
            // той не може да се dispose-ва с using или трябва да се ползва await TryAsync. Така е по-стабилно.
            using (UserManagementService service = new UserManagementService())
            {
                CodeNameModel[] localRoles = await service.GetAssignableLocalRolesAsync(model.CurrentUser);

                await NewLocalRole_SetViewData(model, localRoles);
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> IssueCertificate(string userId, int AdministrationID)
        {
            if (ModelState.IsValid)
            {
                using (var _administrationService = new AdministrationService())
                {
                    var administration = await _administrationService.GetAdministrationByIdAsync(AdministrationID);
                    if (administration.Certificate != null)
                    {
                        var _certificateService = new CertificateService();
                        var CACertificate = _certificateService.LoadCertificate(administration.Certificate, "password");
                        using (ApplicationUserManager userManager = CreateUserManager())
                        {
                            var user = await userManager.FindByIdAsync(userId);
                            string subjectName = $"CN={user.PersonName}";
                            var userCertificate = _certificateService.IssueCertificate(subjectName, CACertificate, new[] { "server", "server.mydomain.com" });
                            byte[] userCertificateData = _certificateService.WriteCertificate(userCertificate, "password");
                            user.Certificate = userCertificateData;
                            await userManager.UpdateAsync(user);
                        }
                        Success("Сертификат издаден успешно");
                        return RedirectToAction("Edit", new { id = userId });
                    }
                    else
                    {
                        return RedirectToAction("Edit", new { id = userId });
                    }
                }
            }
            else
            {
                return RedirectToAction("Edit", new { id = userId });
            }
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            UserEditModel model;
            using (UserManagementService service = new UserManagementService())
            {
                model = await service.GetUserForEditAsync(id);
            }
            await PrepareModel(model);
            return await EditViewAsync(model);
        }

        public async Task<FileContentResult> DownloadCertificateAsync(string id)
        {
            UserEditModel model;
            using (UserManagementService service = new UserManagementService())
            {
                model = await service.GetUserForEditAsync(id);
            }
            await PrepareModel(model);
            return File(model.CertificateData, "application/x-pkcs12", $"{id}.pfx");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserEditModel model)
        {
            await PrepareModel(model);
            if (model.UserLocalRoles == null)
            {
                model.UserLocalRoles = new List<UserLocalRoleEditModel>();
            }
            string userId = model.Id;

            return await TryAsync(true,
                async () =>
                {
                    using (UserManagementService service = new UserManagementService())
                    {
                        await service.UpdateUserAsync(
                            userId,
                            model.UserName,
                            model.PersonName,
                            model.Email,
                            model.PhoneNumber,
                            model.IsGlobalAdmin,
                            model.CurrentUser);

                        await service.SetUserLocalRolesAsync(userId, model.UserLocalRoles.Union(model.NewLocalRole.ToEditModel()), model.CurrentUser);

                        // Промяната на паролата не може да стане в service layer-а, затова се ползва user manager-ът.
                        if (!string.IsNullOrEmpty(model.NewPassword))
                        {
                            // Текущият потребител има право да променя паролата точно когато има право
                            // да редактира данните за избрания потребител.
                            await service.DemandPermission_UpdateUserAsync(userId, "променя паролата на", model.CurrentUser);

                            using (ApplicationUserManager userManager = CreateUserManager())
                            {
                                string token = await userManager.GeneratePasswordResetTokenAsync(userId);
                                DemandSuccess(await userManager.ResetPasswordAsync(userId, token, model.NewPassword));
                            }
                        }
                    }
                },
                () => RedirectToAction(nameof(Index)),
                () => EditViewAsync(model)
            );
        }

        private async Task<ActionResult> EditViewAsync(UserEditModel model)
        {
            // Въпреки че записването и EditViewAsync биха могли да споделят един UserManagementService,
            // той не бива да се dispose-ва с using или трябва да се ползва await TryAsync. Така е по-стабилно.
            using (UserManagementService usrService = new UserManagementService())
            {
                CodeNameModel[] allLocalRoles = await usrService.GetLocalRolesAsync();
                CodeNameModel[] assignableLocalRoles = await usrService.GetAssignableLocalRolesAsync(model.CurrentUser);

                using (AdministrationService admService = new AdministrationService())
                {
                    foreach (UserLocalRoleEditModel role in model.UserLocalRoles)
                    {
                        role.AdministrationName = await admService.GetAdministrationNameAsync(role.AdministrationId);
                        role.LocalRoles = usrService.FilterAssignableLocalRoles(assignableLocalRoles, role.AdministrationId, model.CurrentUser);
                        if (!string.IsNullOrEmpty(role.LocalRoleCode))
                        {
                            role.LocalRoleName = allLocalRoles.First(r => r.Code == role.LocalRoleCode).Name;
                        }
                    }
                }

                await NewLocalRole_SetViewData(model, assignableLocalRoles);
                // От списъка за избор се премахват всички администрации, в които избраният портебител вече работи.
                model.NewLocalRole.Administrations = model.NewLocalRole.Administrations.Where(a => !model.UserLocalRoles.Any(r => r.AdministrationId == a.Id));
            }
            return View(model);
        }

        private static async Task NewLocalRole_SetViewData(UserBaseModel model, IEnumerable<CodeNameModel> localRoles)
        {
            using (AdministrationService service = new AdministrationService())
            {
                model.NewLocalRole.Administrations = await service.GetAllowedAdministrationsAsync(model.CurrentUser);
            }

            // Може да се показват роли Ръководител и Служител, но кои са разрешени зависи от избраната администрация.
            // Това се проверява при запис.
            model.NewLocalRole.LocalRoles = localRoles.Where(r => r.Code != LocalRole.Admin);
        }

        private async Task PrepareModel(UserBaseModel model)
        {
            model.CurrentUser = await User.GetPermissionsAsync();
            if (model.NewLocalRole == null)
            {
                model.NewLocalRole = new UserLocalRoleCreateModel();
            }
        }

        private static void DemandSuccess(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(Environment.NewLine, result.Errors));
            }
        }

        private ApplicationUserManager CreateUserManager()
        {
            return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Delete(string userId)
        {
            _successMessage = "Потребителят е изтрит успешно.";

            return TryAsync(true,
                async () =>
                {
                    using (UserManagementService service = new UserManagementService())
                    {
                        await service.DeleteUserAsync(userId, await User.GetPermissionsAsync());
                    }
                },
                () => RedirectToAction(nameof(Index)),
                () => RedirectToAction(nameof(Edit), new { id = userId })
            );
        }
    }
}
