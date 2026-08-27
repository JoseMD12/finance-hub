using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Commands.UpdateTransactionNotes;

public interface IUpdateTransactionNotesCommandHandler
{
    Task Handle(UpdateTransactionNotesCommand command, CancellationToken cancellationToken);
}
