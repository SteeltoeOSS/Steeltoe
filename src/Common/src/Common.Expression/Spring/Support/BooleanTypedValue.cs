// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class BooleanTypedValue : TypedValue
    {
        public static readonly BooleanTypedValue TRUE = new BooleanTypedValue(true);

        public static readonly BooleanTypedValue FALSE = new BooleanTypedValue(false);

        private BooleanTypedValue(bool b)
            : base(b)
        {
        }

        public static BooleanTypedValue ForValue(bool b)
        {
            return b ? TRUE : FALSE;
        }
    }
}
