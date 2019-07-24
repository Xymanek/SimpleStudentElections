using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Humanizer;
using JetBrains.Annotations;

namespace SimpleStudentElections.Helpers
{
    /// <summary>
    /// Converts FormChangeSet to HTML list
    /// </summary>
    public static class FormChangeSetPrinter
    {
        public static string GenerateHtmlList(ComplexTypeDelta rootDelta)
        {
            return GetChanged(rootDelta).Aggregate("<ul>", (current, path) => current + $"<li>{path}</li>") + "</ul>";
        }

        [Pure]
        private static IEnumerable<string> GetChanged(IPropertyDelta delta)
        {
            switch (delta)
            {
                case ComplexTypeDelta complexTypeDelta:
                    return GetChanged(complexTypeDelta);

                case ListPropertyDelta listPropertyDelta:
                    return GetChanged(listPropertyDelta);
            }

            throw new ArgumentOutOfRangeException(
                nameof(delta),
                $"Expected {nameof(ComplexTypeDelta)} or {nameof(ListPropertyDelta)}"
            );
        }

        [Pure]
        internal static IEnumerable<string> GetChanged(ComplexTypeDelta delta)
        {
            List<string> changed = new List<string>();

            foreach (KeyValuePair<string, IPropertyDelta> propertyDelta in delta.PropertiesDeltas)
            {
                if (!propertyDelta.Value.HasChanged) continue;

                if (propertyDelta.Value is AtomicPropertyDelta)
                {
                    changed.Add(GetDisplayFieldName(propertyDelta.Key, delta.Type));
                }
                else
                {
                    changed.AddRange(
                        GetChanged(propertyDelta.Value)
                            .Select(s => GetDisplayFieldName(propertyDelta.Key, delta.Type) + " > " + s)
                    );
                }
            }

            return changed;
        }

        [Pure]
        internal static string GetDisplayFieldName(string propertyName, Type containerType)
        {
            string fieldName = propertyName;

            // Try to get a better name
            PropertyInfo propertyInfo = containerType.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                string displayName = propertyInfo.GetCustomAttribute<DisplayAttribute>()?.GetName();
                if (displayName != null)
                {
                    fieldName = displayName;
                }

                string section = propertyInfo.GetCustomAttribute<ChangeSetPrinterSection>()?.Section;
                if (section != null)
                {
                    fieldName = section + " > " + fieldName;
                }
            }

            return fieldName;
        }

        [Pure]
        internal static IEnumerable<string> GetChanged(ListPropertyDelta delta)
        {
            List<string> changed = new List<string>();

            for (var i = 0; i < delta.ItemsDeltas.Length; i++)
            {
                int humanIndex = i + 1;
                IPropertyDelta itemDelta = delta.ItemsDeltas[i];

                if (!itemDelta.HasChanged) continue;

                if (itemDelta is AtomicPropertyDelta)
                {
                    changed.Add(humanIndex.ToString());
                }
                else
                {
                    changed.AddRange(GetChanged(itemDelta).Select(s => humanIndex.Ordinalize() + " > " + s));
                }
            }

            return changed;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ChangeSetPrinterSection : Attribute
    {
        public readonly string Section;

        public ChangeSetPrinterSection(string section)
        {
            Section = section;
        }
    }
}