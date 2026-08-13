FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003

COPY src/Services/ItauIntegration/FinanceHub.ItauIntegration.Api/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.ItauIntegration.Api.dll"]
