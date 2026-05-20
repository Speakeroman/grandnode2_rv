namespace Grand.SharedKernel.IntegrationEvents;

public record ProductStockUpdatedIntegrationEvent(
    string ProductId,
    string WarehouseId,
    int NewStockQuantity,
    int PreviousStockQuantity) : IntegrationEvent;
