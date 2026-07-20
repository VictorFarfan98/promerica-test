using System.Data;
using CompanyHierarchy.Api.Models;
using Microsoft.Data.SqlClient;

namespace CompanyHierarchy.Api.Data;

public sealed class SqlPlazaEmpleadoRepository : IPlazaEmpleadoRepository
{
    private readonly string _connectionString;

    public SqlPlazaEmpleadoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CompanyHierarchyDb")
            ?? throw new InvalidOperationException("Missing connection string 'CompanyHierarchyDb'.");
    }

    public async Task<IReadOnlyList<PlazaEmpleadoDto>> GetTreeAsync(int? codigoRaiz, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_ObtenerArbol", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@CodigoRaiz", (object?)codigoRaiz ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadListAsync(reader, includeHierarchyColumns: true, cancellationToken);
    }

    public async Task<IReadOnlyList<PlazaEmpleadoDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_ObtenerTodos", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadListAsync(reader, includeHierarchyColumns: false, cancellationToken);
    }

    public async Task<PlazaEmpleadoDto?> GetByCodeAsync(int codigoPuesto, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_ObtenerPorCodigo", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@CodigoPuesto", codigoPuesto);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFlatRow(reader);
    }

    public async Task<PlazaEmpleadoDto> CreateAsync(PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_Insertar", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Puesto", request.Puesto);
        command.Parameters.AddWithValue("@NombreEmpleado", request.NombreEmpleado);
        command.Parameters.AddWithValue("@CodigoJefe", (object?)request.CodigoJefe ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSavedAsync(reader, cancellationToken);
    }

    public async Task<PlazaEmpleadoDto> UpdateAsync(int codigoPuesto, PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_Modificar", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@CodigoPuesto", codigoPuesto);
        command.Parameters.AddWithValue("@Puesto", request.Puesto);
        command.Parameters.AddWithValue("@NombreEmpleado", request.NombreEmpleado);
        command.Parameters.AddWithValue("@CodigoJefe", (object?)request.CodigoJefe ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSavedAsync(reader, cancellationToken);
    }

    public async Task<PlazaEmpleadoDto> DeleteAsync(int codigoPuesto, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand("dbo.usp_PlazaEmpleado_Eliminar", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@CodigoPuesto", codigoPuesto);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSavedAsync(reader, cancellationToken);
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    private static async Task<IReadOnlyList<PlazaEmpleadoDto>> ReadListAsync(SqlDataReader reader, bool includeHierarchyColumns, CancellationToken cancellationToken)
    {
        var items = new List<PlazaEmpleadoDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(includeHierarchyColumns ? MapTreeRow(reader) : MapFlatRow(reader));
        }

        return items;
    }

    private static async Task<PlazaEmpleadoDto> ReadSingleAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("La operación no devolvió ningún registro.");
        }

        return MapFlatRow(reader);
    }

    private static async Task<PlazaEmpleadoDto> ReadSavedAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("La operación no devolvió ningún registro.");
        }

        return new PlazaEmpleadoDto
        {
            CodigoPuesto = reader.GetInt32(reader.GetOrdinal("CodigoPuesto")),
            Puesto = reader.GetString(reader.GetOrdinal("Puesto")),
            NombreEmpleado = reader.GetString(reader.GetOrdinal("NombreEmpleado")),
            CodigoJefe = reader.IsDBNull(reader.GetOrdinal("CodigoJefe")) ? null : reader.GetInt32(reader.GetOrdinal("CodigoJefe"))
        };
    }

    private static PlazaEmpleadoDto MapFlatRow(SqlDataReader reader)
    {
        return new PlazaEmpleadoDto
        {
            CodigoPuesto = reader.GetInt32(reader.GetOrdinal("CodigoPuesto")),
            Puesto = reader.GetString(reader.GetOrdinal("Puesto")),
            NombreEmpleado = reader.GetString(reader.GetOrdinal("NombreEmpleado")),
            CodigoJefe = reader.IsDBNull(reader.GetOrdinal("CodigoJefe")) ? null : reader.GetInt32(reader.GetOrdinal("CodigoJefe")),
            PuestoJefe = GetNullableString(reader, "PuestoJefe"),
            NombreJefe = GetNullableString(reader, "NombreJefe")
        };
    }

    private static PlazaEmpleadoDto MapTreeRow(SqlDataReader reader)
    {
        return new PlazaEmpleadoDto
        {
            CodigoPuesto = reader.GetInt32(reader.GetOrdinal("CodigoPuesto")),
            Puesto = reader.GetString(reader.GetOrdinal("Puesto")),
            NombreEmpleado = reader.GetString(reader.GetOrdinal("NombreEmpleado")),
            CodigoJefe = reader.IsDBNull(reader.GetOrdinal("CodigoJefe")) ? null : reader.GetInt32(reader.GetOrdinal("CodigoJefe")),
            Nivel = reader.IsDBNull(reader.GetOrdinal("Nivel")) ? null : reader.GetInt32(reader.GetOrdinal("Nivel"))
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
