using CompanyHierarchy.Api.Data;
using CompanyHierarchy.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CompanyHierarchy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PlazasController : ControllerBase
{
    private const int NotFoundMin = 50001;
    private const int NotFoundMax = 50011;

    private readonly IPlazaEmpleadoRepository _repository;
    private readonly ILogger<PlazasController> _logger;

    public PlazasController(IPlazaEmpleadoRepository repository, ILogger<PlazasController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<PlazaEmpleadoDto>>> GetTree([FromQuery] int? codigoRaiz, CancellationToken cancellationToken)
    {
        try
        {
            var data = await _repository.GetTreeAsync(codigoRaiz, cancellationToken);
            return Ok(data);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlazaEmpleadoDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var data = await _repository.GetAllAsync(cancellationToken);
            return Ok(data);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpGet("{codigoPuesto:int}")]
    public async Task<ActionResult<PlazaEmpleadoDto>> GetByCode(int codigoPuesto, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _repository.GetByCodeAsync(codigoPuesto, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<PlazaEmpleadoDto>> Create([FromBody] PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _repository.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetByCode), new { codigoPuesto = created.CodigoPuesto }, created);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpPut("{codigoPuesto:int}")]
    public async Task<ActionResult<PlazaEmpleadoDto>> Update(int codigoPuesto, [FromBody] PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _repository.UpdateAsync(codigoPuesto, request, cancellationToken);
            return Ok(updated);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpDelete("{codigoPuesto:int}")]
    public async Task<ActionResult<PlazaEmpleadoDto>> Delete(int codigoPuesto, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(codigoPuesto, cancellationToken);
            return Ok(deleted);
        }
        catch (SqlException ex)
        {
            return MapSqlException(ex);
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    private ActionResult MapSqlException(SqlException ex)
    {
        _logger.LogWarning(ex, "SQL error handling plaza request.");

        var statusCode = ex.Number switch
        {
            50011 or 50005 or 50001 => StatusCodes.Status404NotFound,
            50012 => StatusCodes.Status409Conflict,
            50002 or 50003 or 50004 or 50006 or 50007 or 50008 or 50009 or 50010 => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            title: "No se pudo completar la operación.",
            detail: ex.Message,
            statusCode: statusCode);
    }
}
