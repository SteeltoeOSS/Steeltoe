// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class ApplicationInstanceCertificateTest
{
    [InlineData("This subject does not match a supported pattern", false)]
    [InlineData("CN=34c9765d-b5da-49a7-4a53-1c58, OU=app:e5950275-afce-480a-a017-babafd3d5798 + OU=space:ab60aac2-fb64-43ab-ba24-c57a15a7e114 + OU=organization:7fe4d027-2058-4539-a40c-702ac1373905", true, "7fe4d027-2058-4539-a40c-702ac1373905", "ab60aac2-fb64-43ab-ba24-c57a15a7e114")]
    [InlineData("CN=69ed1418-8434-4b14-9ad9-636d71ba782a, OU=app:e268b8bf-cdad-4014-a18f-1131c73d9450 + OU=space:122b942a-d7b9-4839-b26e-836654b9785f + OU=organization:a8fef16f-94c0-49e3-aa0b-ced7c3da6229", true, "a8fef16f-94c0-49e3-aa0b-ced7c3da6229", "122b942a-d7b9-4839-b26e-836654b9785f")]
    [InlineData("CN=\"69ed1418\", OU=app:e268b8bf + OU=space:\"122b942a\" + OU=organization:a8fef16f", true, "a8fef16f", "122b942a")]
    [Theory]
    public void SubjectParsingProducesExpectedResults(string subject, bool shouldParse, string? expectedOrgId = null, string? expectedSpaceId = null)
    {
        ApplicationInstanceCertificate.TryParse(subject, out ApplicationInstanceCertificate? instanceCertificate).Should().Be(shouldParse);

        if (shouldParse)
        {
            instanceCertificate.Should().NotBeNull();
            instanceCertificate?.OrganizationId.Should().Be(expectedOrgId);
            instanceCertificate?.SpaceId.Should().Be(expectedSpaceId);
        }
        else
        {
            instanceCertificate.Should().BeNull();
        }
    }
}
