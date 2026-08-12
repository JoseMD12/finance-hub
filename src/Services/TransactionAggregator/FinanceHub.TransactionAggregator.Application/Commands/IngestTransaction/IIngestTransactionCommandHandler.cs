using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;

public interface IIngestTransactionCommandHandler
{
    Task<Guid> Handle(IngestTransactionCommand command, CancellationToken cancellationToken);
}
