using System.Text.Json.Serialization;

namespace BarberPortfolio.Models;

/// <summary>
/// Wrapper genérico para la respuesta raíz de la API de Sanity.
/// Sanity siempre envuelve sus resultados GROQ en la estructura:
/// <code>{ "ms": int, "query": string, "result": T[] }</code>
/// Usar un genérico aquí permite reutilizar el wrapper para cualquier tipo de documento.
/// </summary>
/// <typeparam name="T">Tipo del documento Sanity dentro del array <c>result</c>.</typeparam>
public sealed class SanityResponse<T>
{
    /// <summary>Tiempo de ejecución de la query en milisegundos (informativo).</summary>
    [JsonPropertyName("ms")]
    public int Ms { get; init; }

    /// <summary>Query GROQ ejecutada, devuelta por Sanity para depuración.</summary>
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    /// <summary>Lista de documentos resultantes. Nunca null, vacío si no hay resultados.</summary>
    [JsonPropertyName("result")]
    public List<T> Result { get; init; } = [];
}

/// <summary>
/// DTO que mapea un documento de corte de cabello tal como lo devuelve Sanity
/// tras aplicar la proyección GROQ. Los campos de imagen ya son URLs resueltas
/// porque usamos el operador de derreferencia <c>asset-&gt;url</c> en la query,
/// eliminando la necesidad de parsear manualmente el campo <c>_ref</c>.
/// </summary>
public sealed class SanityHaircutDocument
{
    /// <summary>
    /// Identificador único del documento. En Sanity, los IDs de borradores
    /// tienen el prefijo "drafts." — la query GROQ ya los filtra con
    /// <c>!(_id in path("drafts.**"))</c>.
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Slug de categoría tal como está definido en el schema de Sanity Studio.</summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// URL de imagen resuelta por GROQ mediante <c>image.asset-&gt;url</c>.
    /// El CDN de Sanity sirve las imágenes desde <c>cdn.sanity.io/images/</c>.
    /// Se puede añadir parámetros de transformación a esta URL, ej:
    /// <c>?w=800&amp;h=600&amp;fit=crop&amp;auto=format</c>.
    /// </summary>
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// URL de la imagen "antes". Nullable porque el campo <c>beforeImage</c>
    /// es opcional en el schema de Sanity.
    /// </summary>
    [JsonPropertyName("beforeImageUrl")]
    public string? BeforeImageUrl { get; init; }

    /// <summary>Timestamp de creación del documento en UTC, provisto por Sanity automáticamente.</summary>
    [JsonPropertyName("_createdAt")]
    public DateTime CreatedAt { get; init; }
}