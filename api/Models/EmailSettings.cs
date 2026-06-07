using System.ComponentModel.DataAnnotations;

namespace BarberPortfolio.Api.Models;

/// <summary>
/// Opciones de configuración del servidor SMTP para el envío de correos.
/// Se mapean desde variables de entorno con el prefijo <c>"Email__"</c>
/// (doble guión bajo = separador de sección en .NET para env vars).
/// Ejemplo: la variable <c>Email__SmtpHost</c> llena la propiedad <see cref="SmtpHost"/>.
/// <para>
/// En local: se definen en <c>local.settings.json</c> > <c>Values</c>.
/// En Azure: en el panel "Configuration" > "Application settings" de la Static Web App.
/// </para>
/// </summary>
public sealed class EmailSettings
{
    /// <summary>Clave de sección en la configuración de .NET.</summary>
    public const string SectionName = "Email";

    /// <summary>Hostname del servidor SMTP. Ej: <c>"smtp.gmail.com"</c>.</summary>
    [Required(ErrorMessage = "Email__SmtpHost es obligatorio.")]
    public required string SmtpHost { get; init; }

    /// <summary>
    /// Puerto SMTP. Valores estándar:
    /// <list type="bullet">
    ///   <item>587 — StartTLS (recomendado)</item>
    ///   <item>465 — SSL/TLS implícito</item>
    ///   <item>25  — Sin cifrado (solo redes internas)</item>
    /// </list>
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Email__SmtpPort debe ser un puerto válido (1–65535).")]
    public int SmtpPort { get; init; } = 587;

    /// <summary>Usuario de autenticación SMTP (normalmente el email del remitente).</summary>
    [Required(ErrorMessage = "Email__SmtpUser es obligatorio.")]
    [EmailAddress(ErrorMessage = "Email__SmtpUser debe ser una dirección de email válida.")]
    public required string SmtpUser { get; init; }

    /// <summary>
    /// Contraseña o App Password del servidor SMTP.
    /// Para Gmail con 2FA: generar en "Gestionar cuenta" > "Contraseñas de aplicaciones".
    /// </summary>
    [Required(ErrorMessage = "Email__SmtpPassword es obligatorio.")]
    public required string SmtpPassword { get; init; }

    /// <summary>
    /// Email de destino donde se recibirán los mensajes del formulario (el primo del barbero).
    /// Desacoplado del usuario SMTP para permitir reenvíos a cualquier buzón.
    /// </summary>
    [Required(ErrorMessage = "Email__RecipientEmail es obligatorio.")]
    [EmailAddress(ErrorMessage = "Email__RecipientEmail debe ser una dirección de email válida.")]
    public required string RecipientEmail { get; init; }

    /// <summary>Nombre visible del remitente en el cliente de correo del destinatario.</summary>
    public string SenderName { get; init; } = "Portfolio Barbero";

    /// <summary>
    /// Activa TLS/StartTLS en la conexión SMTP.
    /// Poner en <c>false</c> solo en entornos de test con servidores SMTP locales (ej: MailHog).
    /// </summary>
    public bool UseSsl { get; init; } = true;
}