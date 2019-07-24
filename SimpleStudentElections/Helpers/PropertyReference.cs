using System;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleStudentElections.Helpers
{
    public static class PropertyReference
    {
        /// <summary>
        /// Builds a <see cref="PropertyReference{TProperty}"/>
        /// </summary>
        /// <param name="obj">The object instance on which we are going to be changing the property</param>
        /// <param name="expression">Lambda expression that reads the property</param>
        /// <typeparam name="TContainer">The type of container object</typeparam>
        /// <typeparam name="TProperty">The type of property</typeparam>
        /// <returns></returns>
        /// <exception cref="Exception">If <see cref="expression"/> is invalid</exception>
        public static PropertyReference<TProperty> Create<TContainer, TProperty>(
            TContainer obj,
            Expression<Func<TContainer, TProperty>> expression
        )
        {
            if (!(expression.Body is MemberExpression memberExpression))
            {
                throw new Exception("Only direct property access is allowed");
            }

            if (!(memberExpression.Member is PropertyInfo propertyInfo))
            {
                throw new Exception("Only direct property access is allowed");
            }

            if (memberExpression.Expression != expression.Parameters[0])
            {
                throw new Exception("Only direct property access is allowed");
            }

            return new PropertyReference<TProperty>(
                BuildGetter<TContainer, TProperty>(obj, propertyInfo),
                BuildSetter<TContainer, TProperty>(obj, propertyInfo)
            );
        }

        private static Func<TProperty> BuildGetter<TContainer, TProperty>(TContainer obj, PropertyInfo propertyInfo)
        {
            ConstantExpression objExpression = Expression.Constant(obj);
            MemberExpression memberExpression = Expression.MakeMemberAccess(objExpression, propertyInfo);

            return Expression
                .Lambda<Func<TProperty>>(memberExpression)
                .Compile();
        }
        
        private static Action<TProperty> BuildSetter<TContainer, TProperty>(TContainer obj, PropertyInfo propertyInfo)
        {
            ConstantExpression objExpression = Expression.Constant(obj);
            MemberExpression memberExpression = Expression.MakeMemberAccess(objExpression, propertyInfo);

            ParameterExpression parameterExpression = Expression.Parameter(typeof(TProperty), "val");
            BinaryExpression assignExpression = Expression.Assign(memberExpression, parameterExpression);

            return Expression
                .Lambda<Action<TProperty>>(assignExpression, parameterExpression)
                .Compile();
        }
    }

    /// <summary>
    /// A helper class that allows to pass properties as references (by default C# only allows fields) 
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public class PropertyReference<TProperty>
    {
        private readonly Func<TProperty> _getter;
        private readonly Action<TProperty> _setter;

        public PropertyReference(Func<TProperty> getter, Action<TProperty> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public TProperty Value
        {
            get => _getter();
            set => _setter(value);
        }
    }
}