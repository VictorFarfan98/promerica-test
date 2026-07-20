using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyHierarchy.Web.ViewModels;

public sealed class PlazaFormViewModel
{
    public int? CodigoPuesto { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Puesto")]
    public string Puesto { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Nombre del empleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    [Display(Name = "Jefe")]
    public int? CodigoJefe { get; set; }

    public string? Message { get; set; }

    public IEnumerable<SelectListItem> Jefes { get; set; } = Array.Empty<SelectListItem>();
}
