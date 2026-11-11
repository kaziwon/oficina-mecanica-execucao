using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.UnitTests;

public class ConclusaoReparoTests
{
    [Fact]
    public void Conclusao_EncerraReservasComHistoricoEEventoUmaVez()
    {
        var execucao = InicioReparoTests.Nova(); execucao.IniciarReparo();
        Assert.True(execucao.ConcluirReparo()); Assert.False(execucao.ConcluirReparo());
        Assert.Equal(StatusExecucao.Finalizada, execucao.Status); Assert.Equal(2, execucao.Historico.Count);
        var reserva = Assert.Single(execucao.ReservasPecas); Assert.Equal(execucao.ReparoConcluidoEm, reserva.ConsumidaEm);
        var evento = Assert.Single(execucao.EventosConclusaoReparo);
        Assert.Equal(execucao.ReparoIniciadoEm, evento.IniciadoEm); Assert.Equal(execucao.ReparoConcluidoEm, evento.ConcluidoEm);
        Assert.Equal(execucao.Autorizacao!.MensagemLiberacaoId, evento.CausationId);
        Assert.Equal(execucao.Autorizacao.PagamentoId, evento.PagamentoId);
        Assert.False(execucao.IniciarReparo()); Assert.Equal(StatusExecucao.Finalizada, execucao.Status);
    }

    [Theory]
    [InlineData("inicio")]
    [InlineData("autorizacao")]
    [InlineData("status")]
    [InlineData("evento")]
    [InlineData("reserva")]
    [InlineData("duplicada")]
    [InlineData("consumida")]
    public void EstadoInconsistente_NaoConclui(string erro)
    {
        var execucao = InicioReparoTests.Nova(); execucao.IniciarReparo();
        if (erro == "inicio") execucao.ReparoIniciadoEm = null;
        if (erro == "autorizacao") execucao.Autorizacao = null;
        if (erro == "status") execucao.Status = StatusExecucao.Cancelada;
        if (erro == "evento") execucao.EventosReparo.Clear();
        if (erro == "reserva") execucao.ReservasPecas.Clear();
        if (erro == "duplicada") execucao.ReservasPecas.Add(execucao.ReservasPecas[0]);
        if (erro == "consumida") execucao.ReservasPecas[0].ConsumidaEm = DateTime.UtcNow;
        Assert.Throws<RegraNegocioException>(() => execucao.ConcluirReparo());
        Assert.Null(execucao.ReparoConcluidoEm); Assert.Empty(execucao.EventosConclusaoReparo);
    }

    [Fact]
    public void Consumo_BaixaFisicoEReservadoSemAlterarDisponivel()
    {
        var peca = new PecaInsumo { QuantidadeEstoque = 10, QuantidadeReservada = 4 };
        peca.ConsumirReserva(2);
        Assert.Equal(8, peca.QuantidadeEstoque); Assert.Equal(2, peca.QuantidadeReservada); Assert.Equal(6, peca.QuantidadeDisponivel);
        Assert.Throws<RegraNegocioException>(() => peca.ConsumirReserva(3));
        Assert.Throws<RegraNegocioException>(() => peca.ConsumirReserva(0));
        peca.QuantidadeEstoque = 1;
        Assert.Throws<RegraNegocioException>(() => peca.ConsumirReserva(2));
        Assert.Equal(1, peca.QuantidadeEstoque); Assert.Equal(2, peca.QuantidadeReservada);
        peca.QuantidadeEstoque = 3; peca.QuantidadeReservada = 4;
        Assert.Throws<RegraNegocioException>(() => peca.ConsumirReserva(2));
        Assert.Equal(3, peca.QuantidadeEstoque); Assert.Equal(4, peca.QuantidadeReservada);
    }
}
