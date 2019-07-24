using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using JetBrains.Annotations;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections.Helpers
{
    public static class FormConstants
    {
        public const string DefaultDateTimeFormat = "{0:dd'/'MM'/'yyyy HH:mm:ss}";
        public const string DefaultDateTimeFormatMomentJs = "DD/MM/YYYY HH:mm:ss";

        public const string FieldsInfoKey = "FieldsInfo";
        public const string DisabledKey = "Disabled";
    }

    /// <summary>
    /// Base class for "configuration" of the form's display approach - this is used to allow razor page which
    /// renders the form to only set this config and call @Html.EditorFor without the need to handle the layout.
    /// The layout is completely handled by the editor templates which read the form config and adjust their rendering accordingly
    /// </summary>
    public class FormConfig : IDisposable
    {
        public const string ViewDataKey = "FormConfig";

        private ViewDataDictionary _activeViewData;
        private object _prevValue;

        internal void BeginUse(ViewDataDictionary viewData)
        {
            // Will give null if there is nothing
            _prevValue = viewData[ViewDataKey];

            viewData[ViewDataKey] = this;
            _activeViewData = viewData;
        }

        public void Dispose()
        {
            if (_activeViewData == null) return;

            if (_activeViewData[ViewDataKey] != this)
            {
                throw new ActiveFormConfigIsDifferent();
            }

            _activeViewData[ViewDataKey] = _prevValue;
            _activeViewData = null;
        }
    }

    public class SimpleForm : FormConfig
    {
    }
    
    public class BootstrapHorizontalForm : FormConfig
    {
        public readonly Dictionary<BootstrapColType, int> LabelWidth = new Dictionary<BootstrapColType, int>();
        public readonly Dictionary<BootstrapColType, int> InputContainerWidth = new Dictionary<BootstrapColType, int>();
    }

    public enum BootstrapColType
    {
        ExtraSmall,
        Sm,
        Md,
        Lg,
        Xl
    }

    /// <summary>
    /// Special dictionary class, that's intended to be used for storing and manipulating HTML attributes
    /// </summary>
    public class HtmlAttributeDictionary : Dictionary<string, object>
    {
        public HtmlAttributeDictionary()
        {
        }

        public HtmlAttributeDictionary(int capacity) : base(capacity)
        {
        }

        public HtmlAttributeDictionary(IEqualityComparer<string> comparer) : base(comparer)
        {
        }

        public HtmlAttributeDictionary(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer)
        {
        }

        public HtmlAttributeDictionary([NotNull] IDictionary<string, object> dictionary) : base(dictionary)
        {
        }

        public HtmlAttributeDictionary([NotNull] IDictionary<string, object> dictionary,
            IEqualityComparer<string> comparer) : base(dictionary, comparer)
        {
        }

        protected HtmlAttributeDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public void AddClass(string newClass)
        {
            if (string.IsNullOrEmpty(newClass)) return;

            if (!ContainsKey("class"))
            {
                this["class"] = newClass;
                return;
            }

            string currentClasses = (string) this["class"];
            string[] split = currentClasses.Split(' ');

            if (!split.Contains(newClass))
            {
                this["class"] = currentClasses + " " + newClass;
            }
        }
    }

    public static class HtmlFormExtensions
    {
        public static FormConfig UseFormConfig(this HtmlHelper helper, FormConfig formConfig)
        {
            formConfig.BeginUse(helper.ViewData);
            return formConfig;
        }

        public static T GetFormConfig<T>(this HtmlHelper helper) where T : class
        {
            return helper.ViewData[FormConfig.ViewDataKey] as T;
        }

        public static string BootstrapColumnToClass(this HtmlHelper helper, KeyValuePair<BootstrapColType, int> pair)
        {
            string typeStr;

            switch (pair.Key)
            {
                case BootstrapColType.ExtraSmall:
                    typeStr = "";
                    break;

                case BootstrapColType.Sm:
                    typeStr = "sm";
                    break;

                case BootstrapColType.Md:
                    typeStr = "md";
                    break;

                case BootstrapColType.Lg:
                    typeStr = "lg";
                    break;

                case BootstrapColType.Xl:
                    typeStr = "xl";
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return string.Join("-", "col", typeStr, pair.Value.ToString());
        }

        public static string DatePickerInputGroupId(this HtmlHelper helper, string fieldId)
        {
            return fieldId + "-input-group";
        }

        public static string DatePickerInputGroupId<TModel, TEnum>(
            this HtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression
        )
        {
            return helper.DatePickerInputGroupId(helper.IdFor(expression).ToString());
        }

        /// <summary>
        /// Renders the editor template, controlled by <paramref name="fieldsInfo"/>.
        /// Does nothing if this field is not set to be shown in <paramref name="fieldsInfo"/>.
        /// </summary>
        public static MvcHtmlString EditorFor<TModel, TValue>(
            this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression,
            ModelFieldsAccessibility fieldsInfo
        )
        {
            return DoFieldsInfoAwareEditor(
                html, expression, fieldsInfo,
                () => html.EditorFor(expression)
            );
        }

        /// <summary>
        /// Renders the editor template, controlled by <paramref name="fieldsInfo"/>.
        /// Does nothing if this field is not set to be shown in <paramref name="fieldsInfo"/>.
        /// </summary>
        public static MvcHtmlString EditorFor<TModel, TValue>(
            this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression,
            ModelFieldsAccessibility fieldsInfo,
            object additionalViewData
        )
        {
            return DoFieldsInfoAwareEditor(
                html, expression, fieldsInfo,
                () => html.EditorFor(expression, additionalViewData)
            );
        }

        private static MvcHtmlString DoFieldsInfoAwareEditor<TModel, TValue>(
            HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression,
            ModelFieldsAccessibility fieldsInfo,
            Func<MvcHtmlString> displayDelegate
        )
        {
            string property = html.PropertyName(expression);
            if (!fieldsInfo.IsVisible(property)) return MvcHtmlString.Empty;

            ViewDataDictionary viewData = html.ViewDataContainer.ViewData;
            List<string> dataKeysToRemove = new List<string>();
            Dictionary<string, object> dataPrevValues = new Dictionary<string, object>();

            // "Hide" current fields info from new editor
            viewData.PrepareChangeReversal(FormConstants.FieldsInfoKey, dataKeysToRemove, dataPrevValues);
            viewData.Remove(FormConstants.FieldsInfoKey);

            // Augment new additionalViewData with information from fieldsInfo
            if (fieldsInfo.IsComplex(property))
            {
                viewData[FormConstants.FieldsInfoKey] = fieldsInfo.GetSubFieldInfo(property);
            }
            else if (!fieldsInfo.CanBeChangedByUser(property))
            {
                viewData.PrepareChangeReversal(FormConstants.DisabledKey, dataKeysToRemove, dataPrevValues);
                viewData[FormConstants.DisabledKey] = true;
            }

            // Render the editor
            MvcHtmlString result = displayDelegate();

            // Reverse the changes to ViewData
            dataKeysToRemove.ForEach(s => viewData.Remove(s));
            dataPrevValues.ForEach(pair => viewData[pair.Key] = pair.Value);

            return result;
        }

        private static void PrepareChangeReversal(
            this ViewDataDictionary viewData,
            string key,
            ICollection<string> dataKeysToRemove,
            IDictionary<string, object> dataPrevValues
        )
        {
            if (viewData.ContainsKey(key))
            {
                dataPrevValues[key] = viewData[key];
            }
            else
            {
                dataKeysToRemove.Add(key);
            }
        }

        /// <summary>
        /// Fetches field info for current editor template, using <paramref name="defaultValueProvider"/> if none is currently set
        /// </summary>
        public static ModelFieldsAccessibility GetFieldsInfo(
            this HtmlHelper html,
            Func<ModelFieldsAccessibility> defaultValueProvider
        )
        {
            if (html.ViewData.ContainsKey(FormConstants.FieldsInfoKey))
            {
                return (ModelFieldsAccessibility) html.ViewData[FormConstants.FieldsInfoKey];
            }

            return defaultValueProvider();
        }

        /// <summary>
        /// Adds "disabled" attribute to the collection, if the current editor template was set to be disabled by the fields info
        /// </summary>
        public static void AddDisabledConditionally(this HtmlHelper html, HtmlAttributeDictionary inputAttrs)
        {
            if (!html.ViewData.ContainsKey(FormConstants.DisabledKey)) return;

            if (html.ViewData[FormConstants.DisabledKey] is bool boolValue && boolValue)
            {
                inputAttrs["disabled"] = "disabled";
            }
        }
    }

    public class ActiveFormConfigIsDifferent : Exception
    {
        public ActiveFormConfigIsDifferent() : base(
            "The active FormConfig in the ViewData is different from the one that is disposing")
        {
        }
    }

    public static class ControllerExtension
    {
        /// <summary>
        /// Removes validation errors for fields that the user wasn't able to edit anyway,
        /// based on information provided by <paramref name="fieldsInfo"/>
        /// </summary>
        public static void RemoveIgnoredErrors(this Controller controller, ModelFieldsAccessibility fieldsInfo)
        {
            controller.ModelState
                .Select(pair => pair.Key)
                .Where(path => fieldsInfo.IgnoredValidationPaths.Any(path.StartsWith))
                .ToList() // Break reliance on ModelState, otherwise "Collection was modified; enumeration operation may not execute"
                .ForEach(path => controller.ModelState.Remove(path));
        }
    }
}