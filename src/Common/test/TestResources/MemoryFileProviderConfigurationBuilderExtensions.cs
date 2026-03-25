// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;

namespace Steeltoe.Common.TestResources;

public static class MemoryFileProviderConfigurationBuilderExtensions
{
    internal const string AppSettingsJsonFileName = "appsettings.json";
    internal const string AppSettingsXmlFileName = "appsettings.xml";
    internal const string AppSettingsIniFileName = "appsettings.ini";

    public static void AddInMemoryAppSettingsJsonFile(this IConfigurationBuilder builder, MemoryFileProvider fileProvider)
    {
        AddInMemoryJsonFile(builder, fileProvider, AppSettingsJsonFileName);
    }

    public static void AddInMemoryJsonFile(this IConfigurationBuilder builder, MemoryFileProvider fileProvider, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fileProvider);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var source = new JsonConfigurationSource
        {
            FileProvider = fileProvider,
            Path = path,
            Optional = false,
            ReloadOnChange = true,
            // Turn off debounce, so the change token triggers immediately. Then we don't need to sleep in tests.
            ReloadDelay = 0
        };

        builder.Add(source);
    }

    public static void AddInMemoryAppSettingsXmlFile(this IConfigurationBuilder builder, MemoryFileProvider fileProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fileProvider);

        var source = new XmlConfigurationSource
        {
            FileProvider = fileProvider,
            Path = AppSettingsXmlFileName,
            Optional = false,
            ReloadOnChange = true,
            // Turn off debounce, so the change token triggers immediately. Then we don't need to sleep in tests.
            ReloadDelay = 0
        };

        builder.Add(source);
    }

    public static void AddInMemoryAppSettingsIniFile(this IConfigurationBuilder builder, MemoryFileProvider fileProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fileProvider);

        var source = new IniConfigurationSource
        {
            FileProvider = fileProvider,
            Path = AppSettingsIniFileName,
            Optional = false,
            ReloadOnChange = true,
            // Turn off debounce, so the change token triggers immediately. Then we don't need to sleep in tests.
            ReloadDelay = 0
        };

        builder.Add(source);
    }
}
