namespace CompanyHierarchy.Api.Models;

public sealed class PlazaEmpleadoUpsertRequest
{
    public string Puesto { get; set; } = string.Empty;

    public string NombreEmpleado { get; set; } = string.Empty;

    public int? CodigoJefe { get; set; }
}
