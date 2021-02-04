using Roastery.Model;

namespace Roastery.Data
{
    static class DatabaseMigrator
    {
        public static void Populate(Database database)
        {
            database.BulkLoad(
                new Product("BNZ1A", "Rocket Ship dark roast, whole beans 100g") {Id = "product-8908fd0sa"},
                new Product("BNZ1B", "Rocket Ship dark roast, whole beans 250g") {Id = "product-fsad890fj"},
                new Product("BNZ1C", "Rocket Ship dark roast, whole beans 1kg") {Id = "product-fsdjkljrw"},
                new Product("GRD1A", "Rocket Ship dark roast, ground 100g") {Id = "product-2nkfkdsju"},
                new Product("GRD1B", "Rocket Ship dark roast, ground 250g") {Id = "product-f8sa9newq"},
                new Product("GRD1C", "Rocket Ship dark roast, ground 1kg") {Id = "product-cvsad9033"},
                new Product("MRE1A", "1 AM medium roast, whole beans 100g") {Id = "product-i908jd0sf"},
                new Product("MRE1B", "1 AM medium roast, whole beans 250g") {Id = "product-isadj90fd"},
                new Product("MRE1C", "1 AM medium roast, whole beans 1kg") {Id = "product-isdjjljr3"},
                new Product("MRG1A", "1 AM medium roast, ground 100g") {Id = "product-inkfjdsj2"},
                new Product("MRG1B", "1 AM medium roast, ground 250g") {Id = "product-i8sajnew1"},
                new Product("MRG1C", "1 AM medium roast, ground 1kg") {Id = "product-ivsaj903t"});
        }
    }
}