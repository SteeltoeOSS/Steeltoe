// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

public abstract class AbstractPollableSource<THandler> : IPollableSource<THandler>
{
    public abstract bool Poll(THandler handler);

    public virtual bool Poll(THandler handler, Type type)
    {
        return Poll(handler);
    }

    public bool Poll(object handler)
    {
        return Poll((THandler)handler);
    }

    public bool Poll(object handler, Type type)
    {
        return Poll((THandler)handler, type);
    }
}
