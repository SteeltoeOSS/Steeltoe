// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

public abstract class AbstractMessageHandler : IMessageHandler, IOrdered
{
    private IIntegrationServices _integrationServices;

    public IIntegrationServices IntegrationServices
    {
        get
        {
            _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
            return _integrationServices;
        }
    }

    public IApplicationContext ApplicationContext { get; }

    public virtual string ComponentType => "message-handler";

    public virtual string ServiceName { get; set; }

    public virtual string ComponentName { get; set; }

    public int Order => AbstractOrdered.LowestPrecedence - 1;

    protected AbstractMessageHandler(IApplicationContext context)
    {
        ApplicationContext = context;
        ServiceName = GetType().FullName;
    }

    public virtual void HandleMessage(IMessage message)
    {
        ArgumentGuard.NotNull(message);

        if (message.Payload == null)
        {
            throw new ArgumentException("Message payload must not be null.", nameof(message));
        }

        try
        {
            HandleMessageInternal(message);
        }
        catch (Exception e)
        {
            Exception wrapped = IntegrationUtils.WrapInHandlingExceptionIfNecessary(message, $"error occurred in message handler [{this}]", e);

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
