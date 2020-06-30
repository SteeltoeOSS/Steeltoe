// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Common.Contexts
{
    public class GenericApplicationContext : AbstractApplicationContext
    {
        public GenericApplicationContext(IServiceProvider serviceProvider, IConfiguration configuration)
            : base(serviceProvider, configuration)
        {
        }
    }
}
