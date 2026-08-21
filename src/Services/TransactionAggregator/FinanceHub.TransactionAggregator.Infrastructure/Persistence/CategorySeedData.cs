using System;
using System.Collections.Generic;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public static class CategorySeedData
{
    public static readonly Guid FoodId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid TransportId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid HousingId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid HealthId = Guid.Parse("11111111-1111-1111-1111-111111111104");
    public static readonly Guid LeisureId = Guid.Parse("11111111-1111-1111-1111-111111111105");
    public static readonly Guid ShoppingId = Guid.Parse("11111111-1111-1111-1111-111111111106");
    public static readonly Guid EducationId = Guid.Parse("11111111-1111-1111-1111-111111111107");
    public static readonly Guid FinanceId = Guid.Parse("11111111-1111-1111-1111-111111111108");
    public static readonly Guid IncomeId = Guid.Parse("11111111-1111-1111-1111-111111111109");
    public static readonly Guid OthersId = Guid.Parse("11111111-1111-1111-1111-111111111110");

    public static List<Category> GetDefaultCategories()
    {
        var list = new List<Category>();

        // 1. Alimentação (food)
        list.Add(Category.Create("Alimentação", "food", "utensils", "emerald", isSystemDefault: true, id: FoodId));
        list.Add(Category.Create("Supermercado", "food-supermarket", "shopping-cart", "emerald", isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Restaurante", "food-restaurant", "utensils", "emerald", isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Delivery", "food-delivery", "bike", "emerald", isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Padaria", "food-bakery", "croissant", "emerald", isSystemDefault: true, parentCategoryId: FoodId));

        // 2. Transporte (transport)
        list.Add(Category.Create("Transporte", "transport", "car", "sky", isSystemDefault: true, id: TransportId));
        list.Add(Category.Create("Aplicativos", "transport-rideshare", "car", "sky", isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Combustível", "transport-fuel", "fuel", "sky", isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Passagens", "transport-transit", "ticket", "sky", isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Estacionamento", "transport-parking", "circle-parking", "sky", isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Manutenção", "transport-maintenance", "wrench", "sky", isSystemDefault: true, parentCategoryId: TransportId));

        // 3. Moradia (housing)
        list.Add(Category.Create("Moradia", "housing", "home", "amber", isSystemDefault: true, id: HousingId));
        list.Add(Category.Create("Aluguel", "housing-rent", "building", "amber", isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Condomínio", "housing-condo", "building-2", "amber", isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Energia", "housing-electricity", "zap", "amber", isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Água", "housing-water", "droplet", "amber", isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Gás", "housing-gas", "flame", "amber", isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Internet", "housing-internet", "wifi", "amber", isSystemDefault: true, parentCategoryId: HousingId));

        // 4. Saúde (health)
        list.Add(Category.Create("Saúde", "health", "heart-pulse", "rose", isSystemDefault: true, id: HealthId));
        list.Add(Category.Create("Farmácia", "health-pharmacy", "pill", "rose", isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Consultas", "health-doctor", "stethoscope", "rose", isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Plano", "health-insurance", "shield-plus", "rose", isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Academia", "health-gym", "dumbbell", "rose", isSystemDefault: true, parentCategoryId: HealthId));

        // 5. Lazer (leisure)
        list.Add(Category.Create("Lazer", "leisure", "tv", "purple", isSystemDefault: true, id: LeisureId));
        list.Add(Category.Create("Streaming", "leisure-streaming", "play", "purple", isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Viagens", "leisure-travel", "plane", "purple", isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Eventos", "leisure-events", "ticket", "purple", isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Hobbies", "leisure-hobbies", "gamepad", "purple", isSystemDefault: true, parentCategoryId: LeisureId));

        // 6. Compras (shopping)
        list.Add(Category.Create("Compras", "shopping", "shopping-bag", "indigo", isSystemDefault: true, id: ShoppingId));
        list.Add(Category.Create("Vestuário", "shopping-clothing", "shirt", "indigo", isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Eletrônicos", "shopping-electronics", "laptop", "indigo", isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Cosméticos", "shopping-cosmetics", "sparkles", "indigo", isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Decoração", "shopping-decor", "lamp", "indigo", isSystemDefault: true, parentCategoryId: ShoppingId));

        // 7. Educação (education)
        list.Add(Category.Create("Educação", "education", "graduation-cap", "teal", isSystemDefault: true, id: EducationId));
        list.Add(Category.Create("Cursos", "education-courses", "video", "teal", isSystemDefault: true, parentCategoryId: EducationId));
        list.Add(Category.Create("Livros", "education-books", "book-open", "teal", isSystemDefault: true, parentCategoryId: EducationId));
        list.Add(Category.Create("Mensalidades", "education-tuition", "school", "teal", isSystemDefault: true, parentCategoryId: EducationId));

        // 8. Finanças (finance)
        list.Add(Category.Create("Finanças", "finance", "landmark", "blue", isSystemDefault: true, id: FinanceId));
        list.Add(Category.Create("Tarifas", "finance-fees", "receipt", "blue", isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Impostos", "finance-taxes", "file-text", "blue", isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Juros", "finance-interest", "percent", "blue", isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Seguros", "finance-insurance", "shield", "blue", isSystemDefault: true, parentCategoryId: FinanceId));

        // 9. Receitas (income)
        list.Add(Category.Create("Receitas", "income", "trending-up", "green", isSystemDefault: true, id: IncomeId));
        list.Add(Category.Create("Salário", "income-salary", "briefcase", "green", isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Rendimentos", "income-yield", "coins", "green", isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Reembolsos", "income-refund", "rotate-ccw", "green", isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Freelance", "income-freelance", "laptop", "green", isSystemDefault: true, parentCategoryId: IncomeId));

        // 10. Outros (others)
        list.Add(Category.Create("Outros", "others", "tag", "gray", isSystemDefault: true, id: OthersId));
        list.Add(Category.Create("Ajustes", "others-adjustments", "sliders", "gray", isSystemDefault: true, parentCategoryId: OthersId));
        list.Add(Category.Create("Transferências", "others-transfers", "arrow-left-right", "gray", isSystemDefault: true, parentCategoryId: OthersId));
        list.Add(Category.Create("Diversos", "others-general", "tag", "gray", isSystemDefault: true, parentCategoryId: OthersId));

        return list;
    }
}
