using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;
using OficinaMecanica.Execucao.UnitTests.Fakes;

namespace OficinaMecanica.Execucao.UnitTests;

public class ExecucaoUseCasesTests
{
    private readonly ExecucaoGatewayFake _execucoes = new();
    private readonly CatalogoGatewayFake _catalogo = new();
    private CriarExecucaoUseCase Criar() => new(_execucoes);
    private ConcluirDiagnosticoUseCase Concluir() => new(_execucoes, _catalogo, _catalogo);

    [Fact]
    public void Criar_RegistraFilaHistoricoEVinculoComOS()
    {
        var id = Guid.NewGuid();
        var output = Criar().Executar(new(id, " Revisao "));
        Assert.Equal(id, output.OrdemServicoId);
        Assert.Equal("Revisao", output.DescricaoSolicitacao);
        Assert.Equal(StatusExecucao.AguardandoDiagnostico, output.Status);
        Assert.Single(output.Historico);
        Assert.NotNull(new ObterExecucaoPorIdUseCase(_execucoes).Executar(output.Id));
        Assert.Null(new ObterExecucaoPorIdUseCase(_execucoes).Executar(Guid.NewGuid()));
        Assert.Throws<ConflitoException>(() => Criar().Executar(new(id, "Revisao")));
    }

    [Fact]
    public void Criar_RejeitaIdVazioEDescricaoVaziaOuLonga()
    {
        Assert.Throws<ValidacaoException>(() => Criar().Executar(new(Guid.Empty, "Revisao")));
        Assert.Throws<ValidacaoException>(() => Criar().Executar(new(Guid.NewGuid(), "  ")));
        Assert.Throws<ValidacaoException>(() => Criar().Executar(new(Guid.NewGuid(), new string('a', 1001))));
    }

    [Fact]
    public void Diagnostico_RegistraTransicoesECopiaPrecosSemBaixarEstoque()
    {
        var servico = _catalogo.Adicionar(new Servico { Id = Guid.NewGuid(), Nome = "Revisao", Preco = 100.15m });
        var peca = _catalogo.Adicionar(new PecaInsumo { Id = Guid.NewGuid(), Nome = "Filtro", PrecoUnitario = 20.55m, QuantidadeEstoque = 10 });
        var criada = Criar().Executar(new(Guid.NewGuid(), "Revisao"));
        var iniciada = new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id)!;
        Assert.Equal(StatusExecucao.EmDiagnostico, iniciada.Status);
        var output = Concluir().Executar(criada.Id, new ConcluirDiagnosticoInput
        {
            Diagnostico = "  Trocar filtro  ",
            ServicoIds = [servico.Id],
            PecasInsumos = [new() { PecaInsumoId = peca.Id, Quantidade = 2 }]
        })!;
        Assert.Equal(StatusExecucao.AguardandoLiberacao, output.Status);
        Assert.Equal("Trocar filtro", output.Diagnostico);
        Assert.Equal(100.15m, Assert.Single(output.Servicos).Preco);
        Assert.Equal(20.55m, Assert.Single(output.PecasInsumos).PrecoUnitario);
        Assert.Equal(2, output.Versao);
        Assert.Equal(3, output.Historico.Count);
        Assert.Equal(10, _catalogo.Pecas.Single().QuantidadeEstoque);
    }

    [Fact]
    public void Diagnostico_ImpedeTransicoesForaDeOrdem()
    {
        var criada = Criar().Executar(new(Guid.NewGuid(), "Revisao"));
        Assert.Throws<RegraNegocioException>(() => Concluir().Executar(criada.Id, new() { Diagnostico = "Revisar" }));
        new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id);
        Assert.Throws<RegraNegocioException>(() => new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id));
    }

    [Fact]
    public void Diagnostico_RetornaNullQuandoNaoExiste()
    {
        Assert.Null(new IniciarDiagnosticoUseCase(_execucoes).Executar(Guid.NewGuid()));
        Assert.Null(Concluir().Executar(Guid.NewGuid(), new() { Diagnostico = "Revisar" }));
    }

    [Fact]
    public void Diagnostico_DetectaConflitoDeVersaoSemPersistirAlteracao()
    {
        var criada = Criar().Executar(new(Guid.NewGuid(), "Revisao"));
        _execucoes.PermitirAtualizacao = false;
        Assert.Throws<ConflitoException>(() => new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id));
        Assert.Equal(StatusExecucao.AguardandoDiagnostico, _execucoes.ObterPorId(criada.Id)!.Status);
        _execucoes.PermitirAtualizacao = true;
        new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id);
        _execucoes.PermitirAtualizacao = false;
        Assert.Throws<ConflitoException>(() => Concluir().Executar(criada.Id, new() { Diagnostico = "Revisar" }));
    }

    [Fact]
    public void Diagnostico_RejeitaItensInexistentes()
    {
        var criada = Criar().Executar(new(Guid.NewGuid(), "Revisao"));
        new IniciarDiagnosticoUseCase(_execucoes).Executar(criada.Id);
        Assert.Throws<ConflitoException>(() => Concluir().Executar(criada.Id,
            new() { Diagnostico = "Revisar", ServicoIds = [Guid.NewGuid()] }));
        Assert.Throws<ConflitoException>(() => Concluir().Executar(criada.Id,
            new() { Diagnostico = "Revisar", PecasInsumos = [new() { PecaInsumoId = Guid.NewGuid(), Quantidade = 1 }] }));
    }

    [Fact]
    public void Diagnostico_RejeitaDescricaoInvalidaEItensDuplicados()
    {
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(Guid.NewGuid(), new()));
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(Guid.NewGuid(), new() { Diagnostico = new string('a', 2001) }));
        var id = Guid.NewGuid();
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(id, new() { Diagnostico = "Revisar", ServicoIds = [id, id] }));
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(id, new() { Diagnostico = "Revisar", ServicoIds = [Guid.Empty] }));
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(id, new()
        {
            Diagnostico = "Revisar",
            PecasInsumos = [new() { PecaInsumoId = id, Quantidade = 0 }]
        }));
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(id, new()
        {
            Diagnostico = "Revisar",
            PecasInsumos = [new() { PecaInsumoId = id, Quantidade = 1 }, new() { PecaInsumoId = id, Quantidade = 2 }]
        }));
        Assert.Throws<ValidacaoException>(() => Concluir().Executar(id, new() { Diagnostico = "Revisar", ServicoIds = null! }));
    }

    [Fact]
    public void Fila_OrdenaPorCriacaoEValidaStatus()
    {
        var primeira = Criar().Executar(new(Guid.NewGuid(), "Primeira"));
        Criar().Executar(new(Guid.NewGuid(), "Segunda"));
        new IniciarDiagnosticoUseCase(_execucoes).Executar(primeira.Id);
        var fila = new ObterFilaExecucoesUseCase(_execucoes);
        Assert.Equal(2, fila.Executar(null).Count);
        Assert.Single(fila.Executar(StatusExecucao.EmDiagnostico));
        Assert.Throws<ValidacaoException>(() => fila.Executar((StatusExecucao)99));
    }
}
