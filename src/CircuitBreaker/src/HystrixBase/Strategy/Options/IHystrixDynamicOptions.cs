// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

public interface IHystrixDynamicOptions
{
    string GetString(string name, string fallback);

    int GetInteger(string name, int fallback);

    long GetLong(string name, long fallback);

    bool GetBoolean(string name, bool fallback);
}
