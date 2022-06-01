// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Logging;

/// <summary>
/// Provides the ability to process each log message before it is sent to the Console
/// </summary>
public interface IDynamicMessageProcessor
{
    /// <summary>
    /// Called for each log message just after the parameters have been formatted into the log string
    /// but before the time stamp, category, and level have been applied.
    /// </summary>
    /// <param name="inputLogMessage">The incoming log message</param>
    /// <returns>The updated log message</returns>
    string Process(string inputLogMessage);
}
