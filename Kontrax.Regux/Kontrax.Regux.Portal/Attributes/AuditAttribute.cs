using System;
using System.Web.Mvc;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Portal.Audit;
using Kontrax.Regux.Service;

namespace Kontrax.Regux.Portal.Attributes
{
    public class AuditAttribute : ActionFilterAttribute
    {
        public AuditLevel AuditLevel { get; set; }

        public AuditTypeCode AuditType { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Items["__start_time__"] = DateTime.Now;
            AuditModel audit = AuditUtil.CreateModel(filterContext, AuditLevel, AuditType);
            //ако функцията върне audit == null - AuditingLevel е None
            if (audit != null)
            {
                int auditId;
                using (AuditService auditService = new AuditService())
                {
                    auditId = auditService.Log(audit);
                }
                filterContext.HttpContext.Items["__audit_id__"] = auditId;

                // Достъп до полето се осъществява чрез RouteData.Values["AuditID"];
                if (!filterContext.RouteData.Values.ContainsKey("AuditID"))
                {
                    filterContext.RouteData.Values.Add("AuditID", auditId);
                }
                else
                {
                    filterContext.RouteData.Values["AuditID"] = auditId;
                }
            }

            base.OnActionExecuting(filterContext);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            using (AuditService auditService = new AuditService())
            {
                int? auditId = (int?)filterContext.HttpContext.Items["__audit_id__"];
                //ако няма auditId - вероятно AuditingLevel е None
                if (auditId.HasValue)
                {
                    DateTime start_time = (DateTime)filterContext.HttpContext.Items["__start_time__"];
                    TimeSpan duration = (DateTime.Now - start_time);
                    auditService.UpdateDuration(auditId.Value, duration);
                }
            }
            base.OnResultExecuted(filterContext);
        }
    }
}