// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public interface IAttributeAccessor
{
    void SetAttribute(string name, object value);

    object GetAttribute(string name);

    object RemoveAttribute(string name);

    bool HasAttribute(string name);

    string[] AttributeNames { get; }
}
