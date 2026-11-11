namespace OficinaMecanica.Execucao.Domain.Entities;

public class ReservaPeca
{
    public Guid PecaInsumoId { get; set; }
    public int Quantidade { get; set; }
    public DateTime ReservadaEm { get; set; }
    public DateTime? ConsumidaEm { get; set; }
}
