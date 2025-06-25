// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions
{
    internal static partial class DiagnosticDescriptorHelper
    {
        public static DiagnosticDescriptor Create(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString? description = null,
            params string[] customTags)
        {
            string helpLink = $"https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/{id.ToLowerInvariant()}";

            return new DiagnosticDescriptor(id, title, messageFormat, category, defaultSeverity, isEnabledByDefault, description, helpLink, customTags);
        }
    }
}
