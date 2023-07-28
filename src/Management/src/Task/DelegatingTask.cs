// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Task;

internal sealed class DelegatingTask : IApplicationTask
{
    private readonly Action _run;

    public string Name { get; }

    public DelegatingTask(string name, Action run)
    {
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(run);

        _run = run;
        Name = name;
    }

    public void Run()
    {
        _run();
    }
}
