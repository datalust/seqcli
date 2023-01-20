using System;
using Roastery.Data;

namespace Roastery.Model;

public class OrderItem: IIdentifiable
{
    public string? Id { get; set; }
    public string OrderId { get; set; }
    public string ProductId { get; set; }
        
    [Obsolete("Serialization constructor.")]
#pragma warning disable 8618
    public OrderItem() { }
#pragma warning restore 8618

    public OrderItem(string orderId, string productId)
    {
        OrderId = orderId;
        ProductId = productId;
    }
}