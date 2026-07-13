using CapitalPos.Application.ConfiguracionFiscal;
using CapitalPos.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapitalPos.Infrastructure.Persistence.Repositories;

public sealed class EfConfiguracionFiscalEmpresaRepository : IConfiguracionFiscalEmpresaRepository
{
    private readonly CapitalPosDbContext _dbContext;

    public EfConfiguracionFiscalEmpresaRepository(CapitalPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConfiguracionFiscalEmpresa?> ObtenerPorEmpresaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ConfiguracionesFiscalesEmpresas
            .SingleOrDefaultAsync(
                configuracion => configuracion.EmpresaId == empresaId,
                cancellationToken);
    }

    public async Task GuardarAsync(
        ConfiguracionFiscalEmpresa configuracion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var exists = await _dbContext.ConfiguracionesFiscalesEmpresas
            .AnyAsync(
                actual => actual.EmpresaId == configuracion.EmpresaId,
                cancellationToken);
        if (exists)
        {
            _dbContext.ConfiguracionesFiscalesEmpresas.Update(configuracion);
        }
        else
        {
            await _dbContext.ConfiguracionesFiscalesEmpresas.AddAsync(
                configuracion,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
