// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Configuration.SpringBoot.Test;

// Breaking changes were introduced in https://github.com/dotnet/runtime/pull/116677.
#pragma warning disable
// ReSharper disable All
// @formatter:off

public static class CustomJsonConfigurationExtensions
{
    private static readonly bool IsDotNet10 = typeof(JsonStreamConfigurationProvider).Assembly.GetName().Version?.Major == 10;

    public static IConfigurationBuilder AddCustomJsonStream(this IConfigurationBuilder builder, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (IsDotNet10)
        {
            return builder.Add<CustomJsonStreamConfigurationSource>(s => s.Stream = stream);
        }
        else
        {
            return builder.Add<JsonStreamConfigurationSource>(s => s.Stream = stream);
        }
    }
}

public class CustomJsonStreamConfigurationSource : JsonStreamConfigurationSource
{
    /// <summary>
    /// Builds the <see cref="JsonStreamConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>An <see cref="JsonStreamConfigurationProvider"/></returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
        => new CustomJsonStreamConfigurationProvider(this);
}

public class CustomJsonStreamConfigurationProvider(JsonStreamConfigurationSource source)
    : JsonStreamConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        Data = CustomJsonConfigurationFileParser.Parse(stream);
    }
}

internal sealed class CustomJsonConfigurationFileParser
{
    private CustomJsonConfigurationFileParser() { }

    private readonly Dictionary<string, string?> _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _paths = new Stack<string>();

    public static IDictionary<string, string?> Parse(Stream input)
        => new CustomJsonConfigurationFileParser().ParseStream(input);

    private Dictionary<string, string?> ParseStream(Stream input)
    {
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        using (var reader = new StreamReader(input))
        using (JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions))
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("SR.Error_InvalidTopLevelJSONElement");
            }
            VisitObjectElement(doc.RootElement);
        }

        return _data;
    }

    private void VisitObjectElement(JsonElement element)
    {
        var isEmpty = true;

        foreach (JsonProperty property in element.EnumerateObject())
        {
            isEmpty = false;
            EnterContext(property.Name);
            VisitValue(property.Value);
            ExitContext();
        }

        // START: Patch to express empty object as ""
        //SetNullIfElementIsEmpty(isEmpty);
        SetEmptyIfElementIsEmpty(isEmpty);
        // END: Patch to express empty object as ""
    }

    private void VisitArrayElement(JsonElement element)
    {
        int index = 0;

        foreach (JsonElement arrayElement in element.EnumerateArray())
        {
            EnterContext(index.ToString());
            VisitValue(arrayElement);
            ExitContext();
            index++;
        }

        SetEmptyIfElementIsEmpty(isEmpty: index == 0);
    }

    private void SetNullIfElementIsEmpty(bool isEmpty)
    {
        if (isEmpty && _paths.Count > 0)
        {
            _data[_paths.Peek()] = null;
        }
    }

    private void SetEmptyIfElementIsEmpty(bool isEmpty)
    {
        if (isEmpty && _paths.Count > 0)
        {
            _data[_paths.Peek()] = string.Empty;
        }
    }

    private void VisitValue(JsonElement value)
    {
        Debug.Assert(_paths.Count > 0);

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObjectElement(value);
                break;

            case JsonValueKind.Array:
                VisitArrayElement(value);
                break;

            case JsonValueKind.Number:
            case JsonValueKind.String:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                string key = _paths.Peek();
                if (_data.ContainsKey(key))
                {
                    throw new FormatException("SR.Error_KeyIsDuplicated");
                }
                _data[key] = value.ValueKind == JsonValueKind.Null ? null : value.ToString();
                break;

            default:
                throw new FormatException("SR.Error_UnsupportedJSONToken");
        }
    }

    private void EnterContext(string context) =>
        _paths.Push(_paths.Count > 0 ?
            _paths.Peek() + ConfigurationPath.KeyDelimiter + context :
            context);

    private void ExitContext() => _paths.Pop();
}
