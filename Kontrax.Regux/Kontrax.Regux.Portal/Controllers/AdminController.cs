using System.Threading.Tasks;
using System.Web.Mvc;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Admin;

namespace Kontrax.Regux.Portal.Controllers
{
    public class AdminController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            AdminIndexPageModel model = new AdminIndexPageModel
            {
                IsGlobalAdmin = currentUser.IsGlobalAdmin
            };
            return View(model);
        }
    }
}
