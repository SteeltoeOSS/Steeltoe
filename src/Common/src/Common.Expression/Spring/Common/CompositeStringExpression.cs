// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Common;

public class CompositeStringExpression : IExpression
{
    public CompositeStringExpression(string expressionString, List<IExpression> expressions)
    {
        ExpressionString = expressionString;
        Expressions = expressions;
    }

    public string ExpressionString { get; }

    public List<IExpression> Expressions { get; }

    public string GetValue()
    {
        var sb = new StringBuilder();
        foreach (var expression in Expressions)
        {
            var value = expression.GetValue(typeof(string));
            if (value != null)
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }

    public T GetValue<T>()
    {
        return (T)GetValue(typeof(T));
    }

    public object GetValue(Type desiredResultType)
    {
        var value = GetValue();
        return ExpressionUtils.ConvertTypedValue(null, new TypedValue(value), desiredResultType);
    }

    public string GetValue(object rootObject)
    {
        var sb = new StringBuilder();
        foreach (var expression in Expressions)
        {
            var value = expression.GetValue(rootObject, typeof(string));
            if (value != null)
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }

    public T GetValue<T>(object rootObject)
    {
        return (T)GetValue(rootObject, typeof(T));
    }

    public object GetValue(object rootObject, Type desiredResultType)
    {
        var value = GetValue(rootObject);
        return ExpressionUtils.ConvertTypedValue(null, new TypedValue(value), desiredResultType);
    }

    public string GetValue(IEvaluationContext context)
    {
        var sb = new StringBuilder();
        foreach (var expression in Expressions)
        {
            var value = expression.GetValue(context, typeof(string));
            if (value != null)
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }

    public T GetValue<T>(IEvaluationContext context)
    {
        return (T)GetValue(context, typeof(T));
    }

    public object GetValue(IEvaluationContext context, Type desiredResultType)
    {
        var value = GetValue(context);
        return ExpressionUtils.ConvertTypedValue(context, new TypedValue(value), desiredResultType);
    }

    public string GetValue(IEvaluationContext context, object rootObject)
    {
        var sb = new StringBuilder();
        foreach (var expression in Expressions)
        {
            var value = expression.GetValue(context, rootObject, typeof(string));
            if (value != null)
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }

    public T GetValue<T>(IEvaluationContext context, object rootObject)
    {
        return (T)GetValue(context, rootObject, typeof(T));
    }

    public object GetValue(IEvaluationContext context, object rootObject, Type desiredResultType)
    {
        var value = GetValue(context, rootObject);
        return ExpressionUtils.ConvertTypedValue(context, new TypedValue(value), desiredResultType);
    }

    public Type GetValueType()
    {
        return typeof(string);
    }

    public Type GetValueType(IEvaluationContext context)
    {
        return typeof(string);
    }

    public Type GetValueType(object rootObject)
    {
        return typeof(string);
    }

    public Type GetValueType(IEvaluationContext context, object rootObject)
    {
        return typeof(string);
    }

    public bool IsWritable(object rootObject)
    {
        return false;
    }

    public bool IsWritable(IEvaluationContext context)
    {
        return false;
    }

    public bool IsWritable(IEvaluationContext context, object rootObject)
    {
        return false;
    }

    public void SetValue(object rootObject, object value)
    {
        throw new EvaluationException(ExpressionString, "Cannot call setValue on a composite expression");
    }

    public void SetValue(IEvaluationContext context, object value)
    {
        throw new EvaluationException(ExpressionString, "Cannot call setValue on a composite expression");
    }

    public void SetValue(IEvaluationContext context, object rootObject, object value)
    {
        throw new EvaluationException(ExpressionString, "Cannot call setValue on a composite expression");
    }

    object IExpression.GetValue()
    {
        return GetValue();
    }

    object IExpression.GetValue(object rootObject)
    {
        return GetValue(rootObject);
    }

    object IExpression.GetValue(IEvaluationContext context)
    {
        return GetValue(context);
    }

    object IExpression.GetValue(IEvaluationContext context, object rootObject)
    {
        return GetValue(context, rootObject);
    }
}
