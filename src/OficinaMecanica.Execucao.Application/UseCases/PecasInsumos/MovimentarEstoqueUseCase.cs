using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class MovimentarEstoqueUseCase
{
    private readonly IPecaInsumoGateway _pecas;

    public MovimentarEstoqueUseCase(IPecaInsumoGateway pecas)
    {
        _pecas = pecas;
    }

    public PecaInsumoOutput? Executar(Guid id, int quantidade)
    {
        if (quantidade == 0 || quantidade == int.MinValue)
            throw new ValidacaoException("Informe uma quantidade positiva para entrada ou negativa para saida.");
        var peca = _pecas.MovimentarEstoque(id, quantidade);
        if (peca is not null)
            return PecaInsumoOutputMapper.Mapear(peca);
        if (_pecas.ObterPorId(id) is null)
            return null;
        throw new ConflitoException("A movimentacao excede o saldo disponivel ou o limite do estoque.");
    }
}
