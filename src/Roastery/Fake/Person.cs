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
            "Akeem",
            "Alice",
            "Alok",
            "André",
            "Bob",
            "Bert",
            "Bernice",
            "Carol",
            "Dana",
            "Dwayne",
            "Elaine",
            "Gabriel",
            "Hazel",
            "Jun",
            "Karl",
            "Lina",
            "María",
            "Michał",
            "Nikki",
            "Scott",
            "Trevor",
            "Uri",
            "Yoshi",
            "Zach",
            "Zeynep"
        };
        
        static readonly string[] Surnames =
        {
            "Anderson",
            "Alvarez",
            "Brookes",
            "Benson",
            "Davis",
            "Erdene",
            "García",
            "Jones",
            "Martin",
            "Nkosi",
            "Norman",
            "Papadopoulos",
            "Romano",
            "Smith",
            "Xia",
            "Zheng"
        };
        
        static readonly string[] Streets =
        {
            "Lilac Road",
            "Lilly Street",
            "Carnation Street",
            "Rose Road",
            "Azalea Street",
            "Begonia Terrace",
            "Aster Street",
            "Orchid Street",
            "Daisy Road",
            "Petunia Avenue N.",
            "Zinnia Street",
            "Trillium Creek Parkway",
            "Grevillea Street",
            "Kurrajong Street"
        };

        public static Person Generate()
        {
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