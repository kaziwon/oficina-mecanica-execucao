namespace OficinaMecanica.Execucao.IntegrationTests;

public sealed class SqsTheoryAttribute : TheoryAttribute
{
    public SqsTheoryAttribute(bool exigeMongo = false)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQS_TEST_ENDPOINT")))
            Skip = "Defina SQS_TEST_ENDPOINT para executar com simulador SQS local.";
        else if (exigeMongo && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")))
            Skip = "Defina MONGODB_TEST_CONNECTION_STRING para executar com MongoDB local.";
    }
}
