using OficinaMecanica.Execucao.Application.Outputs.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

internal static class ExecucaoOutputMapper
{
    public static ExecucaoOutput Mapear(ExecucaoServico execucao)
    {
        return new ExecucaoOutput
        {
            Id = execucao.Id,
            OrdemServicoId = execucao.OrdemServicoId,
            DescricaoSolicitacao = execucao.DescricaoSolicitacao,
            Diagnostico = execucao.Diagnostico,
            Status = execucao.Status,
            CriadaEm = execucao.CriadaEm,
            DiagnosticoIniciadoEm = execucao.DiagnosticoIniciadoEm,
            DiagnosticoConcluidoEm = execucao.DiagnosticoConcluidoEm,
            OrcamentoId = execucao.Autorizacao?.OrcamentoId,
            PagamentoId = execucao.Autorizacao?.PagamentoId,
            LiberadaEm = execucao.Autorizacao?.LiberadaEm,
            ReparoIniciadoEm = execucao.ReparoIniciadoEm,
            ReparoConcluidoEm = execucao.ReparoConcluidoEm,
            ReservasPecas = execucao.ReservasPecas.Select(item => new ReservaPecaOutput
            { PecaInsumoId = item.PecaInsumoId, Quantidade = item.Quantidade, ReservadaEm = item.ReservadaEm, ConsumidaEm = item.ConsumidaEm }).ToList(),
            Versao = execucao.Versao,
            Servicos = execucao.Servicos.Select(item => new ItemServicoDiagnosticoOutput
            {
                ServicoId = item.ServicoId,
                Nome = item.Nome,
                Preco = item.Preco
            }).ToList(),
            PecasInsumos = execucao.PecasInsumos.Select(item => new ItemPecaDiagnosticoOutput
            {
                PecaInsumoId = item.PecaInsumoId,
                Nome = item.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario
            }).ToList(),
            Historico = execucao.Historico.Select(item => new HistoricoExecucaoOutput
            {
                Status = item.Status,
                RegistradoEm = item.RegistradoEm
            }).ToList()
        };
    }
}
