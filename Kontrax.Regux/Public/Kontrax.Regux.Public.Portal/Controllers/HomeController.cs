using Kontrax.Regux.Data;
using Kontrax.Regux.Public.Portal.Models;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public ActionResult UsageStats()
        {
            ViewBag.Message = "Справка за използване на системата";

            return View("~/Views/UsageStats/Index.cshtml");
        }
    }
}