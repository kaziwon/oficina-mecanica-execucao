using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Application.Mappers.Servicos;
namespace OficinaMecanica.Execucao.Application.Inputs.Servicos;

public class ServicoInput
{
    public ServicoInput(string nome, string descricao, decimal preco)
    {
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
    }

    public string Nome { get; }
    public string Descricao { get; }
    public decimal Preco { get; }
}
