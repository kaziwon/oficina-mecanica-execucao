namespace OficinaMecanica.Execucao.Domain.Entities;

public class PecaInsumo
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int QuantidadeReservada { get; set; }
    public int QuantidadeDisponivel => QuantidadeEstoque - QuantidadeReservada;
    public bool Ativo { get; set; } = true;

    public void Reservar(int quantidade)
    {
        if (!Ativo || quantidade <= 0 || QuantidadeDisponivel < quantidade)
            throw new Exceptions.RegraNegocioException("Peca inativa ou estoque disponivel insuficiente para a reserva.");
        QuantidadeReservada += quantidade;
    }
}
