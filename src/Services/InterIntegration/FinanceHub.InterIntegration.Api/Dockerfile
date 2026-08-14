FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005

COPY src/Services/InterIntegration/FinanceHub.InterIntegration.Api/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.InterIntegration.Api.dll"]
