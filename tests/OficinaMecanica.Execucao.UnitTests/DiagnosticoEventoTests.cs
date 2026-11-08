using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.UnitTests;

public class DiagnosticoEventoTests
{
    [Fact]
    public void DiagnosticoVinculado_GeraSnapshotIndependenteComCorrelacao()
    {
        var execucao = Nova();
        execucao.IniciarDiagnostico();
        var servico = new ItemServicoDiagnostico { ServicoId = Guid.NewGuid(), Nome = "Revisao", Preco = 100m };
        var peca = new ItemPecaDiagnostico { PecaInsumoId = Guid.NewGuid(), Nome = "Filtro", Quantidade = 2, PrecoUnitario = 50m };
        execucao.ConcluirDiagnostico("Trocar filtro", [servico], [peca]);
        servico.Preco = 999m; peca.Quantidade = 999;
        Assert.Equal(2, execucao.EventosDiagnostico.Count);
        var mensagem = EventoDiagnosticoMapper.Mapear(execucao.EventosDiagnostico[1]);
        Assert.Equal("DiagnosticoConcluido", mensagem.Type);
        Assert.Equal(execucao.PedidoRegistroId, mensagem.CausationId);
        Assert.Equal("teste-saga", mensagem.CorrelationId);
        Assert.Equal(2, mensagem.Payload.Itens!.Count);
        Assert.Equal(100m, mensagem.Payload.Itens[0].PrecoUnitario);
        Assert.Equal(2, mensagem.Payload.Itens[1].Quantidade);
        Assert.Equal(execucao.DiagnosticoIniciadoEm, mensagem.Payload.DiagnosticoIniciadoEm);
        Assert.Throws<Domain.Exceptions.RegraNegocioException>(() => execucao.ConcluirDiagnostico("Repeticao", [], []));
        Assert.Equal(2, execucao.EventosDiagnostico.Count);
    }

    [Fact]
    public void EntradaManual_NaoInventaUmaSaga()
    {
        var execucao = Nova(); execucao.SagaId = null; execucao.PedidoRegistroId = null;
        execucao.IniciarDiagnostico(); execucao.ConcluirDiagnostico("Revisao", [], []);
        Assert.Empty(execucao.EventosDiagnostico);
    }

    private static ExecucaoServico Nova()
    {
        var saga = Guid.NewGuid(); var pedido = Guid.NewGuid();
        return new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = Guid.NewGuid(),
            SagaId = saga,
            PedidoRegistroId = pedido,
            Status = StatusExecucao.AguardandoDiagnostico,
            RespostasEntrada = [new RespostaEntrada { Tipo = "ExecucaoRegistrada", SagaId = saga,
                CausationId = pedido, CorrelationId = "teste-saga" }]
        };
    }
}
