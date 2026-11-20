namespace CatalogoMaui.Services;

public static class FotosProducto
{
    private const long TamanoMaximoBytes = 15L * 1024L * 1024L;
    private const string CarpetaFotos = "fotos_productos";
    private const string PrefijoArchivo = "producto_";

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    public static async Task<string?> ElegirAsync()
    {
        var opciones = new PickOptions
        {
            PickerTitle = "Selecciona una imagen",
            FileTypes = FilePickerFileType.Images
        };

        var resultado = await FilePicker.Default.PickAsync(opciones);
        if (resultado is null)
            return null;

        string extension = Path.GetExtension(resultado.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            throw new InvalidOperationException("Solo se permiten imágenes JPG, JPEG o PNG.");

        string nombreTemporal = $"preview_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        string rutaTemporal = Path.Combine(FileSystem.CacheDirectory, nombreTemporal);

        try
        {
            await using var origen = await resultado.OpenReadAsync();
            await using var destino = File.Create(rutaTemporal);

            byte[] buffer = new byte[81920];
            long totalLeido = 0;
            int leidos;

            while ((leidos = await origen.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalLeido += leidos;
                if (totalLeido > TamanoMaximoBytes)
                    throw new InvalidOperationException("La imagen no debe superar los 15 MB.");

                await destino.WriteAsync(buffer, 0, leidos);
            }
        }
        catch
        {
            if (File.Exists(rutaTemporal))
                File.Delete(rutaTemporal);
            throw;
        }

        return rutaTemporal;
    }

    public static async Task<string> GuardarAsync(string rutaTemporal)
    {
        if (string.IsNullOrWhiteSpace(rutaTemporal) || !File.Exists(rutaTemporal))
            throw new FileNotFoundException("No se encontró la imagen seleccionada.");

        string extension = Path.GetExtension(rutaTemporal).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            extension = ".jpg";

        string carpetaDestino = Path.Combine(FileSystem.AppDataDirectory, CarpetaFotos);
        Directory.CreateDirectory(carpetaDestino);

        string nombreArchivo = $"{PrefijoArchivo}{Guid.NewGuid():N}{extension}";
        string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);

        await using (var origen = File.OpenRead(rutaTemporal))
        await using (var destino = File.Create(rutaDestino))
        {
            await origen.CopyToAsync(destino);
        }

        try
        {
            if (rutaTemporal.StartsWith(FileSystem.CacheDirectory, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(rutaTemporal))
            {
                File.Delete(rutaTemporal);
            }
        }
        catch
        {
        }

        return nombreArchivo;
    }

    public static string Resolver(string? foto)
    {
        if (string.IsNullOrWhiteSpace(foto))
            return "";

        if (Path.IsPathRooted(foto))
            return foto;

        if (foto.StartsWith(PrefijoArchivo, StringComparison.OrdinalIgnoreCase))
            return Path.Combine(FileSystem.AppDataDirectory, CarpetaFotos, foto);

        return foto;
    }

    public static void EliminarSiEsPropia(string? foto)
    {
        if (string.IsNullOrWhiteSpace(foto) ||
            !foto.StartsWith(PrefijoArchivo, StringComparison.OrdinalIgnoreCase))
            return;

        string ruta = Path.Combine(FileSystem.AppDataDirectory, CarpetaFotos, foto);
        if (File.Exists(ruta))
            File.Delete(ruta);
    }
}
