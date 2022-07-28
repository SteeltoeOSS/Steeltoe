// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Steeltoe.Common.Diagnostics;

public interface IDiagnosticObserver : IObserver<KeyValuePair<string, object>>, IDisposable
{
    string ObserverName { get; }

    string ListenerName { get; }

    void Subscribe(DiagnosticListener listener);

    void ProcessEvent(string eventName, object value);
}
