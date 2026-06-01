using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
  
    public class Dessert
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Quantity is required")]
        public string Quantity { get; set; } = "";

        [Required(ErrorMessage = "Time to prepare is required")]
        public string TimeToPrepare { get; set; } = "";

        [MaxLength(200)]
        public string MainIngredients { get; set; } = "";

        [MaxLength(500)]
        public string Description { get; set; } = "";

        [MaxLength(1000)]
        public string PreparationDetails { get; set; } = "";

        public string? ImageUrl { get; set; }
    }
}