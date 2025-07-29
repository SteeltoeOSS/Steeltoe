// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.Diagnostics;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Steeltoe.Common.Utils.Test.Diagnostics;

public class CommandExecutorTest
{
    [Fact]
    public async void SuccessfulCommandShouldReturn0()
    {
        var executor = new CommandExecutor();

        var result = await executor.ExecuteAsync("dotnet --help");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Usage: dotnet");
    }

    [Fact]
    public async void UnsuccessfulCommandShouldNotReturn0()
    {
        var executor = new CommandExecutor();

        var result = await executor.ExecuteAsync("dotnet --no-such-option");

        result.ExitCode.Should().NotBe(0);

        // The message depends on the version of the .NET SDK installed.
        result.Error.Should().ContainAny(
            "Unknown option: --no-such-option",
            "--no-such-option does not exist",
            "Could not execute because the specified command or file was not found.");
    }

    [Fact]
    public async void UnknownCommandShouldThrowException()
    {
        var executor = new CommandExecutor();

        Func<Task> act = async () => { await executor.ExecuteAsync("no-such-command"); };

        await act.Should().ThrowAsync<CommandException>().WithMessage("'no-such-command' failed to start*");
    }
}