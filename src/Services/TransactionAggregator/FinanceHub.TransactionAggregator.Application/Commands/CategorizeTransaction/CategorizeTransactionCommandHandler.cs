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
    bool CreateCustomRule,
    bool ApplyToPastTransactions = false);

public class CategorizeTransactionCommandHandler : ICategorizeTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserCategoryRuleRepository _userCategoryRuleRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CategorizeTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUserCategoryRuleRepository userCategoryRuleRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _userCategoryRuleRepository = userCategoryRuleRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null || transaction.UserId != command.UserId)
        {
            throw new CanonicalTransactionNotFoundDomainException();
        }

        // A natureza acompanha a nova categoria: sem isso, categoria e neutralidade se
        // descolavam permanentemente após a ingestão.
        var nature = await _categoryRepository.GetNatureByCategoryIdAsync(
            command.NewCategoryId, cancellationToken);

        transaction.CategorizeManually(command.NewCategoryId, nature);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        if (command.CreateCustomRule && !string.IsNullOrWhiteSpace(transaction.Description.CleanText))
        {
            var rule = UserCategoryRule.Create(
                command.UserId,
                transaction.Description.CleanText,
                command.NewCategoryId);

            await _userCategoryRuleRepository.AddOrUpdateAsync(rule, cancellationToken);
        }

        if (command.ApplyToPastTransactions && !string.IsNullOrWhiteSpace(transaction.Description.CleanText))
        {
            await _transactionRepository.UpdateCategoryForPatternAsync(
                command.UserId,
                transaction.Description.CleanText,
                command.NewCategoryId,
                cancellationToken);
        }
    }
}
