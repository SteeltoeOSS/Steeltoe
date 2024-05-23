// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Attributes;

namespace Steeltoe.Connectors;

/// <summary>
/// Identify assemblies containing ServiceInfoCreators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ServiceInfoCreatorAssemblyAttribute : AssemblyContainsTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInfoCreatorAssemblyAttribute" /> class. Used to override the default ServiceInfoCreator.
    /// </summary>
    /// <param name="creatorType">
    /// The type of your info creator that inherits from Steeltoe.Connectors.ServiceInfoCreator.
    /// </param>
    public ServiceInfoCreatorAssemblyAttribute(Type creatorType)
        : base(creatorType)
    {
    }
}
