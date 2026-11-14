using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;
namespace OficinaMecanica.Execucao.UnitTests;

public class CompensacaoPagaTests
{
    [Fact]
    public void Cancelamento_LiberaReservaSemReporEstoqueFisico()
    {
        var execucao = InicioReparoTests.Nova(); execucao.IniciarReparo(); execucao.CancelarReparoPago();
        Assert.Equal(StatusExecucao.Cancelada, execucao.Status); Assert.NotNull(execucao.ReparoCanceladoEm);
        Assert.NotNull(Assert.Single(execucao.ReservasPecas).LiberadaEm); Assert.Null(execucao.ReservasPecas[0].ConsumidaEm);
        Assert.Throws<RegraNegocioException>(() => execucao.ConcluirReparo());
        var peca = new PecaInsumo { QuantidadeEstoque = 10, QuantidadeReservada = 2 }; peca.LiberarReserva(2);
        Assert.Equal(10, peca.QuantidadeEstoque); Assert.Equal(0, peca.QuantidadeReservada);
        Assert.Throws<RegraNegocioException>(() => peca.LiberarReserva(2));
    }
    [Fact]
    public void ReparoConcluido_NaoPodeSerCompensadoComoNaoExecutado()
    {
        var execucao = InicioReparoTests.Nova(); execucao.IniciarReparo(); execucao.ConcluirReparo();
        Assert.Throws<RegraNegocioException>(() => execucao.CancelarReparoPago());
        Assert.Equal(StatusExecucao.Finalizada, execucao.Status); Assert.Null(execucao.ReservasPecas[0].LiberadaEm);
    }
}
