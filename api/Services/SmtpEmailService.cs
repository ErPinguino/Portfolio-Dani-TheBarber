using System.Text.Encodings.Web;
using BarberPortfolio.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BarberPortfolio.Api.Services;

/// <summary>
/// Implementación de <see cref="IEmailService"/> que usa MailKit sobre SMTP.
/// Registrar como <c>Transient</c>: <c>SmtpClient</c> de MailKit no es thread-safe
/// y cada envío debe usar su propia instancia con conexión independiente.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger   = logger;
    }

    /// <inheritdoc/>
    public async Task SendContactEmailAsync(ContactMessage message, CancellationToken ct = default)
    {
        var email = BuildEmail(message);

        // SmtpClient de MailKit: crear uno por envío (ver comentario en la clase).
        // El bloque using garantiza Dispose() aunque ocurra una excepción.
        using var client = new SmtpClient();

        try
        {
            _logger.LogInformation(
                "Conectando a {Host}:{Port} para enviar email desde {Sender}.",
                _settings.SmtpHost, _settings.SmtpPort, _settings.SmtpUser);

            // SecureSocketOptions.Auto: MailKit detecta automáticamente StartTLS (puerto 587)
            // o SSL/TLS implícito (puerto 465) según el puerto configurado.
            // Usar SecureSocketOptions.None solo para SMTP locales de desarrollo (MailHog, etc.).
            var secureOptions = _settings.UseSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureOptions, ct);
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, ct);
            await client.SendAsync(email, ct);

            _logger.LogInformation(
                "Email de contacto enviado correctamente. Origen: {Email}, Destino: {Recipient}.",
                message.Email, _settings.RecipientEmail);
        }
        catch (AuthenticationException ex)
        {
            // Credenciales incorrectas o App Password expirada.
            // No loguear la contraseña nunca; solo el usuario.
            _logger.LogError(ex,
                "Autenticación SMTP fallida para el usuario {User}. " +
                "Verifica la contraseña o genera un nuevo App Password.",
                _settings.SmtpUser);
            throw new InvalidOperationException("Fallo en la autenticación SMTP.", ex);
        }
        catch (SmtpCommandException ex)
        {
            // El servidor SMTP aceptó la conexión pero rechazó el comando (ej: destinatario inválido).
            _logger.LogError(ex,
                "Comando SMTP rechazado. StatusCode: {StatusCode}, Mailbox: {Mailbox}.",
                ex.StatusCode, ex.Mailbox);
            throw new InvalidOperationException("El servidor SMTP rechazó el mensaje.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Error de red, TLS, timeout u otro no contemplado.
            _logger.LogError(ex, "Error inesperado al enviar email vía SMTP.");
            throw new InvalidOperationException("Error al enviar el email.", ex);
        }
        finally
        {
            // Desconexión educada del servidor SMTP (envía QUIT antes de cerrar el socket).
            if (client.IsConnected)
                await client.DisconnectAsync(quit: true, ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONSTRUCCIÓN DEL MENSAJE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye el <see cref="MimeMessage"/> con cuerpo dual (HTML + texto plano).
    /// El cuerpo dual garantiza compatibilidad con todos los clientes de correo:
    /// clientes modernos renderizan HTML; clientes legacy o lectores de accesibilidad usan texto plano.
    /// </summary>
    private MimeMessage BuildEmail(ContactMessage msg)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SmtpUser));
        email.To.Add(MailboxAddress.Parse(_settings.RecipientEmail));

        // Reply-To apunta al visitante para que el primo pueda responder directamente
        // con un solo clic desde su cliente de correo.
        email.ReplyTo.Add(new MailboxAddress(msg.Name, msg.Email));

        email.Subject = $"[Portfolio] Nuevo contacto de {msg.Name}";

        var body = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(msg),
            TextBody = BuildTextBody(msg)
        };
        email.Body = body.ToMessageBody();

        return email;
    }

    /// <summary>
    /// Genera el cuerpo HTML del email.
    /// HtmlEncoder.Default.Encode() sanitiza los valores del usuario para prevenir
    /// contenido malformado que rompa el layout del email (aunque los clientes de correo
    /// no ejecutan JS, el encoding protege contra HTML injection).
    /// </summary>
    private string BuildHtmlBody(ContactMessage msg)
    {
        var name    = HtmlEncoder.Default.Encode(msg.Name);
        var email   = HtmlEncoder.Default.Encode(msg.Email);
        var message = HtmlEncoder.Default.Encode(msg.Message)
                          .Replace("&#xA;", "<br>"); // Preservar saltos de línea en HTML.

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f5;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0"
                           style="background-color:#ffffff;border-radius:8px;overflow:hidden;
                                  box-shadow:0 1px 3px rgba(0,0,0,.12);">

                      <!-- Cabecera -->
                      <tr>
                        <td style="background-color:#111827;padding:24px 32px;">
                          <h1 style="margin:0;color:#ffffff;font-size:20px;font-weight:600;
                                     letter-spacing:-.3px;">
                            ✂️ Nuevo mensaje de contacto
                          </h1>
                        </td>
                      </tr>

                      <!-- Cuerpo -->
                      <tr>
                        <td style="padding:32px;">
                          <table width="100%" cellpadding="0" cellspacing="0">

                            <!-- Nombre -->
                            <tr>
                              <td style="padding:0 0 20px 0;">
                                <p style="margin:0 0 4px 0;font-size:11px;font-weight:600;
                                          text-transform:uppercase;letter-spacing:.8px;color:#6b7280;">
                                  Nombre
                                </p>
                                <p style="margin:0;font-size:16px;color:#111827;">{name}</p>
                              </td>
                            </tr>

                            <!-- Email -->
                            <tr>
                              <td style="padding:0 0 20px 0;border-top:1px solid #f3f4f6;padding-top:20px;">
                                <p style="margin:0 0 4px 0;font-size:11px;font-weight:600;
                                          text-transform:uppercase;letter-spacing:.8px;color:#6b7280;">
                                  Email
                                </p>
                                <p style="margin:0;font-size:16px;">
                                  <a href="mailto:{email}" style="color:#2563eb;text-decoration:none;">{email}</a>
                                </p>
                              </td>
                            </tr>

                            <!-- Mensaje -->
                            <tr>
                              <td style="border-top:1px solid #f3f4f6;padding-top:20px;">
                                <p style="margin:0 0 8px 0;font-size:11px;font-weight:600;
                                          text-transform:uppercase;letter-spacing:.8px;color:#6b7280;">
                                  Mensaje
                                </p>
                                <p style="margin:0;font-size:15px;color:#374151;line-height:1.6;
                                          background-color:#f9fafb;border-radius:6px;padding:16px;">
                                  {message}
                                </p>
                              </td>
                            </tr>

                          </table>
                        </td>
                      </tr>

                      <!-- Pie -->
                      <tr>
                        <td style="background-color:#f9fafb;padding:16px 32px;border-top:1px solid #f3f4f6;">
                          <p style="margin:0;font-size:12px;color:#9ca3af;text-align:center;">
                            Mensaje enviado desde el formulario de contacto del portfolio web.
                            Responde directamente a este email para contactar con el visitante.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Versión en texto plano del email para clientes legacy y lectores de pantalla.
    /// Sin sanitización HTML aquí (no necesaria en texto plano).
    /// </summary>
    private static string BuildTextBody(ContactMessage msg) => $"""
        ✂️ NUEVO MENSAJE DE CONTACTO — PORTFOLIO BARBERO
        ═════════════════════════════════════════════════

        Nombre:  {msg.Name}
        Email:   {msg.Email}

        Mensaje:
        ─────────────────────────────────────────────────
        {msg.Message}
        ─────────────────────────────────────────────────

        Responde directamente a este email para contactar con el visitante.
        """;
}