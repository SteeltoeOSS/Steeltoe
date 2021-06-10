// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Diagnostics.Test
{
    public class CommandTest
    {
        [Fact]
        public async void SuccessfulCommandShouldReturn0()
        {
            var result = await Command.ExecuteAsync("dotnet --help");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Usage: dotnet", result.Output);
        }

        [Fact]
        public async void UnsuccessfulCommandShouldNotReturn0()
        {
            var result = await Command.ExecuteAsync("dotnet --no-such-option");
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Unknown option: --no-such-option", result.Error);
        }

        [Fact]
        public async void UnknownCommandShouldThrowException()
        {
            Task Act() => Command.ExecuteAsync("no-such-command");
            var exc = await Assert.ThrowsAsync<CommandException>(Act);
            Assert.Contains("'no-such-command' failed to start", exc.Message);
            Assert.NotNull(exc.InnerException);
        }
    }
}