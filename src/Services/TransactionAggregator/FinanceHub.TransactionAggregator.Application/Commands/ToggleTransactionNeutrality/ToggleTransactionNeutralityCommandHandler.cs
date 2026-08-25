using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Application.Commands.ToggleTransactionNeutrality;

public record ToggleTransactionNeutralityCommand(
    Guid TransactionId,
    string UserId,
    bool IsIgnoredInTotals,
    string? Reason = null);

public class ToggleTransactionNeutralityCommandHandler : IToggleTransactionNeutralityCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public ToggleTransactionNeutralityCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task Handle(ToggleTransactionNeutralityCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null || transaction.UserId != command.UserId)
        {
            throw new CanonicalTransactionNotFoundDomainException();
        }

        transaction.ToggleIgnoreInTotals(command.IsIgnoredInTotals);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
