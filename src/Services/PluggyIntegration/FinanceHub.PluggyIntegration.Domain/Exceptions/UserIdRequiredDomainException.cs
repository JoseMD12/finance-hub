namespace FinanceHub.PluggyIntegration.Domain.Exceptions;

public class UserIdRequiredDomainException : DomainException
{
    public UserIdRequiredDomainException()
        : base("UserId é obrigatório para sincronização.", "PLUGGY_USER_ID_REQUIRED", 400)
    {
    }
}
