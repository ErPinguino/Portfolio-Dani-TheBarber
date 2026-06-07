using BarberPortfolio.Api.Models;
using BarberPortfolio.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// En el modelo Isolated Worker, la función es una app de consola .NET estándar.
// El host se configura aquí exactamente igual que en cualquier Worker Service de .NET.
var host = new HostBuilder()

    // ConfigureFunctionsWorkerDefaults() registra:
    // - El middleware del worker de Azure Functions.
    // - El serializador JSON por defecto (System.Text.Json).
    // - El binding de parámetros (HttpRequestData, etc.).
    // - La integración con ILogger via Application Insights (si el paquete está presente).
    .ConfigureFunctionsWorkerDefaults()

    .ConfigureServices((context, services) =>
    {
        // ── 1. CONFIGURACIÓN FUERTEMENTE TIPADA DE EMAIL ─────────────────────
        // Vincula la sección "Email" a EmailSettings.
        // En local: lee de local.settings.json > "Values" > "Email__*".
        // En Azure: lee de Application Settings con el mismo prefijo "Email__".
        //
        // ValidateDataAnnotations() valida [Required], [Range], etc.
        // ValidateOnStart() lanza una excepción en el arranque si falta algún valor,
        // evitando que la función falle la primera vez que se invoca en producción.
        services
            .AddOptions<EmailSettings>()
            .Bind(context.Configuration.GetSection(EmailSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ── 2. SERVICIO DE EMAIL ──────────────────────────────────────────────
        // Transient es correcto aquí: SmtpClient de MailKit NO es thread-safe
        // y cada invocación de la función debe tener su propia instancia con
        // su propia conexión SMTP independiente.
        services.AddTransient<IEmailService, SmtpEmailService>();

        // ── 3. APPLICATION INSIGHTS (opcional pero recomendado para producción) ─
        // Descomenta si añades el paquete:
        // Microsoft.Azure.Functions.Worker.ApplicationInsights
        //
        // services.AddApplicationInsightsTelemetryWorkerService();
        // services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();