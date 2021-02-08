using Roastery.Util;

namespace Roastery.Fake
{
    class Person
    {
        public string Name { get; }
        public string Address { get; }

        public Person(string name, string address)
        {
            Name = name;
            Address = address;
        }

        static readonly string[] Names =
        {
            "Banzai Bill",
            "Chargin' Chuck",
            "Torpedo Ted"
        };
        
        static readonly string[] Streets =
        {
            "Milky Way",
            "Butter Crescent",
            "Rocky Road",
        };

        public static Person Generate(Distribution distribution)
        {
            if (distribution.OnceIn(400))
                return new Person(null, null);
            
            var streetNumber = (int) distribution.Uniform(1.0, 1000);
            return new Person(
                distribution.Uniform(Names),
                $"{streetNumber} {distribution.Uniform(Streets)}");
        }
    }
}