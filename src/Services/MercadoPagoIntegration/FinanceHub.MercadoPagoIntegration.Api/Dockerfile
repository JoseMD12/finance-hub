FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5004
ENV ASPNETCORE_URLS=http://+:5004

COPY src/Services/MercadoPagoIntegration/FinanceHub.MercadoPagoIntegration.Api/bin/publish .
ENTRYPOINT ["dotnet", "FinanceHub.MercadoPagoIntegration.Api.dll"]
