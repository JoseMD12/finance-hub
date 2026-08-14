FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050

COPY src/Services/ApiGateway/FinanceHub.ApiGateway/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.ApiGateway.dll"]
