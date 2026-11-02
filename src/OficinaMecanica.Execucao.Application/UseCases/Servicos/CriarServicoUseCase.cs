using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Application.Mappers.Servicos;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Validators;

namespace OficinaMecanica.Execucao.Application.UseCases.Servicos;

public class CriarServicoUseCase
{
    private const string NomeJaCadastrado = "Ja existe um serviço cadastrado com este nome.";
    private readonly IServicoGateway _servicoGateway;

    public CriarServicoUseCase(IServicoGateway servicoGateway)
    {
        _servicoGateway = servicoGateway;
    }

    public ServicoOutput Executar(ServicoInput input)
    {
        CatalogoValidator.Validar(input.Nome, input.Descricao, input.Preco);
        if (_servicoGateway.ExistePorNome(input.Nome))
        {
            throw new ConflitoException(NomeJaCadastrado);
        }

        var servico = new Servico
        {
            Id = Guid.NewGuid(),
            Nome = input.Nome.Trim(),
            Descricao = input.Descricao,
            Preco = input.Preco
        };

        return ServicoOutputMapper.Mapear(_servicoGateway.Adicionar(servico));
    }
}
