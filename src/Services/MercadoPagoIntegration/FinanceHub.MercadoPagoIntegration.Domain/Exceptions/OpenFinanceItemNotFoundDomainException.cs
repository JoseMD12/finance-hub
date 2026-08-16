namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class OpenFinanceItemNotFoundDomainException : DomainException
{
    public OpenFinanceItemNotFoundDomainException(string itemId)
        : base($"Conexão Open Finance do Mercado Pago não foi encontrada para o item '{itemId}'.", "OPENFINANCE_ITEM_NOT_FOUND", statusCode: 404)
    {
    }
}
