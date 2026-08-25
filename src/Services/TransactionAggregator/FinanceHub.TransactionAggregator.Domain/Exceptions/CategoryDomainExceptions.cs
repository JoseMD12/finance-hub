namespace FinanceHub.TransactionAggregator.Domain.Exceptions;

public class InvalidCategoryNameDomainException : TransactionAggregatorDomainException
{
    public InvalidCategoryNameDomainException()
        : base("Nome da categoria e obrigatorio e nao pode ser vazio.", "INVALID_CATEGORY_NAME", 400)
    {
    }
}

public class InvalidCategorySlugDomainException : TransactionAggregatorDomainException
{
    public InvalidCategorySlugDomainException()
        : base("Slug da categoria e obrigatorio e deve ser valido.", "INVALID_CATEGORY_SLUG", 400)
    {
    }
}

public class CategoryNotFoundDomainException : TransactionAggregatorDomainException
{
    public CategoryNotFoundDomainException()
        : base("Categoria nao encontrada.", "CATEGORY_NOT_FOUND", 404)
    {
    }
}
