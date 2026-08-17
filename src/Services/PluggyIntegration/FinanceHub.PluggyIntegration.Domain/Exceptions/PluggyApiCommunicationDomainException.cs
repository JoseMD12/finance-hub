namespace FinanceHub.PluggyIntegration.Domain.Exceptions;

public class PluggyApiCommunicationDomainException : DomainException
{
    public PluggyApiCommunicationDomainException(string message, Exception? innerException = null)
        : base(message, innerException!, "PLUGGY_API_UNAVAILABLE", statusCode: 502)
    {
    }
}
