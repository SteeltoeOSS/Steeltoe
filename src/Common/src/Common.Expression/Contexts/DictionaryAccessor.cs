// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class DictionaryAccessor : ICompilablePropertyAccessor
    {
        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return target is IDictionary && ((IDictionary)target).Contains(name);
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return true;
        }

        public Type GetPropertyType()
        {
            return typeof(object);
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type>() { typeof(IDictionary) };
        }

        public bool IsCompilable()
        {
            return true;
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            var asDict = target as IDictionary;
            if (asDict == null)
            {
                throw new ArgumentException("Target must be of type IDictionary");
            }

            if (asDict.Contains(name))
            {
                return new TypedValue(asDict[name]);
            }

            throw new DictionaryAccessException(name);
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            var asDict = target as IDictionary;
            if (asDict == null)
            {
                throw new ArgumentException("Target must be of type IDictionary");
            }

            asDict[name] = newValue;
        }

        public void GenerateCode(string propertyName, DynamicMethod mv, CodeFlow cf)
        {
            // String descriptor = cf.lastDescriptor();
            // if (descriptor == null || !descriptor.equals("Ljava/util/Map"))
            // {
            //    if (descriptor == null)
            //    {
            //        cf.loadTarget(mv);
            //    }
            //    CodeFlow.insertCheckCast(mv, "Ljava/util/Map");
            // }
            // mv.visitLdcInsn(propertyName);
            // mv.visitMethodInsn(INVOKEINTERFACE, "java/util/Map", "get", "(Ljava/lang/Object;)Ljava/lang/Object;", true);
        }

        private class DictionaryAccessException : AccessException
        {
            private readonly string _key;

            public DictionaryAccessException(string key)
                    : base(string.Empty)
            {
                _key = key;
            }

            public override string Message => "Dictionary does not contain a value for key '" + _key + "'";
        }
    }
}
