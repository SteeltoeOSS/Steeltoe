// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ConfigurationAccessor : IPropertyAccessor
    {
        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return target is IConfiguration;
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return false;
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type> { typeof(IConfiguration) };
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            if (target is not IConfiguration asConfig)
            {
                throw new ArgumentException("Target must be of type IConfiguration");
            }

            return new TypedValue(asConfig[name]);
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            // Empty
        }
    }
}
