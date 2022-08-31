// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Tck;

public class GlobalErrorHandlerWithErrorMessageConfiguration
{
    public bool GlobalErrorInvoked { get; set; }

    [StreamListener(ISink.InputName)]
    public void Input(string value)
    {
        throw new Exception("test exception");
    }

    [StreamListener("errorChannel")]
    public void GeneralError(IMessage message)
    {
        GlobalErrorInvoked = true;
    }
}
