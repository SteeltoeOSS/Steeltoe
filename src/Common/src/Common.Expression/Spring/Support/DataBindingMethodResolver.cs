﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class DataBindingMethodResolver : ReflectiveMethodResolver
    {
        private DataBindingMethodResolver()
        : base()
        {
        }

        public static DataBindingMethodResolver ForInstanceMethodInvocation()
        {
            return new DataBindingMethodResolver();
        }

        public override IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
        {
            if (targetObject is Type)
            {
                throw new ArgumentException("DataBindingMethodResolver does not support Class targets");
            }

            return base.Resolve(context, targetObject, name, argumentTypes);
        }

        protected override bool IsCandidateForInvocation(MethodInfo method, Type targetClass)
        {
            if (method.IsStatic)
            {
                return false;
            }

            var clazz = method.DeclaringType;
            return clazz != typeof(object) && clazz != typeof(Type);
        }
    }
}
