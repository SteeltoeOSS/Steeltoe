// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Health.Test;

internal class HealthData
{
    public string StringProperty { get; set; } = "Testdata";

    public int IntProperty { get; set; } = 100;

    public bool BoolProperty { get; set; } = true;
}