// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Integration.Handler;

/// <summary>
/// MessageHandlers implementing this interface can propagate headers from
/// an input message to an output message.
/// </summary>
public interface IHeaderPropagation
{
    /// <summary>
    /// Gets or sets the headers that should not be copied from inbound message if
    /// handler is configured to copy headers
    /// </summary>
    IList<string> NotPropagatedHeaders { get; set; }

    /// <summary>
    /// Add headers that will not be copied from the inbound message if
    /// handler is configured to copy headers
    /// </summary>
    /// <param name="headers">the headers to not copy</param>
    void AddNotPropagatedHeaders(params string[] headers);
}
