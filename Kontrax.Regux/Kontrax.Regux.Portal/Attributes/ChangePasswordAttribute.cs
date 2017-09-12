using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using System.Web.Routing;

namespace Kontrax.Regux.Portal.Attributes
{
    public class ChangePasswordAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (!filterContext.ActionDescriptor.IsDefined(typeof(SkipChangePasswordAttribute), inherit: true)
                && !filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(SkipChangePasswordAttribute), inherit: true))
            {
                IPrincipal user = filterContext.HttpContext.User;
                var userManager = filterContext.HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

                if (user != null && user.Identity.IsAuthenticated)
                {
                    var identityUser = userManager.FindById(user.Identity.GetUserId());

                    if (identityUser.ChangePassword)
                    {
                        HttpContext httpContext = HttpContext.Current;
                        HttpContextBase httpContextBase = new HttpContextWrapper(httpContext);
                        RouteData routeData = new RouteData();
                        RequestContext requestContext = new RequestContext(httpContextBase, routeData);
                        UrlHelper urlHelper = new UrlHelper(requestContext);

                        filterContext.HttpContext.Response.Redirect(urlHelper.Action("ChangePassword", "Manage", new { reason = "Паролата е изтекла и трябва да бъде променена!" }));
                    }
                }
            }
            base.OnAuthorization(filterContext);
        }
    }
}