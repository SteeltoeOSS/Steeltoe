// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Integration.Configuration;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IMethodAttributeProcessor<TAttribute>
#pragma warning restore S2326 // Unused type parameters should be removed
    where TAttribute : Attribute
{
    object PostProcess(object service, string serviceName, MethodInfo method, List<Attribute> attributes);

    bool ShouldCreateEndpoint(MethodInfo method, List<Attribute> attributes);
}
