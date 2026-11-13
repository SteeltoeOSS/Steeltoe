// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using Steeltoe.Common.CasingConventions;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

/// <summary>
/// Formats a thread dump as plain text, similar to the output of the thread dump download in Apps Manager.
/// </summary>
internal static class ThreadDumpPlainTextFormatter
{
    public static string Format(IList<ThreadInfo> threads)
    {
        ArgumentNullException.ThrowIfNull(threads);

        var builder = new StringBuilder();

        foreach (ThreadInfo thread in threads)
        {
            AppendThread(builder, thread);
        }

        return builder.ToString();
    }

    private static void AppendThread(StringBuilder builder, ThreadInfo thread)
    {
        builder.Append(CultureInfo.InvariantCulture, $"\"{thread.ThreadName}\" #{thread.ThreadId}\n");
        builder.Append(CultureInfo.InvariantCulture, $"   Thread State: {thread.ThreadState.ToSnakeCaseString(SnakeCaseStyle.AllCaps)}\n");

        foreach (StackTraceElement stackFrame in thread.StackTrace)
        {
            builder.Append("  at ");
            AppendStackFrame(builder, stackFrame);
            builder.Append('\n');
        }

        builder.Append('\n');

        // Locked synchronizers are not available in .NET.
        builder.Append("  Locked ownable synchronizers:\n");
        builder.Append("  - None\n");
        builder.Append('\n');
    }

    private static void AppendStackFrame(StringBuilder builder, StackTraceElement stackFrame)
    {
        if (!string.IsNullOrEmpty(stackFrame.ClassName))
        {
            builder.Append(stackFrame.ClassName);
            builder.Append('.');
        }

        builder.Append(stackFrame.MethodName);
        builder.Append('(');

        if (string.IsNullOrEmpty(stackFrame.FileName))
        {
            builder.Append("Unknown Source");
        }
        else
        {
            builder.Append(stackFrame.FileName);

            if (stackFrame.LineNumber != null)
            {
                builder.Append(CultureInfo.InvariantCulture, $":{stackFrame.LineNumber}");
            }
        }

        builder.Append(')');
    }
}
