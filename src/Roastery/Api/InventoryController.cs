using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Roastery.Data;
using Roastery.Metrics;
using Roastery.Model;
using Roastery.Web;
using Serilog;
using Serilog.Context;

// ReSharper disable UnusedType.Global

namespace Roastery.Api;

class InventoryController : Controller
{
    readonly Database _database;

    public InventoryController(ILogger logger, RoasteryWebMetrics metrics, Database database)
        : base(logger, metrics)
    {
        _database = database;
    }

    [Route("GET", "api/inventory")]
    public async Task<HttpResponse> List(HttpRequest request)
    {
        var inventory = await _database.SelectAsync<Inventory>();

        foreach (var item in inventory)
            Metrics.RecordStockLevel(new RoasteryWebMetrics.Sample.StockLevelKey(item.Blend), item.QuantityKilograms);

        return Json(inventory);
    }

    [Route("POST", "api/inventory/{id}/receipts")]
    public async Task<HttpResponse> ReceiveStock(HttpRequest request)
    {
        if (request.Body == null)
            return BadRequest("A stock receipt is required.");

        var receipt = (InventoryReceipt) request.Body;
        var inventoryId = request.Path.Split('/')[3];

        var inventory = (await _database.SelectAsync<Inventory>(i => i.Id == inventoryId, $"id = '{inventoryId}'")).SingleOrDefault();
        if (inventory == null)
            return NotFound();

        using var _ = LogContext.PushProperty("InventoryId", inventory.Id);

        inventory.QuantityKilograms = Math.Round(inventory.QuantityKilograms + receipt.Kilograms, 2);
        await _database.UpdateAsync(inventory, $"quantitykilograms = {inventory.QuantityKilograms.ToString(CultureInfo.InvariantCulture)}");

        Metrics.RecordStockLevel(new RoasteryWebMetrics.Sample.StockLevelKey(inventory.Blend), inventory.QuantityKilograms);
        Log.Information("Received {ReceiptKilograms}kg of {Blend} into the warehouse; stock is now {StockKilograms}kg",
            receipt.Kilograms, inventory.Blend, inventory.QuantityKilograms);

        return Json(inventory);
    }
}
