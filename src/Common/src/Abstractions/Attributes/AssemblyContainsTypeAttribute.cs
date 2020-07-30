// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly)]
#pragma warning disable CA1813 // Avoid unsealed attributes
    public class AssemblyContainsTypeAttribute : Attribute
#pragma warning restore CA1813 // Avoid unsealed attributes
    {
        public Type ContainedType { get; private set; }

        public AssemblyContainsTypeAttribute(Type type)
        {
            ContainedType = type;
        }
    }
}
