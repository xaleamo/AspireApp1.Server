using System.Collections.Generic;
using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories
{
    public class DessertInMemoryRepository : IDessertRepository
    {
        private List<Dessert> _desserts = new()
        {
            new Dessert { Id = 1, Name = "Boules de neige",           Quantity = "250g",     TimeToPrepare = "4h", MainIngredients = "Flour, sugar, butter",               Description = "Soft snowball cookies",          PreparationDetails = "Mix, roll, bake at 180C for 15min.",              ImageUrl="BoulesDeNeiges.png"},
            new Dessert { Id = 2, Name = "Chocolate cake",            Quantity = "420g",     TimeToPrepare = "4h", MainIngredients = "Flour, cocoa, eggs, butter",          Description = "Rich layered chocolate cake",    PreparationDetails = "Mix dry and wet separately, combine, bake 30min.", ImageUrl="ChocolateCake.jpg"},
            new Dessert { Id = 3, Name = "Apricot cheesecake",        Quantity = "320g",     TimeToPrepare = "4h", MainIngredients = "Cream cheese, apricot, biscuits",     Description = "Creamy cheesecake with apricot", PreparationDetails = "Prepare base, fill, refrigerate 2h.",              ImageUrl="ApricotCheesecake.jpg"},
            new Dessert { Id = 4, Name = "Creme brulee",              Quantity = "300g",     TimeToPrepare = "4h", MainIngredients = "Cream, eggs, sugar, vanilla",         Description = "Classic French creme brulee",    PreparationDetails = "Bake in bain-marie, caramelize sugar on top.",     ImageUrl="CremeBrulee.jpg"},
            new Dessert { Id = 5, Name = "Chocolate American biscuits",Quantity = "10 pieces",TimeToPrepare = "4h", MainIngredients = "Flour, chocolate chips, butter",     Description = "American style choc chip cookies",PreparationDetails = "Cream butter and sugar, fold in chips, bake 12min.", ImageUrl="ChocolateAmericanBiscuits.jpg"},
            new Dessert { Id = 6, Name = "Chocolate muffins",         Quantity = "5 pieces", TimeToPrepare = "4h", MainIngredients = "Flour, milk, banana, chocolate chips",Description = "Muffin with chocolate chips and banana flavour", PreparationDetails = "Mix wet and dry, fold in chips, bake at 190C for 20min.", ImageUrl="ChocolateMuffins.jpg"},
            new Dessert { Id = 7, Name = "Blueberry muffins",         Quantity = "5 pieces", TimeToPrepare = "4h", MainIngredients = "Flour, milk, blueberries",            Description = "Fluffy muffins with blueberries",PreparationDetails = "Fold blueberries into batter gently, bake 20min.", ImageUrl="BlueberryMuffins.jpg"},
            new Dessert { Id = 8, Name = "Plain pancakes",            Quantity = "5 pieces", TimeToPrepare = "4h", MainIngredients = "Flour, milk, eggs, butter",           Description = "Classic plain pancakes",         PreparationDetails = "Mix batter, cook on medium heat 2min per side.",   ImageUrl="PlainPancakes.jpg"},
            new Dessert { Id = 9, Name = "American chocolate pancakes",Quantity = "5 pieces", TimeToPrepare = "4h", MainIngredients = "Flour, cocoa, milk, eggs",           Description = "Thick chocolate pancakes",       PreparationDetails = "Add cocoa to base batter, cook on low heat.",      ImageUrl="ChocolatePancakes.jpg"},
            new Dessert { Id = 10,Name = "American blueberry pancakes",Quantity = "6 pieces", TimeToPrepare = "4h", MainIngredients = "Flour, milk, eggs, blueberries",     Description = "Fluffy pancakes with blueberries",PreparationDetails = "Drop blueberries onto batter while cooking.",     ImageUrl="BlueberryPancakes.jpg"},
            new Dessert { Id = 11,Name = "Lemon pie",                 Quantity = "360g",     TimeToPrepare = "4h", MainIngredients = "Lemon, eggs, sugar, pastry",          Description = "Tangy lemon tart",               PreparationDetails = "Prepare curd, fill shell, bake 25min.",            ImageUrl="LemonPie.jpg"},
            new Dessert { Id = 12,Name = "Pumpkin pie",               Quantity = "360g",     TimeToPrepare = "4h", MainIngredients = "Pumpkin, cinnamon, eggs, pastry",     Description = "Spiced pumpkin pie",             PreparationDetails = "Blend filling, pour into shell, bake 45min.",      ImageUrl="PumpkinPie.jpg"},
            new Dessert { Id = 13,Name = "Tiramisu",                  Quantity = "400g",     TimeToPrepare = "5h", MainIngredients = "Mascarpone, espresso, ladyfingers",   Description = "Classic Italian tiramisu",       PreparationDetails = "Layer soaked ladyfingers with mascarpone cream, chill 3h.", ImageUrl="Tiramisu.jpg"},
            new Dessert { Id = 14,Name = "Eclair",                    Quantity = "6 pieces", TimeToPrepare = "3h", MainIngredients = "Choux pastry, cream, chocolate",      Description = "French pastry with cream filling",PreparationDetails = "Pipe choux, bake, fill with cream, glaze with chocolate.", ImageUrl="Eclair.jpg"},
        };

        private int _nextId = 15;

        public List<Dessert> GetAll(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(search))
                return _desserts;

            return _desserts
                .Where(d => d.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public Dessert? GetById(int id)
        {
            foreach (Dessert dessert in _desserts)
            {
                if (dessert.Id == id)
                    return dessert;
            }
            return null;
        }

        public Dessert Add(Dessert dessert)
        {
            dessert.Id = _nextId++;
            _desserts.Add(dessert);
            return dessert;
        }

        public Dessert? Update(int id, Dessert updated)
        {
            var existing = GetById(id);
            if (existing == null) return null;

            existing.Name = updated.Name;
            existing.Quantity = updated.Quantity;
            existing.TimeToPrepare = updated.TimeToPrepare;
            existing.MainIngredients = updated.MainIngredients;
            existing.Description = updated.Description;
            existing.PreparationDetails = updated.PreparationDetails;
            existing.ImageUrl = updated.ImageUrl;

            return existing;
        }

        public bool Delete(int id)
        {
            var existing = GetById(id);
            if (existing == null) return false;
            _desserts.Remove(existing);
            return true;
        }
    }
}