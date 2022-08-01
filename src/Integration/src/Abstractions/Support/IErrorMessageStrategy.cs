// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Support;

/// <summary>
/// A strategy to build an ErrorMessage based on the provided Exception AttributeAccessor as a context.
/// </summary>
public interface IErrorMessageStrategy
{
    /// <summary>
    /// Build the error message.
    /// </summary>
    /// <param name="exception">the payload of the error message.</param>
    /// <param name="attributeAccessor">the context to use.</param>
    /// <returns>the error message.</returns>
    ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor attributeAccessor);
}
