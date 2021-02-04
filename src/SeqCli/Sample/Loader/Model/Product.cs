namespace SeqCli.Sample.Loader.Model
{
    class Product
    {
        public string Id { get; }
        public string Code { get; }
        public string Description { get; }

        public Product(string id, string code, string description)
        {
            Id = id;
            Code = code;
            Description = description;
        }
    }
}
