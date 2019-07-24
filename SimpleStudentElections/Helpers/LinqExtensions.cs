using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SimpleStudentElections.Helpers
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Helper to chain <code>foreach</code> with other LINQ methods. 
        /// </summary>
        /// <param name="enumeration">A collection or LINQ "setup"</param>
        /// <param name="action">The action to preform on each item</param>
        public static void ForEach<T>([InstantHandle] this IEnumerable<T> enumeration, [InstantHandle] Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
    }
}