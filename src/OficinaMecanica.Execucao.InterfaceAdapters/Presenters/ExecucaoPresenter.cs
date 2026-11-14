using OficinaMecanica.Execucao.Application.Outputs.Execucoes;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Presenters;

public static class ExecucaoPresenter
{
    public static ExecucaoResponseDto Apresentar(ExecucaoOutput output) => new()
    {
        Id = output.Id,
        OrdemServicoId = output.OrdemServicoId,
        DescricaoSolicitacao = output.DescricaoSolicitacao,
        Diagnostico = output.Diagnostico,
        Status = output.Status,
        CriadaEm = output.CriadaEm,
        DiagnosticoIniciadoEm = output.DiagnosticoIniciadoEm,
        DiagnosticoConcluidoEm = output.DiagnosticoConcluidoEm,
        OrcamentoId = output.OrcamentoId,
        PagamentoId = output.PagamentoId,
        LiberadaEm = output.LiberadaEm,
        ReparoIniciadoEm = output.ReparoIniciadoEm,
        ReparoConcluidoEm = output.ReparoConcluidoEm,
        ReparoCanceladoEm = output.ReparoCanceladoEm,
        ReservasPecas = output.ReservasPecas.Select(item => new ReservaPecaResponseDto
        { PecaInsumoId = item.PecaInsumoId, Quantidade = item.Quantidade, ReservadaEm = item.ReservadaEm, ConsumidaEm = item.ConsumidaEm, LiberadaEm = item.LiberadaEm }).ToList(),
        Versao = output.Versao,
        Historico = output.Historico.Select(item => new HistoricoExecucaoResponseDto
        {
            Status = item.Status,
            RegistradoEm = item.RegistradoEm
        }).ToList(),
        Servicos = output.Servicos.Select(item => new ItemServicoDiagnosticoResponseDto
        {
            ServicoId = item.ServicoId,
            Nome = item.Nome,
            Preco = item.Preco
        }).ToList(),
        PecasInsumos = output.PecasInsumos.Select(item => new ItemPecaDiagnosticoResponseDto
        {
            PecaInsumoId = item.PecaInsumoId,
            Nome = item.Nome,
            Quantidade = item.Quantidade,
            PrecoUnitario = item.PrecoUnitario
        }).ToList()
    };
}
