using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ConcluirDiagnosticoUseCase
{
    private readonly IExecucaoGateway _execucoes;
    private readonly IServicoGateway _servicos;
    private readonly IPecaInsumoGateway _pecas;

    public ConcluirDiagnosticoUseCase(IExecucaoGateway execucoes, IServicoGateway servicos, IPecaInsumoGateway pecas)
    {
        _execucoes = execucoes;
        _servicos = servicos;
        _pecas = pecas;
    }

    public ExecucaoOutput? Executar(Guid id, ConcluirDiagnosticoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Diagnostico) || input.Diagnostico.Trim().Length > 2000)
            throw new ValidacaoException("Informe o diagnostico com ate 2000 caracteres.");
        if (input.ServicoIds is null || input.PecasInsumos is null)
            throw new ValidacaoException("As listas de itens nao podem ser nulas.");
        if (input.ServicoIds.Contains(Guid.Empty) || input.ServicoIds.Distinct().Count() != input.ServicoIds.Count)
            throw new ValidacaoException("Informe servicos validos, sem duplicatas.");
        if (input.PecasInsumos.Any(item => item is null || item.PecaInsumoId == Guid.Empty || item.Quantidade <= 0)
            || input.PecasInsumos.Select(item => item.PecaInsumoId).Distinct().Count() != input.PecasInsumos.Count)
            throw new ValidacaoException("Informe pecas validas e quantidades positivas, sem duplicatas.");

        var execucao = _execucoes.ObterPorId(id);
        if (execucao is null) return null;
        var servicos = input.ServicoIds.Select(servicoId =>
        {
            var servico = _servicos.ObterPorId(servicoId)
                ?? throw new ConflitoException("Um servico informado nao esta ativo no catalogo.");
            return new ItemServicoDiagnostico { ServicoId = servico.Id, Nome = servico.Nome, Preco = servico.Preco };
        }).ToList();
        var pecas = input.PecasInsumos.Select(item =>
        {
            var peca = _pecas.ObterPorId(item.PecaInsumoId)
                ?? throw new ConflitoException("Uma peca informada nao esta ativa no catalogo.");
            return new ItemPecaDiagnostico
            {
                PecaInsumoId = peca.Id,
                Nome = peca.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = peca.PrecoUnitario
            };
        }).ToList();

        var versao = execucao.Versao;
        execucao.ConcluirDiagnostico(input.Diagnostico, servicos, pecas);
        if (!_execucoes.Atualizar(execucao, versao))
            throw new ConflitoException("A execucao foi alterada por outra requisicao. Consulte novamente.");
        return ExecucaoOutputMapper.Mapear(execucao);
    }
}
