using CatalogoMaui.Models;
using CatalogoMaui.ViewModels;

namespace CatalogoMaui;

public partial class MainPage : ContentPage
{
    private readonly CatalogoViewModel vm = new();
    private bool abriendoDetalle;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await vm.CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se cargó el catálogo",
                $"Revisa el almacenamiento disponible del dispositivo. {ex.Message}", "Aceptar");
        }
    }

    private async void AlAgregarProducto(object? sender, EventArgs e)
    {
        if (abriendoDetalle)
            return;

        abriendoDetalle = true;
        try
        {
            await Shell.Current.GoToAsync("producto-detalle");
        }
        finally
        {
            abriendoDetalle = false;
        }
    }

    private async void AlSeleccionarProducto(object? sender, SelectionChangedEventArgs e)
    {
        if (abriendoDetalle || e.CurrentSelection.Count == 0)
            return;

        if (sender is not CollectionView lista || e.CurrentSelection[0] is not Producto producto)
            return;

        abriendoDetalle = true;

        try
        {
            lista.SelectedItem = null;

            var datos = new ShellNavigationQueryParameters
            {
                { "producto", producto }
            };

            await Shell.Current.GoToAsync("producto-detalle", datos);
        }
        finally
        {
            abriendoDetalle = false;
        }
    }
}
