using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Controllers
{
    public class WebPageContentController : Controller
    {
        [AllowAnonymous]
        public ViewResult TooManyRequests()
        {
            return View();
        }
    }
}