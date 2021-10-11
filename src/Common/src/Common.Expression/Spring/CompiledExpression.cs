// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public abstract class CompiledExpression
    {
        internal readonly Dictionary<string, object> _dynamicFields = new ();

        internal Delegate MethodDelegate { get; set; }

        internal Delegate InitDelegate { get; set; }

        protected CompiledExpression()
        {
        }

        public virtual object GetValue(object target, IEvaluationContext context)
        {
            return null;
        }
    }
}
