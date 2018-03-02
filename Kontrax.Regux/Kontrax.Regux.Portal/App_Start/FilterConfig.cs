using System.Web.Mvc;
using Kontrax.Regux.Portal.Attributes;
using Kontrax.Regux.Portal.Audit;

namespace Kontrax.Regux.Portal
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // През 2018-02 атрибутите ChangePassword и SkipChangePassword са заменени със стандартния ResetPassword процес.  Виж повече в ChangePasswordAttribute.cs.
            //filters.Add(new ChangePasswordAttribute());  // Наследява AuthorizeAttribute.
            filters.Add(new AuthorizeAttribute());

            filters.Add(new AuditAttribute() { AuditType = Model.Audit.AuditTypeCode.Read, AuditLevel = AuditLevel.Form });
        }
    }
}
