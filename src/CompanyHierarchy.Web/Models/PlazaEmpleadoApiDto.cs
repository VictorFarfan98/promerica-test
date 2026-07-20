namespace CompanyHierarchy.Web.Models;

public sealed class PlazaEmpleadoApiDto
{
    public int CodigoPuesto { get; set; }

    public string Puesto { get; set; } = string.Empty;

    public string NombreEmpleado { get; set; } = string.Empty;

    public int? CodigoJefe { get; set; }

    public string? PuestoJefe { get; set; }

    public string? NombreJefe { get; set; }

    public int? Nivel { get; set; }
}
