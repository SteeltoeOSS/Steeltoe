// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Fluent assertions against the captured stdout/stderr of a "dotnet build"/"publish" call, for the two diagnostic-severity checks nearly every
/// warn-by-default test repeats.
/// </summary>
internal static class BuildOutputAssertions
{
    public static void AssertWarned(this string output, string code)
    {
        output.Should().Contain($"warning {code}");
    }

    /// <summary>
    /// GitPropertiesEnableWarnings=false downgrades a diagnostic from a warning to a plain informational message - with no code at all (see
    /// GenerateGitPropertiesCacheTask.ReportDiagnostic's remarks for why).
    /// </summary>
    public static void AssertReportedAsInfoOnly(this string output, string code, string messageSnippet)
    {
        output.Should().NotContain(code);
        output.Should().Contain(messageSnippet);
    }
}
