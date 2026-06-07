using System.ComponentModel.DataAnnotations;

namespace BarberPortfolio.Models;

/// <summary>
/// Opciones fuertemente tipadas para el CMS Sanity.io.
/// Se mapean desde la sección <c>"Sanity"</c> de <c>appsettings.json</c>
/// usando el patrón Options de .NET (<see cref="Microsoft.Extensions.Options.IOptions{T}"/>).
/// Las anotaciones de <c>DataAnnotations</c> permiten validar la configuración
/// en el arranque con <c>ValidateDataAnnotations()</c>.
/// </summary>
public sealed class SanitySettings
{
    /// <summary>
    /// Nombre de la sección en <c>appsettings.json</c>.
    /// Centralizado como constante para evitar strings mágicas en el contenedor IoC.
    /// </summary>
    public const string SectionName = "Sanity";

    /// <summary>
    /// ID del proyecto Sanity. Visible en <c>sanity.io/manage</c>.
    /// Ejemplo: <c>"abc12def"</c>.
    /// </summary>
    [Required]
    public required string ProjectId { get; init; }

    /// <summary>
    /// Dataset del proyecto, normalmente <c>"production"</c> o <c>"staging"</c>.
    /// </summary>
    [Required]
    public required string Dataset { get; init; }

    /// <summary>
    /// Versión de la API de Sanity en formato de fecha ISO (<c>"YYYY-MM-DD"</c>).
    /// Nunca usar <c>"latest"</c> en producción; fija una versión concreta para
    /// garantizar comportamiento estable ante actualizaciones del CMS.
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
        ErrorMessage = "ApiVersion debe tener formato YYYY-MM-DD, ej: '2024-01-15'.")]
    public string ApiVersion { get; init; } = "2024-01-15";

    /// <summary>
    /// Token de acceso de solo lectura (rol "Viewer") para datasets privados.
    /// <para>
    /// ⚠️ ADVERTENCIA DE SEGURIDAD: En Blazor WASM standalone, el archivo
    /// <c>wwwroot/appsettings.json</c> es descargado por el navegador y queda
    /// completamente expuesto al cliente. Recomendaciones:
    /// <list type="bullet">
    ///   <item>Para datasets públicos: dejar <c>null</c> y no usar token.</item>
    ///   <item>Para datos sensibles: implementar un patrón BFF
    ///     (Backend for Frontend) con ASP.NET Core que actúe como proxy seguro.</item>
    ///   <item>Si debes usar token en WASM: asegúrate de que sea de solo lectura.</item>
    /// </list>
    /// </para>
    /// </summary>
    public string? Token { get; init; }
}