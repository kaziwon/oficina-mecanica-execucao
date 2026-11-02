using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Validators;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class CriarPecaInsumoUseCase
{
    private const string NomeJaCadastrado = "Ja existe uma peca ou insumo cadastrado com este nome.";
    private readonly IPecaInsumoGateway _pecaInsumoGateway;

    public CriarPecaInsumoUseCase(IPecaInsumoGateway pecaInsumoGateway)
    {
        _pecaInsumoGateway = pecaInsumoGateway;
    }

    public PecaInsumoOutput Executar(PecaInsumoInput input)
    {
        CatalogoValidator.Validar(input.Nome, input.Descricao, input.PrecoUnitario);
        if (input.QuantidadeEstoque < 0)
            throw new ValidacaoException("O estoque inicial nao pode ser negativo.");
        if (_pecaInsumoGateway.ExistePorNome(input.Nome))
        {
            throw new ConflitoException(NomeJaCadastrado);
        }

        var pecaInsumo = new PecaInsumo
        {
            Id = Guid.NewGuid(),
            Nome = input.Nome.Trim(),
            Descricao = input.Descricao,
            PrecoUnitario = input.PrecoUnitario,
            QuantidadeEstoque = input.QuantidadeEstoque
        };

        return PecaInsumoOutputMapper.Mapear(_pecaInsumoGateway.Adicionar(pecaInsumo));
    }
}
