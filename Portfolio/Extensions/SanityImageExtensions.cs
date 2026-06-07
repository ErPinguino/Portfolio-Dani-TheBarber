namespace BarberPortfolio.Extensions;

public static class SanityImageExtensions
{
    /// <summary>
    /// Optimiza una URL de imagen del CDN de Sanity añadiendo parámetros de transformación.
    /// </summary>
    /// <param name="url">URL original de la imagen.</param>
    /// <param name="width">Ancho opcional en píxeles.</param>
    /// <param name="height">Alto opcional en píxeles.</param>
    /// <param name="format">Formato automático (por defecto 'webp' para máxima compresión).</param>
    /// <param name="quality">Calidad de la compresión de 0 a 100 (por defecto 80, equilibrio perfecto).</param>
    public static string OptimizeSanityImage(
        this string url, 
        int? width = null, 
        int? height = null, 
        string format = "webp", 
        int? quality = 80)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // Validar que realmente sea una URL del CDN de Sanity para no romper enlaces externos
        if (!url.Contains("cdn.sanity.io", StringComparison.OrdinalIgnoreCase))
            return url;

        var queryParams = new List<string>();

        if (width.HasValue) 
            queryParams.Add($"w={width.Value}");
            
        if (height.HasValue) 
            queryParams.Add($"h={height.Value}");
            
        if (!string.IsNullOrWhiteSpace(format)) 
            queryParams.Add($"auto={format}");
            
        if (quality.HasValue) 
            queryParams.Add($"q={quality.Value}");

        if (queryParams.Count == 0)
            return url;

        // Comprobamos si la URL ya tiene alguna query string previa para concatenar bien con '?' o '&'
        string separator = url.Contains('?') ? "&" : "?";
        
        return $"{url}{separator}{string.Join("&", queryParams)}";
    }
}