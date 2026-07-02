#nullable enable

namespace PosLocal.Services;

public sealed class AppSettings
{
    public bool HabilitarSistemaCaja { get; set; } = true;

    public bool GuardadoItemsRemovidos { get; set; }

    public bool ProductosConImagenes { get; set; }

    public bool CalculoPrecioPorCosto { get; set; }

    public bool ConfigurarImpresion { get; set; }

    public bool RedondeoPreciosPesoVolumen { get; set; }

    public bool CatalogoOnlineActivo { get; set; }

    public bool ListasDePrecios { get; set; }

    public bool SistemaBalanza { get; set; } = true;

    public bool ModuloIa { get; set; }
}
