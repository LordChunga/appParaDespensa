#nullable enable

namespace PosLocal.ViewModels;

public sealed class KpiReporteCardViewModel
{
    public KpiReporteCardViewModel(
        string icono,
        string titulo,
        string valor,
        string? ayuda = null,
        string iconFontFamily = "Segoe UI")
    {
        Icono = icono;
        Titulo = titulo;
        Valor = valor;
        Ayuda = ayuda;
        IconFontFamily = iconFontFamily;
    }

    public string Icono { get; }

    public string Titulo { get; }

    public string Valor { get; }

    public string? Ayuda { get; }

    public string IconFontFamily { get; }
}
