// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

public static class MemoryFileProviderAppSettingsExtensions
{
    public static void IncludeAppSettingsJsonFile(this MemoryFileProvider fileProvider, string contents)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);

        fileProvider.IncludeFile(MemoryFileProviderConfigurationBuilderExtensions.AppSettingsJsonFileName, contents);
    }

    public static void IncludeAppSettingsXmlFile(this MemoryFileProvider fileProvider, string contents)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);

        fileProvider.IncludeFile(MemoryFileProviderConfigurationBuilderExtensions.AppSettingsXmlFileName, contents);
    }

    public static void IncludeAppSettingsIniFile(this MemoryFileProvider fileProvider, string contents)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);

        fileProvider.IncludeFile(MemoryFileProviderConfigurationBuilderExtensions.AppSettingsIniFileName, contents);
    }

    public static void ReplaceAppSettingsJsonFile(this MemoryFileProvider fileProvider, string contents)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);

        fileProvider.ReplaceFile(MemoryFileProviderConfigurationBuilderExtensions.AppSettingsJsonFileName, contents);
    }
}
