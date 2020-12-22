// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Spring.TestResources
{
    public class Company
    {
        public string Address { get; }

        public Company(string str)
        {
            Address = str;
        }
    }
}
