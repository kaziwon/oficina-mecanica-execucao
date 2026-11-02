using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.IntegrationTests;

internal static class ApiTestHelper
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public static async Task Autenticar(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "SenhaDosTestes@123" });
        response.EnsureSuccessStatusCode();
        var output = (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", output.Token);
    }
    public static async Task<ServicoResponseDto> CriarServico(HttpClient client, string nome = "Revisao")
    {
        var response = await client.PostAsJsonAsync("/api/servicos", new { nome, descricao = "Revisao geral", preco = 100.15m });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServicoResponseDto>())!;
    }
    public static async Task<PecaInsumoResponseDto> CriarPeca(HttpClient client, int saldo = 5, string nome = "Filtro")
    {
        var response = await client.PostAsJsonAsync("/api/pecas", new { nome, descricao = "Filtro de oleo", precoUnitario = 20.55m, quantidadeEstoque = saldo });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PecaInsumoResponseDto>())!;
    }
    public static async Task<ExecucaoResponseDto> CriarExecucao(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/execucoes", new { ordemServicoId = Guid.NewGuid(), descricaoSolicitacao = "Revisar freios" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExecucaoResponseDto>(Json))!;
    }
}
