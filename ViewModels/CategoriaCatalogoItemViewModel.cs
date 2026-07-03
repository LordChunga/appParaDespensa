#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed partial class CategoriaCatalogoItemViewModel : ObservableObject
{
    public CategoriaCatalogoItemViewModel(CategoriaCatalogoDto dto)
    {
        Id = dto.Id;
        Nombre = dto.Nombre;
        Descripcion = dto.Descripcion;
        Activa = dto.Activa;
        ProductosAsignados = dto.ProductosAsignados;
    }

    public int Id { get; }

    public string Nombre { get; }

    public string? Descripcion { get; }

    public bool Activa { get; }

    public int ProductosAsignados { get; }

    [ObservableProperty]
    private bool _seleccionada;
}
