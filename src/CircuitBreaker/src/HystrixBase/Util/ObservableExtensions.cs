// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public static class ObservableExtensions
    {
        public static IObservable<TSource> OnSubscribe<TSource>(this IObservable<TSource> source, Action onSubscribe)
        {
            return Observable.Create<TSource>(o =>
            {
                onSubscribe();
                var d = source.Subscribe(o);
                return d;
            });
        }

        public static IObservable<T> OnDispose<T>(this IObservable<T> source, Action action)
        {
            return source.Finally(action);
        }
    }
    }
