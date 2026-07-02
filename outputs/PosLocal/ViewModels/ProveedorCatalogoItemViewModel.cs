#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using PosLocal.Services;

namespace PosLocal.ViewModels;

public sealed class ProveedorCatalogoItemViewModel : ObservableObject
{
    public ProveedorCatalogoItemViewModel(ProveedorCatalogoDto dto)
    {
        Id = dto.Id;
        Nombre = dto.Nombre;
        Telefono = dto.Telefono;
        Email = dto.Email;
        Notas = dto.Notas;
        Activo = dto.Activo;
        ProductosAsignados = dto.ProductosAsignados;
    }

    public int Id { get; }

    public string Nombre { get; }

    public string? Telefono { get; }

    public string? Email { get; }

    public string? Notas { get; }

    public bool Activo { get; }

    public int ProductosAsignados { get; }
}
