using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Web;

namespace Roastery.Agents
{
    class Customers : Agent
    {
        readonly HttpClient _httpClient;

        public Customers(HttpClient httpClient)
            : base(5000)
        {
            _httpClient = httpClient;
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            yield return CreateOrder;
        }

        async Task CreateOrder(CancellationToken cancellationToken)
        {
            var _ = await _httpClient.PostAsync<Order>("api/orders", new Order {CustomerName = "A. Customer", ShippingAddress = "123 A Street"});
        }
    }
}
