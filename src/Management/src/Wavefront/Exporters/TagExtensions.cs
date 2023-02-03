using OpenTelemetry;

namespace Steeltoe.Management.Wavefront.Exporters;
internal static class TagExtensions
{
    public static IDictionary<string, string> AsDictionary(this ReadOnlyTagCollection tagCollection)
    {
        var tags = new Dictionary<string, string>();

        foreach (KeyValuePair<string, object> tag in tagCollection)
        {
            if (string.IsNullOrEmpty(tag.Key) || string.IsNullOrEmpty(tag.Value.ToString()))
            {
                continue;
            }

            tags.Add(tag.Key, tag.Value.ToString());
        }

        return tags;
    }
}
