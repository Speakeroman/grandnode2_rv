using Grand.Domain.Cart;
using Grand.Domain.Common;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Cart;

/// <summary>
/// Cart service using the ShoppingCart aggregate — the target interface once
/// ShoppingCartItem[] is fully extracted from the Customer document.
/// Replaces the Customer-coupled IShoppingCartService.
/// </summary>
public interface ICartService
{
    Task<ShoppingCart> GetCartAsync(string customerId, string storeId, CancellationToken cancellationToken = default);

    Task<ShoppingCartItem> FindCartItemAsync(
        ShoppingCart cart,
        ShoppingCartType cartType,
        string productId,
        string warehouseId = null,
        IList<CustomAttribute> attributes = null,
        double? customerEnteredPrice = null,
        DateTime? rentalStartDate = null,
        DateTime? rentalEndDate = null);

    Task<(IList<string> warnings, ShoppingCartItem item)> AddItemAsync(
        string customerId,
        string productId,
        ShoppingCartType cartType,
        string storeId,
        string warehouseId = null,
        IList<CustomAttribute> attributes = null,
        double? customerEnteredPrice = null,
        DateTime? rentalStartDate = null,
        DateTime? rentalEndDate = null,
        int quantity = 1);

    Task<IList<string>> UpdateItemAsync(
        string customerId,
        string cartItemId,
        string warehouseId,
        IList<CustomAttribute> attributes,
        double? customerEnteredPrice = null,
        DateTime? rentalStartDate = null,
        DateTime? rentalEndDate = null,
        int quantity = 1);

    Task RemoveItemAsync(string customerId, string cartItemId);

    Task MigrateCartAsync(string fromCustomerId, string toCustomerId, bool includeCouponCodes);
}
