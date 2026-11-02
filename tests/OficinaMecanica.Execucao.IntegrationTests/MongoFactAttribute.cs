namespace OficinaMecanica.Execucao.IntegrationTests;

public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")))
            Skip = "Defina MONGODB_TEST_CONNECTION_STRING para executar com MongoDB real.";
    }
}
