using CompanyHierarchy.Web.Models;
using CompanyHierarchy.Web.Services;
using CompanyHierarchy.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyHierarchy.Web.Controllers;

public sealed class PlazasController : Controller
{
    private readonly IPlazasApiClient _apiClient;
    private readonly ILogger<PlazasController> _logger;

    public PlazasController(IPlazasApiClient apiClient, ILogger<PlazasController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int? codigoRaiz, CancellationToken cancellationToken)
    {
        var flatTree = await _apiClient.GetTreeAsync(codigoRaiz, cancellationToken);

        var model = new PlazaTreeIndexViewModel
        {
            RootCodigo = codigoRaiz,
            Tree = BuildTree(flatTree),
            Message = TempData["Message"] as string
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new PlazaFormViewModel
        {
            Jefes = await BuildJefesAsync(null, cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlazaFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Jefes = await BuildJefesAsync(null, cancellationToken);
            return View(model);
        }

        try
        {
            await _apiClient.CreateAsync(ToRequest(model), cancellationToken);
            TempData["Message"] = "La plaza se creó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "Error creando plaza.");
            model.Message = ex.Message;
            model.Jefes = await BuildJefesAsync(null, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _apiClient.GetByCodeAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var model = new PlazaFormViewModel
        {
            CodigoPuesto = item.CodigoPuesto,
            Puesto = item.Puesto,
            NombreEmpleado = item.NombreEmpleado,
            CodigoJefe = item.CodigoJefe,
            Jefes = await BuildJefesAsync(item.CodigoPuesto, cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PlazaFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.CodigoPuesto is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.Jefes = await BuildJefesAsync(model.CodigoPuesto, cancellationToken);
            return View(model);
        }

        try
        {
            await _apiClient.UpdateAsync(model.CodigoPuesto.Value, ToRequest(model), cancellationToken);
            TempData["Message"] = "La plaza se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "Error actualizando plaza.");
            model.Message = ex.Message;
            model.Jefes = await BuildJefesAsync(model.CodigoPuesto, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _apiClient.GetByCodeAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var model = new PlazaDeleteViewModel
        {
            CodigoPuesto = item.CodigoPuesto,
            Puesto = item.Puesto,
            NombreEmpleado = item.NombreEmpleado,
            JefeDescripcion = item.CodigoJefe is null
                ? "Sin jefe"
                : $"{item.CodigoJefe} - {item.PuestoJefe} - {item.NombreJefe}"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int codigoPuesto, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteAsync(codigoPuesto, cancellationToken);
            TempData["Message"] = "La plaza se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "Error eliminando plaza.");
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private static PlazaEmpleadoUpsertRequest ToRequest(PlazaFormViewModel model)
        => new()
        {
            Puesto = model.Puesto,
            NombreEmpleado = model.NombreEmpleado,
            CodigoJefe = model.CodigoJefe
        };

    private static IReadOnlyList<PlazaTreeNodeViewModel> BuildTree(IEnumerable<PlazaEmpleadoApiDto> flatItems)
    {
        var nodes = flatItems
            .Select(item => new PlazaTreeNodeViewModel
            {
                CodigoPuesto = item.CodigoPuesto,
                Puesto = item.Puesto,
                NombreEmpleado = item.NombreEmpleado,
                CodigoJefe = item.CodigoJefe
            })
            .ToDictionary(item => item.CodigoPuesto);

        var roots = new List<PlazaTreeNodeViewModel>();

        foreach (var node in nodes.Values.OrderBy(item => item.CodigoPuesto))
        {
            if (node.CodigoJefe is int jefeCodigo && nodes.TryGetValue(jefeCodigo, out var parent))
            {
                parent.Children.Add(node);
                continue;
            }

            roots.Add(node);
        }

        return roots.OrderBy(item => item.CodigoPuesto).ToList();
    }

    private async Task<IEnumerable<SelectListItem>> BuildJefesAsync(int? codigoPuestoActual, CancellationToken cancellationToken)
    {
        var plazas = await _apiClient.GetAllAsync(cancellationToken);

        var items = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "Sin jefe" }
        };

        items.AddRange(
            plazas
                .Where(item => item.CodigoPuesto != codigoPuestoActual)
                .OrderBy(item => item.CodigoPuesto)
                .Select(item => new SelectListItem
                {
                    Value = item.CodigoPuesto.ToString(),
                    Text = $"{item.CodigoPuesto} - {item.Puesto} - {item.NombreEmpleado}"
                }));

        return items;
    }
}
