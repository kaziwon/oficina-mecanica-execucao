using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace OficinaMecanica.Execucao.Infrastructure.Persistence;

public static class MongoSerialization
{
    private static readonly object Sync = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (Sync)
        {
            if (_configured) return;
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            ConventionRegistry.Register("execucao", new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            }, type => type.Namespace == "OficinaMecanica.Execucao.Domain.Entities");
            _configured = true;
        }
    }
}
