using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;

public interface ICategorizeTransactionCommandHandler
{
    Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken);
}
