using System.Globalization;
using CatalogoMaui.Models;
using CatalogoMaui.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CatalogoMaui.Views;

public partial class ProductoDetallePage : ContentPage, IQueryAttributable
{
    private int id;
    private string fotoOriginal = "";
    private string? rutaFotoTemporal;

    public ProductoDetallePage()
    {
        InitializeComponent();
        Title = "Agregar producto";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("producto", out var dato) || dato is not Producto p)
            return;

        id = p.Id;
        fotoOriginal = p.Foto;
        rutaFotoTemporal = null;

        Title = "Editar producto";
        Nombre.Text = p.Nombre;
        Descripcion.Text = p.Descripcion;
        Detalle.Text = p.Detalle;
        Precio.Text = p.Precio.ToString("0.00", CultureInfo.CurrentCulture);

        VistaFoto.Source = p.FotoVisible;
        VistaFoto.IsVisible = !string.IsNullOrWhiteSpace(p.FotoVisible);
        Eliminar.IsVisible = true;
    }

    private async void AlElegirFoto(object? sender, EventArgs e)
    {
        if (!ElegirFoto.IsEnabled)
            return;

        ElegirFoto.IsEnabled = false;
        try
        {
            string? rutaTemporal = await FotosProducto.ElegirAsync();
            if (string.IsNullOrEmpty(rutaTemporal))
                return;

            rutaFotoTemporal = rutaTemporal;
            VistaFoto.Source = rutaTemporal;
            VistaFoto.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se cargó la foto", ex.Message, "Aceptar");
        }
        finally
        {
            ElegirFoto.IsEnabled = true;
        }
    }

    private async void AlGuardar(object? sender, EventArgs e)
    {
        if (!Guardar.IsEnabled)
            return;

        var textoPrecio = Precio.Text?.Trim().Replace(',', '.');
        bool tieneFoto = !string.IsNullOrEmpty(rutaFotoTemporal)
                         || !string.IsNullOrEmpty(fotoOriginal);

        if (string.IsNullOrWhiteSpace(Nombre.Text) ||
            string.IsNullOrWhiteSpace(Descripcion.Text) ||
            string.IsNullOrWhiteSpace(Detalle.Text) ||
            !decimal.TryParse(textoPrecio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var importe) ||
            importe < 0 || importe > 99999999.99m ||
            decimal.Round(importe, 2) != importe ||
            !tieneFoto)
        {
            await DisplayAlert("Revisa los datos",
                "Completa todos los campos, selecciona una foto y escribe un precio válido de hasta dos decimales.", "Aceptar");
            return;
        }

        string? nuevaFotoGuardada = null;

        await EjecutarAsync(async () =>
        {
            string fotoFinal;

            if (!string.IsNullOrEmpty(rutaFotoTemporal))
            {
                nuevaFotoGuardada = await FotosProducto.GuardarAsync(rutaFotoTemporal);
                fotoFinal = nuevaFotoGuardada;
            }
            else
            {
                fotoFinal = fotoOriginal;
            }

            var producto = new Producto
            {
                Id = id,
                Nombre = Nombre.Text.Trim(),
                Descripcion = Descripcion.Text.Trim(),
                Detalle = Detalle.Text.Trim(),
                Precio = importe,
                Foto = fotoFinal
            };

            try
            {
                if (id == 0)
                {
                    await ProductosSqlite.Instancia.Insertar(producto);
                }
                else if (!await ProductosSqlite.Instancia.Actualizar(producto))
                {
                    throw new InvalidOperationException("El producto ya no existe.");
                }
            }
            catch
            {
                if (!string.IsNullOrEmpty(nuevaFotoGuardada))
                    FotosProducto.EliminarSiEsPropia(nuevaFotoGuardada);
                throw;
            }

            if (!string.IsNullOrEmpty(fotoOriginal) &&
                !string.Equals(fotoOriginal, fotoFinal, StringComparison.OrdinalIgnoreCase))
            {
                FotosProducto.EliminarSiEsPropia(fotoOriginal);
            }

            return true;
        });
    }

    private async void AlEliminar(object? sender, EventArgs e)
    {
        if (!Eliminar.IsEnabled || id == 0)
            return;

        await EjecutarAsync(async () =>
        {
            if (!await DisplayAlert("Eliminar producto", "¿Deseas eliminar este producto?",
                "Eliminar", "Cancelar"))
                return false;

            if (!await ProductosSqlite.Instancia.Eliminar(id))
                throw new InvalidOperationException("El producto ya no existe.");

            FotosProducto.EliminarSiEsPropia(fotoOriginal);
            return true;
        });
    }

    private async Task EjecutarAsync(Func<Task<bool>> operacion)
    {
        Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = false;
        try
        {
            if (await operacion())
                await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se completó la operación", ex.Message, "Aceptar");
        }
        finally
        {
            Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = true;
        }
    }
}
