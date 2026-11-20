using CatalogoMaui.Services;
using SQLite;

namespace CatalogoMaui.Models;

[Table("Productos")]
public class Producto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public long PrecioCentavos { get; set; }

    [Ignore]
    public decimal Precio
    {
        get => PrecioCentavos / 100m;
        set => PrecioCentavos = (long)Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero);
    }

    public string Detalle { get; set; } = "";
    public string Foto { get; set; } = "";

    [Ignore]
    public string FotoVisible => FotosProducto.Resolver(Foto);
}
