using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace EasyCore.Audit;

/// <summary>
/// Serializes audit payload objects to JSON with safe defaults and truncation.
/// </summary>
public sealed class AuditPayloadSerializer
{
    private static readonly HashSet<Type> IgnoredTypes =
    [
        typeof(CancellationToken),
        typeof(HttpContext),
        typeof(Stream)
    ];

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxLength;

    /// <summary>
    /// Creates a serializer with the given maximum output length.
    /// </summary>
    public AuditPayloadSerializer(int maxLength)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }

        _maxLength = maxLength;
        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Serializes an object for audit capture.
    /// </summary>
    public string? Serialize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            var prepared = PrepareValue(value);
            var json = JsonSerializer.Serialize(prepared, _jsonOptions);
            return Truncate(json);
        }
        catch
        {
            return Truncate(value.ToString());
        }
    }

    /// <summary>
    /// Serializes a dictionary for audit capture.
    /// </summary>
    public string? SerializeDictionary(IDictionary<string, object?> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        try
        {
            var prepared = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in dictionary)
            {
                prepared[pair.Key] = PrepareValue(pair.Value);
            }

            var json = JsonSerializer.Serialize(prepared, _jsonOptions);
            return Truncate(json);
        }
        catch
        {
            return "[serialization failed]";
        }
    }

    private object? PrepareValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();

        if (IgnoredTypes.Contains(type) || value is CancellationToken or HttpContext or Stream)
        {
            return null;
        }

        if (value is IFormFile formFile)
        {
            return new
            {
                formFile.FileName,
                formFile.ContentType,
                formFile.Length,
                formFile.Name
            };
        }

        if (value is IEnumerable<IFormFile> formFiles)
        {
            return formFiles.Select(f => new { f.FileName, f.ContentType, f.Length, f.Name }).ToList();
        }

        if (value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or char or DateTime or DateTimeOffset or Guid)
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key)
                {
                    dict[key] = PrepareValue(entry.Value);
                }
            }

            return dict;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(PrepareValue(item));
            }

            return list;
        }

        if (type.IsClass || type.IsValueType)
        {
            if (HasIgnoredPropertyOnly(type))
            {
                return value.ToString();
            }
        }

        return value;
    }

    private static bool HasIgnoredPropertyOnly(Type type)
    {
        return type == typeof(CancellationToken) || type == typeof(HttpContext) || typeof(Stream).IsAssignableFrom(type);
    }

    private string? Truncate(string? value)
    {
        if (value is null || _maxLength == 0)
        {
            return value;
        }

        if (value.Length <= _maxLength)
        {
            return value;
        }

        return value[.._maxLength] + "...[truncated]";
    }
}
