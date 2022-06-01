// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Config;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class DeclarationExceptionEvent : RabbitAdminEvent
{
    public DeclarationExceptionEvent(object source, IDeclarable declarable, Exception exception)
        : base(source)
    {
        Declarable = declarable;
        Exception = exception;
    }

    public IDeclarable Declarable { get; }

    public Exception Exception { get; }

    public override string ToString()
    {
        return $"DeclarationExceptionEvent [declarable={Declarable}, throwable={Exception}, source={Source}]";
    }
}
