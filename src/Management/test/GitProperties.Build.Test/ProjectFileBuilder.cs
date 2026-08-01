// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Builds the XML content of an MSBuild project file.
/// </summary>
internal sealed class ProjectFileBuilder
{
    private const string GroupIndent = "    ";
    private readonly List<string> _itemGroupLines = [];
    private string[] _targetFrameworks = [TestAppTargetFramework.Default];
    private bool? _generateGitProperties;

    public bool IsExecutable { get; set; }

    public ProjectFileBuilder WithTargetFrameworks(string[] targetFrameworks)
    {
        _targetFrameworks = targetFrameworks;
        return this;
    }

    public ProjectFileBuilder WithGenerateGitProperties(bool value)
    {
        _generateGitProperties = value;
        return this;
    }

    public ProjectFileBuilder WithProjectReference(ProjectReference reference)
    {
        _itemGroupLines.Add($"""<ProjectReference Include="{reference.Include}" />""");
        return this;
    }

    public ProjectFileBuilder WithPackageReference(PackageReference reference)
    {
        string xml = reference.PrivateAssets == null
            ? $"""<PackageReference Include="{reference.PackageId}" Version="{reference.Version}" />"""
            : $"""
            <PackageReference Include="{reference.PackageId}" Version="{reference.Version}">
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
              <PrivateAssets>{reference.PrivateAssets}</PrivateAssets>
            </PackageReference>
            """;

        _itemGroupLines.Add(xml);
        return this;
    }

    public string Build()
    {
        string propertyGroup = GetPropertyGroup();
        string itemGroup = GetItemGroup();

        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                {propertyGroup}
              </PropertyGroup>

              <ItemGroup>
                {itemGroup}
              </ItemGroup>
            </Project>
            """;
    }

    private string GetPropertyGroup()
    {
        List<string> propertyLines = [GetTargetFrameworkElement()];

        if (IsExecutable)
        {
            propertyLines.Add("<OutputType>Exe</OutputType>");
        }

        propertyLines.Add("<ImplicitUsings>enable</ImplicitUsings>");
        propertyLines.Add("<Nullable>enable</Nullable>");

        if (_generateGitProperties != null)
        {
            string propertyValue = _generateGitProperties.Value ? "true" : "false";
            propertyLines.Add($"<GenerateGitProperties>{propertyValue}</GenerateGitProperties>");
        }

        return string.Join($"{Environment.NewLine}{GroupIndent}", propertyLines);
    }

    private string GetTargetFrameworkElement()
    {
        return _targetFrameworks.Length == 1
            ? $"<TargetFramework>{_targetFrameworks[0]}</TargetFramework>"
            : $"<TargetFrameworks>{string.Join(';', _targetFrameworks)}</TargetFrameworks>";
    }

    private string GetItemGroup()
    {
        return string.Join($"{Environment.NewLine}{GroupIndent}", _itemGroupLines.SelectMany(item => item.Split(Environment.NewLine)));
    }
}
