// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using FluentAssertions.Primitives;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class DotNetCommandOutputAssertions(DotNetCommandOutput subject)
    : ReferenceTypeAssertions<DotNetCommandOutput, DotNetCommandOutputAssertions>(subject)
{
    private const string DiagnosticPrefix = "GITPROPS";

    protected override string Identifier => nameof(DotNetCommandOutput);

    [CustomAssertion]
    public void ContainOnlyGitWarning(GitDiagnostic diagnostic)
    {
        AssertOnlyContainsDiagnosticLine("warning", diagnostic);
    }

    [CustomAssertion]
    public void NotContainAnyGitWarnings()
    {
        Subject.Value.Should().NotContain($"warning {DiagnosticPrefix}");
    }

    [CustomAssertion]
    public void ContainOnlyGitMessage(GitDiagnostic diagnostic)
    {
        AssertOnlyContainsDiagnosticLine("message", diagnostic);
    }

    [CustomAssertion]
    private void AssertOnlyContainsDiagnosticLine(string kind, GitDiagnostic diagnostic)
    {
        string code = FormatCode(diagnostic);
        string marker = $"{kind} {code}";
        Subject.Value.Should().Contain(marker);

        if (diagnostic.MessageSnippet != null)
        {
            string[] lines = Subject.Value.Split('\n');

            lines.Should().Contain(line =>
                line.Contains(marker, StringComparison.Ordinal) && line.Contains(diagnostic.MessageSnippet, StringComparison.Ordinal));
        }

        string otherCodesPattern = $@"{Regex.Escape(kind)} (?!{Regex.Escape(code)}){DiagnosticPrefix}\d{{3}}";
        Subject.Value.Should().NotMatchRegex(otherCodesPattern, "no other {0} diagnostics are expected alongside {1}", kind, code);
    }

    private static string FormatCode(GitDiagnostic diagnostic)
    {
        return $"{DiagnosticPrefix}{diagnostic.Code:D3}";
    }
}
