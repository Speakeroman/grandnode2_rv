namespace Grand.SharedKernel.IntegrationEvents;

public record CustomerRegisteredIntegrationEvent(
    string CustomerId,
    string Email,
    string StoreId,
    string LanguageId) : IntegrationEvent;
