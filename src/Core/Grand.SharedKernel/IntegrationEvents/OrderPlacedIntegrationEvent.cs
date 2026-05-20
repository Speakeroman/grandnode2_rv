namespace Grand.SharedKernel.IntegrationEvents;

public record OrderPlacedIntegrationEvent(
    string OrderId,
    string CustomerId,
    string StoreId,
    IReadOnlyList<OrderItem> Items,
    decimal OrderTotal,
    string CurrencyCode) : IntegrationEvent;

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
