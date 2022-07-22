// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Integration;

/// <summary>
/// TODO: See if this can be internal
/// </summary>
/// <typeparam name="T">input tye</typeparam>
public interface ISelector<in T>
{
    bool Accept(T source);
}