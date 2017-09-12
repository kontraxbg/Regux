using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Controllers
{
    public class AuditController : Controller
    {
        protected readonly AuditService _auditService = new AuditService();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult IndexGrid(AuditSearchModel model)
        {
            var audits = _auditService.GetFiltered(model);
            return PartialView("_IndexGrid", audits);
        }

        public async Task<ActionResult> Details(int id)
        {
            var model = await _auditService.GetViewModelByIdAsync(id);
            return View(model);
        }
    }
}