using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Commands.ToggleBillPayment;

public interface IToggleBillPaymentCommandHandler
{
    Task Handle(ToggleBillPaymentCommand command, CancellationToken cancellationToken);
}
