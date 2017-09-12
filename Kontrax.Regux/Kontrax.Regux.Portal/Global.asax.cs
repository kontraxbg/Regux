using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.ComponentModel.DataAnnotations;
using Kontrax.Regux.Portal.Adapters;
using Kontrax.Regux.Portal.Attributes;
using System.Net;
using System.Web.Helpers;

namespace Kontrax.Regux.Portal
{
    public class App : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Активира TLS 1.2 и спира всички стари протоколи. Това се отнася само за връзката към други системи
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.SecurityProtocol &= ~(SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11);


            // Регистриране само на Razor View Engine
            ViewEnginesConfiguration();

            InitLocalizedErrorMessages();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            IFilterProvider[] providers = FilterProviders.Providers.ToArray();
            FilterProviders.Providers.Clear();
            FilterProviders.Providers.Add(new ExcludeFilterProvider(providers));

            // SECURE: Remove automatic XFrame option header so we can add it in filters to entire site
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
            // Не показва MVC версията в header-ите
            MvcHandler.DisableMvcResponseHeader = true;
        }

        public void ViewEnginesConfiguration()
        {
            const string CsFileExtensions = "cshtml";
            ViewEngines.Engines.Clear();
            var razorEngine = new RazorViewEngine() { FileExtensions = new[] { CsFileExtensions } };
            ViewEngines.Engines.Add(razorEngine);
        }

        /// <summary>
        /// Локализирани съобщения за грешки при unobtrusive валидацията
        /// </summary>
        private void InitLocalizedErrorMessages()
        {
            ClientDataTypeModelValidatorProvider.ResourceClassKey = "Messages";
            DefaultModelBinder.ResourceClassKey = "Messages";
            DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(RequiredAttribute), typeof(LocalizedRequiredAttributeAdapter));
            DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(StringLengthAttribute), typeof(LocalizedStringLengthAttributeAdapter));
            DataAnnotationsModelValidatorProvider.RegisterAdapter(typeof(RangeAttribute), typeof(LocalizedRangeAttributeAdapter));
        }
    }
}
