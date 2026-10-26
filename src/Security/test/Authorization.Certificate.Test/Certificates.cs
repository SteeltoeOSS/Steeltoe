// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Security.Authorization.Certificate.Test;

internal static class Certificates
{
    internal static Guid ServerOrgId { get; } = new("a8fef16f-94c0-49e3-aa0b-ced7c3da6229");
    internal static Guid ServerSpaceId { get; } = new("122b942a-d7b9-4839-b26e-836654b9785f");

    public static X509Certificate2 FromDiego { get; } = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
}
