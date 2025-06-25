// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace SourceGenerators
{
    /// <summary>
    /// An equatable value representing type identity.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public sealed class TypeRef : IEquatable<TypeRef>
    {
        public TypeRef(ITypeSymbol type)
        {
            Name = type.Name;
            FullyQualifiedName = type.GetFullyQualifiedName();
            IsValueType = type.IsValueType;
            TypeKind = type.TypeKind;
            SpecialType = type.SpecialType;
        }

        public string Name { get; }

        /// <summary>
        /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
        /// </summary>
        public string FullyQualifiedName { get; }

        public bool IsValueType { get; }
        public TypeKind TypeKind { get; }
        public SpecialType SpecialType { get; }

        public bool CanBeNull => !IsValueType || SpecialType is SpecialType.System_Nullable_T;

        public bool Equals(TypeRef? other) => other != null && FullyQualifiedName == other.FullyQualifiedName;
        public override bool Equals(object? obj) => Equals(obj as TypeRef);
        public override int GetHashCode() => FullyQualifiedName.GetHashCode();
    }
}
