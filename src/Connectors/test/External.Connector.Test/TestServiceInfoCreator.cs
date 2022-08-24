// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;

namespace External.Connector.Test;

internal sealed class TestServiceInfoCreator : ServiceInfoCreator
{
    public new static bool IsRelevant => bool.Parse(Environment.GetEnvironmentVariable("TestServiceInfoCreator") ?? "true");

    internal TestServiceInfoCreator(IConfiguration configuration)
        : base(configuration)
    {
    }

    public new static TestServiceInfoCreator Instance(IConfiguration configuration)
    {
        var creator = new TestServiceInfoCreator(configuration);
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
        IServiceInfoFactory factory = FindFactory(new Service());
        ServiceInfos.Add(factory.Create(null));
    }
}
