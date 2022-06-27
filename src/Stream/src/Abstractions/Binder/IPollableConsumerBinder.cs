// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder;

/// <summary>
/// A binder that supports pollable message sources.
/// </summary>
public interface IPollableConsumerBinder
{
}

/// <summary>
///  A binder that supports pollable message sources.
/// </summary>
/// <typeparam name="H">the polled consumer handler type.</typeparam>
public interface IPollableConsumerBinder<H> : IBinder<IPollableSource<H>>, IPollableConsumerBinder
{
}
