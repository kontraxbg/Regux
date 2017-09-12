using System.Collections.Generic;
using System.Web.Mvc;
using Kontrax.Regux.Portal.Attributes;

namespace Kontrax.Regux.Portal.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [SkipChangePassword]
        [ChildActionOnly]
        public PartialViewResult AlertsPartial()
        {
            List<AlertUtil.AlertModel> model = AlertUtil.GetAlerts(TempData);
            return PartialView(model);
        }
   }
}