using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.UnitTests;

public class InicioReparoTests
{
    [Fact]
    public void InicioAutorizado_GeraReservasEEventoUmaVez()
    {
        var execucao = Nova();
        Assert.True(execucao.IniciarReparo()); Assert.False(execucao.IniciarReparo());
        Assert.Equal(StatusExecucao.EmExecucao, execucao.Status);
        var reserva = Assert.Single(execucao.ReservasPecas);
        Assert.Equal(2, reserva.Quantidade); Assert.Equal(execucao.PecasInsumos[0].PecaInsumoId, reserva.PecaInsumoId);
        var evento = Assert.Single(execucao.EventosReparo);
        Assert.Equal(execucao.Autorizacao!.MensagemLiberacaoId, evento.CausationId);
        Assert.Equal(execucao.Autorizacao.PagamentoId, evento.PagamentoId);
        Assert.Equal(execucao.ReparoIniciadoEm, evento.IniciadoEm); Assert.Single(execucao.Historico);
    }

    [Theory]
    [InlineData("autorizacao")]
    [InlineData("liberacao")]
    [InlineData("status")]
    [InlineData("saga")]
    public void SemVinculoValido_NaoIniciaNemGeraReservas(string erro)
    {
        var execucao = Nova();
        if (erro == "autorizacao") execucao.Autorizacao = null;
        if (erro == "liberacao") execucao.RespostasLiberacao.Clear();
        if (erro == "status") execucao.Status = StatusExecucao.AguardandoLiberacao;
        if (erro == "saga") execucao.SagaId = null;
        Assert.Throws<RegraNegocioException>(() => execucao.IniciarReparo());
        Assert.Empty(execucao.ReservasPecas); Assert.Empty(execucao.EventosReparo); Assert.Null(execucao.ReparoIniciadoEm);
    }

    [Fact]
    public void Reserva_UsaApenasSaldoDisponivelSemBaixarFisico()
    {
        var peca = new PecaInsumo { QuantidadeEstoque = 5 };
        peca.Reservar(3);
        Assert.Equal(5, peca.QuantidadeEstoque); Assert.Equal(3, peca.QuantidadeReservada); Assert.Equal(2, peca.QuantidadeDisponivel);
        Assert.Throws<RegraNegocioException>(() => peca.Reservar(3));
        peca.Ativo = false; Assert.Throws<RegraNegocioException>(() => peca.Reservar(1));
        Assert.Equal(3, peca.QuantidadeReservada);
    }

    internal static ExecucaoServico Nova()
    {
        var saga = Guid.NewGuid(); var liberacao = Guid.NewGuid(); var pagamento = Guid.NewGuid(); var orcamento = Guid.NewGuid();
        return new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = Guid.NewGuid(),
            SagaId = saga,
            Status = StatusExecucao.AguardandoExecucao,
            DiagnosticoConcluidoEm = DateTime.UtcNow,
            Autorizacao = new AutorizacaoExecucao
            { MensagemLiberacaoId = liberacao, PagamentoId = pagamento, OrcamentoId = orcamento, ProvedorId = "mock", ValorPago = 100m },
            RespostasLiberacao = [new RespostaLiberacaoExecucao
            { Tipo = "ExecucaoLiberada", CausationId = liberacao, SagaId = saga, PagamentoId = pagamento, OrcamentoId = orcamento, CorrelationId = "inicio" }],
            PecasInsumos = [new ItemPecaDiagnostico { PecaInsumoId = Guid.NewGuid(), Quantidade = 2, PrecoUnitario = 50m }]
        };
    }
}
