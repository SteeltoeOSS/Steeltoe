// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal static class DictionaryExtensions
{
    internal static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> to, IDictionary<TKey, TValue> from) =>
        from?.ToList().ForEach(item => to.Add(item));
}