// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Integration.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
    public class PayloadsAttribute : Attribute
    {
        public PayloadsAttribute()
        {
            Expression = string.Empty;
        }

        public PayloadsAttribute(string expression)
        {
            Expression = expression;
        }

        public virtual string Expression { get; set; }
    }
}
