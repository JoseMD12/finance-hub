using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;

public record CategorizeTransactionCommand(
    Guid TransactionId,
    string UserId,
    Guid NewCategoryId,
    bool CreateCustomRule);

public interface ICategorizeTransactionCommandHandler
{
    Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken);
}

public class CategorizeTransactionCommandHandler : ICategorizeTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserCategoryRuleRepository _userCategoryRuleRepository;

    public CategorizeTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUserCategoryRuleRepository userCategoryRuleRepository)
    {
        _transactionRepository = transactionRepository;
        _userCategoryRuleRepository = userCategoryRuleRepository;
    }

    public async Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null || transaction.UserId != command.UserId)
        {
            throw new CanonicalTransactionNotFoundDomainException();
        }

        transaction.CategorizeManually(command.NewCategoryId);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        if (command.CreateCustomRule && !string.IsNullOrWhiteSpace(transaction.Description.CleanText))
        {
            var rule = UserCategoryRule.Create(
                command.UserId,
                transaction.Description.CleanText,
                command.NewCategoryId);

            await _userCategoryRuleRepository.AddOrUpdateAsync(rule, cancellationToken);
        }
    }
}
