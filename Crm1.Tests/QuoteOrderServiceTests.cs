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
            QuoteDate = DateTime.Today,
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
            OrderDate = DateTime.Today,
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

    [Fact]
    public void Quote_with_a_date_before_today_is_rejected_with_a_message()
    {
        QuoteValidationError? error = NewQuoteService().Validate(ValidQuote(q => q.QuoteDate = DateTime.Today.AddDays(-1)));
        Assert.Equal(nameof(Quote.QuoteDate), error!.Field);
        Assert.Equal("La fecha no puede ser anterior a hoy.", error.Message);
    }

    [Fact]
    public void Quote_with_today_or_a_future_date_is_valid()
    {
        Assert.Null(NewQuoteService().Validate(ValidQuote(q => q.QuoteDate = DateTime.Today)));
        Assert.Null(NewQuoteService().Validate(ValidQuote(q => q.QuoteDate = DateTime.Today.AddMonths(2))));
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
    public void Order_with_a_date_before_today_is_rejected_with_a_message()
    {
        OrderValidationError? error = NewOrderService().Validate(ValidOrder(o =>
        {
            o.OrderDate = DateTime.Today.AddDays(-3);
            o.Kind = OrderKind.CreditNote;
        }));
        Assert.Equal(nameof(Order.OrderDate), error!.Field);
        Assert.Equal("La fecha no puede ser anterior a hoy.", error.Message);
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

    // Caducidad y avisos automáticos (specs/009 §10, decisión del 18/11).

    // Presupuesto Pendiente con la antigüedad indicada.
    private static Quote ExpiryQuote(int daysOld, QuoteStatus status = QuoteStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        QuoteDate = DateTime.Today.AddDays(-daysOld),
        Amount = 100m,
        Description = "Mobiliario de oficina",
        Status = status,
        RelatedId = Guid.NewGuid(),
        RelatedType = TaskRelatedType.Client
    };

    [Theory]
    [InlineData(15, 15)]
    [InlineData(23, 7)]
    [InlineData(27, 3)]
    [InlineData(28, 2)]
    [InlineData(29, 1)]
    [InlineData(30, 0)]
    public void GetExpiring_notifies_on_each_warning_day_with_days_left(int daysOld, int daysLeft)
    {
        Quote quote = ExpiryQuote(daysOld);

        List<(Quote Quote, int DaysLeft)> expiring =
            QuoteService.GetExpiring(new[] { quote }, DateTime.Today);

        (Quote found, int left) = Assert.Single(expiring);
        Assert.Equal(quote.Id, found.Id);
        Assert.Equal(daysLeft, left);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(22)]
    [InlineData(24)]
    [InlineData(26)]
    [InlineData(31)]
    public void GetExpiring_skips_days_outside_the_warning_window(int daysOld)
    {
        Assert.Empty(QuoteService.GetExpiring(new[] { ExpiryQuote(daysOld) }, DateTime.Today));
    }

    [Fact]
    public void GetExpiring_ignores_non_pending_and_future_quotes()
    {
        Quote accepted = ExpiryQuote(27, QuoteStatus.Accepted);
        Quote dismissed = ExpiryQuote(28, QuoteStatus.Dismissed);
        Quote future = ExpiryQuote(-5);

        Assert.Empty(QuoteService.GetExpiring(new[] { accepted, dismissed, future }, DateTime.Today));
    }

    [Fact]
    public void GetExpired_returns_only_pending_quotes_past_day_30()
    {
        Quote day31 = ExpiryQuote(31);
        Quote day60 = ExpiryQuote(60);
        Quote day30 = ExpiryQuote(30);
        Quote acceptedOld = ExpiryQuote(60, QuoteStatus.Accepted);
        Quote future = ExpiryQuote(-3);

        List<Quote> expired = QuoteService.GetExpired(
            new[] { day31, day60, day30, acceptedOld, future }, DateTime.Today);

        Assert.Equal(2, expired.Count);
        Assert.Contains(expired, quote => quote.Id == day31.Id);
        Assert.Contains(expired, quote => quote.Id == day60.Id);
    }

    [Fact]
    public void GetExpired_includes_prospect_quotes_too()
    {
        Quote prospectQuote = ExpiryQuote(45);
        prospectQuote.RelatedType = TaskRelatedType.Prospect;

        List<Quote> expired = QuoteService.GetExpired(new[] { prospectQuote }, DateTime.Today);

        Assert.Single(expired);
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
