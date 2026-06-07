using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using BarberPortfolio.Api.Models;
using BarberPortfolio.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BarberPortfolio.Api.Functions;

/// <summary>
/// Azure Function con HTTP Trigger que expone el endpoint POST /api/contact
/// para procesar los envíos del formulario de contacto del portfolio.
/// <para>
/// En Azure Static Web Apps, la ruta efectiva es:
/// <c>https://tu-app.azurestaticapps.net/api/contact</c>.
/// CORS está gestionado automáticamente por la plataforma ASWA para peticiones
/// desde el propio dominio estático; no requiere configuración adicional en la función.
/// </para>
/// </summary>
public sealed class ContactFormFunction
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactFormFunction> _logger;

    /// <summary>
    /// Opciones de deserialización JSON compartidas.
    /// <c>PropertyNameCaseInsensitive = true</c> acepta tanto camelCase (JS frontend)
    /// como PascalCase sin necesidad de atributos <c>[JsonPropertyName]</c> en el modelo.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // El constructor recibe las dependencias inyectadas por el contenedor IoC de la función.
    // En Isolated Worker, la DI funciona igual que en cualquier app genérica de .NET.
    public ContactFormFunction(IEmailService emailService, ILogger<ContactFormFunction> logger)
    {
        _emailService = emailService;
        _logger       = logger;
    }

    /// <summary>
    /// Punto de entrada de la función. Acepta únicamente peticiones POST en /api/contact.
    /// <para>
    /// Flujo:
    /// 1. Deserializa el body JSON al modelo <see cref="ContactMessage"/>.
    /// 2. Valida las DataAnnotations del modelo.
    /// 3. Envía el email mediante <see cref="IEmailService"/>.
    /// 4. Devuelve la respuesta HTTP apropiada en cada caso.
    /// </para>
    /// </summary>
    [Function(nameof(ContactFormFunction))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")]
        HttpRequestData req,
        CancellationToken ct)
    {
        // Registrar el origen de la petición sin loguear datos sensibles del body.
        // X-Forwarded-For es el header estándar de IP real detrás de proxies/CDN.
        string clientIp = req.Headers.TryGetValues("X-Forwarded-For", out var ipValues)
            ? ipValues.FirstOrDefault() ?? "unknown"
            : "unknown";

        _logger.LogInformation(
            "Petición POST /api/contact recibida desde {ClientIp}.", clientIp);

        // ── PASO 1: DESERIALIZACIÓN ──────────────────────────────────────────
        ContactMessage? message;
        try
        {
            message = await JsonSerializer.DeserializeAsync<ContactMessage>(
                req.Body, JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "JSON malformado en la petición de contacto desde {ClientIp}.", clientIp);
            return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new
            {
                success = false,
                error   = "El cuerpo de la petición no es JSON válido."
            });
        }

        if (message is null)
        {
            _logger.LogWarning("Petición de contacto con body vacío desde {ClientIp}.", clientIp);
            return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new
            {
                success = false,
                error   = "El cuerpo de la petición no puede estar vacío."
            });
        }

        // ── PASO 2: VALIDACIÓN DE DATAANNOTATIONS ────────────────────────────
        // TryValidateObject con validateAllProperties: true evalúa TODAS las anotaciones
        // del modelo en un solo pase, no solo [Required].
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(message);

        if (!Validator.TryValidateObject(message, validationContext, validationResults,
                validateAllProperties: true))
        {
            // Agrupar errores por campo para que el frontend pueda mapearlos
            // a los campos del formulario y mostrarlos inline.
            var errors = validationResults
                .GroupBy(vr => vr.MemberNames.FirstOrDefault() ?? "general",
                         StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(vr => vr.ErrorMessage).ToArray());

            _logger.LogWarning(
                "Validación fallida para contacto desde {ClientIp}: {ErrorCount} campo(s) inválido(s).",
                clientIp, errors.Count);

            return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new
            {
                success = false,
                errors
            });
        }

        // ── PASO 3: ENVÍO DEL EMAIL ──────────────────────────────────────────
        try
        {
            await _emailService.SendContactEmailAsync(message, ct);

            _logger.LogInformation(
                "Formulario de contacto procesado correctamente. Remitente: {Email}.",
                message.Email);

            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, new
            {
                success = true,
                message = "Tu mensaje ha sido enviado correctamente. ¡Te contactaremos pronto!"
            });
        }
        catch (OperationCanceledException)
        {
            // El cliente cerró la conexión antes de recibir la respuesta.
            // Re-lanzar para que el runtime de Azure Functions lo gestione.
            _logger.LogDebug(
                "Petición de contacto cancelada antes de completar el envío del email.");
            throw;
        }
        catch (Exception ex)
        {
            // Error en el servidor SMTP (credenciales, red, rechazo del servidor...).
            // Loguear el error completo internamente pero devolver al cliente solo un
            // mensaje genérico: no exponer detalles de infraestructura al exterior.
            _logger.LogError(ex,
                "Error al enviar email de contacto. Remitente: {Email}, IP: {ClientIp}.",
                message.Email, clientIp);

            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new
            {
                success = false,
                error   = "No se pudo enviar tu mensaje. Por favor, inténtalo de nuevo más tarde."
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una <see cref="HttpResponseData"/> con body JSON y el Content-Type correcto.
    /// Centralizado para garantizar que TODAS las respuestas de la función tengan
    /// el mismo formato de serialización y cabeceras.
    /// </summary>
    private static async Task<HttpResponseData> CreateJsonResponseAsync(
        HttpRequestData req, HttpStatusCode statusCode, object body)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteAsJsonAsync(body);
        return response;
    }
}