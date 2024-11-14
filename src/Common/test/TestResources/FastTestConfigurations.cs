// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

[Flags]
public enum FastTestConfigurations
{
    ConfigServer = 1,
    Discovery = 2,
    Connectors = 4,
    All = ConfigServer | Discovery | Connectors
}
