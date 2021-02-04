using Roastery.Data;

namespace Roastery.Model
{
    class Product: IIdentifiable
    {
        public string Id { get; set; }
        public string Code { get; }
        public string Description { get; }

        public Product(string code, string description)
        {
            Code = code;
            Description = description;
        }
    }
}
