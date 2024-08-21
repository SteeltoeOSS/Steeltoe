// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Wraps a nullable value type, so it can be declared as volatile (prevents stale reads).
/// </summary>
internal sealed class NullableValueWrapper<T>(T? value)
    where T : struct
{
    public T? Value { get; } = value;

    public override string ToString()
    {
        return $"NullableValueWrapper: {Value}";
    }
}
