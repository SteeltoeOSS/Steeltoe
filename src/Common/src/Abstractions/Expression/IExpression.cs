// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
