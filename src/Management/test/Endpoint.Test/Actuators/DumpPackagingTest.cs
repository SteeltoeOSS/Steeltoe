// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Steeltoe.Management.Endpoint.Test.Actuators;

/// <summary>
/// Used to verify whether the version of Microsoft.Diagnostics.FastSerialization embedded in dotnet-gcdump is binary compatible with the version
/// referenced by Microsoft.Diagnostics.Tracing.TraceEvent. Unskip and run this manually after version bumps (takes several minutes).
/// </summary>
[Collection("TestsForMemoryDumpsMustRunSequentially")]
[Trait("Category", "MemoryDumps")]
public sealed class DumpPackagingTest(DumpPackagingTest.PackSteeltoeLibrariesOnceFixture fixture)
    : IClassFixture<DumpPackagingTest.PackSteeltoeLibrariesOnceFixture>
{
    private const string EndpointProjectName = "Steeltoe.Management.Endpoint";

    // Tip: Set SkipReason to null to unskip all tests.
    private const string? SkipReason =
        "Slow: packs NuGet packages and builds/runs a console app per TFM. Unskip and run locally after changing a dump-related package version.";

    // To reproduce test failures, here's a combination that compiles fine, while crashing at runtime due to binary breaking changes:
    // - dotnet-gcdump v9.0.621003 (embeds Microsoft.Diagnostics.FastSerialization v3.1.16.0)
    // - Microsoft.Diagnostics.Tracing.TraceEvent v3.2.6 (embeds Microsoft.Diagnostics.FastSerialization v3.2.6.0)

    [Theory(Skip = SkipReason)]
    [MemberData(nameof(TestTargetFrameworks))]
    public async Task Can_take_gcdump_from_packaged_Steeltoe_library(string targetFramework)
    {
        Assert.SkipWhen(!IsHighestTestHostFramework(), "Running this test only on the latest .NET SDK is sufficient.");

        var app = await DumpVerificationApp.CreateForGCDumpAsync(fixture, targetFramework);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = app.RunAsync;

        await action.Should().NotThrowAsync();
    }

    [Theory(Skip = SkipReason)]
    [MemberData(nameof(TestTargetFrameworks))]
    public async Task Can_take_minidump_from_packaged_Steeltoe_library(string targetFramework)
    {
        Assert.SkipWhen(!IsHighestTestHostFramework(), "Running this test only on the latest .NET SDK is sufficient.");

        var app = await DumpVerificationApp.CreateForMinidumpAsync(fixture, targetFramework);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = app.RunAsync;

        await action.Should().NotThrowAsync();
    }

    [Theory(Skip = SkipReason)]
    [MemberData(nameof(TestTargetFrameworks))]
    public async Task Can_take_thread_dump_from_packaged_Steeltoe_library(string targetFramework)
    {
        Assert.SkipWhen(!IsHighestTestHostFramework(), "Running this test only on the latest .NET SDK is sufficient.");

        var app = await DumpVerificationApp.CreateForThreadDumpAsync(fixture, targetFramework);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = app.RunAsync;

        await action.Should().NotThrowAsync();
    }

    public static TheoryData<string> TestTargetFrameworks()
    {
        var theoryData = new TheoryData<string>();

        foreach (string targetFramework in ResolveTestTargetFrameworks())
        {
            theoryData.Add(targetFramework);
        }

        return theoryData;
    }

    private static string[] ResolveTestTargetFrameworks()
    {
        AssemblyMetadataAttribute? attribute = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(candidate => candidate.Key == "TestTargetFrameworks");

        if (attribute?.Value == null)
        {
            throw new InvalidOperationException("Could not resolve TestTargetFrameworks from AssemblyMetadata in test project file.");
        }

        string[] targetFrameworks = attribute.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (targetFrameworks.Length == 0)
        {
            throw new InvalidOperationException("TestTargetFrameworks from AssemblyMetadata in test project file is empty.");
        }

        return targetFrameworks;
    }

    private static bool IsHighestTestHostFramework()
    {
        // The InlineData entries already cover every supported TFM, so running tests on all target frameworks would just repeat identical work.

        Version hostVersion = typeof(object).Assembly.GetName().Version!;
        int highestMajorVersion = ResolveTestTargetFrameworks().Max(targetFramework => Version.Parse(targetFramework["net".Length..]).Major);

        return hostVersion.Major == highestMajorVersion;
    }

    /// <summary>
    /// Packs project Steeltoe.Management.Endpoint (along with the Steeltoe projects it references) into an isolated local NuGet feed (once per test run) to
    /// speed up running tests.
    /// </summary>
    public sealed class PackSteeltoeLibrariesOnceFixture : IAsyncLifetime
    {
        private string? _sessionDirectory;

        internal string SessionDirectory => _sessionDirectory!;
        internal NuGetSource Source { get; private set; } = null!;
        internal string PackageVersion { get; } = $"9.9.9-test.{$"{Guid.NewGuid():N}"[..8]}";

        public async ValueTask InitializeAsync()
        {
            if (!IsHighestTestHostFramework())
            {
                return;
            }

            string directoryName = $"steeltoe-dumps-test-session-{$"{Guid.NewGuid():N}"[..8]}";
            string tempPath = Path.GetTempPath();
            string sessionDirectory = new DirectoryInfo(tempPath).CreateSubdirectory(directoryName).FullName;
            _sessionDirectory = sessionDirectory;

            var sessionDirectoryInfo = new DirectoryInfo(sessionDirectory);
            Source = CreateNuGetSource(sessionDirectoryInfo);

            string endpointProjectPath = GetEndpointProjectPath();

            foreach (string projectPath in ResolveSteeltoeProjectFilePaths(endpointProjectPath))
            {
                await PackAsync(projectPath, Source.FeedDirectory, PackageVersion);
            }
        }

        private static NuGetSource CreateNuGetSource(DirectoryInfo sessionDirectoryInfo)
        {
            string feedDirectory = sessionDirectoryInfo.CreateSubdirectory("feed").FullName;
            string packagesDirectory = sessionDirectoryInfo.CreateSubdirectory("packages").FullName;
            return new NuGetSource(feedDirectory, packagesDirectory);
        }

        private static string GetEndpointProjectPath()
        {
            string testDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", "..");
            string projectFilePath = Path.Combine(testDirectory, "..", "..", "src", "Endpoint", $"{EndpointProjectName}.csproj");
            return Path.GetFullPath(projectFilePath);
        }

        private static HashSet<string> ResolveSteeltoeProjectFilePaths(string startProjectPath)
        {
            var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pending = new Queue<string>();
            pending.Enqueue(Path.GetFullPath(startProjectPath));

            while (pending.TryDequeue(out string? nextProjectPath))
            {
                if (discovered.Add(nextProjectPath))
                {
                    XDocument document = XDocument.Load(nextProjectPath);
                    string directory = Path.GetDirectoryName(nextProjectPath)!;

                    foreach (string relativePath in document.Descendants("ProjectReference").Attributes("Include").Select(attribute => attribute.Value))
                    {
                        string absolutePath = Path.GetFullPath(Path.Combine(directory, NormalizePath(relativePath)));
                        pending.Enqueue(absolutePath);
                    }
                }
            }

            return discovered;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        private static async Task PackAsync(string projectPath, string feedDirectory, string packageVersion)
        {
            string projectDirectory = Path.GetDirectoryName(projectPath)!;

            await DotNetProcessRunner.RunAsync(projectDirectory, "build", projectPath, "-c", "Release", $"-p:Version={packageVersion}",
                $"-p:PackageOutputPath={feedDirectory}");
        }

        public ValueTask DisposeAsync()
        {
            if (_sessionDirectory != null)
            {
                try
                {
                    Directory.Delete(_sessionDirectory, true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan) must not fail the test run.
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    internal sealed class DumpVerificationApp
    {
        private const string LibrarySource = """
            using Microsoft.Extensions.DependencyInjection;
            using Steeltoe.Management.Endpoint.Actuators.HeapDump;
            using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

            namespace DumpTestLibrary;

            public static class DumpProvider
            {
                public static void RegisterHeapDump(IServiceCollection services)
                {
                    services.AddHeapDumpActuator(false);
                }

                public static void RegisterThreadDump(IServiceCollection services)
                {
                    services.AddThreadDumpActuator(false);
                }

                public static async Task TakeHeapDumpAsync(IServiceProvider services)
                {
                    var handler = services.GetRequiredService<IHeapDumpEndpointHandler>();
                    string path = await handler.InvokeAsync(null, CancellationToken.None);
                    File.Delete(path);
                }

                public static async Task TakeThreadDumpAsync(IServiceProvider services)
                {
                    var handler = services.GetRequiredService<IThreadDumpEndpointHandler>();
                    await handler.InvokeAsync(null, CancellationToken.None);
                }
            }
            """;

        private const string GCDumpAppSource = """
            using DumpTestLibrary;

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Configuration["Management:Endpoints:Heapdump:HeapDumpType"] = "GCDump";
            DumpProvider.RegisterHeapDump(builder.Services);

            await using WebApplication app = builder.Build();

            try
            {
                await DumpProvider.TakeHeapDumpAsync(app.Services);
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"FAILED: {exception}");
                return 1;
            }
            """;

        private const string MinidumpAppSource = """
            using DumpTestLibrary;

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Configuration["Management:Endpoints:Heapdump:HeapDumpType"] = "Mini";
            DumpProvider.RegisterHeapDump(builder.Services);

            await using WebApplication app = builder.Build();

            try
            {
                await DumpProvider.TakeHeapDumpAsync(app.Services);
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"FAILED: {exception}");
                return 1;
            }
            """;

        private const string ThreadDumpAppSource = """
            using DumpTestLibrary;

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            DumpProvider.RegisterThreadDump(builder.Services);

            await using WebApplication app = builder.Build();

            try
            {
                await DumpProvider.TakeThreadDumpAsync(app.Services);
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"FAILED: {exception}");
                return 1;
            }
            """;

        private readonly string _appDirectory;

        private DumpVerificationApp(string appDirectory)
        {
            _appDirectory = appDirectory;
        }

        internal static async Task<DumpVerificationApp> CreateForGCDumpAsync(PackSteeltoeLibrariesOnceFixture fixture, string targetFramework)
        {
            return await CreateAsync(fixture, targetFramework, "GCDumpTestApp", GCDumpAppSource);
        }

        internal static async Task<DumpVerificationApp> CreateForMinidumpAsync(PackSteeltoeLibrariesOnceFixture fixture, string targetFramework)
        {
            return await CreateAsync(fixture, targetFramework, "MinidumpTestApp", MinidumpAppSource);
        }

        internal static async Task<DumpVerificationApp> CreateForThreadDumpAsync(PackSteeltoeLibrariesOnceFixture fixture, string targetFramework)
        {
            return await CreateAsync(fixture, targetFramework, "ThreadDumpTestApp", ThreadDumpAppSource);
        }

        private static async Task<DumpVerificationApp> CreateAsync(PackSteeltoeLibrariesOnceFixture fixture, string targetFramework, string testAppName,
            string appSource)
        {
            const string testLibraryName = "DumpTestLibrary";

            string rootDirectory = Path.Combine(fixture.SessionDirectory, "projects");
            string libraryDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory, testLibraryName)).FullName;
            string appDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory, testAppName)).FullName;

            await WriteNuGetConfigFileAsync(rootDirectory, fixture.Source);
            await WriteLibraryProjectAsync(libraryDirectory, testLibraryName, fixture.PackageVersion, targetFramework);
            await WriteAppProjectAsync(appDirectory, testAppName, testLibraryName, targetFramework, appSource);

            return new DumpVerificationApp(appDirectory);
        }

        private static async Task WriteNuGetConfigFileAsync(string directory, NuGetSource source)
        {
            string contents = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <config>
                    <add key="globalPackagesFolder" value="{source.PackagesDirectory}" />
                  </config>
                  <packageSources>
                    <add key="local-steeltoe" value="{source.FeedDirectory}" />
                  </packageSources>
                </configuration>
                """;

            string nuGetConfigPath = Path.Combine(directory, "nuget.config");
            await File.WriteAllTextAsync(nuGetConfigPath, contents);
        }

        private static async Task WriteLibraryProjectAsync(string libraryDirectory, string libraryName, string packageVersion, string targetFramework)
        {
            string projectFilePath = Path.Combine(libraryDirectory, $"{libraryName}.csproj");
            string projectFileContents = GetLibraryProjectFile(packageVersion, targetFramework);
            await File.WriteAllTextAsync(projectFilePath, projectFileContents);

            string sourcePath = Path.Combine(libraryDirectory, "DumpProvider.cs");
            await File.WriteAllTextAsync(sourcePath, LibrarySource);
        }

        private static async Task WriteAppProjectAsync(string appDirectory, string testAppName, string testLibraryName, string targetFramework,
            string appSource)
        {
            string projectFilePath = Path.Combine(appDirectory, $"{testAppName}.csproj");
            string projectFileContents = GetAppProjectFile(testLibraryName, targetFramework);
            await File.WriteAllTextAsync(projectFilePath, projectFileContents);

            string sourcePath = Path.Combine(appDirectory, "Program.cs");
            await File.WriteAllTextAsync(sourcePath, appSource);
        }

        private static string GetLibraryProjectFile(string packageVersion, string targetFramework)
        {
            return $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{targetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="{EndpointProjectName}" Version="{packageVersion}" />
                  </ItemGroup>
                </Project>
                """;
        }

        private static string GetAppProjectFile(string testLibraryName, string targetFramework)
        {
            return $"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>{targetFramework}</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\\{testLibraryName}\\{testLibraryName}.csproj" />
                  </ItemGroup>
                </Project>
                """;
        }

        public async Task RunAsync()
        {
            await DotNetProcessRunner.RunAsync(_appDirectory, "run");
        }
    }

    private static class DotNetProcessRunner
    {
        private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromMinutes(5);

        public static async Task RunAsync(string workingDirectory, params string[] arguments)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            string[] dotNetArguments =
            [
                .. arguments,
                "-p:RunAnalyzers=false",
                "-p:NuGetAudit=false"
            ];

            var outputBuilder = new StringBuilder();
            object outputLock = new();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (string argument in dotNetArguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // Without this, a spawned "dotnet build"/"run" leaves a persistent MSBuild worker node running in the background for reuse by a later
            // build. That node inherits our redirected stdout/stderr pipe handles and keeps them open after the process we launched exits, so the
            // read end never sees EOF and awaiting exit below would block forever even though the build already completed successfully.
            startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";

            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
            process.ErrorDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(ProcessExitTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException)
            {
                KillEntireProcessTreeInBackground(process.Id);

                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new TimeoutException($"'dotnet {string.Join(' ', dotNetArguments)}' in '{workingDirectory}' did not exit within {ProcessExitTimeout}.");
            }

            string output = outputBuilder.ToString();

            process.ExitCode.Should().Be(0, "'dotnet {0}' in '{1}' was expected to exit successfully. Output:\n{2}", string.Join(' ', dotNetArguments),
                workingDirectory, output);

            void AppendLine(string? line)
            {
                if (line == null)
                {
                    return;
                }

#pragma warning disable S6507 // Blocks should not be synchronized on local variables
                // Justification: Deliberately a call-scoped lock, not a shared static one: a global lock would serialize stdout/stderr callbacks across
                // every concurrently running process, starving the thread pool under high-volume output (e.g. "dotnet build -v:detailed").
                lock (outputLock)
#pragma warning restore S6507 // Blocks should not be synchronized on local variables
                {
                    outputBuilder.AppendLine(line);
                }
            }
        }

        private static void KillEntireProcessTreeInBackground(int processId)
        {
            // Fire-and-forget, so that pressing the Stop button in an IDE responds immediately.
            _ = Task.Run(() =>
            {
                try
                {
                    using var process = Process.GetProcessById(processId);
                    process.Kill(true);
                }
                catch (Exception)
                {
                    // Best-effort kill of an already-timed-out process.
                }
            });
        }
    }

    internal sealed record NuGetSource(string FeedDirectory, string PackagesDirectory);
}
