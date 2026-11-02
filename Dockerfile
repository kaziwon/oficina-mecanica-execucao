FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/OficinaMecanica.Execucao.Api/OficinaMecanica.Execucao.Api.csproj
RUN dotnet publish src/OficinaMecanica.Execucao.Api/OficinaMecanica.Execucao.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "OficinaMecanica.Execucao.Api.dll"]
