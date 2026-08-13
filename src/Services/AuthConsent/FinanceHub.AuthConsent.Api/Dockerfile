FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

COPY src/Services/AuthConsent/FinanceHub.AuthConsent.Api/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.AuthConsent.Api.dll"]
