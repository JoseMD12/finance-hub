using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Commands.ToggleTransactionNeutrality;

public interface IToggleTransactionNeutralityCommandHandler
{
    Task Handle(ToggleTransactionNeutralityCommand command, CancellationToken cancellationToken);
}
