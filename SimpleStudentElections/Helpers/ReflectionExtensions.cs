using System;
using System.Linq;

namespace SimpleStudentElections.Helpers
{
    public static class ReflectionExtensions
    {
        public static bool ImplementsGenericInterface(this Type type, Type genericInterfaceType)
        {
            return type.ImplementsGenericInterface(genericInterfaceType, out _);
        }

        /// <summary>
        /// Helper to check whether a type implements a generic interface (eg. <see cref="ILookup{TKey,TElement}"/>)
        /// or not without checking against concrete values of type parameters
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <param name="genericInterfaceType">
        /// A generic type definition for an interface, e.g. typeof(ICollection&lt;&gt;) or typeof(IDictionary&lt;,&gt;).
        /// </param>
        /// <param name="realizedInterfaceType">The interface type with type parameters filled</param>
        public static bool ImplementsGenericInterface(
            this Type type,
            Type genericInterfaceType,
            out Type realizedInterfaceType
        )
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericInterfaceType)
            {
                realizedInterfaceType = genericInterfaceType;
                return true;
            }

            realizedInterfaceType = type
                .FindInterfaces((t, _) => t.IsGenericType && t.GetGenericTypeDefinition() == genericInterfaceType, null)
                .FirstOrDefault();

            return realizedInterfaceType != null;
        }

        /// <summary>
        ///    Determines whether the current type is or implements the specified generic interface, and determines that
        ///    interface's generic type parameters.
        ///    <br/>
        ///    Based on https://stackoverflow.com/a/24059740/2588539
        /// </summary>
        /// <param name="type">The current type.</param>
        /// <param name="interface">
        ///     A generic type definition for an interface, e.g. typeof(ICollection&lt;&gt;) or typeof(IDictionary&lt;,&gt;).
        /// </param>
        /// <param name="typeParameters">
        ///     Will receive an array containing the generic type parameters of the interface.
        /// </param>
        /// <returns>
        ///     True if the current type is or implements the specified generic interface.
        /// </returns>
        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
        {
            typeParameters = null;

            if (!type.ImplementsGenericInterface(@interface, out var foundInterface))
            {
                return false;
            }

            typeParameters = foundInterface.GetGenericArguments();
            return true;
        }

        public static Type[] GetInterfaceGenericParameters(this Type type, Type @interface)
        {
            type.TryGetInterfaceGenericParameters(@interface, out Type[] typeParameters);
            return typeParameters;
        }
    }
}