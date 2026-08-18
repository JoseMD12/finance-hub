FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish \
    src/Services/TransactionAggregator/FinanceHub.TransactionAggregator.Api/FinanceHub.TransactionAggregator.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinanceHub.TransactionAggregator.Api.dll"]
