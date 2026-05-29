// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Steeltoe.Common.TestResources;

public static class FluentAssertionsExtensions
{
    private static readonly JsonWriterOptions NormalizedJsonWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Asserts that a JSON-formatted string matches the specified JSON text, ignoring differences in insignificant whitespace and line endings. Using this
    /// extension enables writing assertions on JSON in a more readable fashion.
    /// </summary>
    /// <param name="source">
    /// The source JSON to assert on.
    /// </param>
    /// <param name="expected">
    /// The expected JSON text.
    /// </param>
    [CustomAssertion]
    public static void BeJson(this StringAssertions source, string expected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expected);

        using JsonDocument sourceJson = JsonDocument.Parse(source.Subject);
        using JsonDocument expectedJson = JsonDocument.Parse(expected);

        string sourceText = ToJsonString(sourceJson);
        string expectedText = ToJsonString(expectedJson);

        sourceText.Should().Be(expectedText);
    }

    private static string ToJsonString(JsonDocument document)
    {
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream, NormalizedJsonWriterOptions);

        document.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Same as the built-in Be() method, but allows specifying a custom comparer.
    /// </summary>
    /// <param name="source">
    /// The source text to assert on.
    /// </param>
    /// <param name="expected">
    /// The expected text.
    /// </param>
    /// <param name="comparer">
    /// An equality comparer to compare values.
    /// </param>
    [CustomAssertion]
    public static void Be(this StringAssertions source, string expected, IEqualityComparer<string> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(comparer);

        object subject = source.Subject;
        subject.Should().Be(expected, comparer);
    }

    /// <summary>
    /// Same as the built-in Contain() method, but normalizes line endings upfront.
    /// </summary>
    /// <param name="source">
    /// The source text to assert on.
    /// </param>
    /// <param name="expected">
    /// The expected text.
    /// </param>
    [CustomAssertion]
    public static void ContainLines(this StringAssertions source, string expected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expected);

        string sourceText = source.Subject.ReplaceLineEndings();
        string expectedText = expected.ReplaceLineEndings();

        sourceText.Should().Contain(expectedText);
    }
}
