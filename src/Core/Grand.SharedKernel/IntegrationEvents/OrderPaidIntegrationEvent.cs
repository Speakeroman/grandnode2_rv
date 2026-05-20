namespace Grand.SharedKernel.IntegrationEvents;

public record OrderPaidIntegrationEvent(
    string OrderId,
    string CustomerId,
    string PaymentMethodSystemName,
    decimal AmountPaid,
    string CurrencyCode) : IntegrationEvent;
