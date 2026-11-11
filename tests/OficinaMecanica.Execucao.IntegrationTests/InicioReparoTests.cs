using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.Infrastructure.Sources;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.IntegrationTests;

public class InicioReparoTests
{
    [MongoFact]
    public async Task RequisicoesConcorrentes_ReservamUmaVezEPublicamEvento()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client); var execucao = Criar(factory, 10);
        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)));
        Assert.All(respostas, item => Assert.Equal(HttpStatusCode.OK, item.StatusCode));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        foreach (var resposta in respostas)
        {
            var inicial = await resposta.Content.ReadFromJsonAsync<ExecucaoResponseDto>(ApiTestHelper.Json);
            Assert.Equal(salva.ReparoIniciadoEm, inicial!.ReparoIniciadoEm);
        }
        Assert.Equal(StatusExecucao.EmExecucao, salva.Status); Assert.Single(salva.ReservasPecas); Assert.Single(salva.Historico);
        var peca = db.Pecas.Find(item => item.Id == execucao.PecasInsumos[0].PecaInsumoId).Single();
        Assert.Equal(10, peca.QuantidadeEstoque); Assert.Equal(2, peca.QuantidadeReservada); Assert.Equal(8, peca.QuantidadeDisponivel);
        var outbox = new MongoOutboxDataSource(db); var evento = Assert.Single(outbox.ObterPendentes(10));
        Assert.Equal("ReparoIniciado", evento.Type); Assert.Equal(salva.Autorizacao!.MensagemLiberacaoId, evento.CausationId);
        Assert.Equal(salva.ReparoIniciadoEm, evento.Payload.ReparoIniciadoEm);
        var antiga = new MongoExecucaoDataSource(db).ObterPorId(salva.Id)!;
        outbox.ConfirmarPublicacao(evento.MessageId); antiga.Versao++;
        Assert.False(new MongoExecucaoDataSource(db).Atualizar(antiga, antiga.Versao - 1));
        (await client.PostAsync($"/api/execucoes/{salva.Id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        Assert.Empty(outbox.ObterPendentes(10));
        var dto = await client.GetFromJsonAsync<ExecucaoResponseDto>($"/api/execucoes/{salva.Id}", ApiTestHelper.Json);
        Assert.NotNull(dto!.ReparoIniciadoEm); Assert.Single(dto.ReservasPecas);
    }

    [MongoFact]
    public async Task DuasOSConcorrentes_NaoReservamAMesmaUltimaPeca()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var primeira = Criar(factory, 3); var segunda = Criar(factory, 3, primeira.PecasInsumos[0].PecaInsumoId);
        var respostas = await Task.WhenAll(new[] { primeira, segunda }.Select(item => client.PostAsync($"/api/execucoes/{item.Id}/iniciar-reparo", null)));
        Assert.Single(respostas, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, item => item.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        Assert.Equal(2, db.Pecas.Find(item => item.Id == primeira.PecasInsumos[0].PecaInsumoId).Single().QuantidadeReservada);
        Assert.Single(db.Execucoes.Find(item => item.Status == StatusExecucao.EmExecucao).ToList());
    }

    [MongoFact]
    public async Task FaltaNaSegundaPeca_DesfazPrimeiraReservaEstadoEOutbox()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var segunda = new PecaInsumo { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Nome = "Segunda", QuantidadeEstoque = 0 };
        db.Pecas.InsertOne(segunda);
        execucao.PecasInsumos.Add(new ItemPecaDiagnostico { PecaInsumoId = segunda.Id, Quantidade = 1, PrecoUnitario = 50m });
        db.Execucoes.ReplaceOne(item => item.Id == execucao.Id, execucao);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).StatusCode);
        Assert.All(db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).ToList(), item => Assert.Equal(0, item.QuantidadeReservada));
        var salva = db.Execucoes.Find(item => item.Id == execucao.Id).Single();
        Assert.Equal(StatusExecucao.AguardandoExecucao, salva.Status); Assert.Empty(salva.ReservasPecas); Assert.Empty(salva.EventosReparo); Assert.Null(salva.ReparoIniciadoEm);
        // Repor o saldo permite uma nova tentativa; nenhuma reserva parcial ficou para tras.
        db.Pecas.UpdateOne(item => item.Id == segunda.Id, Builders<PecaInsumo>.Update.Set(item => item.QuantidadeEstoque, 1));
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        Assert.Equal(2, db.Execucoes.Find(item => item.Id == execucao.Id).Single().ReservasPecas.Count);
    }

    [MongoTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PecaAusenteOuInativa_NaoInicia(bool inativa)
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 10); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        if (inativa) db.Pecas.UpdateMany(FilterDefinition<PecaInsumo>.Empty, Builders<PecaInsumo>.Update.Set(item => item.Ativo, false));
        else db.Pecas.DeleteMany(FilterDefinition<PecaInsumo>.Empty);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).StatusCode);
        Assert.Equal(StatusExecucao.AguardandoExecucao, db.Execucoes.Find(item => item.Id == execucao.Id).Single().Status);
    }

    [MongoFact]
    public async Task EstoqueReservado_NaoPodeSerRetiradoOuDesativado()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 3); var id = execucao.PecasInsumos[0].PecaInsumoId;
        (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/pecas/{id}/movimentacoes", new { quantidade = -2 })).StatusCode);
        (await client.PostAsJsonAsync($"/api/pecas/{id}/movimentacoes", new { quantidade = -1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/pecas/{id}")).StatusCode);
        var peca = await client.GetFromJsonAsync<PecaInsumoResponseDto>($"/api/pecas/{id}");
        Assert.Equal(2, peca!.QuantidadeEstoque); Assert.Equal(2, peca.QuantidadeReservada); Assert.Equal(0, peca.QuantidadeDisponivel);
    }

    [MongoFact]
    public async Task AutorizacaoAusente_OuExecucaoInexistente_NaoIniciam()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var execucao = Criar(factory, 10);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/execucoes/{Guid.NewGuid()}/iniciar-reparo", null)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        db.Execucoes.UpdateOne(item => item.Id == execucao.Id, Builders<ExecucaoServico>.Update.Set(item => item.Autorizacao, null));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/execucoes/{execucao.Id}/iniciar-reparo", null)).StatusCode);
        Assert.Equal(0, db.Pecas.Find(FilterDefinition<PecaInsumo>.Empty).Single().QuantidadeReservada);
    }

    [MongoFact]
    public async Task SomenteServicos_IniciaSemReservaDePecas()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); var execucao = Criar(factory, 10);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        execucao.PecasInsumos.Clear();
        execucao.Servicos.Add(new ItemServicoDiagnostico { ServicoId = Guid.NewGuid(), Nome = "Revisao", Preco = 100m });
        db.Execucoes.ReplaceOne(item => item.Id == execucao.Id, execucao);
        var output = scope.ServiceProvider.GetRequiredService<IniciarReparoUseCase>().Executar(execucao.Id);
        Assert.Equal(StatusExecucao.EmExecucao, output!.Status); Assert.Empty(output.ReservasPecas);
    }

    [MongoFact]
    public async Task PecaHistoricaSemCampoReserva_ContinuaMovimentavelEDesativavel()
    {
        using var factory = new ExecucaoApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var execucao = Criar(factory, 3); var id = execucao.PecasInsumos[0].PecaInsumoId;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        db.Pecas.UpdateOne(item => item.Id == id, Builders<PecaInsumo>.Update.Unset(item => item.QuantidadeReservada));
        (await client.PostAsJsonAsync($"/api/pecas/{id}/movimentacoes", new { quantidade = -1 })).EnsureSuccessStatusCode();
        var peca = await client.GetFromJsonAsync<PecaInsumoResponseDto>($"/api/pecas/{id}");
        Assert.Equal(0, peca!.QuantidadeReservada); Assert.Equal(2, peca.QuantidadeDisponivel);
        (await client.DeleteAsync($"/api/pecas/{id}")).EnsureSuccessStatusCode();
    }

    private static ExecucaoServico Criar(ExecucaoApiFactory factory, int estoque, Guid? pecaId = null)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var id = pecaId ?? Guid.NewGuid(); var saga = Guid.NewGuid(); var liberacao = Guid.NewGuid(); var pagamento = Guid.NewGuid(); var orcamento = Guid.NewGuid();
        if (!pecaId.HasValue) db.Pecas.InsertOne(new PecaInsumo { Id = id, Nome = "Filtro " + id, QuantidadeEstoque = estoque, PrecoUnitario = 50m });
        var execucao = new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = Guid.NewGuid(),
            SagaId = saga,
            Status = StatusExecucao.AguardandoExecucao,
            DiagnosticoConcluidoEm = DateTime.UtcNow,
            Autorizacao = new AutorizacaoExecucao
            { MensagemLiberacaoId = liberacao, PagamentoId = pagamento, OrcamentoId = orcamento, ValorPago = 100m, ProvedorId = "mock" },
            RespostasLiberacao = [new RespostaLiberacaoExecucao
            { Tipo = "ExecucaoLiberada", CausationId = liberacao, SagaId = saga, PagamentoId = pagamento, OrcamentoId = orcamento, CorrelationId = "reparo", PublicadaEm = DateTime.UtcNow }],
            PecasInsumos = [new ItemPecaDiagnostico { PecaInsumoId = id, Quantidade = 2, PrecoUnitario = 50m }]
        };
        db.Execucoes.InsertOne(execucao); return execucao;
    }
}
