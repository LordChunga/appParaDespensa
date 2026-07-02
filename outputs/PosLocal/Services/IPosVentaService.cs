#nullable enable

namespace PosLocal.Services;

public interface IPosVentaService
{
    Task<int> RegistrarVentaAsync(
        RegistrarVentaRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransferenciaPendienteDto>> ObtenerTransferenciasPendientesAsync(
        CancellationToken cancellationToken = default);

    Task AprobarTransferenciaAsync(
        int ventaId,
        CancellationToken cancellationToken = default);
}
