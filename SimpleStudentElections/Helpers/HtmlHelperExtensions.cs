using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace SimpleStudentElections.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string StringIfFieldError(
            this HtmlHelper helper,
            string propertyName,
            string errorClass = "is-invalid"
        )
        {
            if (helper.ViewData.ModelState != null && !helper.ViewData.ModelState.IsValidField(propertyName))
            {
                return errorClass;
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the <see cref="errorClass"/> if <see cref="expression"/> has a validation error.
        /// Intended to be used with bootstrap's is-invalid form class  
        /// </summary>
        /// <returns></returns>
        public static string StringIfFieldError<TModel, TEnum>(
            this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression,
            string errorClass = "is-invalid"
        )
        {
            return StringIfFieldError(helper, helper.PropertyPath(expression), errorClass);
        }

        /// <summary>
        /// Returns the <see cref="value"/> as dictionary.
        /// <ul>
        ///     <li>If dictionary returns as it</li>
        ///     <li>If null uses <see cref="nullReplacer"/> to return another value or null if replacer is null</li>
        ///     <li>Otherwise assumes that the object is an anonymous class and makes a dictionary from its properties</li>
        /// </ul>
        /// </summary>
        public static IDictionary<string, object> GetDictionaryFromViewBagValue(
            this HtmlHelper helper,
            object value, Func<IDictionary<string, object>> nullReplacer = null
        )
        {
            switch (value)
            {
                case null:
                    return nullReplacer?.Invoke();

                case IDictionary<string, object> asDictionary:
                    return asDictionary;
            }

            return HtmlHelper.AnonymousObjectToHtmlAttributes(value);
        }

        /// <summary>
        /// Same as <see cref="PropertyName{TModel,TEnum}"/> but returns full path (accounts for nested EditorTemplates)
        /// </summary>
        public static string PropertyPath<TModel, TEnum>(
            this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression
        )
        {
            return helper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(helper.PropertyName(expression));
        }

        /// <summary>
        /// Helper to get property name from lambda <see cref="expression"/>. Allows getting property names in
        /// compiler-checked manner and without explicitly referencing classes but using the same approach that other
        /// <see cref="HtmlHelper{TModel}"/> functions use 
        /// </summary>
        public static string PropertyName<TModel, TEnum>(
            this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression
        )
        {
            return ExpressionHelper.GetExpressionText(expression);
        }

        /// <summary>
        /// Helper for Html.RenderPartial that accepts an anonymous object for additionalViewData instead of
        /// <see cref="ViewDataDictionary"/> 
        /// </summary>
        public static void RenderPartial(
            this HtmlHelper htmlHelper,
            string partialViewName,
            object model,
            object additionalViewData
        )
        {
            RouteValueDictionary addData = HtmlHelper.AnonymousObjectToHtmlAttributes(additionalViewData);
            ViewDataDictionary newDataDictionary = new ViewDataDictionary(htmlHelper.ViewData);

            foreach (var pair in addData)
            {
                newDataDictionary[pair.Key] = pair.Value;
            }

            htmlHelper.RenderPartial(partialViewName, model, viewData: newDataDictionary);
        }
    }
}