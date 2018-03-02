using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Administration;
using Kontrax.Regux.Service;

namespace Kontrax.Regux.Portal.Controllers
{
    public class AdministrationController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index(bool psp)
        {
            var model = new AdministrationsViewModel
            {
                CurrentUser = await User.GetPermissionsAsync(),
                IsPublicServiceProvider = psp
            };
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                model.Administrations = await service.GetAdministrationsAsync(psp);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            AdministrationDetailsModel model;
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                model = await service.GetAdministrationAsync(id);
            }
            model.CurrentUser = await User.GetPermissionsAsync();
            return View(model);
        }

        [HttpGet]
        public ActionResult Create(bool psp)
        {
            var model = new AdministrationCreateModel { IsPublicServiceProvider = psp };
            return CreateView(model);
        }

        [GlobalAdmin]
        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Create(bool psp, AdministrationCreateModel model)
        {
            model.IsPublicServiceProvider = psp;
            _successMessage = (psp ? "Доставчикът на обществени услуги е добавен успешно." : "Администрацията е добавена успешно.") + " Продължете с указване на останалите настройки.";
            int administrationId = 0;
            return TryAsync(
               async () =>
               {
                   using (AdministrationManagementService service = new AdministrationManagementService())
                   {
                       administrationId = await service.CreateAdministrationAsync(model, await User.GetPermissionsAsync(), GetAuditModel());
                   }
               },
               () => RedirectToAction(nameof(Edit), new { id = administrationId }),
               () => CreateView(model)
           );
        }

        public ActionResult CreateView(AdministrationCreateModel model)
        {
            return View(model.IsPublicServiceProvider ? "CreatePublicServiceProvider" : "CreateAdministration", model);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                AdministrationEditModel model = await service.GetAdministrationForEditAsync(id);
                return await EditView(model, service);
            }
        }

        [GlobalAdmin]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AdministrationEditModel model)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                return await TryAsync(
                    async () => await service.UpdateAdministrationAsync(model, await User.GetPermissionsAsync(), GetAuditModel()),
                    () => RedirectToAction(nameof(Details), new { id = model.Id }),
                    () => EditView(model, service)
                );
            }
        }

        private async Task<ActionResult> EditView(AdministrationEditModel model, AdministrationManagementService service)
        {
            model.Kinds = await service.GetAdministrationKindsAsync();
            return View(model);
        }

        [GlobalAdmin]
        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Delete(int id, bool psp)
        {
            _successMessage = psp ? "Доставчикът на обществени услуги е изтрит успешно." : "Администрацията е изтрита успешно.";

            return TryAsync(
                async () =>
                {
                    using (AdministrationManagementService service = new AdministrationManagementService())
                    {
                        await service.DeleteAdministrationAsync(id, await User.GetPermissionsAsync(), GetAuditModel());
                    }
                },
                () => RedirectToAction(nameof(Index), new { psp }),
                () => RedirectToAction(nameof(Edit), new { id })
            );
        }

        #region Сертификати

        [HttpGet]
        public async Task<FileContentResult> DownloadCertificate(int id, CertTypeCode typeCode)
        {
            byte[] bytes;
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                bytes = await service.DownloadCertificateAsync(id, typeCode, await User.GetPermissionsAsync());
            }
            return File(bytes, "application/x-pkcs12", $"{typeCode}_{id}.pfx");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> UploadCertificate(int id, CertTypeCode typeCode, string password)
        {
            _successMessage = "Сертификатът е качен успешно.";
            return TryAsync(
                async () =>
                {
                    if (Request.Files.Count == 0)
                    {
                        throw new Exception("Не е избран файл.");
                    }
                    HttpPostedFileBase file = Request.Files[0];
                    if (file.ContentLength == 0)
                    {
                        throw new Exception("Не е избран файл.");
                    }
                    byte[] data;
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        data = binaryReader.ReadBytes(file.ContentLength);
                    }

                    using (AdministrationManagementService service = new AdministrationManagementService())
                    {
                        await service.UploadCertificateAsync(id, typeCode, data, password, await User.GetPermissionsAsync(), GetAuditModel());
                    }
                },
                () => RedirectToAction(nameof(Details), new { id }),
                () => RedirectToAction(nameof(Details), new { id })
            );
        }

        [HttpPost]
        public async Task GenerateProposedRootCertificate(int id)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                await service.GenerateProposedRootCertificateAsync(id, await User.GetPermissionsAsync(), GetAuditModel());
            }
        }

        [HttpPost]
        public async Task ApproveProposedRootCertificate(int id)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                await service.ApproveProposedRootCertificateAsync(id, await User.GetPermissionsAsync(), GetAuditModel());
            }
        }

        [HttpPost]
        public async Task DeleteCertificate(int id, CertTypeCode typeCode)
        {
            using (AdministrationManagementService service = new AdministrationManagementService())
            {
                await service.DeleteCertificateAsync(id, typeCode, await User.GetPermissionsAsync(), GetAuditModel());
            }
        }

        #endregion
    }
}
