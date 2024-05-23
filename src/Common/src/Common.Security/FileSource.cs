// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Security;

internal sealed class FileSource : FileConfigurationSource
{
    internal string BasePath { get; set; }

    internal string Key { get; }

    public FileSource(string key)
    {
        Key = key;
    }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new FileProvider(this);
    }
}
