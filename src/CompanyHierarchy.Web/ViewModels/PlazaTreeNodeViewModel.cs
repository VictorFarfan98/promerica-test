namespace CompanyHierarchy.Web.ViewModels;

public sealed class PlazaTreeNodeViewModel
{
    public int CodigoPuesto { get; set; }

    public string Puesto { get; set; } = string.Empty;

    public string NombreEmpleado { get; set; } = string.Empty;

    public int? CodigoJefe { get; set; }

    public List<PlazaTreeNodeViewModel> Children { get; } = new();
}
