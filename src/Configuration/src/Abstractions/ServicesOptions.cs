// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration;

public class ServicesOptions : AbstractServiceOptions
{
    // This constructor is for use with IOptions.
    public ServicesOptions()
    {
    }

    public ServicesOptions(IConfiguration configuration)
        : this(configuration, null)
    {
    }

    public ServicesOptions(IConfiguration configuration, string sectionPrefix)
        : base(configuration, sectionPrefix)
    {
    }
}
