using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Kontrax.Regux.Public.Portal.Models
{
    public static class HtmlExtensions
    {
        // TODO: MAKE WORK
        public static MvcHtmlString KnockoutDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression,
            string value, string options, string optionsText, string optionsValue, string optionsCaption, bool uniqueName, string classes = "")
        {
            return htmlHelper.DropDownListFor(
                expression,
                new SelectListItem[0],
                new
                {
                    @class = "form-control chosen-ignore " + classes,
                    data_bind = string.Format(
                        "value: {0}, " +
                        "options: {1}, " +
                        "optionsText: '{2}', " +
                        "optionsValue: '{3}', " +
                        "optionsCaption: '{4}', " +
                        "uniqueName2: {5}, ",
                        value, options, optionsText, optionsValue, optionsCaption, uniqueName.ToString().ToLower())
                });
        }
    }
}