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
    public void ContainGitWarning(GitDiagnosticId diagnosticId, string? messageSnippet = null)
    {
        string code = FormatCode(diagnosticId);
        AssertContainsDiagnosticLine("warning", code, messageSnippet);
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
        Subject.Value.Should().NotContain($"warning {DiagnosticPrefix}");
    }

    [CustomAssertion]
    public void ContainGitMessage(GitDiagnosticId diagnosticId, string? messageSnippet = null)
    {
        string code = FormatCode(diagnosticId);
        AssertContainsDiagnosticLine("message", code, messageSnippet);
    }

    [CustomAssertion]
    private void AssertContainsDiagnosticLine(string kind, string code, string? messageSnippet)
    {
        string marker = $"{kind} {code}";
        Subject.Value.Should().Contain(marker);

        if (messageSnippet != null)
        {
            string[] lines = Subject.Value.Split('\n');
            lines.Should().Contain(line => line.Contains(marker, StringComparison.Ordinal) && line.Contains(messageSnippet, StringComparison.Ordinal));
        }
    }

    private static string FormatCode(GitDiagnosticId diagnosticId)
    {
        return $"{DiagnosticPrefix}{diagnosticId.Value:D3}";
    }
}
