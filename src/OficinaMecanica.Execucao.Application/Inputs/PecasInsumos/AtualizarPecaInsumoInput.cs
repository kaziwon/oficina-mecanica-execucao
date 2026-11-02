namespace OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;

public class AtualizarPecaInsumoInput
{
    public AtualizarPecaInsumoInput(string nome, string descricao, decimal precoUnitario)
    {
        Nome = nome;
        Descricao = descricao;
        PrecoUnitario = precoUnitario;
    }

    public string Nome { get; }
    public string Descricao { get; }
    public decimal PrecoUnitario { get; }
}
