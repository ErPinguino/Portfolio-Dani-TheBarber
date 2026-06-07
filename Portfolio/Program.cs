using BarberPortfolio;
using Microsoft.AspNetCore.Components.Web;
using BarberPortfolio.Models;
using BarberPortfolio.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── 1. CONFIGURACIÓN DE SANITY ──────────────────────────────
builder.Services
    .AddOptions<SanitySettings>()
    .Bind(builder.Configuration.GetSection(SanitySettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── 2. HTTPCLIENT BASE ──────────────────────────────────────
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ── 3. SERVICIO DE PORTFOLIO ────────────────────────────────
#if DEBUG
builder.Services.AddScoped<IPortfolioService, MockPortfolioService>();
#else
builder.Services.AddHttpClient<IPortfolioService, SanityPortfolioService>();
#endif

await builder.Build().RunAsync();