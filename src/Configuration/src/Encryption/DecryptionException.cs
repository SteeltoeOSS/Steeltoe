// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption;

public sealed class DecryptionException : Exception
{
    public DecryptionException(string? message)
        : base(message)
    {
    }

    public DecryptionException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
