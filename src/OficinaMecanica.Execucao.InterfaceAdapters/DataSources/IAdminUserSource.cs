namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IAdminUserSource
{
    bool CredenciaisValidas(string username, string password);
}
