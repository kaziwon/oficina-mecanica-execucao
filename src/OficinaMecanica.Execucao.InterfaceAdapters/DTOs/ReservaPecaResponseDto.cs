namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ReservaPecaResponseDto
{
    public Guid PecaInsumoId { get; set; }
    public int Quantidade { get; set; }
    public DateTime ReservadaEm { get; set; }
    public DateTime? ConsumidaEm { get; set; }
    public DateTime? LiberadaEm { get; set; }
}
