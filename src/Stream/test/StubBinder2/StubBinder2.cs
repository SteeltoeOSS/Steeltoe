﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.StubBinder2
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class StubBinder2 : IBinder<object>
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public const string BINDER_NAME = "binder2";

        public string ServiceName { get; set; } = BINDER_NAME;

        public Type TargetType => typeof(object);

        public IServiceProvider ServiceProvider { get; }

        public StubBinder2Dependency StubBinder2Dependency { get; }

        public StubBinder2(IServiceProvider serviceProvider, StubBinder2Dependency stubBinder2Dependency)
        {
            ServiceProvider = serviceProvider;
            StubBinder2Dependency = stubBinder2Dependency;
        }

        public IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions) => null;

        public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions) => null;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
