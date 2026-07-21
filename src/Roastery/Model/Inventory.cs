using System;
using Roastery.Data;

namespace Roastery.Model;

class Inventory : IIdentifiable
{
    public string? Id { get; set; }
    public string Blend { get; set; }
    public double QuantityKilograms { get; set; }

    [Obsolete("Serialization constructor.")]
#pragma warning disable 8618
    public Inventory() { }
#pragma warning restore 8618

    public Inventory(string blend, double quantityKilograms)
    {
        Blend = blend;
        QuantityKilograms = quantityKilograms;
    }
}
