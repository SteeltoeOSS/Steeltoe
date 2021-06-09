// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public interface ICompilablePropertyAccessor : IPropertyAccessor
    {
        bool IsCompilable();

        Type GetPropertyType();

        void GenerateCode(string propertyName, ILGenerator gen, CodeFlow cf);
    }
}
