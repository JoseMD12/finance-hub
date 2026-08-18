FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Services/PluggyIntegration/FinanceHub.PluggyIntegration.Api/FinanceHub.PluggyIntegration.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinanceHub.PluggyIntegration.Api.dll"]
