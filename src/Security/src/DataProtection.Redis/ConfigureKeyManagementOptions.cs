// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace Steeltoe.Security.DataProtection.Redis;

internal sealed class ConfigureKeyManagementOptions : IConfigureNamedOptions<KeyManagementOptions>
{
    private readonly IXmlRepository _xmlRepository;

    public ConfigureKeyManagementOptions(IXmlRepository xmlRepository)
    {
        ArgumentNullException.ThrowIfNull(xmlRepository);

        _xmlRepository = xmlRepository;
    }

    public void Configure(KeyManagementOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    public void Configure(string? name, KeyManagementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.XmlRepository = _xmlRepository;
    }
}
