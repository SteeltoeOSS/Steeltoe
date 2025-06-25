// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Logging;

/// <summary>
/// Provides the ability to decorate log messages before they are sent downstream.
/// </summary>
public interface IDynamicMessageProcessor
{
    /// <summary>
    /// Replaces the contents of an incoming log message.
    /// </summary>
    /// <param name="message">
    /// The incoming message text, just after formatting its parameters, but before the time stamp, category, level, exception and scopes are added.
    /// </param>
    /// <returns>
    /// The decorated log message text.
    /// </returns>
    string Process(string message);
}
