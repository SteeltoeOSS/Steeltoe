// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Services;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class Declarables : IServiceNameAware
{
    public ICollection<IDeclarable> DeclarableList { get; }

    public string ServiceName { get; set; }

    public Declarables(string name, params IDeclarable[] declarables)
    {
        ArgumentGuard.NotNull(declarables);
        ArgumentGuard.NotNull(name);

        DeclarableList = new List<IDeclarable>(declarables);
        ServiceName = name;
    }

    public Declarables(string name, ICollection<IDeclarable> declarables)
    {
        ArgumentGuard.NotNull(declarables);
        ArgumentGuard.NotNull(name);

        DeclarableList = new List<IDeclarable>(declarables);
        ServiceName = name;
    }

    public IEnumerable<T> GetDeclarablesByType<T>()
    {
        return DeclarableList.OfType<T>();
    }
}
