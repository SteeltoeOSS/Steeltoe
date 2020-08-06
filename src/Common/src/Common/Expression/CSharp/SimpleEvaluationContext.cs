// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Common.Expression.CSharp
{
    public class SimpleEvaluationContext : IEvaluationContext
    {
        private readonly IApplicationContext _context;

        public SimpleEvaluationContext(IApplicationContext context)
        {
            _context = context;
        }

        public ITypeConverter TypeConverter { get; set; }
    }
}
