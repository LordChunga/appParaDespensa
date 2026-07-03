#nullable enable

namespace PosLocal.Services;

public interface IDatabaseSeederService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
