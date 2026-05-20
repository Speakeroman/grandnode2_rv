using Grand.Domain.Orders;

namespace Grand.Domain.Cart;

/// <summary>
/// Shopping cart aggregate root — owns cart items independently of the Customer aggregate.
/// This replaces the embedded ShoppingCartItem[] collection on Customer.
/// </summary>
public class ShoppingCart : BaseEntity
{
    private ICollection<ShoppingCartItem> _items;

    public string CustomerId { get; set; }
    public string StoreId { get; set; }

    public virtual ICollection<ShoppingCartItem> Items {
        get => _items ??= new List<ShoppingCartItem>();
        protected set => _items = value;
    }
}
