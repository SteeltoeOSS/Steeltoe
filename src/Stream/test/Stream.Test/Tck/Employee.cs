// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Test.Tck;

public sealed class Employee<TPerson>
{
    public int Id { get; set; }

    public TPerson Person { get; set; }
}
