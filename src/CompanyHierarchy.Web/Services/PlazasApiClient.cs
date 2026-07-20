using System.Net;
using System.Net.Http.Json;
using CompanyHierarchy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CompanyHierarchy.Web.Services;

public sealed class PlazasApiClient : IPlazasApiClient
{
    private readonly HttpClient _httpClient;

    public PlazasApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<PlazaEmpleadoApiDto>> GetTreeAsync(int? codigoRaiz, CancellationToken cancellationToken)
        => SendListAsync($"api/plazas/tree?codigoRaiz={codigoRaiz}", cancellationToken);

    public Task<IReadOnlyList<PlazaEmpleadoApiDto>> GetAllAsync(CancellationToken cancellationToken)
        => SendListAsync("api/plazas", cancellationToken);

    public async Task<PlazaEmpleadoApiDto?> GetByCodeAsync(int codigoPuesto, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/plazas/{codigoPuesto}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PlazaEmpleadoApiDto>(cancellationToken: cancellationToken);
    }

    public async Task<PlazaEmpleadoApiDto> CreateAsync(PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/plazas", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadItemAsync(response, cancellationToken);
    }

    public async Task<PlazaEmpleadoApiDto> UpdateAsync(int codigoPuesto, PlazaEmpleadoUpsertRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/plazas/{codigoPuesto}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadItemAsync(response, cancellationToken);
    }

    public async Task<PlazaEmpleadoApiDto> DeleteAsync(int codigoPuesto, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"api/plazas/{codigoPuesto}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadItemAsync(response, cancellationToken);
    }

    private async Task<IReadOnlyList<PlazaEmpleadoApiDto>> SendListAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<PlazaEmpleadoApiDto>>(cancellationToken: cancellationToken)
            ?? [];
    }

    private static async Task<PlazaEmpleadoApiDto> ReadItemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<PlazaEmpleadoApiDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API response did not contain a plaza.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await TryReadProblemDetailAsync(response, cancellationToken);
        throw new ApiClientException(response.StatusCode, detail);
    }

    private static async Task<string> TryReadProblemDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            return problem?.Detail ?? problem?.Title ?? response.ReasonPhrase ?? "Request failed.";
        }
        catch
        {
            return response.ReasonPhrase ?? "Request failed.";
        }
    }
}
