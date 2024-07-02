// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record SourceGenerationSpec
    {
        public required InterceptorInfo InterceptorInfo { get; init; }
        public required BindingHelperInfo BindingHelperInfo { get; init; }
        public required ImmutableEquatableArray<TypeSpec> ConfigTypes { get; init; }
        public required bool EmitEnumParseMethod { get; set; }
        public required bool EmitGenericParseEnum { get; set; }
        public required bool EmitThrowIfNullMethod { get; set; }
    }
}
