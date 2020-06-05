// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public interface IExchange : IDeclarable, IServiceNameAware
    {
        string Type { get; }

        bool IsDurable { get; }

        bool IsAutoDelete { get; }

        Dictionary<string, object> Arguments { get; }

        bool IsDelayed { get; }

        bool IsInternal { get; }
    }
}
