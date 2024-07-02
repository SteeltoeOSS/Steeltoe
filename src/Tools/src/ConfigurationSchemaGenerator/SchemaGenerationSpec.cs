// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using SourceGenerators;

namespace ConfigurationSchemaGenerator;

public sealed record SchemaGenerationSpec
{
    public required List<TypeSpec> ConfigurationTypes { get; init; }
    public required List<string>? ConfigurationPaths { get; init; }
    public required List<string>? ExclusionPaths { get; init; }
    public required List<string>? LogCategories { get; init; }
    public required ImmutableEquatableArray<TypeSpec> AllTypes { get; init; }
}
