FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056

COPY src/Services/PluggyIntegration/FinanceHub.PluggyIntegration.Api/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.PluggyIntegration.Api.dll"]
