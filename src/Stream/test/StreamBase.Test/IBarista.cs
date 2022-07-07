// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream;

public interface IBarista
{
    [Input]
    ISubscribableChannel Orders();

    [Output]
    IMessageChannel HotDrinks();

    [Output]
    IMessageChannel ColdDrinks();
}
