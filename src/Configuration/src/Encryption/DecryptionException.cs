// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption;

public sealed class DecryptionException : Exception
{
    internal DecryptionException(string message)
        : base(message)
    {
    }

    internal DecryptionException(string message, Exception exception)
        : base(message, exception)
    {
    }
}
