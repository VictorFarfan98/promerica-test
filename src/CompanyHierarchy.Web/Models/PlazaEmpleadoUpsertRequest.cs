using System.ComponentModel.DataAnnotations;

namespace CompanyHierarchy.Web.Models;

public sealed class PlazaEmpleadoUpsertRequest
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
}
