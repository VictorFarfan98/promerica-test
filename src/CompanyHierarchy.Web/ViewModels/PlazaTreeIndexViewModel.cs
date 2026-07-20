namespace CompanyHierarchy.Web.ViewModels;

public sealed class PlazaTreeIndexViewModel
{
    public string? Message { get; set; }

    public int? RootCodigo { get; set; }

    public IReadOnlyList<PlazaTreeNodeViewModel> Tree { get; set; } = Array.Empty<PlazaTreeNodeViewModel>();
}
