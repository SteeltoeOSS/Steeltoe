// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Steeltoe: This file was copied from the .NET Aspire Configuration Schema generator
// at https://github.com/dotnet/aspire/tree/cb7cc4d78f8dd2b4df1053a229493cdbf88f50df/src/Tools/ConfigurationSchemaGenerator.
#pragma warning disable

using System.CommandLine;

namespace ConfigurationSchemaGenerator;

internal static class RootGenerateCommand
{
    private static readonly Option<string> s_inputOption = new("--input")
    {
        Required = true,
        Description = "The assembly to generate a ConfigurationSchema.json file for.",
    };

    private static readonly Option<string[]> s_referencesOption = new("--reference")
    {
        AllowMultipleArgumentsPerToken = true,
        Required = true,
        Description = "The assemblies referenced by the input assembly.",
    };

    private static readonly Option<string> s_outputOption = new("--output")
    {
        Required = true,
        Description = "The FilePath assembly to generate a ConfigurationSchema.json file for.",
    };

    public static RootCommand GetCommand()
    {
        var formatCommand = new RootCommand("Generates ConfigurationSchema.json files.")
        {
            s_inputOption,
            s_referencesOption,
            s_outputOption,
        };

        formatCommand.SetAction(static parseResult =>
        {
            var inputAssembly = parseResult.GetValue(s_inputOption);
            var references = parseResult.GetValue(s_referencesOption);
            var outputFile = parseResult.GetValue(s_outputOption);

            ConfigSchemaGenerator.GenerateSchema(inputAssembly, references, outputFile);
            return 0;
        });

        return formatCommand;
    }
}
