using CatalogoMaui.Models;
using SQLite;

namespace CatalogoMaui.Services;

public sealed class ProductosSqlite
{
    public static ProductosSqlite Instancia { get; } = new();

    private readonly Lazy<Task<SQLiteAsyncConnection>> conexion;

    private ProductosSqlite() => conexion = new(AbrirAsync);

    private static async Task<SQLiteAsyncConnection> AbrirAsync()
    {
        string ruta = Path.Combine(FileSystem.AppDataDirectory, "catalogo.db3");
        var db = new SQLiteAsyncConnection(ruta);

        await db.RunInTransactionAsync(c =>
        {
            c.CreateTable<Producto>();

            if (c.ExecuteScalar<int>("PRAGMA user_version") != 0)
                return;

            c.InsertAll(new[]
            {
                new Producto
                {
                    Nombre = "Mochila urbana",
                    Descripcion = "Con espacio para laptop y cuadernos.",
                    Detalle = "Mochila para uso diario. Incluye un compartimento acolchado para laptop y un bolsillo frontal para objetos pequeños.",
                    Precio = 499.00m,
                    Foto = "mochila.png"
                },
                new Producto
                {
                    Nombre = "Libreta A5",
                    Descripcion = "80 hojas rayadas con pasta dura.",
                    Detalle = "Libreta de tamaño A5 con 80 hojas rayadas. Su pasta dura protege los apuntes dentro de la mochila.",
                    Precio = 89.50m,
                    Foto = "cuaderno.png"
                },
                new Producto
                {
                    Nombre = "Termo de acero",
                    Descripcion = "Botella reutilizable de 500 ml.",
                    Detalle = "Termo de acero de 500 ml, con tapa de rosca. Su tamaño permite llevarlo en el bolsillo lateral de una mochila.",
                    Precio = 219.00m,
                    Foto = "termo.png"
                },
                new Producto
                {
                    Nombre = "Lámpara de lava",
                    Descripcion = "Con brillo dorado y base metálica.",
                    Detalle = "Lámpara decorativa tipo cohete con líquido de brillantina dorada y base metálica cromada. Incluye cable de corriente con interruptor integrado para escritorio o recámara.",
                    Precio = 349.00m,
                    Foto = "lampara.jpg"
                }
            });

            c.Execute("PRAGMA user_version = 1");
        });

        return db;
    }

    public async Task<List<Producto>> Consultar() =>
        await (await conexion.Value).Table<Producto>().OrderBy(p => p.Id).ToListAsync();

    public async Task<int> Insertar(Producto producto) =>
        await (await conexion.Value).InsertAsync(producto);

    public async Task<bool> Actualizar(Producto producto) =>
        await (await conexion.Value).UpdateAsync(producto) > 0;

    public async Task<bool> Eliminar(int id) =>
        await (await conexion.Value).DeleteAsync<Producto>(id) > 0;
}
