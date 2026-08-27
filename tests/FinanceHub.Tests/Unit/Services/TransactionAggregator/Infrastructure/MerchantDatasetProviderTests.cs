using System;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence.Datasets;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Infrastructure;

public class MerchantDatasetProviderTests
{
    private readonly MerchantDatasetProvider _provider = new();

    [Theory]
    [InlineData("IFD*REFEICAO", "iFood", "11111111-1111-1111-1111-111111110103")] // Delivery
    [InlineData("IFOOD.COM", "iFood", "11111111-1111-1111-1111-111111110103")] // Delivery
    [InlineData("CANVA PTY LTD", "Canva", "11111111-1111-1111-1111-111111110501")] // Streaming/Digital
    [InlineData("SUPERMAGO ZONA NORTE", "Supermago", "11111111-1111-1111-1111-111111110101")] // Supermercado
    [InlineData("REALIZE CREDITO FINANC", "Renner", "11111111-1111-1111-1111-111111110601")] // Vestuário
    [InlineData("AUTOPARK ESTAC", "AutoPark", "11111111-1111-1111-1111-111111110204")] // Estacionamento
    [InlineData("99FOOD REST", "99Food", "11111111-1111-1111-1111-111111110103")] // Delivery
    [InlineData("MERCADO SILVA LTDA", "Mercado Silva", "11111111-1111-1111-1111-111111110101")] // Supermercado
    [InlineData("CESTTO ATACADISTA", "Cestto", "11111111-1111-1111-1111-111111110101")] // Supermercado
    [InlineData("EVENTO ROCK IN RIO", "Eventos", "11111111-1111-1111-1111-111111110503")] // Eventos
    [InlineData("ARCOS DOURADOS COMERCIO", "McDonald's", "11111111-1111-1111-1111-111111110102")] // Restaurante
    [InlineData("MC DONALD S SHOPPING", "McDonald's", "11111111-1111-1111-1111-111111110102")] // Restaurante
    [InlineData("REND APLIC FINANC", "Rendimentos", "11111111-1111-1111-1111-111111110902")] // Rendimentos
    [InlineData("PIX ENVIADO PARA FULANO", "Pix Enviado", "11111111-1111-1111-1111-111111111002")] // Transferências (quando não há loja)
    [InlineData("PIX RECEBIDO DE CICLANO", "Pix Recebido", "11111111-1111-1111-1111-111111110905")] // Pix Recebido (quando não há loja)
    [InlineData("MELIMAIS ASSINATURA", "Meli+", "11111111-1111-1111-1111-111111110607")] // Marketplace
    [InlineData("FOLHA DE PAGAMENTO EMPRESA", "Salário", "11111111-1111-1111-1111-111111110901")] // Salário
    [InlineData("ARMAZEM DO ZE", "Armazém", "11111111-1111-1111-1111-111111110101")] // Supermercado
    [InlineData("TEMBICI BIKE POA", "Tembici / Bike Itaú", "11111111-1111-1111-1111-111111110203")] // Passagens/Mobilidade
    [InlineData("TIKTOK LIVE BR", "TikTok", "11111111-1111-1111-1111-111111110501")] // Streaming/Digital
    [InlineData("NATURA COSMETICOS", "Natura", "11111111-1111-1111-1111-111111110603")] // Cosméticos
    [InlineData("COBASI PET SHOP", "Cobasi", "11111111-1111-1111-1111-111111110605")] // Pet Shop
    [InlineData("PETLOVE PRODUTOS", "Petlove", "11111111-1111-1111-1111-111111110605")] // Pet Shop
    [InlineData("PET CLINIC CLINICA", "Pet Shop", "11111111-1111-1111-1111-111111110605")] // Pet Shop
    [InlineData("LOJA DE CONVENIENCIA POSTO", "Conveniência", "11111111-1111-1111-1111-111111110101")] // Supermercado/Conveniência
    [InlineData("BURGUER ARTESANAL", "Restaurante / Lanchonete", "11111111-1111-1111-1111-111111110102")] // Restaurante
    [InlineData("JET BRASIL PATINETE", "Patinetes / Micromobilidade", "11111111-1111-1111-1111-111111110203")] // Passagens/Mobilidade
    [InlineData("WHOOSH SCOOTERS", "Patinetes / Micromobilidade", "11111111-1111-1111-1111-111111110203")] // Passagens/Mobilidade
    [InlineData("SYMPLA INGRESSOS", "Sympla", "11111111-1111-1111-1111-111111110503")] // Eventos
    [InlineData("SHOTGUN LIVE TICKET", "Shotgun", "11111111-1111-1111-1111-111111110503")] // Eventos
    [InlineData("KALUNGA COM BR", "Kalunga", "11111111-1111-1111-1111-111111110606")] // Papelaria
    // Novos itens com pesos e resolução de conflito de canais (PIX / TED)
    [InlineData("PIX ENVIADO - SUPERLEGAL BRINQUEDOS", "Superlegal", "11111111-1111-1111-1111-111111110607")] // Prioridade da Loja sobre Pix Enviado
    [InlineData("PIX ENVIADO - RI HAPPY", "Ri Happy", "11111111-1111-1111-1111-111111110607")] // Prioridade da Loja sobre Pix Enviado
    [InlineData("PIX ENVIADO - ERENITO XAVIER RESTAURANTE", "Restaurante Erenito Xavier", "11111111-1111-1111-1111-111111110102")] // Prioridade do RU Erenito Xavier
    [InlineData("TED RECEBIDA INSTITUTO FUNDATEC", "Fundatec", "11111111-1111-1111-1111-111111110901")] // Prioridade de Salário Fundatec
    [InlineData("PAGAMENTO PIX MINI KALZONE", "Mini Kalzone", "11111111-1111-1111-1111-111111110102")] // Prioridade do Mini Kalzone
    [InlineData("DINHEIRO RESERVADO NO COFRINHO", "Dinheiro Reservado (Cofrinho)", "11111111-1111-1111-1111-111111110805")] // Investimentos
    // Resgate de cofrinho aponta para Finanças > Investimentos, e não para Receitas > Rendimentos.
    // Dinheiro voltando de aplicação não é receita nova: catalogado como Rendimentos, ele era
    // somado à renda do período e inflava tanto as "entradas" quanto o disponível para gastar.
    // Investimentos tem natureza Investment, logo é neutro nos totais.
    [InlineData("DINHEIRO RETIRADO COFRINHO", "Dinheiro Retirado (Cofrinho)", "11111111-1111-1111-1111-111111110805")] // Investimentos / Resgate
    [InlineData("PAGAMENTO DE FATURA CARTAO DE CREDITO", "Fatura de Cartão", "11111111-1111-1111-1111-111111110801")] // Fatura
    [InlineData("PAGAMENTO RECEBIDO DE CLIENTE", "Pagamento Recebido", "11111111-1111-1111-1111-111111110905")] // Recebimentos
    [InlineData("PIX ENVIADO - FERNANDA DE OLIVEIRA CLIMUS", "Fernanda de Oliveira (Terapeuta)", "11111111-1111-1111-1111-111111110402")] // Consultas / Terapia
    [InlineData("PAGAMENTO PIX CARTAO DE TODOS SAUDE", "Cartão de TODOS", "11111111-1111-1111-1111-111111110403")] // Plano de Saúde
    [InlineData("PAGAMENTO COM QR PIX SOCIEDADE DE EDUCACAO RITTER DOS REIS LTDA.", "UniRitter", "11111111-1111-1111-1111-111111110703")] // Mensalidades UniRitter
    [InlineData("PAGTO MENSALIDADE PUCRS TECNOPUC", "PUCRS / Tecnopuc", "11111111-1111-1111-1111-111111110703")] // Mensalidades PUCRS
    [InlineData("COMPRA CARTAO CENTER SHOP PORTO ALEGRE", "Center Shop", "11111111-1111-1111-1111-111111110101")] // Supermercado Center Shop
    [InlineData("SUPERMERCADO GECEPEL POA", "Gecepel", "11111111-1111-1111-1111-111111110101")] // Supermercado Gecepel
    public void Match_ShouldIdentifyCorrectMerchantAndSubcategory(string input, string expectedCleanNameOrName, string expectedCategoryId)
    {
        // Act
        var match = _provider.Match(input);

        // Assert
        match.Should().NotBeNull();
        match!.CategoryId.Should().Be(Guid.Parse(expectedCategoryId));
        (match.CleanName == expectedCleanNameOrName || match.Name == expectedCleanNameOrName).Should().BeTrue();
    }
}
