using System.Linq.Expressions;

namespace PersistenceToolkit.Persistence.Helpers
{
    internal class ExpressionHelper
    {
        internal static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return expression.Body switch
            {
                MemberExpression member => member.Member.Name,
                UnaryExpression { Operand: MemberExpression inner } => inner.Member.Name,
                _ => throw new ArgumentException("Invalid navigation expression")
            };
        }
        internal static string GetPropertyName<T>(Expression<Func<T, object>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression member)
            {
                return member.Member.Name;
            }

            if (propertyExpression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberOperand)
            {
                return memberOperand.Member.Name;
            }

            throw new ArgumentException("Invalid property expression", nameof(propertyExpression));
        }
    }
}
