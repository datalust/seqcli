using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Web;
using Serilog;
using Serilog.Context;

namespace Roastery.Agents;

class CatalogBatch : Agent
{
    readonly HttpClient _httpClient;
    readonly ILogger _logger;

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

    const double LowStockThresholdKilograms = 40;

    async Task CheckStock(CancellationToken cancellationToken)
    {
        using var _ = LogContext.PushProperty("BatchId", Guid.NewGuid());
        try
        {
            _logger.Information("Checking stock levels");

            var inventory = await _httpClient.GetAsync<List<Inventory>>("api/inventory");
            var stockByBlend = inventory.ToDictionary(i => i.Blend, i => i.QuantityKilograms);

            foreach (var product in await _httpClient.GetAsync<List<Product>>("api/products"))
            {
                _logger.Information("Checking product {ProductDescription} ({ProductId})", product.FormatDescription(), product.Id);

                var stock = stockByBlend.GetValueOrDefault(product.Blend);
                if (stock < product.SizeInGrams / 1000.0)
                    _logger.Warning("Product {ProductId} is out of stock", product.Id);
                else if (stock < LowStockThresholdKilograms)
                    _logger.Warning("Product {ProductId} is low on stock", product.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Could not check stock levels");
        }
    }
}