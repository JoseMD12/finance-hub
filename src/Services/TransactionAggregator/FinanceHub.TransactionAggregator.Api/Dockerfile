FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002

COPY src/Services/TransactionAggregator/FinanceHub.TransactionAggregator.Api/bin/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FinanceHub.TransactionAggregator.Api.dll"]
