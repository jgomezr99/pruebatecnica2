using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ZiurInventario.Models;
using ZiurInventario.Options;

namespace ZiurInventario.Services;

public sealed class ZiurApiService(
    HttpClient httpClient,
    IOptions<ZiurApiOptions> options,
    ILogger<ZiurApiService> logger) : IZiurApiService
{
    private readonly ZiurApiOptions _options = options.Value;

    public async Task<ApiTableData> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BearerToken))
            throw new ZiurApiException(
                "Falta configurar el token Bearer en los secretos locales del proyecto.");

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new ZiurApiException("No se configuró el endpoint de Ziur.");

        using var response = await httpClient.GetAsync(
            _options.Endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Ziur respondió HTTP {StatusCode}.", (int)response.StatusCode);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new ZiurApiException(
                $"Ziur rechazó la autenticación o los permisos (HTTP {(int)response.StatusCode}). Verifica el token y sus permisos.");

        if (!response.IsSuccessStatusCode)
            throw new ZiurApiException(
                $"La API de Ziur respondió HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return new ApiTableData([], [], "$", "respuesta vacía");

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("La API devolvió contenido que no es JSON válido.");
            throw new ZiurApiException("La respuesta de Ziur no contiene JSON válido.", ex);
        }

        if (root is null)
            return new ApiTableData([], [], "$", "null");

        var candidate = FindBestArray(root, "$");
        if (candidate.Node is JsonArray array)
        {
            var objectRows = array.OfType<JsonObject>().ToList();
            if (objectRows.Count > 0)
                return ToTable(objectRows, candidate.Path, "array de objetos");

            // An array of primitives is still displayed without assuming field names.
            if (array.Count > 0)
            {
                var rows = array.Select((item, index) =>
                    (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
                    {
                        ["Índice"] = index.ToString(),
                        ["Valor"] = FormatNode(item)
                    }).ToList();
                return new ApiTableData(["Índice", "Valor"], rows, candidate.Path, "array de valores");
            }

            return new ApiTableData([], [], candidate.Path, "array vacío");
        }

        if (root is JsonObject singleObject)
            return ToTable([singleObject], "$", "objeto JSON");

        return new ApiTableData(["Valor"],
            [new Dictionary<string, string> { ["Valor"] = FormatNode(root) }],
            "$", "valor JSON");
    }

    private static (JsonNode? Node, string Path, int Score) FindBestArray(JsonNode node, string path)
    {
        if (node is JsonArray array)
        {
            var objectCount = array.Count(x => x is JsonObject);
            var score = objectCount > 0 ? 100 + objectCount : array.Count > 0 ? 10 : 1;
            return (array, path, score);
        }

        (JsonNode? Node, string Path, int Score) best = (null, path, -1);
        if (node is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (pair.Value is null) continue;
                var candidate = FindBestArray(pair.Value, $"{path}.{pair.Key}");
                if (candidate.Score > best.Score) best = candidate;
            }
        }
        else if (node is JsonArray children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is null) continue;
                var candidate = FindBestArray(children[i]!, $"{path}[{i}]");
                if (candidate.Score > best.Score) best = candidate;
            }
        }
        return best;
    }

    private static ApiTableData ToTable(
        IReadOnlyList<JsonObject> objects, string path, string rootKind)
    {
        var columns = objects
            .SelectMany(x => x.Select(p => p.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = objects.Select(obj =>
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
                row[column] = obj.TryGetPropertyValue(column, out var value)
                    ? FormatNode(value) : "";
            return (IReadOnlyDictionary<string, string>)row;
        }).ToList();

        return new ApiTableData(columns, rows, path, rootKind);
    }

    private static string FormatNode(JsonNode? node)
    {
        if (node is null) return "";
        if (node is JsonValue value)
        {
            try
            {
                if (value.TryGetValue<string>(out var text)) return text ?? "";
            }
            catch (InvalidOperationException) { }
        }
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}

public sealed class ZiurApiException : Exception
{
    public ZiurApiException(string message) : base(message) { }
    public ZiurApiException(string message, Exception innerException)
        : base(message, innerException) { }
}
