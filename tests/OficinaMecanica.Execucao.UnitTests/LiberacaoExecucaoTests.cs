using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.UnitTests;

public class LiberacaoExecucaoTests
{
    [Fact]
    public void AutorizacaoValida_LiberaFilaSemIniciarReparo()
    {
        var execucao = Nova(); var autorizacao = Autorizacao();
        execucao.LiberarExecucao(autorizacao);
        Assert.Equal(StatusExecucao.AguardandoExecucao, execucao.Status);
        Assert.Equal("Revisao completa", execucao.Diagnostico);
        Assert.Equal(autorizacao.PagamentoId, execucao.Autorizacao!.PagamentoId);
        Assert.Equal(4, execucao.Historico.Count); Assert.Equal(2, execucao.EventosDiagnostico.Count);
        Assert.Throws<RegraNegocioException>(() => execucao.LiberarExecucao(Autorizacao()));
        Assert.Equal(4, execucao.Historico.Count);
    }

    [Theory]
    [InlineData(StatusExecucao.AguardandoDiagnostico)]
    [InlineData(StatusExecucao.EmDiagnostico)]
    [InlineData(StatusExecucao.Cancelada)]
    [InlineData(StatusExecucao.EmExecucao)]
    [InlineData(StatusExecucao.Finalizada)]
    public void StatusIncompativel_NaoLibera(StatusExecucao status)
    {
        var execucao = Nova(); execucao.Status = status;
        Assert.Throws<RegraNegocioException>(() => execucao.LiberarExecucao(Autorizacao()));
        Assert.Null(execucao.Autorizacao); Assert.Equal(status, execucao.Status);
    }

    [Theory]
    [InlineData("valor")]
    [InlineData("pagamento")]
    [InlineData("provedor")]
    [InlineData("saga")]
    public void ProvaInvalida_NaoMudaStatusOuHistorico(string erro)
    {
        var execucao = Nova(); var autorizacao = Autorizacao();
        if (erro == "valor") autorizacao.ValorPago = 199m;
        if (erro == "pagamento") autorizacao.PagamentoId = Guid.Empty;
        if (erro == "provedor") autorizacao.ProvedorId = "";
        if (erro == "saga") execucao.SagaId = null;
        Assert.Throws<RegraNegocioException>(() => execucao.LiberarExecucao(autorizacao));
        Assert.Equal(StatusExecucao.AguardandoLiberacao, execucao.Status); Assert.Null(execucao.Autorizacao); Assert.Equal(3, execucao.Historico.Count);
    }

    private static AutorizacaoExecucao Autorizacao() => new()
    {
        OrcamentoId = Guid.NewGuid(),
        PagamentoId = Guid.NewGuid(),
        ProvedorId = "mock-id",
        ValorPago = 200m,
        MensagemLiberacaoId = Guid.NewGuid(),
        ConfirmacaoPagamentoId = Guid.NewGuid()
    };

    private static ExecucaoServico Nova()
    {
        var saga = Guid.NewGuid(); var pedido = Guid.NewGuid(); var agora = DateTime.UtcNow;
        var execucao = new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = Guid.NewGuid(),
            SagaId = saga,
            PedidoRegistroId = pedido,
            Status = StatusExecucao.AguardandoDiagnostico,
            Historico = [new HistoricoExecucao { Status = StatusExecucao.AguardandoDiagnostico, RegistradoEm = agora }],
            RespostasEntrada = [new RespostaEntrada { Tipo = "ExecucaoRegistrada", SagaId = saga, CausationId = pedido, CorrelationId = "teste" }]
        };
        execucao.IniciarDiagnostico();
        execucao.ConcluirDiagnostico("Revisao completa", [new ItemServicoDiagnostico { ServicoId = Guid.NewGuid(), Nome = "Revisao", Preco = 100m }],
            [new ItemPecaDiagnostico { PecaInsumoId = Guid.NewGuid(), Nome = "Filtro", Quantidade = 2, PrecoUnitario = 50m }]);
        return execucao;
    }
}
