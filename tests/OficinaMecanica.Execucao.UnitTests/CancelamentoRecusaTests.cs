using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.UnitTests;

public class CancelamentoRecusaTests
{
    [Fact]
    public void Cancelamento_ApenasAposDiagnosticoESemLiberacao()
    {
        var execucao = new ExecucaoServico { Status = StatusExecucao.AguardandoDiagnostico };
        Assert.Throws<RegraNegocioException>(execucao.CancelarPorRecusaOrcamento);
        execucao.IniciarDiagnostico(); execucao.ConcluirDiagnostico("Revisao", [], []);
        execucao.CancelarPorRecusaOrcamento();
        Assert.Equal(StatusExecucao.Cancelada, execucao.Status); Assert.Equal("Revisao", execucao.Diagnostico);
        Assert.Equal(3, execucao.Historico.Count);
        Assert.Throws<RegraNegocioException>(execucao.CancelarPorRecusaOrcamento);
    }
}
