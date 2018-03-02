using System.Web;
using System.Web.Optimization;

namespace Kontrax.Regux.Public.Portal
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                      "~/Scripts/chosen.jquery.js",
                      "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                      "~/Scripts/jquery.unobtrusive*",
                      "~/Scripts/jquery.validate*",
                      "~/Scripts/app.validator.js",
                      "~/Scripts/locales/validate.bg.js"));

            bundles.Add(new ScriptBundle("~/bundles/knockout").Include(
                      "~/Scripts/knockout-{version}.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                      "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap-datetimepicker.js",
                      "~/Scripts/locales/bootstrap-datetimepicker.bg.js",
                      //"~/Scripts/locales/select2_locale_bg.js",
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/utils").Include(
                      "~/Scripts/moment.js",
                      "~/Scripts/moment-with-locales.js",
                      "~/Scripts/select2.js",
                      "~/Scripts/respond.js",
                      "~/Scripts/Chart.js",
                      "~/Scripts/init.js"));

            bundles.Add(new StyleBundle("~/Content/styles").Include(
                      "~/Content/bootstrap-datetimepicker.css",
                      "~/Content/css/select2.css",
                      "~/Content/select2-bootstrap.css",
                      "~/Content/bootstrap.css",
                      "~/Content/bootstrap-theme.css",
                      "~/Content/carousel.css",
                      "~/Content/site.css"));
        }
    }
}
