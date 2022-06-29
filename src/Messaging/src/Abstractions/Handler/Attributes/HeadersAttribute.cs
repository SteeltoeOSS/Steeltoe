// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Handler.Attributes;

/// <summary>
///  Attribute which indicates that a method parameter should be bound to the headers of a message.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class HeadersAttribute : Attribute
{
}
