// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Services;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public interface IMessageListenerContainer : ISmartLifecycle, IDisposable, IServiceNameAware
{
    /// <summary>
    /// Setup the message listener to use.
    /// </summary>
    /// <param name="messageListener">the message listener</param>
    void SetupMessageListener(IMessageListener messageListener);

    /// <summary>
    /// Do not check for missing or mismatched queues during startup. Used for lazily
    /// loaded message listener containers to avoid a deadlock when starting such
    /// containers. Applications lazily loading containers should verify the queue
    /// configuration before loading the container bean.
    /// </summary>
    void LazyLoad();

    void Initialize();
}
