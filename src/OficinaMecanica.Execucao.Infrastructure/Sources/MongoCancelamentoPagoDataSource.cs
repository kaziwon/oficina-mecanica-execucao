using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;
namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoCancelamentoPagoDataSource : ICancelamentoPagoDataSource
{
    private readonly IMongoClient _client;
    private readonly MongoDbContext _context;
    public MongoCancelamentoPagoDataSource(IMongoClient client, MongoDbContext context) { _client = client; _context = context; }
    public bool LiberarECancelar(ExecucaoServico execucao, long versaoEsperada)
    {
        using var session = _client.StartSession();
        return session.WithTransaction((transaction, cancellation) =>
        {
            var atualizada = _context.Execucoes.ReplaceOne(transaction,
                item => item.Id == execucao.Id && item.Versao == versaoEsperada, execucao, cancellationToken: cancellation);
            if (atualizada.MatchedCount != 1) return false;
            foreach (var reserva in execucao.ReservasPecas.OrderBy(item => item.PecaInsumoId))
            {
                var peca = _context.Pecas.Find(transaction, item => item.Id == reserva.PecaInsumoId).FirstOrDefault(cancellation)
                    ?? throw new RegraNegocioException("Peca reservada nao encontrada; cancelamento nao confirmado.");
                peca.LiberarReserva(reserva.Quantidade);
                _context.Pecas.UpdateOne(transaction, item => item.Id == peca.Id,
                    Builders<PecaInsumo>.Update.Set(item => item.QuantidadeReservada, peca.QuantidadeReservada), cancellationToken: cancellation);
            }
            return true;
        }, new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
    }
}
