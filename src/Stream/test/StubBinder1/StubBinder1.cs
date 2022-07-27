// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.StubBinder1;
#pragma warning disable S3881 // "IDisposable" should be implemented correctly: No unmanaged resources here.
public class StubBinder1 : IBinder<object>
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
{
    public const string BINDER_NAME = "binder1";

    public Func<string, string, object, IConsumerOptions, IBinding> BindConsumerFunc { get; set; }

    public Func<string, object, IProducerOptions, IBinding> BindProducerFunc { get; set; }

    public string ServiceName { get; set; } = BINDER_NAME;

    public Type TargetType => typeof(object);

    public IServiceProvider ServiceProvider { get; }

    public IConfiguration Configuration { get; }

    public StubBinder1(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;
    }

    public IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
    {
        if (BindConsumerFunc != null)
        {
            return BindConsumerFunc(name, group, inboundTarget, consumerOptions);
        }

        return null;
    }

    public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
    {
        if (BindProducerFunc != null)
        {
            return BindProducerFunc(name, outboundTarget, producerOptions);
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}