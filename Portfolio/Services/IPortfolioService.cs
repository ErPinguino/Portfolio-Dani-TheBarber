using BarberPortfolio.Models;

namespace BarberPortfolio.Services;

/// <summary>
/// Contrato del servicio de portafolio del barbero.
/// Desacopla completamente la capa de presentación (componentes Blazor)
/// de cualquier implementación concreta de acceso a datos.
/// Esto permite intercambiar Sanity por otro CMS, BD, o un Mock sin
/// tocar ningún componente de la UI.
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Obtiene todos los cortes activos del portafolio, ordenados por fecha descendente.
    /// Un corte "activo" tiene el campo <c>active: true</c> en el CMS.
    /// </summary>
    /// <param name="ct">Token de cancelación para abortar la operación si el
    /// componente se desmonta antes de que la llamada HTTP complete.</param>
    /// <returns>
    /// Lista de <see cref="Haircut"/> ordenada por <see cref="Haircut.DateCreated"/> desc.
    /// Devuelve una lista vacía (no null) ante cualquier error.
    /// </returns>
    Task<List<Haircut>> GetActivePortfolioAsync(CancellationToken ct = default);

    /// <summary>
    /// Filtra el portafolio activo por categoría.
    /// </summary>
    /// <param name="category">
    /// Slug de categoría definido en el CMS, ej: <c>"fade"</c>, <c>"beard"</c>, <c>"design"</c>.
    /// Si es <c>null</c> o vacío, delega a <see cref="GetActivePortfolioAsync"/>.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// Lista filtrada de <see cref="Haircut"/> o lista vacía si no hay resultados / error.
    /// </returns>
    Task<List<Haircut>> GetPortfolioByCategoryAsync(string category, CancellationToken ct = default);
}