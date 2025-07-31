// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IRabbitListenerEndpointRegistry : ISmartLifecycle, IDisposable, IServiceNameAware
{
    public IMessageListenerContainer GetListenerContainer(string id);

    public ISet<string> GetListenerContainerIds();

    public ICollection<IMessageListenerContainer> GetListenerContainers();

    public void RegisterListenerContainer(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory);

    public void RegisterListenerContainer(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory, bool startImmediately);

    public IMessageListenerContainer UnregisterListenerContainer(string id);
}