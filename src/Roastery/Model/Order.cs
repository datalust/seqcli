using System;
using Roastery.Data;

namespace Roastery.Model
{
    class Order : IIdentifiable
    {
        public string Id { get; set; }
        
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        
        public string CustomerName { get; set; }
        
        public string ShippingAddress { get; set; }
        
        public OrderStatus Status { get; set; }
    }
}
