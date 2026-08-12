using FinanceHub.AuthConsent.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceHub.AuthConsent.Infrastructure.BackgroundServices;

public sealed class TokenRenewalBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenRenewalBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _expiringThreshold = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_checkInterval, timeProvider);

        logger.LogInformation("Worker proativo de renovação de tokens iniciado. Intervalo: {Interval}", _checkInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessTokenRenewalAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no loop do worker de renovação proativa de tokens.");
            }
        }
    }

    public async Task ProcessTokenRenewalAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBankConsentRepository>();
        var strategyFactory = scope.ServiceProvider.GetRequiredService<IKeyedOAuthStrategyFactory>();

        var expiringConsents = await repository.GetExpiringConsentsAsync(_expiringThreshold, cancellationToken);

        foreach (var consent in expiringConsents)
        {
            if (consent.Token.RefreshToken is null)
                continue;

            try
            {
                var strategy = strategyFactory.GetStrategy(consent.InstitutionId);
                var renewedResult = await strategy.RefreshTokenAsync(consent.Token.RefreshToken, cancellationToken);

                consent.RotateTokens(
                    newAccessToken: renewedResult.AccessToken,
                    newRefreshToken: renewedResult.RefreshToken,
                    expiresInSeconds: renewedResult.ExpiresInSeconds,
                    timeProvider: timeProvider
                );

                await repository.UpdateAsync(consent, cancellationToken);

                logger.LogInformation("Token do consentimento {ConsentId} (User: {UserId}, Banco: {InstitutionId}) renovado proativamente com sucesso.",
                    consent.Id, consent.UserId, consent.InstitutionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao renovar proativamente o token do consentimento {ConsentId}.", consent.Id);
            }
        }
    }
}
