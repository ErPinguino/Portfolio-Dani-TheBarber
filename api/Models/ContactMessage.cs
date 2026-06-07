using System.ComponentModel.DataAnnotations;

namespace BarberPortfolio.Api.Models;

/// <summary>
/// Modelo que representa los datos recibidos desde el formulario de contacto.
/// Usa DataAnnotations para la validación declarativa, que el controlador
/// de la función ejecuta mediante <see cref="Validator.TryValidateObject"/>.
/// Inmutable por diseño: los datos del formulario no deben modificarse tras la recepción.
/// </summary>
public sealed class ContactMessage
{
    /// <summary>Nombre completo del visitante.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public required string Name { get; init; }

    /// <summary>
    /// Dirección de correo electrónico del visitante.
    /// <c>[EmailAddress]</c> valida el formato RFC 5322 básico.
    /// En producción considerar validación adicional mediante DNS MX lookup.
    /// </summary>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    [StringLength(254, ErrorMessage = "El email no puede superar los 254 caracteres.")]
    public required string Email { get; init; }

    /// <summary>Cuerpo del mensaje libre. Longitud limitada para prevenir payloads abusivos.</summary>
    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "El mensaje debe tener entre 10 y 2000 caracteres.")]
    public required string Message { get; init; }
}