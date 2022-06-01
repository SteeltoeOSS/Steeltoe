// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector;
using Steeltoe.Extensions.Configuration;
using System;

namespace External.Connector.Test;

internal class TestServiceInfoCreator : ServiceInfoCreator
{
    internal TestServiceInfoCreator(IConfiguration configuration)
        : base(configuration)
    {
    }

    public static new bool IsRelevant => bool.Parse(Environment.GetEnvironmentVariable("TestServiceInfoCreator") ?? "true");

    public static new TestServiceInfoCreator Instance(IConfiguration config)
    {
        var creator = new TestServiceInfoCreator(config);
        creator.BuildServiceInfoFactories();
        creator.BuildServiceInfos();
        return creator;
    }

    protected override void BuildServiceInfoFactories()
    {
        Factories.Clear();
        Factories.Add(new TestServiceInfoFactory());
    }

    private void BuildServiceInfos()
    {
        var factory = FindFactory(new Service());
        ServiceInfos.Add(factory.Create(null));
    }
}
