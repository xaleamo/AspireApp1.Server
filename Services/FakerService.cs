using AspireApp1.Server.Models;
using Bogus;

namespace AspireApp1.Server.Services
{
    public class FakerService
    {
        private static readonly Faker<Dessert> _dessertFaker = new Faker<Dessert>()
            .RuleFor(d => d.Id, f => 0)
            .RuleFor(d => d.Name, f => f.Commerce.ProductName())
            .RuleFor(d => d.Quantity, f => f.Random.Int(1, 50).ToString())
            .RuleFor(d => d.TimeToPrepare, f => $"{f.Random.Int(5, 120)} minutes")
            .RuleFor(d => d.MainIngredients, f => string.Join(", ", f.Commerce.Categories(3)))
            .RuleFor(d => d.Description, f => f.Lorem.Sentence())
            .RuleFor(d => d.PreparationDetails, f => f.Lorem.Paragraph())
            .RuleFor(d => d.ImageUrl, f => null);

        public List<Dessert> GenerateDessertBatch(int size = 5)
            => _dessertFaker.Generate(size);

        public List<Order> GenerateOrderBatch(
            List<Dessert> availableDesserts,
            List<User> availableUsers,
            int size = 5)
        {
            if (availableDesserts.Count == 0 || availableUsers.Count == 0) return [];

            var faker = new Faker<Order>()
                .RuleFor(o => o.Id, f => 0)
                .RuleFor(o => o.Dessert, f => f.PickRandom(availableDesserts))
                .RuleFor(o => o.DessertId, (f, o) => o.Dessert.Id)
                .RuleFor(o => o.User, f => f.PickRandom(availableUsers))
                .RuleFor(o => o.UserId, (f, o) => o.User.Id)
                .RuleFor(o => o.OrderedAt, f => f.Date.Recent(30).ToUniversalTime())
                .RuleFor(o => o.Archived, f => false);

            return faker.Generate(size);
        }
    }
}
