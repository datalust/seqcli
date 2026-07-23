using Roastery.Model;

namespace Roastery.Data;

static class DatabaseMigrator
{
    const string RocketShip = "Rocket Ship Dark Roast";
    const string OneAm = "1 AM Medium Roast";

    public static void Populate(Database database)
    {
        database.BulkLoad(
            new Product(RocketShip, "Rocket Ship Dark Roast, Whole Beans", 100) { Id = "product-8908fd0sa" },
            new Product(RocketShip, "Rocket Ship Dark Roast, Whole Beans", 250) { Id = "product-fsad890fj" },
            new Product(RocketShip, "Rocket Ship Dark Roast, Whole Beans", 1000) { Id = "product-fsdjkljrw" },
            new Product(RocketShip, "Rocket Ship Dark Roast, Ground", 100) { Id = "product-2nkfkdsju" },
            new Product(RocketShip, "Rocket Ship Dark Roast, Ground", 250) { Id = "product-f8sa9newq" },
            new Product(RocketShip, "Rocket Ship Dark Roast, Ground", 1000) { Id = "product-cvsad9033" },
            new Product(OneAm, "1 AM Medium Roast, Whole Beans", 100) { Id = "product-i908jd0sf" },
            new Product(OneAm, "1 AM Medium Roast, Whole Beans", 250) { Id = "product-isadj90fd" },
            new Product(OneAm, "1 AM Medium Roast, Whole Beans", 1000) { Id = "product-isdjjljr3" },
            new Product(OneAm, "1 AM Medium Roast, Ground", 100) { Id = "product-inkfjdsj2" },
            new Product(OneAm, "1 AM Medium Roast, Ground", 250) { Id = "product-i8sajnew1" },
            new Product(OneAm, "1 AM Medium Roast, Ground", 1000) { Id = "product-ivsaj903t" });

        // Starting stock sits just below the warehouse's reorder point, so
        // production kicks off as soon as the simulation starts
        database.BulkLoad(
            new Inventory(RocketShip, 280) { Id = "inventory-88dfk01ka" },
            new Inventory(OneAm, 280) { Id = "inventory-73hdka92f" });
    }
}