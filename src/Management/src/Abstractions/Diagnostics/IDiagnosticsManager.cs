// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Diagnostics;

public interface IDiagnosticsManager
{
    void Start();

#pragma warning disable CA1716 // Identifiers should not match keywords
    void Stop();
#pragma warning restore CA1716 // Identifiers should not match keywords
}
