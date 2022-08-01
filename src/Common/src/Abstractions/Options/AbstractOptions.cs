// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Options;

public abstract class AbstractOptions
{
    // This constructor is for use with IOptions
    protected AbstractOptions()
    {
    }

    protected AbstractOptions(IConfiguration root, string sectionPrefix = null)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (!string.IsNullOrEmpty(sectionPrefix))
        {
            var section = root.GetSection(sectionPrefix);
            section.Bind(this);
        }
        else
        {
            root.Bind(this);
        }
    }
}
