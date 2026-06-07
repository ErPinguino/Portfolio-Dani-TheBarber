using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BarberPortfolio.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BarberPortfolio.Extensions;

namespace BarberPortfolio.Services;

/// <summary>
/// Implementación real de <see cref="IPortfolioService"/> que consulta Sanity.io
/// a través de su API HTTP usando el lenguaje de queries GROQ.
/// </summary>
/// <remarks>
/// Registrar con <c>AddHttpClient&lt;IPortfolioService, SanityPortfolioService&gt;()</c>
/// para que <c>IHttpClientFactory</c> gestione el pool de conexiones correctamente
/// y evitar el problema de agotamiento de sockets (<c>HttpClient</c> estático implícito).
/// </remarks>
public sealed class SanityPortfolioService : IPortfolioService
{
    private readonly HttpClient _http;
    private readonly SanitySettings _settings;
    private readonly ILogger<SanityPortfolioService> _logger;

    /// <summary>
    /// Opciones de deserialización compartidas como campo estático para evitar
    /// re-instanciación en cada llamada (JsonSerializerOptions es costosa de crear).
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SanityPortfolioService(
        HttpClient http,
        IOptions<SanitySettings> options,
        ILogger<SanityPortfolioService> logger)
    {
        _http     = http;
        _settings = options.Value;
        _logger   = logger;
    }

    /// <inheritdoc/>
    public async Task<List<Haircut>> GetActivePortfolioAsync(CancellationToken ct = default)
    {
        // GROQ:
        // 1. Filtra documentos de tipo "haircut" con active == true.
        // 2. Excluye borradores mediante la expresión de ruta "drafts.**".
        // 3. Ordena descendente por fecha de creación.
        // 4. Proyecta solo los campos necesarios (evita traer todo el documento).
        // 5. "asset->url" dereferencia la referencia del asset en un solo round-trip,
        //    devolviendo la URL pública del CDN de Sanity directamente.
        const string query = """
            *[_type == "haircut" && active == true && !(_id in path("drafts.**"))]
            | order(_createdAt desc) {
              "_id":            _id,
              "title":          title,
              "category":       category,
              "description":    description,
              "imageUrl":       image.asset->url,
              "beforeImageUrl": beforeImage.asset->url,
              "_createdAt":     _createdAt
            }
            """;

        return await ExecuteQueryAsync(query, ct);
    }

    /// <inheritdoc/>
    public async Task<List<Haircut>> GetPortfolioByCategoryAsync(
        string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return await GetActivePortfolioAsync(ct);

        // La interpolación directa es segura aquí porque `category` es un valor
        // interno controlado por la aplicación (slugs del CMS), no input libre del usuario.
        // Si en el futuro viniera de un campo de texto libre, escapar con comillas simples.
        string query = $$"""
            *[_type == "haircut" && active == true && category == "{{category}}"
              && !(_id in path("drafts.**"))]
            | order(_createdAt desc) {
              "_id":            _id,
              "title":          title,
              "category":       category,
              "description":    description,
              "imageUrl":       image.asset->url,
              "beforeImageUrl": beforeImage.asset->url,
              "_createdAt":     _createdAt
            }
            """;

        return await ExecuteQueryAsync(query, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MÉTODOS PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye la URL de la API de Sanity, ejecuta la query GROQ,
    /// deserializa la respuesta y la mapea al modelo de dominio.
    /// Toda la lógica de red y gestión de errores se centraliza aquí
    /// para no duplicarla en los métodos públicos.
    /// </summary>
    private async Task<List<Haircut>> ExecuteQueryAsync(string groqQuery, CancellationToken ct)
    {
        try
        {
            string encodedQuery = Uri.EscapeDataString(groqQuery);

            // Construir la URL completa de la API de Sanity.
            // Formato: https://{projectId}.api.sanity.io/v{version}/data/query/{dataset}
            string requestUrl =
                $"https://{_settings.ProjectId}.api.sanity.io" +
                $"/v{_settings.ApiVersion}" +
                $"/data/query/{_settings.Dataset}" +
                $"?query={encodedQuery}";

            // Crear una nueva HttpRequestMessage por petición en lugar de modificar
            // DefaultRequestHeaders del cliente compartido, que NO es thread-safe.
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Añadir token solo si el dataset es privado; los públicos no lo necesitan.
            if (!string.IsNullOrEmpty(_settings.Token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.Token);
            }

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode(); // Lanza HttpRequestException para 4xx/5xx.

            var sanityResponse = await response.Content
                .ReadFromJsonAsync<SanityResponse<SanityHaircutDocument>>(JsonOptions, ct);

            // Result nunca debería ser null tras una respuesta 200, pero la guarda
            // defensiva evita NullReferenceException ante respuestas malformadas.
            if (sanityResponse?.Result is not { Count: > 0 })
            {
                _logger.LogInformation("La query GROQ devolvió 0 resultados.");
                return [];
            }

            return sanityResponse.Result
                .Select(MapToHaircut)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            // El componente Blazor se desmontó (navegación) antes de que la respuesta
            // llegara. No es un error real; re-lanzar para que el caller sepa.
            _logger.LogDebug("La llamada a Sanity fue cancelada.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Error de red (sin conexión, timeout, respuesta 4xx/5xx).
            _logger.LogError(ex, "Error de red al consultar Sanity.io. URL: {ProjectId}", _settings.ProjectId);
            return [];
        }
        catch (JsonException ex)
        {
            // La respuesta HTTP fue 200 pero el JSON no coincide con los DTOs.
            // Suele indicar un cambio en el schema de Sanity no reflejado en los DTOs.
            _logger.LogError(ex, "Error de deserialización. Verifica que los DTOs coincidan con el schema de Sanity.");
            return [];
        }
    }

    /// <summary>
    /// Función de mapeo pura: transforma el DTO de Sanity en el modelo de dominio.
    /// Centralizada como método privado para que los cambios futuros en el schema
    /// de Sanity solo requieran modificar este método, no los métodos públicos.
    /// </summary>
    private static Haircut MapToHaircut(SanityHaircutDocument doc) => new()
{
    Id             = doc.Id,
    Title          = doc.Title,
    Category       = doc.Category,
    Description    = doc.Description,
    // Forzamos a que la imagen principal baje adaptada a un tamaño estándar de porfolio (ej. 800px ancho)
    ImageUrl       = doc.ImageUrl.OptimizeSanityImage(width: 800, quality: 75),
    // Si hay foto del antes, la optimizamos igual
    BeforeImageUrl = doc.BeforeImageUrl?.OptimizeSanityImage(width: 800, quality: 75),
    DateCreated    = doc.CreatedAt
};
}