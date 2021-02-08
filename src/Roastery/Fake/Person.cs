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

        static readonly string[] Forenames =
        {
            "Romeo",
            "Juliette"
        };
        
        static readonly string[] Surnames =
        {
            "Capulet",
            "Montague",
        };
        
        static readonly string[] Streets =
        {
            "Milky Way",
            "Butter Crescent",
            "Rocky Road",
        };

        public static Person Generate()
        {
            if (Distribution.OnceIn(400))
                return new Person(null, null);
            
            var streetNumber = (int) Distribution.Uniform(1.0, 1000);
            var name = $"{Distribution.Uniform(Forenames)} {Distribution.Uniform(Surnames)}";
            if (Distribution.OnceIn(20))
                name += $"-{Distribution.Uniform(Surnames)}";
            
            return new Person(
                name,
                $"{streetNumber} {Distribution.Uniform(Streets)}");
        }
    }
}