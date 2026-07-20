namespace CompanyHierarchy.Web.ViewModels;

public sealed class PlazaDeleteViewModel
{
    public int CodigoPuesto { get; set; }

    public string Puesto { get; set; } = string.Empty;

    public string NombreEmpleado { get; set; } = string.Empty;

    public string? JefeDescripcion { get; set; }
}
