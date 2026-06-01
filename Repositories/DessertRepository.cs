using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories
{
    public class DessertRepository : IDessertRepository
    {
        private readonly AppDbContext _context;

        public DessertRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Dessert> GetAll(string? search = null)
        {
            IQueryable<Dessert> query = _context.Desserts;
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.Name.Contains(search));
            }

            return query.ToList();
        }

        public Dessert? GetById(int id)
        {
            return _context.Desserts.Find(id);
        }

        public Dessert Add(Dessert dessert)
        {
            if (dessert.Id < 0)
            {
                dessert.Id = 0;
            }
            _context.Desserts.Add(dessert);
            _context.SaveChanges();
            return dessert;
        }

        public Dessert? Update(int id, Dessert updated)
        {
            Dessert? existing = _context.Desserts.Find(id);
            if (existing == null) return null;
            existing.Name = updated.Name;
            existing.Quantity = updated.Quantity;
            existing.TimeToPrepare = updated.TimeToPrepare;
            existing.MainIngredients = updated.MainIngredients;
            existing.Description = updated.Description;
            existing.PreparationDetails = updated.PreparationDetails;
            existing.ImageUrl = updated.ImageUrl;

            _context.SaveChanges();
            return existing;
        }

        public bool Delete(int id)
        {
            Dessert? existing = _context.Desserts.Find(id);
            if (existing == null) return false;
            _context.Desserts.Remove(existing);
            _context.SaveChanges();
            return true;
        }
    }
}
