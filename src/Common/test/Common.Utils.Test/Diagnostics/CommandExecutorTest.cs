// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.Diagnostics;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Steeltoe.Common.Utils.Test.Diagnostics
{
    public class CommandExecutorTest
    {
        [Fact]
        public async void SuccessfulCommandShouldReturn0()
        {
            // Arrange
            var executor = new CommandExecutor();

            // Act
            var result = await executor.ExecuteAsync("dotnet --help");

            // Assert
            result.ExitCode.Should().Be(0);
            result.Output.Should().Contain("Usage: dotnet");
        }

        [Fact]
        public async void UnsuccessfulCommandShouldNotReturn0()
        {
            // Arrange
            var executor = new CommandExecutor();

            // Act
            var result = await executor.ExecuteAsync("dotnet --no-such-option");

            // Assert
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
        public async void UnknownCommandShouldThrowException()
        {
            // Arrange
            var executor = new CommandExecutor();

            // Act
            Func<Task> act = async () => { await executor.ExecuteAsync("no-such-command"); };

            // Assert
            await act.Should().ThrowAsync<CommandException>().WithMessage("'no-such-command' failed to start*");
        }
    }
}