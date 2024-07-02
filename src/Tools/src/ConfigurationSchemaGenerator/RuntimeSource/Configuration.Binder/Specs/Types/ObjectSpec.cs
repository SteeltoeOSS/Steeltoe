// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record ObjectSpec : ComplexTypeSpec
    {
        public ObjectSpec(
            INamedTypeSymbol type,
            ObjectInstantiationStrategy instantiationStrategy,
            ImmutableEquatableArray<PropertySpec>? properties,
            ImmutableEquatableArray<ParameterSpec>? constructorParameters,
            string? initExceptionMessage) : base(type)
        {
            InstantiationStrategy = instantiationStrategy;
            Properties = properties;
            ConstructorParameters = constructorParameters;
            InitExceptionMessage = initExceptionMessage;
        }

        public ObjectInstantiationStrategy InstantiationStrategy { get; }

        public ImmutableEquatableArray<PropertySpec>? Properties { get; }

        public ImmutableEquatableArray<ParameterSpec>? ConstructorParameters { get; }

        public string? InitExceptionMessage { get; }
    }

    public enum ObjectInstantiationStrategy
    {
        None = 0,
        ParameterlessConstructor = 1,
        ParameterizedConstructor = 2,
    }
}
