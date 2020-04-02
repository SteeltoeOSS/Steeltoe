﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.Common.Expression;
using System;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class ValueExpression<V> : IExpression
    {
        private V _value;
        private Type _asClass;

        public string ExpressionString => throw new NotImplementedException();

        public ValueExpression(V value)
        {
            _value = value;
            _asClass = value.GetType();
        }

        public object GetValue()
        {
            return _value;
        }

        public object GetValue(Type desiredResultType)
        {
            if (!desiredResultType.IsInstanceOfType(_value))
            {
                throw new EvaluationException(_value.ToString(), "value is not of correct type");
            }

            return _value;
        }

        public T GetValue<T>()
        {
            return (T)GetValue(typeof(T));
        }

        public object GetValue(object rootObject)
        {
            return _value;
        }

        public object GetValue(object rootObject, Type desiredResultType)
        {
            return GetValue(desiredResultType);
        }

        public T GetValue<T>(object rootObject)
        {
            return (T)GetValue(typeof(T));
        }

        public object GetValue(IEvaluationContext context)
        {
            return _value;
        }

        public object GetValue(IEvaluationContext context, object rootObject)
        {
            return _value;
        }

        public object GetValue(IEvaluationContext context, object rootObject, Type desiredResultType)
        {
            return GetValue(desiredResultType);
        }

        public T GetValue<T>(IEvaluationContext context)
        {
            return (T)GetValue(typeof(T));
        }

        public T GetValue<T>(IEvaluationContext context, object rootObject)
        {
            return (T)GetValue(typeof(T));
        }

        public Type GetValueType()
        {
            return _asClass;
        }

        public Type GetValueType(object rootObject)
        {
            return _asClass;
        }

        public Type GetValueType(IEvaluationContext context)
        {
            return _asClass;
        }

        public Type GetValueType(IEvaluationContext context, object rootObject)
        {
            return _asClass;
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

        public bool SetValue(object rootObject, object value)
        {
            throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
        }

        public bool SetValue(IEvaluationContext context, object value)
        {
            throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
        }

        public bool SetValue(IEvaluationContext context, object rootObject, object value)
        {
            throw new EvaluationException(_value.ToString(), "Cannot call SetValue() on a ValueExpression");
        }
    }
}
