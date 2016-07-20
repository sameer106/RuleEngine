using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RuleEngine.DataObject;

namespace RuleEngine.Core
{
    public class PrecompiledRules
    {
        public static List<Func<T, bool>> CompileRule<T>(List<T> targetEntity, List<RuleExpression> rules)
        {
            var compiledRules = new List<Func<T, bool>>();

            // Loop through the rules and compile them against the properties of the supplied object 
            rules.ForEach(rule =>
            {
                var genericType = Expression.Parameter(typeof(T));
                var key = MemberExpression.Property(genericType, rule.propertyName.Trim());
                var propertyType = typeof(T).GetProperty(rule.propertyName.Trim()).PropertyType;
                var value = Expression.Constant(Convert.ChangeType(rule.value.Trim(), propertyType));
                var binaryExpression = Expression.MakeBinary(rule.operation, key, value);

                
                compiledRules.Add(Expression.Lambda<Func<T,bool>>(binaryExpression, genericType).Compile());
            });

            // Return the compiled rules
            return compiledRules;
        }

        public static List<Func<T, bool>> CompileRule<T>(T targetEntity, List<RuleExpression> rules)
        {
            var compiledRules = new List<Func<T, bool>>();

            // Loop through the rules and compile them against the properties of the supplied object 
            rules.ForEach(rule =>
            {
                var genericType = Expression.Parameter(typeof(T));
                var key = MemberExpression.Property(genericType, rule.propertyName.Trim());
                var propertyType = typeof(T).GetProperty(rule.propertyName.Trim()).PropertyType;
                var value = Expression.Constant(Convert.ChangeType(rule.value.Trim(), propertyType));
                var binaryExpression = Expression.MakeBinary(rule.operation, key, value);


                compiledRules.Add(Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile());
            });

            // Return the compiled rules
            return compiledRules;
        }

        public static Func<T, bool> CompileRule<T>(T targetEntity, RuleExpression rule)
        {
            
            // Loop through the rules and compile them against the properties of the supplied object 
            
                var genericType = Expression.Parameter(typeof(T));
                var key = MemberExpression.Property(genericType, rule.propertyName.Trim());
                var propertyType = typeof(T).GetProperty(rule.propertyName.Trim()).PropertyType;
                var value = Expression.Constant(Convert.ChangeType(rule.value.Trim(), propertyType));
                var binaryExpression = Expression.MakeBinary(rule.operation, key, value);


                var  compiledRules = Expression.Lambda<Func<T, bool>>(binaryExpression, genericType).Compile();

            // Return the compiled rules
            return compiledRules;
        }
    }
}
