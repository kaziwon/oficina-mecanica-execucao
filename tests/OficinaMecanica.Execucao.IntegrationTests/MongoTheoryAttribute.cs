namespace OficinaMecanica.Execucao.IntegrationTests;

public sealed class MongoTheoryAttribute : TheoryAttribute
{
    public MongoTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")))
            Skip = "Configure MONGODB_TEST_CONNECTION_STRING para executar com MongoDB real.";
    }
}
