// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler;

public abstract class AbstractMessageHandler : IMessageHandler, IOrdered
{
    private IIntegrationServices _integrationServices;

    protected AbstractMessageHandler(IApplicationContext context)
    {
        ApplicationContext = context;
        ServiceName = GetType().FullName;
    }

    public IIntegrationServices IntegrationServices
    {
        get
        {
            if (_integrationServices == null)
            {
                _integrationServices = IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
            }

            return _integrationServices;
        }
    }

    public IApplicationContext ApplicationContext { get; }

    public virtual string ComponentType
    {
        get { return "message-handler"; }
    }

    public virtual string ServiceName { get; set; }

    public virtual string ComponentName { get; set; }

    public int Order => AbstractOrdered.LOWEST_PRECEDENCE - 1;

    public virtual void HandleMessage(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Payload == null)
        {
            throw new ArgumentNullException("Message payload is null");
        }

        try
        {
            HandleMessageInternal(message);
        }
        catch (Exception e)
        {
            var wrapped = IntegrationUtils.WrapInHandlingExceptionIfNecessary(message, "error occurred in message handler [" + this + "]", e);
            if (wrapped != e)
            {
                throw wrapped;
            }

            throw;
        }
    }

    public abstract void Initialize();

    protected abstract void HandleMessageInternal(IMessage message);
}