// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression
{
    /// <summary>
    /// An expression capable of evaluating itself against context objects.
    /// Encapsulates the details of a previously parsed expression string.
    /// Provides a common abstraction for expression evaluation.
    /// TODO:  This interface is not complete
    /// </summary>
    public interface IExpression
    {
        string ExpressionString { get; }

        object GetValue();

        object GetValue(Type desiredResultType);

        T GetValue<T>();

        object GetValue(object rootObject);

        object GetValue(object rootObject, Type desiredResultType);

        T GetValue<T>(object rootObject);

        object GetValue(IEvaluationContext context);

        object GetValue(IEvaluationContext context, object rootObject);

        object GetValue(IEvaluationContext context, Type desiredResultType);

        object GetValue(IEvaluationContext context, object rootObject, Type desiredResultType);

        T GetValue<T>(IEvaluationContext context);

        T GetValue<T>(IEvaluationContext context, object rootObject);

        Type GetValueType();

        Type GetValueType(object rootObject);

        Type GetValueType(IEvaluationContext context);

        Type GetValueType(IEvaluationContext context, object rootObject);

        bool IsWritable(object rootObject);

        bool IsWritable(IEvaluationContext context);

        bool IsWritable(IEvaluationContext context, object rootObject);

        bool SetValue(object rootObject, object value);

        bool SetValue(IEvaluationContext context, object value);

        bool SetValue(IEvaluationContext context, object rootObject, object value);
    }
}
