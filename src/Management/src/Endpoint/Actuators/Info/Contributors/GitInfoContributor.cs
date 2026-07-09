// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

internal sealed partial class GitInfoContributor : ConfigurationContributor, IInfoContributor
{
    private const string GitSettingsPrefix = "git";
    private const string GitPropertiesFileName = "git.properties";

    private static readonly List<string> DateTimeInputKeys = ["time"];

    private readonly string _propertiesPath;
    private readonly ILogger _logger;

    public GitInfoContributor(ILogger<GitInfoContributor> logger)
        : this(ResolveDefaultPropertiesPath(), logger)
    {
    }

    public GitInfoContributor(string propertiesPath, ILogger<GitInfoContributor> logger)
        : base(null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertiesPath);
        ArgumentNullException.ThrowIfNull(logger);

        _propertiesPath = propertiesPath;
        _logger = logger;
    }

    private static string ResolveDefaultPropertiesPath()
    {
        return ResolveDefaultPropertiesPath(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
    }

    /// <summary>
    /// Prefers the directory the running assembly was loaded from (where a build tool like Steeltoe.Management.GitProperties.Build copies git.properties to)
    /// over the process's current working directory, since the latter depends entirely on how the application was launched (for example, `dotnet run` and
    /// directly invoking a built DLL both leave the current directory pointed at the project directory, not the output directory the assembly - and
    /// git.properties - actually live in) and can't be relied on to match. Takes both directories as parameters purely so tests can exercise this resolution
    /// logic against isolated temporary directories, without touching either of this process's real ones.
    /// </summary>
    internal static string ResolveDefaultPropertiesPath(string baseDirectory, string currentDirectory)
    {
        string baseDirectoryPath = Path.Combine(baseDirectory, GitPropertiesFileName);

        if (File.Exists(baseDirectoryPath))
        {
            return baseDirectoryPath;
        }

        return Path.Combine(currentDirectory, GitPropertiesFileName);
    }

    public async Task ContributeAsync(InfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Configuration = await ReadGitPropertiesAsync(cancellationToken);
        Contribute(builder, GitSettingsPrefix, true);
    }

    private async Task<IConfiguration?> ReadGitPropertiesAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_propertiesPath))
        {
            string[] lines = await File.ReadAllLinesAsync(_propertiesPath, cancellationToken);

            if (lines.Length > 0)
            {
                var dictionary = new Dictionary<string, string?>();

                foreach (string line in lines)
                {
                    if (line.StartsWith('#') || !line.StartsWith("git.", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] keyValuePair = line.Split('=', 2);

                    if (keyValuePair.Length != 2)
                    {
                        continue;
                    }

                    string key = keyValuePair[0].Trim().Replace('.', ':');
                    string value = keyValuePair[1].Replace("\\:", ":", StringComparison.Ordinal);

                    dictionary[key] = value;
                }

                var builder = new ConfigurationBuilder();
                builder.AddInMemoryCollection(dictionary);
                return builder.Build();
            }
        }
        else
        {
            LogFileNotFound(_propertiesPath);
        }

        return null;
    }

    protected override void AddKeyValue(IDictionary<string, object?> dictionary, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrEmpty(key);

        object? valueToInsert = value;

        if (DateTimeInputKeys.Contains(key) && value is string stringValue)
        {
            // Normalize datetime values to ISO8601 format
            valueToInsert = DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }

        dictionary[key] = valueToInsert;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "File '{Path}' does not exist.")]
    private partial void LogFileNotFound(string path);
}
