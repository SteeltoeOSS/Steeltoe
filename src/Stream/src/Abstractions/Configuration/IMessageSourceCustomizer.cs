// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Configuration;

/// <summary>
/// TODO: Look at making internal.
/// </summary>
public interface IMessageSourceCustomizer
{
    void Configure(object source, string destinationName, string group);
}
