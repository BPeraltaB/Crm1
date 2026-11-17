using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de las reglas de negocio de presupuestos y pedidos (specs/009):
/// fecha y precio obligatorios, vínculos existentes (pedidos solo a
/// clientes) y totales con abonos.
/// </summary>
public sealed class QuoteOrderServiceTests
{
    private readonly Client _client = new() { Id = Guid.NewGuid(), FirstName = "Ana" };
    private readonly Contact _prospect = new() { Id = Guid.NewGuid(), FirstName = "Eva" };

    private QuoteService NewQuoteService()
    {
        var records = new Dictionary<Guid, TaskRelatedType>
        {
            [_client.Id] = TaskRelatedType.Client,
            [_prospect.Id] = TaskRelatedType.Prospect
        };
        return new QuoteService(records);
    }

    private OrderService NewOrderService()
    {
        var records = new Dictionary<Guid, TaskRelatedType>
        {
            [_client.Id] = TaskRelatedType.Client,
            [_prospect.Id] = TaskRelatedType.Prospect
        };
        return new OrderService(records);
    }

    private Quote ValidQuote(Action<Quote>? tweak = null)
    {
        var quote = new Quote
        {
            QuoteDate = new DateTime(2026, 11, 10),
            Amount = 1200.56m,
            RelatedId = _client.Id,
            RelatedType = TaskRelatedType.Client
        };
        tweak?.Invoke(quote);
        return quote;
    }

    private Order ValidOrder(Action<Order>? tweak = null)
    {
        var order = new Order
        {
            Kind = OrderKind.Order,
            OrderDate = new DateTime(2026, 11, 12),
            Amount = 1200.56m,
            ClientId = _client.Id
        };
        tweak?.Invoke(order);
        return order;
    }

    // Presupuestos.

    [Fact]
    public void Quote_without_date_is_rejected()
    {
        QuoteValidationError? error = NewQuoteService().Validate(ValidQuote(q => q.QuoteDate = default));
        Assert.Equal(nameof(Quote.QuoteDate), error!.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Quote_with_non_positive_amount_is_rejected(decimal amount)
    {
        QuoteValidationError? error = NewQuoteService().Validate(ValidQuote(q => q.Amount = amount));
        Assert.Equal(nameof(Quote.Amount), error!.Field);
    }

    [Fact]
    public void Quote_for_client_and_prospect_are_both_valid()
    {
        Assert.Null(NewQuoteService().Validate(ValidQuote()));
        Assert.Null(NewQuoteService().Validate(ValidQuote(q =>
        {
            q.RelatedId = _prospect.Id;
            q.RelatedType = TaskRelatedType.Prospect;
        })));
    }

    [Fact]
    public void Quote_with_unknown_link_is_rejected()
    {
        QuoteValidationError? error = NewQuoteService().Validate(ValidQuote(q => q.RelatedId = Guid.NewGuid()));
        Assert.Equal(nameof(Quote.RelatedId), error!.Field);
    }

    [Fact]
    public void Quote_with_wrong_link_type_is_rejected()
    {
        // El Id del futuro con tipo Cliente: el tipo debe coincidir.
        QuoteValidationError? error = NewQuoteService().Validate(ValidQuote(q => q.RelatedId = _prospect.Id));
        Assert.Equal(nameof(Quote.RelatedId), error!.Field);
    }

    // Pedidos y abonos.

    [Fact]
    public void Order_without_date_or_amount_is_rejected()
    {
        Assert.Equal(nameof(Order.OrderDate),
            NewOrderService().Validate(ValidOrder(o => o.OrderDate = default))!.Field);
        Assert.Equal(nameof(Order.Amount),
            NewOrderService().Validate(ValidOrder(o => o.Amount = 0))!.Field);
    }

    [Fact]
    public void Order_for_existing_client_is_valid()
    {
        Assert.Null(NewOrderService().Validate(ValidOrder()));
    }

    [Fact]
    public void Order_for_a_prospect_is_rejected()
    {
        // Un futuro cliente con pedido ya sería cliente (specs/009 §1).
        OrderValidationError? error = NewOrderService().Validate(ValidOrder(o => o.ClientId = _prospect.Id));
        Assert.Equal(nameof(Order.ClientId), error!.Field);
        Assert.Contains("cliente", error.Message);
    }

    [Fact]
    public void Order_for_unknown_client_is_rejected()
    {
        Assert.Equal(nameof(Order.ClientId),
            NewOrderService().Validate(ValidOrder(o => o.ClientId = Guid.NewGuid()))!.Field);
    }

    // Totales con abonos (specs/009 §4.2).

    [Fact]
    public void Totals_subtract_credit_notes_from_orders()
    {
        List<Order> orders = new()
        {
            ValidOrder(o => o.Amount = 1000m),
            ValidOrder(o => o.Amount = 500.50m),
            ValidOrder(o => { o.Kind = OrderKind.CreditNote; o.Amount = 150m; }),
            ValidOrder(o => { o.Kind = OrderKind.CreditNote; o.Amount = 0.50m; })
        };

        var (orderCount, orderTotal, creditNoteCount, creditNoteTotal, net) =
            Crm1.WindowsForms.Services.OrderService.Totals(orders);

        Assert.Equal(2, orderCount);
        Assert.Equal(1500.50m, orderTotal);
        Assert.Equal(2, creditNoteCount);
        Assert.Equal(150.50m, creditNoteTotal);
        Assert.Equal(1350m, net);
    }

    [Fact]
    public void Totals_of_an_empty_list_are_zero()
    {
        var (orderCount, orderTotal, creditNoteCount, creditNoteTotal, net) =
            Crm1.WindowsForms.Services.OrderService.Totals(Array.Empty<Order>());

        Assert.Equal(0, orderCount);
        Assert.Equal(0m, orderTotal);
        Assert.Equal(0, creditNoteCount);
        Assert.Equal(0m, creditNoteTotal);
        Assert.Equal(0m, net);
    }
}
