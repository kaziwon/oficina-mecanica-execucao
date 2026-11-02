namespace OficinaMecanica.Execucao.Domain.Entities;

public enum StatusExecucao
{
    AguardandoDiagnostico = 1,
    EmDiagnostico = 2,
    AguardandoLiberacao = 3,
    AguardandoExecucao = 4,
    EmExecucao = 5,
    Finalizada = 6,
    Cancelada = 7
}
