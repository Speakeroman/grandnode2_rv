namespace Grand.SharedKernel.IntegrationEvents;

public record OrderCancelledIntegrationEvent(
    string OrderId,
    string CustomerId,
    string StoreId,
    string Reason) : IntegrationEvent;
