using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Default implementation that masks configured sensitive field names in JSON and dictionaries.
/// </summary>
public sealed class DefaultAuditDataMasker : IAuditDataMasker
{
    private const string MaskedPlaceholder = "***MASKED***";
    private readonly HashSet<string> _sensitiveFieldNames;

    /// <summary>
    /// Creates a masker using sensitive field names from options.
    /// </summary>
    public DefaultAuditDataMasker(IOptions<AuditOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _sensitiveFieldNames = new HashSet<string>(
            options.Value.SensitiveFieldNames,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string? Mask(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        try
        {
            var node = JsonNode.Parse(input);
            if (node is null)
            {
                return input;
            }

            MaskNode(node);
            return node.ToJsonString();
        }
        catch
        {
            return MaskedPlaceholder;
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object?>? MaskDictionary(IDictionary<string, object?>? input)
    {
        if (input is null || input.Count == 0)
        {
            return input;
        }

        try
        {
            var masked = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in input)
            {
                if (_sensitiveFieldNames.Contains(pair.Key))
                {
                    masked[pair.Key] = MaskedPlaceholder;
                }
                else if (pair.Value is IDictionary<string, object?> nestedDict)
                {
                    masked[pair.Key] = MaskDictionary(nestedDict);
                }
                else
                {
                    masked[pair.Key] = pair.Value;
                }
            }

            return masked;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["_maskError"] = MaskedPlaceholder
            };
        }
    }

    private void MaskNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (property.Key is not null && _sensitiveFieldNames.Contains(property.Key))
                    {
                        obj[property.Key] = MaskedPlaceholder;
                    }
                    else if (property.Value is not null)
                    {
                        MaskNode(property.Value);
                    }
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        MaskNode(item);
                    }
                }

                break;
        }
    }
}
