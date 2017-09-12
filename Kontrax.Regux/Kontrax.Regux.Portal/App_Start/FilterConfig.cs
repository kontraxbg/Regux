using Kontrax.Regux.Portal.Attributes;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ChangePasswordAttribute());
        }
    }
}
