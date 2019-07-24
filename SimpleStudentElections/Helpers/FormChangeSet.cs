using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// This code in this file is responsible for recording "edited fields" for audit screen
// It generates a serializable graph of "IPropertyDelta"s, with ComplexTypeDelta as the root
// The values are read via reflection and compared for changes using Object.Equals
// The behaviour of this system (such as excluding certain fields) is controlled via attributes
// Note that the purpose of this system has changed during development - as such there is some currently unused code here

namespace SimpleStudentElections.Helpers
{
    public static class FormChangeSet
    {
        public static FormChangeSet<TForm> Generate<TForm>(TForm form, TForm originalForm)
        {
            return new FormChangeSet<TForm>(
                new FormChangeSetGenerator().Execute(form, originalForm)
            );
        }
    }

    [Serializable]
    public class FormChangeSet<TForm>
    {
        public ComplexTypeDelta Delta { get; }

        public FormChangeSet(ComplexTypeDelta delta)
        {
            Delta = delta;
        }

        public bool IsChanged<TProperty>(Expression<Func<TForm, TProperty>> expression)
        {
            Stack<IFormChangeSetAccess> stack = BuildAccess(expression.Body, expression.Parameters[0]);
            IPropertyDelta currentDelta = Delta;

            while (stack.Count > 0)
            {
                IFormChangeSetAccess accessor = stack.Pop();
                currentDelta = accessor.Retrieve(currentDelta);
            }

            return currentDelta.HasChanged;
        }

        private static Stack<IFormChangeSetAccess> BuildAccess(Expression expression, ParameterExpression formParameter)
        {
            Stack<IFormChangeSetAccess> stack = new Stack<IFormChangeSetAccess>();

            while (expression != null)
            {
                if (expression == formParameter)
                {
                    // We are done
                    expression = null;
                    break;
                }

                switch (expression)
                {
                    case MemberExpression memberExpression:
                        stack.Push(new FormChangeSetPropertyAccess() {Name = memberExpression.Member.Name});
                        expression = memberExpression.Expression;

                        continue;

                    case MethodCallExpression methodCallExpression when
                        methodCallExpression.Method.Name == "get_Item"
                        && methodCallExpression.Arguments.Count == 1
                        && methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant
                        && methodCallExpression.Arguments[0].Type == typeof(int):
                    {
                        int index = Expression
                            .Lambda<Func<int>>(methodCallExpression.Arguments[0])
                            .Compile()
                            .Invoke();

                        stack.Push(new FormChangeSetListItemAccessAccess() {Index = index});
                        expression = methodCallExpression.Object;

                        continue;
                    }

                    default:
                        throw new Exception(
                            $"Expression {expression} is not supported for accessing change set values"
                        );
                }
            }

            return stack;
        }
    }

    public interface IFormChangeSetAccess
    {
        IPropertyDelta Retrieve(IPropertyDelta outerDelta);
    }

    public class FormChangeSetPropertyAccess : IFormChangeSetAccess
    {
        public string Name { get; set; }

        public IPropertyDelta Retrieve(IPropertyDelta outerDelta)
        {
            try
            {
                return ((ComplexTypeDelta) outerDelta).PropertiesDeltas[Name];
            }
            catch (KeyNotFoundException e)
            {
                throw new Exception($"Property {Name} is not part of change set", e);
            }
        }
    }

    public class FormChangeSetListItemAccessAccess : IFormChangeSetAccess
    {
        public int Index { get; set; }

        public IPropertyDelta Retrieve(IPropertyDelta outerDelta)
        {
            try
            {
                return ((ListPropertyDelta) outerDelta).ItemsDeltas[Index];
            }
            catch (IndexOutOfRangeException e)
            {
                throw new Exception($"Element {Index} is not part of change set", e);
            }
        }
    }

    public class FormChangeSetGenerator
    {
        public ComplexTypeDelta Execute<TForm>(TForm form, TForm originalForm)
        {
            return ProcessComplex(form.GetType(), form, originalForm);
        }

        private ComplexTypeDelta ProcessComplex(Type type, object newForm, object originalForm)
        {
            if (!TryGetComplexAttribute(type, out FormComplexAttribute formComplexAttribute))
            {
                throw new Exception($"{type} is not marked with {nameof(FormComplexAttribute)}");
            }

            Dictionary<string, IPropertyDelta> propertyDeltas = new Dictionary<string, IPropertyDelta>();
            bool defaultInclusion = formComplexAttribute.DefaultIncludeProperties;

            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                bool? include = propertyInfo.GetCustomAttribute<ChangeSetInclusionAttribute>()?.Include;

                if (include.GetValueOrDefault(defaultInclusion))
                {
                    propertyDeltas[propertyInfo.Name] = ProcessVariable(
                        propertyInfo.PropertyType,
                        propertyInfo.GetValue(newForm),
                        propertyInfo.GetValue(originalForm)
                    );
                }
            }

            return new ComplexTypeDelta(propertyDeltas)
            {
                Type = type,
                NewValue = newForm,
                OriginalValue = originalForm
            };
        }

        private static bool TryGetComplexAttribute(Type type, out FormComplexAttribute attribute)
        {
            attribute = type.GetCustomAttribute<FormComplexAttribute>(true);

            return attribute != null;
        }

        private static bool IsComplex(Type type)
        {
            return TryGetComplexAttribute(type, out _);
        }

        private IPropertyDelta ProcessVariable(Type type, object newValue, object originalValue)
        {
            if (newValue == null || originalValue == null)
            {
                // Null can be handled only as atomic value
                return ProcessAtomic(type, newValue, originalValue);
            }

            if (IsComplex(type))
            {
                return ProcessComplex(type, newValue, originalValue);
            }

            if (IsList(type))
            {
                return ProcessList(type, newValue, originalValue);
            }

            return ProcessAtomic(type, newValue, originalValue);
        }

        private static bool IsList(Type type)
        {
            return type.ImplementsGenericInterface(typeof(IList<>)) || type.IsSubclassOf(typeof(IList));
        }

        private ListPropertyDelta ProcessList(Type type, object newValue, object originalValue)
        {
            Type itemsType = TryGetListItemsType(newValue, originalValue);

            // Note that itemsType can be null. If that's the case then type resolution is delegated to
            // passes over individual items as the list can be non-homogeneous 

            IList newList = (IList) newValue;
            IList originalList = (IList) originalValue;

            int length = Math.Max(newList.Count, originalList.Count);
            IPropertyDelta[] itemsDeltas = new IPropertyDelta[length];

            for (int i = 0; i < length; i++)
            {
                object newItem = GetListItem(newList, i);
                object originalItem = GetListItem(originalList, i);

                itemsDeltas[i] = ProcessListItem(itemsType, newItem, originalItem);
            }

            return new ListPropertyDelta(itemsDeltas)
            {
                Type = type,
                NewValue = newList,
                OriginalValue = originalList
            };
        }

        private static Type TryGetListItemsType(object newValue, object originalValue)
        {
            Type newType = TryGetListItemsType(newValue);
            Type originalType = TryGetListItemsType(originalValue);

            if (newType == null || originalType == null)
            {
                // Non-generic list probably
                return null;
            }

            return newType == originalType
                ? newType // Success!!!! 
                : null; // Ummmm...
        }

        private static Type TryGetListItemsType(object list)
        {
            return list.GetType().GetInterfaceGenericParameters(typeof(IList<>))?[0];
        }

        private static object GetListItem(IList list, int i)
        {
            try
            {
                return list[i];
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private IPropertyDelta ProcessListItem(Type type, object newValue, object originalValue)
        {
            if (type == null)
            {
                // We don't know the type, figure it out on the go
                type = newValue.GetType() == originalValue.GetType()
                    ? newValue.GetType() // Luckily they are the same
                    : typeof(object); // Just set to object - this shouldn't ever be reached under normal circumstances anyway
            }

            return ProcessVariable(type, newValue, originalValue);
        }

        private AtomicPropertyDelta ProcessAtomic(Type type, object newValue, object originalValue)
        {
            bool hasChanged;

            if (newValue == null && originalValue == null)
            {
                hasChanged = false;
            }
            else if (newValue == null || originalValue == null)
            {
                // One of them is null
                hasChanged = true;
            }
            else
            {
                hasChanged = !newValue.Equals(originalValue);
            }

            return new AtomicPropertyDelta()
            {
                Type = type,
                NewValue = newValue,
                OriginalValue = originalValue,

                HasChanged = hasChanged
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FormComplexAttribute : Attribute
    {
        public bool DefaultIncludeProperties { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ChangeSetInclusionAttribute : Attribute
    {
        public bool Include { get; }

        public ChangeSetInclusionAttribute(bool include)
        {
            Include = include;
        }
    }

    public class ChangeSetIncludeAttribute : ChangeSetInclusionAttribute
    {
        public ChangeSetIncludeAttribute() : base(true)
        {
        }
    }

    public class ChangeSetExcludeAttribute : ChangeSetInclusionAttribute
    {
        public ChangeSetExcludeAttribute() : base(false)
        {
        }
    }

    public interface IPropertyDelta
    {
        Type Type { get; }

        object NewValue { get; }
        object OriginalValue { get; }

        bool HasChanged { get; }
        
        /// <summary>
        /// If true, indicates that NewValue and OriginalValue can be used freely at any time after change set creation.
        /// Otherwise, reading and writing needs to be done carefully (or a copy needs to be created)
        /// </summary>
        bool AreValuesImmutable { get; }
    }

    /// <summary>
    /// Represents that this property contains other information inside.
    /// Examples: list, complex type
    /// </summary>
    public interface IContainerProperty
    {
        IPropertyDelta GetDeltaForItem(object identifier);
    }

    /// <summary>
    /// Represents the change in a simple value such as a string or a value object/struct (e.g. DateTime)
    /// </summary>
    [Serializable]
    public sealed class AtomicPropertyDelta : IPropertyDelta
    {
        public Type Type { get; set; }

        public object NewValue { get; set; }
        public object OriginalValue { get; set; }

        public bool HasChanged { get; set; }
        public bool AreValuesImmutable => Type.IsValueType || Type == typeof(string);
    }

    [Serializable]
    public sealed class ListPropertyDelta : IPropertyDelta, IContainerProperty
    {
        public Type Type { get; set; }

        public object NewValue { get; set; }
        public object OriginalValue { get; set; }
        public readonly IPropertyDelta[] ItemsDeltas;

        public bool HasChanged => ItemsDeltas.Any(delta => delta.HasChanged);
        public bool AreValuesImmutable => false;

        public ListPropertyDelta(IPropertyDelta[] itemsDeltas)
        {
            ItemsDeltas = itemsDeltas;
        }

        public IPropertyDelta GetDeltaForItem(object identifier)
        {
            int i;
            try
            {
                i = (int) identifier;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"{nameof(ListPropertyDelta)} accepts only ints for {nameof(GetDeltaForItem)}", e);
            }

            return ItemsDeltas[i];
        }

        public bool IsAdded(int i)
        {
            IPropertyDelta delta = ItemsDeltas[i];

            return delta.OriginalValue == null && delta.NewValue != null;
        }

        public bool IsRemoved(int i)
        {
            IPropertyDelta delta = ItemsDeltas[i];

            return delta.OriginalValue != null && delta.NewValue == null;
        }
    }

    [Serializable]
    public sealed class ComplexTypeDelta : IPropertyDelta, IContainerProperty
    {
        public Type Type { get; set; }

        public object NewValue { get; set; }
        public object OriginalValue { get; set; }
        public readonly IDictionary<string, IPropertyDelta> PropertiesDeltas;

        public bool HasChanged => PropertiesDeltas.Any(pair => pair.Value.HasChanged);
        public bool AreValuesImmutable => false;

        public ComplexTypeDelta(IDictionary<string, IPropertyDelta> propertiesDeltas)
        {
            PropertiesDeltas = propertiesDeltas;
        }

        public IPropertyDelta GetDeltaForItem(object identifier)
        {
            string key;
            try
            {
                key = (string) identifier;
            }
            catch (InvalidCastException e)
            {
                throw new Exception(
                    $"{nameof(ComplexTypeDelta)} accepts only strings for {nameof(GetDeltaForItem)}", e
                );
            }

            return PropertiesDeltas[key];
        }
    }
}