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
    public async Task SuccessfulCommandShouldReturn0()
    {
        var executor = new CommandExecutor();

        var result = await executor.ExecuteAsync("dotnet --help");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Usage: dotnet");
    }

    [Fact]
    public async Task UnsuccessfulCommandShouldNotReturn0()
    {
        var executor = new CommandExecutor();

        var result = await executor.ExecuteAsync("dotnet --no-such-option");

        result.ExitCode.Should().NotBe(0);
        try
        {
            result.Error.Should().Contain("Unknown option: --no-such-option");
        }
        catch (XunitException)
        {
            // message changes if .NET 6 sdk is installed
            result.Error.Should().Contain("--no-such-option does not exist");
        }
    }

    [Fact]
    public async Task UnknownCommandShouldThrowException()
    {
        var executor = new CommandExecutor();

        Func<Task> act = async () => { await executor.ExecuteAsync("no-such-command"); };

        await act.Should().ThrowAsync<CommandException>().WithMessage("'no-such-command' failed to start*");
    }
}
