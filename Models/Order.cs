namespace AspireApp1.Server.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int DessertId { get; set; }
        public Dessert Dessert { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
        public bool Archived { get; set; } = false;
    }
}
