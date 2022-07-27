// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Binder;

public abstract class AbstractPollableSource<H> : IPollableSource<H>
{
    public abstract bool Poll(H handler);

    public virtual bool Poll(H handler, Type type)
    {
        return Poll(handler);
    }

    public bool Poll(object handler)
    {
        return Poll((H)handler);
    }

    public bool Poll(object handler, Type type)
    {
        return Poll((H)handler, type);
    }
}