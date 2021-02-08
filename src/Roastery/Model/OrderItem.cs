using Roastery.Data;

namespace Roastery.Model
{
    public class OrderItem: IIdentifiable
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }
    }
}