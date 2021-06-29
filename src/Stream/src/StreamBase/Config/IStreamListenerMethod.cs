﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Config
{
    public interface IStreamListenerMethod
    {
        public MethodInfo Method { get; }

        public StreamListenerAttribute Attribute { get; }

        public Type ImplementationType { get; }
    }
}
