using System;
using Roastery.Data;

namespace Roastery.Model
{
    class Product: IIdentifiable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SizeInGrams { get; set; }
        
        public string FormatDescription() => $"{Name} {SizeInGrams}g"; 

        [Obsolete("Serialization constructor.")]
        public Product() { }
        
        public Product(string name, int sizeInGrams)
        {
            Name = name;
            SizeInGrams = sizeInGrams;
        }
    }
}
