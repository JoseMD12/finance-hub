using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Application.Commands.ToggleBillPayment;

public record ToggleBillPaymentCommand(
    Guid TransactionId,
    string UserId,
    bool IsBillPayment);

public class ToggleBillPaymentCommandHandler : IToggleBillPaymentCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public ToggleBillPaymentCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task Handle(ToggleBillPaymentCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null || transaction.UserId != command.UserId)
        {
            throw new CanonicalTransactionNotFoundDomainException();
        }

        if (command.IsBillPayment)
        {
            transaction.MarkAsBillPayment();
        }
        else
        {
            transaction.UnmarkBillPayment();
        }

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
