using BarberPortfolio.Models;

namespace BarberPortfolio.Services;

/// <summary>
/// Implementación en memoria de <see cref="IPortfolioService"/> para desarrollo local.
/// Permite trabajar en la UI y en los componentes Blazor sin depender
/// de una conexión activa a Sanity.io ni de datos reales en el CMS.
/// </summary>
/// <remarks>
/// Registrar en <c>Program.cs</c> condicionalmente mediante <c>#if DEBUG</c>
/// o una bandera <c>"UseMock": true</c> en <c>appsettings.Development.json</c>.
/// Las URLs de imagen apuntan a <c>placehold.co</c> como servicio de placeholders.
/// </remarks>
public sealed class MockPortfolioService : IPortfolioService
{
    /// <summary>
    /// Dataset estático inicializado una sola vez. Al ser <c>static readonly</c>,
    /// la lista no se recrea en cada instancia del servicio (que en Blazor WASM
    /// con Scoped puede ser frecuente).
    /// Los datos simulan un portafolio realista con las tres categorías principales.
    /// </summary>
    private static readonly List<Haircut> MockData =
    [
        new()
        {
            Id          = "mock-001",
            Title       = "Mid Fade Clásico",
            Category    = "fade",
            Description = "Degradado medio limpio ejecutado con máquina y cero en las patillas. " +
                          "Transición suave y uniforme ideal para perfiles cuadrados y ovalados. " +
                          "Acabado con navaja recta en la nuca y contornos.",
            ImageUrl       = "https://placehold.co/800x1000/111827/ffffff?text=Mid+Fade",
            BeforeImageUrl = "https://placehold.co/800x1000/374151/ffffff?text=Antes",
            DateCreated    = new DateTime(2024, 5, 20, 10, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id          = "mock-002",
            Title       = "Perfilado de Barba Completo",
            Category    = "beard",
            Description = "Arreglo integral de barba con navaja recta: definición de líneas " +
                          "de mejillas y cuello, igualado de largos y acabado con aceite de argán " +
                          "nutritivo. Incluye mascarilla hidratante post-servicio.",
            ImageUrl       = "https://placehold.co/800x1000/111827/ffffff?text=Perfilado+Barba",
            BeforeImageUrl = null, // Sin foto "antes" para este trabajo
            DateCreated    = new DateTime(2024, 4, 15, 11, 30, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id          = "mock-003",
            Title       = "Buzz Cut con Diseño Geométrico",
            Category    = "design",
            Description = "Buzz cut al número 2 en toda la cabeza con diseño geométrico " +
                          "lateral grabado a navaja. Motivo minimalista de líneas paralelas " +
                          "y ángulo de 45°. Acabado con cera mate de fijación extra-fuerte.",
            ImageUrl       = "https://placehold.co/800x1000/111827/ffffff?text=Buzz+Cut+Design",
            BeforeImageUrl = "https://placehold.co/800x1000/374151/ffffff?text=Antes",
            DateCreated    = new DateTime(2024, 3, 8, 9, 0, 0, DateTimeKind.Utc)
        }
    ];

    /// <inheritdoc/>
    public Task<List<Haircut>> GetActivePortfolioAsync(CancellationToken ct = default)
    {
        // Task.FromResult evita la creación de una máquina de estados async innecesaria
        // para una operación que ya es síncrona. Sigue siendo awaitable.
        // OrderByDescending replica exactamente el comportamiento del servicio real
        // (query GROQ con | order(_createdAt desc)).
        var result = MockData
            .OrderByDescending(h => h.DateCreated)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<List<Haircut>> GetPortfolioByCategoryAsync(
        string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return GetActivePortfolioAsync(ct);

        var result = MockData
            .Where(h => h.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.DateCreated)
            .ToList();

        return Task.FromResult(result);
    }
}