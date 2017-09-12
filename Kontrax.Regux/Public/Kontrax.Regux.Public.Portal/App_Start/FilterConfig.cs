using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
