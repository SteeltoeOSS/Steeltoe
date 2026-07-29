// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

internal static class GitDiagnosticReporter
{
    private const string DiagnosticPrefix = "GITPROPS";

    public static void Report(TaskLoggingHelper log, GitDiagnosticId diagnosticId, bool enableWarnings, string message)
    {
        string code = $"{DiagnosticPrefix}{(int)diagnosticId:D3}";

        if (enableWarnings)
        {
            log.LogWarning(null, code, null, null, 0, 0, 0, 0, message);
        }
        else
        {
            log.LogMessage(null, code, null, null, 0, 0, 0, 0, MessageImportance.High, message);
        }
    }
}
