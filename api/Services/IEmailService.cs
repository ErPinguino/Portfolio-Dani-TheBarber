using BarberPortfolio.Api.Models;

namespace BarberPortfolio.Api.Services;

/// <summary>
/// Contrato del servicio de envío de correo electrónico.
/// Abstraer el envío detrás de una interfaz permite:
/// <list type="bullet">
///   <item>Sustituir MailKit/SMTP por SendGrid, AWS SES u otro proveedor sin tocar la función.</item>
///   <item>Usar una implementación fake en tests unitarios sin enviar emails reales.</item>
/// </list>
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía el mensaje de contacto al email configurado en <c>Email__RecipientEmail</c>.
    /// </summary>
    /// <param name="message">Datos validados del formulario de contacto.</param>
    /// <param name="ct">Token de cancelación para abortar la operación SMTP en curso.</param>
    /// <exception cref="InvalidOperationException">
    /// Si el servidor SMTP rechaza la conexión, las credenciales o el envío.
    /// La función HTTP debe capturar esta excepción y devolver un 500.
    /// </exception>
    Task SendContactEmailAsync(ContactMessage message, CancellationToken ct = default);
}