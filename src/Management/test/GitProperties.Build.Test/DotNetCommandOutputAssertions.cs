// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Primitives;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class DotNetCommandOutputAssertions(DotNetCommandOutput subject)
    : ReferenceTypeAssertions<DotNetCommandOutput, DotNetCommandOutputAssertions>(subject)
{
    private const string DiagnosticPrefix = "GITPROPS";
    protected override string Identifier => nameof(DotNetCommandOutput);

    [CustomAssertion]
    public void ContainGitWarning(GitDiagnosticId diagnosticId)
    {
        string code = FormatCode(diagnosticId);
        Subject.Value.Should().Contain($"warning {code}");
    }

    [CustomAssertion]
    public void NotContainGitWarning(GitDiagnosticId diagnosticId)
    {
        string code = FormatCode(diagnosticId);
        Subject.Value.Should().NotContain($"warning {code}");
    }

    [CustomAssertion]
    public void NotContainAnyGitWarnings()
    {
        Subject.Value.Should().NotContain(DiagnosticPrefix);
    }

    [CustomAssertion]
    public void ContainGitInfo(GitDiagnosticId diagnosticId, string messageSnippet)
    {
        string code = FormatCode(diagnosticId);
        Subject.Value.Should().NotContain(code);
        Subject.Value.Should().Contain(messageSnippet);
    }

    private static string FormatCode(GitDiagnosticId diagnosticId)
    {
        return $"{DiagnosticPrefix}{diagnosticId.Value:D3}";
    }
}
