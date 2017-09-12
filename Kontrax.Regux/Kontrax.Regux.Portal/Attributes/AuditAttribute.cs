using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Portal.Util;
using Kontrax.Regux.Service;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Attributes
{
    public enum AuditingLevelEnum { None, QueryParameters, Data, Custom, All }

    public class AuditAttribute : System.Web.Mvc.ActionFilterAttribute
    {
        private static string SessionCookieName = ".AspNet.ApplicationCookie";
        public AuditingLevelEnum AuditingLevel { get; set; }
        protected readonly AuditService _auditService = new AuditService();
        protected DateTime start_time;
        protected int auditId { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            start_time = DateTime.Now;
            TimeSpan duration = (DateTime.Now - start_time);

            //Stores the Request in an Accessible object
            var request = filterContext.HttpContext.Request;

            //Generate the appropriate key based on the user's Authentication Cookie
            //This is overkill as you should be able to use the Authorization Key from
            //Forms Authentication to handle this. 
            string sessionIdentifier = null;
            if (request.Cookies[SessionCookieName] != null)
            {
                sessionIdentifier = string.Join("", SHA512.Create().ComputeHash(Encoding.ASCII.GetBytes(request.Cookies[SessionCookieName].Value)).Select(s => s.ToString("x2")));
            }

            string userId = null;
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                userId = filterContext.HttpContext.User.Identity.GetUserId();
            }

            //Generate an audit
            AuditModel audit = new AuditModel
            {
                TimeAccessed = DateTime.Now,
                IPAddress = request.ClientIpAddress(),
                URLAccessed = request.RawUrl,
                RequestMethod = request.HttpMethod,
                Data = SerializeRequest(filterContext),
                UserName = (request.IsAuthenticated) ? filterContext.HttpContext.User.Identity.Name : "Anonymous",
                Controller = (string)filterContext.RouteData.Values["controller"],
                Action = (string)filterContext.RouteData.Values["action"],
                SessionID = sessionIdentifier,
                Duration = duration,
                UserID = userId
            };

            auditId = _auditService.Add(audit);

            // Достъп до полето се осъществява чрез RouteData.Values["AuditID"];
            if (!filterContext.RouteData.Values.ContainsKey("AuditID"))
            {
                filterContext.RouteData.Values.Add("AuditID", auditId);
            }
            else
            {
                filterContext.RouteData.Values["AuditID"] = auditId;
            }

            base.OnActionExecuting(filterContext);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            var audit = _auditService.GetById(auditId);
            if (audit != null)
            {
                TimeSpan duration = (DateTime.Now - start_time);
                _auditService.UpdateDuration(audit.ID.Value, duration);
            }

            base.OnResultExecuted(filterContext);
        }


        //This will serialize the Request object based on the level that you determine
        private string SerializeRequest(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var settings = new JsonSerializerSettings();
            settings.StringEscapeHandling = StringEscapeHandling.Default;

            switch (AuditingLevel)
            {
                //No Request Data will be serialized
                case AuditingLevelEnum.None:
                default:
                    return "";
                //Basic Request Serialization - stores parameters
                case AuditingLevelEnum.QueryParameters:
                    {
                        Dictionary<string, object> QueryParams = new Dictionary<string, object>();
                        Dictionary<string, string> QueryString = new Dictionary<string, string>();

                        QueryString = request.QueryString.AllKeys.ToDictionary(k => k, k => request.QueryString[k].Replace("\r\n", "").Replace("\\", "\\\\"));
                        QueryParams = filterContext.ActionParameters.ToDictionary(k => k.Key, k => k.Value);
                        return (QueryString.Count > 0 || QueryParams.Count > 0) ? JsonConvert.SerializeObject(new { QueryString, QueryParams }, settings) : "";
                    }
                //Basic Request Serialization - just stores Data
                case AuditingLevelEnum.Data:
                    return System.Web.Helpers.Json.Encode(new { request.Cookies, request.Headers, request.Files });
                //Middle Level - Customize to your Preferences
                case AuditingLevelEnum.Custom:
                    return JsonConvert.SerializeObject(new { request.Cookies, request.Headers, request.Files, request.Form, request.QueryString, request.Params });
                //Highest Level - Serialize the entire Request object
                case AuditingLevelEnum.All:
                    {
                        //We can't simply just Encode the entire request string due to circular references as well
                        //as objects that cannot "simply" be serialized such as Streams, References etc.
                        //return Json.Encode(request);

                        Dictionary<string, string> QueryString = new Dictionary<string, string>();
                        Dictionary<string, string> Params = new Dictionary<string, string>();
                        Dictionary<string, string> Cookies = new Dictionary<string, string>();
                        Dictionary<string, string> Headers = new Dictionary<string, string>();
                        Dictionary<string, string> Files = new Dictionary<string, string>();
                        Dictionary<string, string> Form = new Dictionary<string, string>();

                        // Dictionary<string, string> full = new Dictionary<string, string>();

                        Cookies = request.Cookies.AllKeys.ToDictionary(k => k, k => request.Cookies[k].ToString().Replace("\r\n", "").Replace("\\", "\\\\"));
                        Headers = request.Headers.AllKeys.ToDictionary(k => k, k => request.Headers[k].Replace("\r\n", "").Replace("\\", "\\\\"));
                        Files = request.Files.AllKeys.ToDictionary(k => k, k => request.Files[k].ToString().Replace("\r\n", "").Replace("\\", "\\\\"));
                        Form = request.Form.AllKeys.ToDictionary(k => k, k => request.Form[k].Replace("\r\n", "").Replace("\\", "\\\\"));
                        QueryString = request.QueryString.AllKeys.ToDictionary(k => k, k => request.QueryString[k].Replace("\r\n", "").Replace("\\", "\\\\"));
                        Params = request.Params.AllKeys.ToDictionary(k => k, k => request.Params[k].Replace("\r\n", "").Replace("\\", "\\\\"));

                        return JsonConvert.SerializeObject(new { Cookies, Headers, Files, Form, QueryString, Params }, settings);
                    }
            }
        }
    }
}