// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class Declarables : IServiceNameAware
{
    public Declarables(string name, params IDeclarable[] declarables)
    {
        if (declarables == null)
        {
            throw new ArgumentNullException(nameof(declarables));
        }

        DeclarableList = new List<IDeclarable>(declarables);
        ServiceName = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Declarables(string name, List<IDeclarable> declarables)
    {
        if (declarables == null)
        {
            throw new ArgumentNullException(nameof(declarables));
        }

        DeclarableList = new List<IDeclarable>(declarables);
        ServiceName = name ?? throw new ArgumentNullException(nameof(name));
    }

    public List<IDeclarable> DeclarableList { get; }

    public string ServiceName { get; set; }

    public IEnumerable<T> GetDeclarablesByType<T>()
    {
        return DeclarableList.OfType<T>();
    }
}
