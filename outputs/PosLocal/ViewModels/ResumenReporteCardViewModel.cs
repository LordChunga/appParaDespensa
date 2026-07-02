#nullable enable

namespace PosLocal.ViewModels;

public sealed class ResumenReporteCardViewModel
{
    public ResumenReporteCardViewModel(string icono, string titulo, string valor, string colorClave, string? subtitulo = null)
    {
        Icono = icono;
        Titulo = titulo;
        Valor = valor;
        ColorClave = colorClave;
        Subtitulo = subtitulo;
    }

    public string Icono { get; }

    public string Titulo { get; }

    public string Valor { get; }

    public string ColorClave { get; }

    public string? Subtitulo { get; }
}
