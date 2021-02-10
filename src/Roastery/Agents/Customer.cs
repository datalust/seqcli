using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Fake;
using Roastery.Model;
using Roastery.Util;
using Roastery.Web;

namespace Roastery.Agents
{
    class Customer : Agent
    {
        readonly HttpClient _httpClient;
        readonly Person _person;

        public Customer(HttpClient httpClient, Person person, int meanBehaviorIntervalMilliseconds)
            : base(meanBehaviorIntervalMilliseconds)
        {
            _httpClient = httpClient;
            _person = person;
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            yield return CreateOrder;
        }

        async Task CreateOrder(CancellationToken cancellationToken)
        {
            // Trigger an error when no name/address provided.
            var person = Distribution.OnceIn(400) ? new Person(null, null) : _person;

            var order = await _httpClient.PostAsync<Order>("api/orders", new Order
            {
                CustomerName = person.Name,
                ShippingAddress = person.Address
            });

            var orderPath = $"api/orders/{order.Id}";
            var addItemPath = $"{orderPath}/items";
            var products = await _httpClient.GetAsync<List<Product>>("api/products");
            var items = (int) Distribution.Uniform(1, 5);
            for (var i = 0; i < items; ++i)
            {
                await Task.Delay((int)Distribution.Uniform(5000, 20000), cancellationToken);
                
                var product = Distribution.Uniform(products);
                await _httpClient.PostAsync<OrderItem>(addItemPath, new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id
                });
            }

            if (Distribution.OnceIn(15))
                return; // Abandon cart :-)
            
            // Customer has ~90s to place order before it'll be cleaned up as abandoned; some will be too slow
            await Task.Delay((int)Distribution.Uniform(10000, 70000), cancellationToken);

            // Place order
            order.Status = OrderStatus.PendingShipment;
            await _httpClient.PutAsync(orderPath, order);
        }
    }
}
