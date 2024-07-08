// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using System.Globalization;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record ParameterSpec : MemberSpec
    {
        public ParameterSpec(IParameterSymbol parameter, TypeRef typeRef) : base(parameter, typeRef)
        {
            RefKind = parameter.RefKind;

            if (parameter.HasExplicitDefaultValue)
            {
                DefaultValueExpr = CSharpSyntaxUtilities.FormatLiteral(parameter.ExplicitDefaultValue, TypeRef);
            }
            else
            {
                ErrorOnFailedBinding = true;
            }
        }

        public bool ErrorOnFailedBinding { get; private set; }

        public RefKind RefKind { get; }

        public override bool CanGet => false;

        public override bool CanSet => true;
    }
}
