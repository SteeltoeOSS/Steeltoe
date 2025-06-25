// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

public sealed class Sample
{
    public string Covered(bool condition)
    {
        if (condition)
        {
            return "yes";
        }

        return "no";
    }

    public string NotCovered(bool condition)
    {
        if (condition)
        {
            return "yes-uncovered";
        }

        return "no-uncovered";
    }
}
