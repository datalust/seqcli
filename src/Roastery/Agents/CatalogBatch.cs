using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Util;
using Roastery.Web;
using Serilog;
using Serilog.Context;

namespace Roastery.Agents
{
    class CatalogBatch : Agent
    {
        readonly HttpClient _httpClient;
        readonly ILogger _logger;
        readonly Distribution _distribution = new();

        public CatalogBatch(HttpClient httpClient, ILogger logger)
            : base(60000)
        {
            _httpClient = httpClient;
            _logger = logger.ForContext<CatalogBatch>();
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            yield return CheckStock;
        }

        async Task CheckStock(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Information("Checking stock levels");
                foreach (var product in await _httpClient.GetAsync<List<Product>>("api/products"))
                {
                    using var _ = LogContext.PushProperty("BatchId", Guid.NewGuid());
                    
                    _logger.Information("Checking product {ProductDescription} ({ProductId})", product.Description, product.Id);
                    
                    if (_distribution.OnceIn(30))
                        _logger.Warning("Product {ProductId} is low on stock", product.Id);
                    else if (_distribution.OnceIn(70))
                        _logger.Warning("Product {ProductId} is out of stock", product.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not check stock levels");
            }
        }
    }
}