// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;

namespace Steeltoe.Management.TaskCore;

public class DelegatingTask : IApplicationTask
{
    private readonly Action _run;

    public DelegatingTask(string name, Action run)
    {
        _run = run;
        Name = name;
    }

    public string Name { get; }

    public void Run() => _run();
}