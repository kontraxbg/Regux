using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Kontrax.Regux.Shared.Portal.Adapters;

namespace Kontrax.Regux.Public.Portal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Регистриране само на Razor View Engine
            ViewEnginesConfiguration();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Не показва MVC версията в header-ите
            MvcHandler.DisableMvcResponseHeader = true;

            InitLocalizedErrorMessages();
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
