using CompanyHierarchy.Api.Models;

namespace CompanyHierarchy.Api.Data;

public interface IPlazaEmpleadoRepository
{
    Task<IReadOnlyList<PlazaEmpleadoDto>> GetTreeAsync(int? codigoRaiz, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlazaEmpleadoDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<PlazaEmpleadoDto?> GetByCodeAsync(int codigoPuesto, CancellationToken cancellationToken);

    Task<PlazaEmpleadoDto> CreateAsync(PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken);

    Task<PlazaEmpleadoDto> UpdateAsync(int codigoPuesto, PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken);

    Task<PlazaEmpleadoDto> DeleteAsync(int codigoPuesto, CancellationToken cancellationToken);
}
