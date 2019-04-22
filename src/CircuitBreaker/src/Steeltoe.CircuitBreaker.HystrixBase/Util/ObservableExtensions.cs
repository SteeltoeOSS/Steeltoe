// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
