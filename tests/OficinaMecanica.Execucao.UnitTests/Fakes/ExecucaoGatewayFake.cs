using System.Text.Json;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.UnitTests.Fakes;

public class ExecucaoGatewayFake : IExecucaoGateway
{
    public List<ExecucaoServico> Execucoes { get; } = [];
    public bool PermitirAtualizacao { get; set; } = true;
    private static ExecucaoServico Copiar(ExecucaoServico item) => JsonSerializer.Deserialize<ExecucaoServico>(JsonSerializer.Serialize(item))!;
    public ExecucaoServico Adicionar(ExecucaoServico item) { Execucoes.Add(Copiar(item)); return Copiar(item); }
    public ExecucaoServico? ObterPorId(Guid id)
    {
        var item = Execucoes.FirstOrDefault(existing => existing.Id == id);
        return item is null ? null : Copiar(item);
    }
    public ExecucaoServico? ObterPorOrdemServicoId(Guid id)
    {
        var item = Execucoes.FirstOrDefault(existing => existing.OrdemServicoId == id);
        return item is null ? null : Copiar(item);
    }
    public List<ExecucaoServico> ObterFila(StatusExecucao? status) => Execucoes
        .Where(item => item.Status is not (StatusExecucao.Finalizada or StatusExecucao.Cancelada)
            && (!status.HasValue || item.Status == status.Value))
        .OrderBy(item => item.CriadaEm).Select(Copiar).ToList();
    public bool Atualizar(ExecucaoServico item, long versaoEsperada)
    {
        var index = Execucoes.FindIndex(existing => existing.Id == item.Id && existing.Versao == versaoEsperada);
        if (index < 0 || !PermitirAtualizacao) return false;
        Execucoes[index] = Copiar(item);
        return true;
    }
}
