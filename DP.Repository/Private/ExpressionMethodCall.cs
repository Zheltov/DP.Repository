using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DP.Repository.Private
{
    class ExpressionMethodCall
    {
        public Expression<Action> Expression { get; set; }
        public IList<object> Arguments { get; set; }
        public string MethodName { get; set; }
        public ExpressionMethodCall( Expression<Action> expression )
        {
            if ( !( expression.Body is MethodCallExpression ) )
                throw new NotSupportedException( string.Format( "Not supported expression of type [{0}]", expression.Body.GetType().Name ) );

            Expression = expression;
            var mce = (MethodCallExpression)expression.Body;

            MethodName = mce.Method.ReflectedType.FullName + "." + mce.Method.Name;
            ArgumentsParse( mce );
        }

        void ArgumentsParse( MethodCallExpression expression )
        {
            Arguments = new List<object>();
            foreach ( var arg in expression.Arguments )
            {
                var objectMember = System.Linq.Expressions.Expression.Convert( arg, typeof( object ) );

                var getterLambda = System.Linq.Expressions.Expression.Lambda<Func<object>>( objectMember );

                var getter = getterLambda.Compile();

                Arguments.Add( getter() );
            }
        }

        public override bool Equals( object obj )
        {
            if ( !( obj is ExpressionMethodCall ) )
                return false;

            var item = (ExpressionMethodCall)obj;

            if ( MethodName != item.MethodName )
                return false;

            if ( Arguments.Count != item.Arguments.Count )
                return false;

            for ( int i = 0; i < Arguments.Count; i++ )
                if ( !Arguments[i].Equals( item.Arguments[i] ) )
                    return false;

            return true;
        }
        public override int GetHashCode()
        {
            return Expression.GetHashCode() + Arguments.GetHashCode() + MethodName.GetHashCode();
        }
    }
}
