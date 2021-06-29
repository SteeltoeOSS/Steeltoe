// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter
{
    /// <summary>
    /// A generic converter that may conditionally execute based on the source type and
    /// target types.
    /// </summary>
    public interface IConditionalGenericConverter : IConditionalConverter, IGenericConverter
    {
    }
}
