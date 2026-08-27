using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Application.Commands.UpdateTransactionNotes;

public record UpdateTransactionNotesCommand(
    Guid TransactionId,
    string UserId,
    string? Notes);

public class UpdateTransactionNotesCommandHandler : IUpdateTransactionNotesCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public UpdateTransactionNotesCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task Handle(UpdateTransactionNotesCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null || transaction.UserId != command.UserId)
        {
            throw new CanonicalTransactionNotFoundDomainException();
        }

        transaction.UpdateNotes(command.Notes);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
