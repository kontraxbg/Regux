using Kontrax.Regux.Model.Administration;
using Kontrax.Regux.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Controllers
{
    public class AdministrationController : BaseController
    {
        protected readonly AdministrationService _administrationService = new AdministrationService();

        public async Task<ActionResult> Index()
        {
            var model = await _administrationService.GetAdministrationIndexAsync();
            return View(model);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var model = await _administrationService.GetAdministrationForEditAsync(id);
            model.Administrations = await _administrationService.GetAdministrationsAsync(true);
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(AdministrationEditModel model, ICollection<HttpPostedFileBase> proposedCertificateFile)
        {
            if (ModelState.IsValid)
            {
                if (proposedCertificateFile != null && proposedCertificateFile.Count > 0)
                {
                    var certificate = proposedCertificateFile.FirstOrDefault();
                    if (certificate.ContentLength > 0)
                    {
                        var ms = new MemoryStream();
                        await certificate.InputStream.CopyToAsync(ms);
                        model.ProposedCertificate = ms.ToArray();
                    }
                }

                await _administrationService.UpdateAdministrationAsync(model);
                Success("Редакция успешна");
                return RedirectToAction("Details", new { id = model.Id });
            }
            else
            {
                return View(model);
            }
        }

        public async Task<ActionResult> Details(int id)
        {
            var model = await _administrationService.GetAdministrationByIdAsync(id);
            return View(model);
        }
    }
}