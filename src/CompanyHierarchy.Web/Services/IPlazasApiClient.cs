using CompanyHierarchy.Web.Models;

namespace CompanyHierarchy.Web.Services;

public interface IPlazasApiClient
{
    Task<IReadOnlyList<PlazaEmpleadoApiDto>> GetTreeAsync(int? codigoRaiz, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlazaEmpleadoApiDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<PlazaEmpleadoApiDto?> GetByCodeAsync(int codigoPuesto, CancellationToken cancellationToken);

    Task<PlazaEmpleadoApiDto> CreateAsync(PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken);

    Task<PlazaEmpleadoApiDto> UpdateAsync(int codigoPuesto, PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken);

    Task<PlazaEmpleadoApiDto> DeleteAsync(int codigoPuesto, CancellationToken cancellationToken);
}
