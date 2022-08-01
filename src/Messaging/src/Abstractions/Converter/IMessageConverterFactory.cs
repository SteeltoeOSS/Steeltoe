// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Converter;

/// <summary>
/// An implementation provides a factory for obtaining message converters.
/// </summary>
public interface IMessageConverterFactory
{
    /// <summary>
    /// Obtain a message converter for the given MimeType.
    /// </summary>
    /// <param name="mimeType">the MimeType to obtain a converter for.</param>
    /// <returns>a message converter or null if no converter exists.</returns>
    IMessageConverter GetMessageConverterForType(MimeType mimeType);

    /// <summary>
    /// Gets a single composite message converter for all registered converters.
    /// </summary>
    ISmartMessageConverter MessageConverterForAllRegistered { get; }

    /// <summary>
    /// Gets all the message converters provided by this factory.
    /// </summary>
    IList<IMessageConverter> AllRegistered { get; }
}
