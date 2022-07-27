// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

/// <summary>
/// For registering converters with a type conversion system.
/// </summary>
public interface IConverterRegistry
{
    /// <summary>
    /// Adds a generic converter to this registry
    /// </summary>
    /// <param name="converter">the converter to add</param>
    void AddConverter(IGenericConverter converter);
}