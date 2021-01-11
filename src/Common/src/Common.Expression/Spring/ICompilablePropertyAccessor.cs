// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public interface ICompilablePropertyAccessor : IPropertyAccessor
    {
        bool IsCompilable();

        Type GetPropertyType();

        // TODO: Add this when we support code generation
        // void GenerateCode(string propertyName, MethodVisitor mv, CodeFlow cf);
    }
}
