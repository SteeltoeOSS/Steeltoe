// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Steeltoe.Common.Json;

/// <summary>
/// Indicates that a collection property should not be serialized if the collection is empty. Use
/// <see cref="JsonSerializerOptionsExtensions.AddJsonIgnoreEmptyCollection" /> to register in <see cref="JsonSerializerOptions" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonIgnoreEmptyCollectionAttribute : Attribute;
