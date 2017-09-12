using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Kontrax.Regux.Public.Portal.Models
{
    public static class HtmlExtensions
    {
        //public static MvcHtmlString FormDropDownListFor<TModel, TValue>(
        //    this HtmlHelper<TModel> html,
        //    Expression<Func<TModel, TValue>> expression,
        //    IEnumerable<SelectListItem> selectList,
        //    string optionsLabel,
        //    string koPropertyName)
        //{
        //    string output;
        //    if (FormIsEditing<TModel>(html))
        //    {
        //        Dictionary<string, object> htmlAttributes = new Dictionary<string, object>();
        //        htmlAttributes.Add("class", "form-control");
        //        if (!string.IsNullOrEmpty(koPropertyName))
        //        {
        //            htmlAttributes.Add("data-bind", string.Format("value: {0}", koPropertyName));
        //        }
        //        output = string.Format("<span class=\"form-dropdownlist\">{0}</span>{1}",
        //            html.DropDownListFor(expression, selectList, optionsLabel, htmlAttributes),
        //            html.ValidationMessageFor(expression));
        //    }
        //    else
        //    {
        //        TModel model = (TModel)html.ViewContext.ViewData.ModelMetadata.Model;
        //        TValue selectedValue = expression.Compile().Invoke(model);
        //        if (selectedValue != null)
        //        {
        //            string selectedValueText = selectedValue.ToString();
        //            SelectListItem item = selectList.FirstOrDefault(i => i.Value == selectedValueText);
        //            output = item != null
        //                ? DecorateEditableText(item.Text)
        //                : string.Format("<span class=\"alert-danger\">[ невалидна стойност {0} ]</span>", selectedValueText);
        //        }
        //        else
        //        {
        //            output = string.Format("<span class=\"alert-warning\">[ {0} ]</span>", optionsLabel ?? "Моля изберете стойност");
        //        }
        //    }
        //    return MvcHtmlString.Create(output);
        //}

        public static MvcHtmlString FormDropDownListFor<TModel, TValue>(
        this HtmlHelper<TModel> html,
        Expression<Func<TModel, TValue>> expression,
        IEnumerable<SelectListItem> selectList,
        string optionsLabel,
        string koPropertyName)
        {
            string output;
            if (FormIsEditing<TModel>(html))
            {
                Dictionary<string, object> htmlAttributes = new Dictionary<string, object>();
                htmlAttributes.Add("class", "form-control");
                if (!string.IsNullOrEmpty(koPropertyName))
                {
                    htmlAttributes.Add("data-bind", string.Format("value: {0}", koPropertyName));
                }
                output = string.Format("<span class=\"form-dropdownlist\">{0}</span>{1}",
                    html.DropDownListFor(expression, selectList, optionsLabel, htmlAttributes),
                    html.ValidationMessageFor(expression));
            }
            else
            {
                TModel model = (TModel)html.ViewContext.ViewData.ModelMetadata.Model;
                TValue selectedValue = expression.Compile().Invoke(model);
                if (selectedValue != null)
                {
                    string selectedValueText = selectedValue.ToString();
                    SelectListItem item = selectList.FirstOrDefault(i => i.Value == selectedValueText);
                    output = item != null
                        ? DecorateEditableText(item.Text)
                        : string.Format("<span class=\"alert-danger\">[ невалидна стойност {0} ]</span>", selectedValueText);
                }
                else
                {
                    output = string.Format("<span class=\"alert-warning\">[ {0} ]</span>", optionsLabel ?? "Моля изберете стойност");
                }
            }
            return MvcHtmlString.Create(output);
        }

        private static bool FormIsEditing<TModel>(HtmlHelper<TModel> html)
        {
            return !html.ViewContext.RouteData.Values["action"].Equals("Details");
        }

        private static string DecorateEditableText(string text)
        {
            return string.Format("<span class=\"form-display form-display-editor\">{0}</span>", text);
        }

        public static MvcHtmlString CheckBoxEditorFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression,
            string classes = "")
        {
            return htmlHelper.EditorFor(expression, new
            {
                htmlAttributes = new
                {
                    @class = "form-control " + classes,
                    //data_bind = string.Format("value: {0}, uniqueName2: {1}", value, uniqueName.ToString().ToLower())
                }
            });
        }
    }
}