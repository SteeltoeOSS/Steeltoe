// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Integration.Config;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IMethodAttributeProcessor<A>
    where A : Attribute
{
    object PostProcess(object service, string serviceName, MethodInfo method, List<Attribute> attributes);

    bool ShouldCreateEndpoint(MethodInfo method, List<Attribute> attributes);
}