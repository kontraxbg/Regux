using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Shared.Portal.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Attributes
{
    // През 2018-02 атрибутите ChangePassword и SkipChangePassword са заменени със стандартния ResetPassword процес:
    // - Саморегистриран потребител получава link за избор на парола след одобрение от администратор.
    // - Потребител, който е създаден от администратор, получава link за избор на парола по преценка на администратор.

    /// <summary>
    /// Този атрибут обезсилва ChangePasswordAttribute. Използва се за екраните, чрез които потребителят трябва да смени паролата си.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipChangePasswordAttribute : Attribute
    {
    }

    /// <summary>
    /// Този атрибут задължава логнатия потребител да промени паролата си ако тя е била генерирана.
    /// </summary>
    public class ChangePasswordAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            ActionDescriptor action = filterContext.ActionDescriptor;
            ControllerDescriptor controller = action.ControllerDescriptor;
            bool demandForAction = action.IsDefined(typeof(ChangePasswordAttribute), inherit: true);
            bool skipForAction =
                action.IsDefined(typeof(SkipChangePasswordAttribute), inherit: true) ||
                action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            bool skipForController =
                controller.IsDefined(typeof(SkipChangePasswordAttribute), inherit: true) ||
                controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            // Skip/Allow е по-силно от Authorize на същото ниво.
            bool demand = !skipForAction && (demandForAction || !skipForController);

            if (demand)
            {
                HttpContextBase contextBase = filterContext.HttpContext;
                IPrincipal user = contextBase.User;
                if (user != null && user.Identity.IsAuthenticated)
                {
                    ApplicationUserManager userManager = contextBase.GetOwinContext().GetUserManager<ApplicationUserManager>();
                    ApplicationUser appUser = userManager.FindById(user.Identity.GetUserId());

                    // Ако използването на този атрибут бъде възобновено, property-то ApplicationUser.ChangePassword трябва да се върне
                    // и по подразбиране да бъде true при създаване на потребител.
                    if (appUser != null/* && appUser.ChangePassword*/)
                    {
                        UrlHelper urlHelper = new UrlHelper(contextBase.Request.RequestContext);
                        contextBase.Response.Redirect(urlHelper.Action(nameof(Controllers.ManageController.ChangePassword), "Manage",
                            new { renew = true, returnUrl = contextBase.Request.RawUrl }));
                    }
                }
            }
            base.OnAuthorization(filterContext);
        }
    }
}