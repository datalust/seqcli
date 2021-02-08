using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Fake;
using Roastery.Model;
using Roastery.Util;
using Roastery.Web;

namespace Roastery.Agents
{
    class Customers : Agent
    {
        readonly HttpClient _httpClient;
        readonly Distribution _distribution = new();

        public Customers(HttpClient httpClient)
            : base(10000)
        {
            _httpClient = httpClient;
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            yield return CreateOrder;
        }

        async Task CreateOrder(CancellationToken cancellationToken)
        {
            var customer = Person.Generate(_distribution);
            
            var order = await _httpClient.PostAsync<Order>("api/orders", new Order
            {
                CustomerName = customer.Name,
                ShippingAddress = customer.Address
            });

            var addItem = $"api/orders/{order.Id}/items";
            var products = await _httpClient.GetAsync<List<Product>>("api/products");
            var items = (int) _distribution.Uniform(1, 5);
            for (var i = 0; i < items; ++i)
            {
                var product = _distribution.Uniform(products);
                await _httpClient.PostAsync<OrderItem>(addItem, new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id
                });
            }
        }
    }
}
