using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CommonExtensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Evaluates the expression without parameters. Applicable to constant expressions and member expressions.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object Evaluate(this Expression expression)
        {
            UnaryExpression objectMember = Expression.Convert(expression, typeof(object));
            var getLambda = Expression.Lambda<Func<object>>(objectMember);

            return getLambda.Compile().Invoke();
        }

        /// <summary>
        /// Gets the arguments with values from a call expression
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static IDictionary<string, object> GetCallParameters<T, TResult>(this Expression<Func<T, TResult>> expression)
        {
            Expression innerExpression = expression.Body;
            if (innerExpression.NodeType != ExpressionType.Call)
            {
                return new Dictionary<string, object>();
            }

            var methodCallExpression = (MethodCallExpression)innerExpression;
            ParameterInfo[] parameters = methodCallExpression.Method.GetParameters();
            var arguments = methodCallExpression.Arguments.Select(argument => new { argument, value = argument.GetValue() });

            return parameters
                .Zip(arguments, (parameter, argument) => new KeyValuePair<string, object>(parameter.Name, ValueOrNothing(parameter, argument.value)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Use the expression
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static Expression<Func<T, TResult>> GetExpression<T, TResult>(this T subject, Expression<Func<T, TResult>> expression)
        {
            return expression;
        }

        /// <summary>
        /// Gets the MemberExpression of the given Func Expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TT"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static MemberExpression GetMemberExpression<T, TT>(this Expression<Func<T, TT>> expression)
        {
            if (expression?.Body == null) return null;
            switch (expression.Body)
            {
                case MemberExpression memberExpression:
                    return memberExpression;
                case UnaryExpression unaryExpression when unaryExpression.Operand is MemberExpression memExp:
                    return memExp;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression), "expression does not contain a member expression");
            }
        }

        /// <summary>
        /// Gets the value for a simple expression, otherwise ToString() :)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object GetValue(this Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constantExpression:
                    return constantExpression.Evaluate();
                case DefaultExpression defaultExpression:
                    return defaultExpression.Type.IsValueType ? Activator.CreateInstance(defaultExpression.Type) : null;
                case MemberExpression memberExpression:
                    return memberExpression.Evaluate();
                case UnaryExpression unaryExpression:
                    return unaryExpression.Evaluate();

                default:
                    return expression.ToString();
            }
        }

        /// <summary>
        /// Gets the expression print. `c => c.Prop.Value` prints `Prop.Value`
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static string Text<T, TResult>(this Expression<Func<T, TResult>> expression)
        {
            var source = new Stack<string>();
            Expression expression1 = expression.Body;
            while (expression1 != null)
            {
                if (expression1.NodeType == ExpressionType.Call)
                {
                    var methodCallExpression = (MethodCallExpression)expression1;
                    if (IsSingleArgumentIndexer(methodCallExpression))
                    {
                        source.Push(GetIndexerInvocation(methodCallExpression.Arguments.Single(), expression.Parameters.ToArray()));
                        expression1 = methodCallExpression.Object;
                    }
                    else
                    {
                        source.Push(methodCallExpression.Method.Name);

                        break;
                    }

                    ;
                }
                else if (expression1.NodeType == ExpressionType.ArrayIndex)
                {
                    var binaryExpression = (BinaryExpression)expression1;
                    source.Push(GetIndexerInvocation(binaryExpression.Right, expression.Parameters.ToArray()));
                    expression1 = binaryExpression.Left;
                }
                else if (expression1.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)expression1;
                    source.Push("." + memberExpression.Member.Name);
                    expression1 = memberExpression.Expression;
                }
                else if (expression1.NodeType == ExpressionType.Parameter)
                {
                    source.Push(string.Empty);
                    expression1 = null;
                }
                else if (expression1.NodeType == ExpressionType.Convert || expression1.NodeType == ExpressionType.ConvertChecked)
                {
                    source.Push(string.Empty);
                    var unaryExpression = (UnaryExpression)expression1;
                    expression1 = unaryExpression.Operand;
                }
                else
                {
                    break;
                }
            }

            if (source.Count > 0 && string.Equals(source.Peek(), ".model", StringComparison.OrdinalIgnoreCase))
            {
                source.Pop();
            }

            if (source.Count <= 0)
            {
                return string.Empty;
            }

            return source.Aggregate((left, right) => left + right).TrimStart('.');
        }

        private static string GetIndexerInvocation(
            Expression expression,
            ParameterExpression[] parameters)
        {
            var lambdaExpression = Expression.Lambda<Func<object, object>>(Expression.Convert(expression, typeof(object)), Expression.Parameter(typeof(object), null));
            Func<object, object> func;
            try
            {
                func = lambdaExpression.Compile();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid expression", new object[2]
                {
                    expression,
                    parameters[0].Name
                }), ex);
            }

            return "[" + Convert.ToString(func(null), CultureInfo.InvariantCulture) + "]";
        }

        private static bool IsSingleArgumentIndexer(Expression expression)
        {
            var methodExpression = expression as MethodCallExpression;

            return methodExpression != null && methodExpression.Arguments.Count == 1 && methodExpression.Method.DeclaringType.GetDefaultMembers().OfType<PropertyInfo>()
                .Any(p => p.GetGetMethod() == methodExpression.Method);
        }

        private static object ValueOrNothing(ParameterInfo parameter, object value)
        {
            // if the method parameter has a default value and the value is equal, return nothing
            if (parameter.HasDefaultValue && Equals(parameter.DefaultValue, value)) return null;

            // if the parameter is not nullable and the value is null, get default
            if (!typeof(Nullable).IsAssignableFrom(parameter.ParameterType) && value == null) return Activator.CreateInstance(parameter.ParameterType);

            return value;
        }
    }
}