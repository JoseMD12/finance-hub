FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish \
    src/Services/ApiGateway/FinanceHub.ApiGateway/FinanceHub.ApiGateway.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050
USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinanceHub.ApiGateway.dll"]
