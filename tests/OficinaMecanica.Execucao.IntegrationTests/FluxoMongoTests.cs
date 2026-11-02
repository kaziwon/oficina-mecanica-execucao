using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class FluxoMongoTests
{
    [MongoFact]
    public async Task HealthSwaggerEAutenticacao_FuncionamComMongoReal()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "execucao-teste-001");
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("execucao-teste-001", Assert.Single(health.Headers.GetValues("X-Correlation-ID")));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        foreach (var rota in new[] { "/api/servicos", "/api/pecas", "/api/execucoes" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "errada" })).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/servicos")).StatusCode);
    }

    [MongoFact]
    public async Task CatalogoServico_CRUD_ValidaUnicidadeSemDiferenciarMaiusculas()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var servico = await ApiTestHelper.CriarServico(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/servicos",
            new { nome = "REVISAO", descricao = "", preco = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/servicos/{servico.Id}",
            new { nome = "Revisao atualizada", descricao = "Nova", preco = 120.75m })).StatusCode);
        var atualizado = await client.GetFromJsonAsync<ServicoResponseDto>($"/api/servicos/{servico.Id}");
        Assert.Equal(120.75m, atualizado!.Preco);
        Assert.Single((await client.GetFromJsonAsync<List<ServicoResponseDto>>("/api/servicos"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/servicos/{servico.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/servicos/{servico.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/servicos/{servico.Id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<ServicoResponseDto>>("/api/servicos"))!);
    }

    [MongoFact]
    public async Task CatalogoPeca_PreservaSaldoNaAtualizacaoEValidaMovimentacoes()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var peca = await ApiTestHelper.CriarPeca(client, 5);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/pecas",
            new { nome = "FILTRO", descricao = "", precoUnitario = 10, quantidadeEstoque = 0 })).StatusCode);
        var update = await client.PutAsJsonAsync($"/api/pecas/{peca.Id}",
            new { nome = "Filtro atualizado", descricao = "Nova", precoUnitario = 25.15m });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(5, (await update.Content.ReadFromJsonAsync<PecaInsumoResponseDto>())!.QuantidadeEstoque);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/pecas/{peca.Id}",
            new { nome = "Filtro", descricao = "", precoUnitario = 10, quantidadeEstoque = 0 })).StatusCode);
        var entrada = await client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = 2 });
        Assert.Equal(7, (await entrada.Content.ReadFromJsonAsync<PecaInsumoResponseDto>())!.QuantidadeEstoque);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = -8 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = 0 })).StatusCode);
        Assert.Equal(7, (await client.GetFromJsonAsync<PecaInsumoResponseDto>($"/api/pecas/{peca.Id}"))!.QuantidadeEstoque);
        Assert.Single((await client.GetFromJsonAsync<List<PecaInsumoResponseDto>>("/api/pecas"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/pecas/{peca.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pecas/{peca.Id}")).StatusCode);
    }

    [MongoFact]
    public async Task Estoque_AtualizacoesConcorrentes_NaoPermitemSaldoNegativo()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var peca = await ApiTestHelper.CriarPeca(client, 5);
        var respostas = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = -1 })));
        Assert.Equal(5, respostas.Count(item => item.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, respostas.Count(item => item.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, (await client.GetFromJsonAsync<PecaInsumoResponseDto>($"/api/pecas/{peca.Id}"))!.QuantidadeEstoque);
        foreach (var resposta in respostas) resposta.Dispose();
    }

    [MongoFact]
    public async Task Estoque_NaoPermiteEstourarInt32()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var peca = await ApiTestHelper.CriarPeca(client, int.MaxValue);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/pecas/{peca.Id}/movimentacoes", new { quantidade = 1 })).StatusCode);
        Assert.Equal(int.MaxValue, (await client.GetFromJsonAsync<PecaInsumoResponseDto>($"/api/pecas/{peca.Id}"))!.QuantidadeEstoque);
    }

    [MongoFact]
    public async Task Diagnostico_FluxoCompleto_PreservaSnapshotsHistoricoEEstoque()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var servico = await ApiTestHelper.CriarServico(client);
        var peca = await ApiTestHelper.CriarPeca(client, 9);
        var execucao = await ApiTestHelper.CriarExecucao(client);
        Assert.Equal(StatusExecucao.AguardandoDiagnostico, execucao.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/execucoes", new
        {
            ordemServicoId = execucao.OrdemServicoId,
            descricaoSolicitacao = "Duplicada"
        })).StatusCode);
        var iniciada = await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-diagnostico", null);
        Assert.Equal(HttpStatusCode.OK, iniciada.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-diagnostico", null)).StatusCode);
        var response = await client.PostAsJsonAsync($"/api/execucoes/{execucao.Id}/concluir-diagnostico", new
        {
            diagnostico = "Filtro obstruido",
            servicoIds = new[] { servico.Id },
            pecasInsumos = new[] { new { pecaInsumoId = peca.Id, quantidade = 2 } }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var concluida = (await response.Content.ReadFromJsonAsync<ExecucaoResponseDto>(ApiTestHelper.Json))!;
        Assert.Equal(StatusExecucao.AguardandoLiberacao, concluida.Status);
        Assert.Equal(3, concluida.Historico.Count);
        Assert.Equal(2, concluida.Versao);
        Assert.Equal(100.15m, Assert.Single(concluida.Servicos).Preco);
        Assert.Equal(20.55m, Assert.Single(concluida.PecasInsumos).PrecoUnitario);
        (await client.PutAsJsonAsync($"/api/servicos/{servico.Id}", new { nome = "Revisao", descricao = "", preco = 999 })).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/pecas/{peca.Id}")).EnsureSuccessStatusCode();
        var consultada = (await client.GetFromJsonAsync<ExecucaoResponseDto>($"/api/execucoes/{execucao.Id}", ApiTestHelper.Json))!;
        Assert.Equal(100.15m, consultada.Servicos.Single().Preco);
        Assert.Equal(20.55m, consultada.PecasInsumos.Single().PrecoUnitario);
        var database = factory.Mongo.GetDatabase(factory.DatabaseName);
        var documento = await database.GetCollection<BsonDocument>("execucoes").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(BsonBinarySubType.UuidStandard, documento["_id"].AsBsonBinaryData.SubType);
        Assert.Equal(BsonType.Decimal128, documento["Servicos"][0]["Preco"].BsonType);
        var estoque = await database.GetCollection<BsonDocument>("pecas_insumos").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(9, estoque["QuantidadeEstoque"].AsInt32);
    }

    [MongoFact]
    public async Task Diagnostico_ImpedeConclusaoAntesDoInicioEItensInvalidos()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var execucao = await ApiTestHelper.CriarExecucao(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/execucoes/{execucao.Id}/concluir-diagnostico", new { diagnostico = "Revisar" })).StatusCode);
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-diagnostico", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/execucoes/{execucao.Id}/concluir-diagnostico", new
        {
            diagnostico = "Revisar",
            servicoIds = new[] { Guid.NewGuid() }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/execucoes/{execucao.Id}/concluir-diagnostico", new
        {
            diagnostico = "Revisar",
            pecasInsumos = new object?[] { null }
        })).StatusCode);
    }

    [MongoFact]
    public async Task Execucao_DuasGravacoesComMesmaVersao_NaoSobrescrevemHistorico()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var execucao = await ApiTestHelper.CriarExecucao(client);
        using var scope = factory.Services.CreateScope();
        var source = new MongoExecucaoDataSource(scope.ServiceProvider.GetRequiredService<MongoDbContext>());
        var primeira = source.ObterPorId(execucao.Id)!;
        var segunda = source.ObterPorId(execucao.Id)!;
        primeira.IniciarDiagnostico();
        segunda.IniciarDiagnostico();
        Assert.True(source.Atualizar(primeira, 0));
        Assert.False(source.Atualizar(segunda, 0));
        Assert.Equal(2, source.ObterPorId(execucao.Id)!.Historico.Count);
    }

    [MongoFact]
    public async Task Fila_FiltraStatusEOrdenaMaisAntigasPrimeiro()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var primeira = await ApiTestHelper.CriarExecucao(client);
        await ApiTestHelper.CriarExecucao(client);
        (await client.PostAsync($"/api/execucoes/{primeira.Id}/iniciar-diagnostico", null)).EnsureSuccessStatusCode();
        var fila = (await client.GetFromJsonAsync<List<ExecucaoResponseDto>>("/api/execucoes", ApiTestHelper.Json))!;
        Assert.Equal(2, fila.Count);
        Assert.Equal(primeira.Id, fila[0].Id);
        Assert.Single((await client.GetFromJsonAsync<List<ExecucaoResponseDto>>("/api/execucoes?status=EmDiagnostico", ApiTestHelper.Json))!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/execucoes?status=99")).StatusCode);
    }

    [MongoFact]
    public async Task Mongo_IndicesUnicos_ProtegemMesmoSemPreConsulta()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        await ApiTestHelper.CriarServico(client);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Throws<MongoWriteException>(() => new MongoServicoDataSource(context)
            .Adicionar(new Servico { Id = Guid.NewGuid(), Nome = "REVISAO", Preco = 10 }));
        var execucao = await ApiTestHelper.CriarExecucao(client);
        Assert.Throws<MongoWriteException>(() => new MongoExecucaoDataSource(context).Adicionar(new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = execucao.OrdemServicoId,
            DescricaoSolicitacao = "Duplicada"
        }));
    }

    [MongoFact]
    public async Task ConsultasEMutacoesDeIdsInexistentes_Retornam404()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var id = Guid.NewGuid();
        foreach (var rota in new[] { $"/api/servicos/{id}", $"/api/pecas/{id}", $"/api/execucoes/{id}" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/servicos/{id}", new { nome = "Outro", descricao = "", preco = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/pecas/{id}", new { nome = "Outra", descricao = "", precoUnitario = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/pecas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/execucoes/{id}/iniciar-diagnostico", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/execucoes/{id}/concluir-diagnostico", new { diagnostico = "Revisar" })).StatusCode);
    }

    [MongoFact]
    public async Task EntradaHTTP_RejeitaCamposDesconhecidosEValidacoesDeNegocio()
    {
        using var factory = new ExecucaoApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/servicos", new { nome = "Servico", descricao = "", preco = 1.111m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/pecas", new { nome = "Peca", precoUnitario = 10, quantidadeEstoque = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/execucoes", new { ordemServicoId = Guid.Empty, descricaoSolicitacao = "Revisar" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/execucoes", new { ordemServicoId = Guid.NewGuid(), descricaoSolicitacao = "Revisar", pagamentoAprovado = true })).StatusCode);
    }
}
