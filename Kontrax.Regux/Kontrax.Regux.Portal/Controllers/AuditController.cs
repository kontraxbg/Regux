using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Service;

namespace Kontrax.Regux.Portal.Controllers
{
    public class AuditController : BaseController
    {
        protected readonly AuditService _auditService = new AuditService();

        public ActionResult Index()
        {
            return View();
        }

        // не е добре да се игнорира логването на търсенето? защото този метод се вика 
        // не само при смяна на страницата, а и при промяна на филтрите
        //[AuditAttribute(AuditingLevel = AuditingLevelEnum.None)]
        // Използва се и първоначално(GET), и за филтриране(POST)
        public async Task<ActionResult> IndexGrid(AuditSearchModel model)
        {
            IQueryable<AuditViewModel> audits = await _auditService.GetFilteredAsync(model, await User.GetPermissionsAsync());
            return PartialView("_IndexGrid", audits);
        }

        public async Task<ActionResult> Details(int id)
        {
            AuditViewModel model = await _auditService.GetAuditAsync(id, await User.GetPermissionsAsync());
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            if (Request.HttpMethod == WebRequestMethods.Http.Post)
            {
                Danger($"Грешка при опита за корекция!{ Environment.NewLine } Администратор беше уведомен.");
                //TODO - уведомяване на администратора :D (в журнала си го пише и без друго)
                return View();
            }
            else
            {
                return View();
            }
        }

        #region Check Audit chain
        [HttpGet]
        public ActionResult CheckAuditChain(string fromDate, string toDate)
        {
            DateTime from = DateTime.TryParse(fromDate, out from) ? from : DateTime.Now.Date.AddDays(-7);
            DateTime to = DateTime.TryParse(toDate, out to) ? to : DateTime.Now;

            AuditHashCheckModel model = new AuditHashCheckModel()
            {
                StartDateTime = from,
                EndDateTime = to,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult CheckAuditChain(AuditHashCheckModel model)
        {
            return View(_auditService.CheckAuditChain(model.StartDateTime, model.EndDateTime));
        }
        #endregion

        [HttpGet]
        public async Task<FileResult> ExportIndex(AuditSearchModel filter)
        {
            string templateFilePath = Server.MapPath("~/Content/Excel/Audit.xlsx");
            // В Excel-а трябва да има link-ове, затова се подава root пътят до сървъра (не до web сайта).
            string serverUrl = string.Format("{0}://{1}", Request.Url.Scheme, Request.Url.Authority);
            byte[] bytes = await _auditService.ExportAsync(filter, templateFilePath, serverUrl, await User.GetPermissionsAsync());
            return ReportController.ExcelResultFromTemplate(templateFilePath, bytes);
        }
    }
}