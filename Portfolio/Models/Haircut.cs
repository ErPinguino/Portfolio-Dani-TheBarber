namespace BarberPortfolio.Models;

/// <summary>
/// Modelo de dominio principal que representa un corte de cabello en el portafolio.
/// Completamente desacoplado de cualquier proveedor de datos (Sanity, BD, etc.).
/// Usa <c>init</c> + <c>required</c> (.NET 8) para inmutabilidad y seguridad
/// en la inicialización sin necesidad de constructores explícitos.
/// </summary>
public sealed class Haircut
{
    /// <summary>Identificador único proveniente del CMS (_id en Sanity).</summary>
    public required string Id { get; init; }

    /// <summary>Nombre visible del corte, ej: "Mid Fade Clásico".</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Slug de categoría definido en el CMS, ej: "fade", "beard", "design".
    /// Se usa como filtro en <see cref="IPortfolioService.GetPortfolioByCategoryAsync"/>.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>Descripción larga del servicio o técnica aplicada.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>URL pública de la imagen principal del resultado final.</summary>
    public required string ImageUrl { get; init; }

    /// <summary>
    /// URL de la imagen "antes del corte". Nullable porque no todos los trabajos
    /// tienen comparativa. La UI debe manejar explícitamente su ausencia.
    /// </summary>
    public string? BeforeImageUrl { get; init; }

    /// <summary>Fecha de creación del documento en el CMS (_createdAt de Sanity, UTC).</summary>
    public DateTime DateCreated { get; init; }
}