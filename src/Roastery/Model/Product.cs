using System;
using Roastery.Data;

namespace Roastery.Model;

class Product : IIdentifiable
{
    public string? Id { get; set; }
    public string Blend { get; set; }
    public string Name { get; set; }
    public int SizeInGrams { get; set; }

    public string FormatDescription() => $"{Name} {SizeInGrams}g";

    [Obsolete("Serialization constructor.")]
#pragma warning disable 8618
    public Product()
    {
    }
#pragma warning restore 8618

    public Product(string blend, string name, int sizeInGrams)
    {
        Blend = blend;
        Name = name;
        SizeInGrams = sizeInGrams;
    }
}