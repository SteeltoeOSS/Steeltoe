// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring;

public class InternalParseException : Exception
{
    public SpelParseException Cause => (SpelParseException)InnerException;

    public InternalParseException(SpelParseException innerException)
        : base("Internal Parse Error", innerException)
    {
    }
}
