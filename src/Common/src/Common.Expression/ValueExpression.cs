// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal;

public class ValueExpression<V> : IExpression
{
    private V _value;
    private Type _asClass;

    public string ExpressionString => _value.ToString();

    public ValueExpression(V value)
    {
        _value = value;
        _asClass = value.GetType();
    }

    public virtual object GetValue()
    {
        return _value;
    }

    public virtual object GetValue(Type desiredResultType)
    {
        if (!desiredResultType.IsInstanceOfType(_value))
        {
            throw new EvaluationException(_value.ToString(), "value is not of correct type");
        }

        return _value;
    }

    public virtual T GetValue<T>()
    {
        return (T)GetValue(typeof(T));
    }

    public virtual object GetValue(object rootObject)
    {
        return _value;
    }

    public virtual object GetValue(IEvaluationContext context, Type desiredResultType)
    {
        return GetValue(desiredResultType);
    }

    public virtual object GetValue(object rootObject, Type desiredResultType)
    {
        return GetValue(desiredResultType);
    }

    public virtual T GetValue<T>(object rootObject)
    {
        return (T)GetValue(typeof(T));
    }

    public virtual object GetValue(IEvaluationContext context)
    {
        return _value;
    }

    public virtual object GetValue(IEvaluationContext context, object rootObject)
    {
        return _value;
    }

    public virtual object GetValue(IEvaluationContext context, object rootObject, Type desiredResultType)
    {
        return GetValue(desiredResultType);
    }

    public virtual T GetValue<T>(IEvaluationContext context)
    {
        return (T)GetValue(typeof(T));
    }

    public virtual T GetValue<T>(IEvaluationContext context, object rootObject)
    {
        return (T)GetValue(typeof(T));
    }

    public virtual Type GetValueType()
    {
        return _asClass;
    }

    public virtual Type GetValueType(object rootObject)
    {
        return _asClass;
    }

    public virtual Type GetValueType(IEvaluationContext context)
    {
        return _asClass;
    }

    public virtual Type GetValueType(IEvaluationContext context, object rootObject)
    {
        return _asClass;
    }

    public virtual bool IsWritable(object rootObject)
    {
        return false;
    }

    public virtual bool IsWritable(IEvaluationContext context)
    {
        return false;
    }

    public virtual bool IsWritable(IEvaluationContext context, object rootObject)
    {
        return false;
    }

    public virtual void SetValue(object rootObject, object value)
    {
        throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
    }

    public virtual void SetValue(IEvaluationContext context, object value)
    {
        throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
    }

    public virtual void SetValue(IEvaluationContext context, object rootObject, object value)
    {
        throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
    }
}
