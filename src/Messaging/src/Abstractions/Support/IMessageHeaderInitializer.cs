// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Support;

/// <summary>
/// TODO:  Try to make this internal.
/// </summary>
public interface IMessageHeaderInitializer
{
    void InitHeaders(IMessageHeaderAccessor headerAccessor);
}
