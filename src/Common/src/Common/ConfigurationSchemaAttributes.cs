#nullable enable

#pragma warning disable SA1636
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

#pragma warning disable SA1623 // Property summary documentation should match accessors

namespace Aspire;

/// <summary>
/// Attribute used to automatically generate a JSON schema for a component's configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class ConfigurationSchemaAttribute : Attribute
{
    public ConfigurationSchemaAttribute(string path, Type type, string[]? exclusionPaths = null)
    {
        Path = path;
        Type = type;
        ExclusionPaths = exclusionPaths;
    }

    /// <summary>
    /// The path corresponding to which config section <see cref="Type"/> binds to.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The type that is bound to the configuration. This type will be walked and generate a JSON schema for all the properties.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// (optional) The config sections to exclude from the ConfigurationSchema. This is useful if there are properties you don't want to publicize in the config schema.
    /// </summary>
    public string[]? ExclusionPaths { get; }
}

/// <summary>
/// Provides information to describe the logging categories produced by a component.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
internal sealed class LoggingCategoriesAttribute : Attribute
{
    public LoggingCategoriesAttribute(params string[] categories)
    {
        Categories = categories;
    }

    /// <summary>
    /// The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.
    /// </summary>
    public string[] Categories { get; set; }
}
