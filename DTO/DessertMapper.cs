using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;
namespace AspireApp1.Server.DTO
{
    public static class DessertMapper
    {
        public static DessertSummaryDto ToSummary(Dessert dessert)
        {
            return new DessertSummaryDto
            {
                Id = dessert.Id,
                Name = dessert.Name,
                Quantity = dessert.Quantity,
                TimeToPrepare = dessert.TimeToPrepare,
                ImageUrl = dessert.ImageUrl
            };
        }

        public static DessertDetailDto ToDetail(Dessert dessert)
        {
            return new DessertDetailDto
            {
                Id = dessert.Id,
                Name = dessert.Name,
                Quantity = dessert.Quantity,
                TimeToPrepare = dessert.TimeToPrepare,
                MainIngredients = dessert.MainIngredients,
                Description = dessert.Description,
                PreparationDetails = dessert.PreparationDetails,
                ImageUrl = dessert.ImageUrl
            };
        }

        public static Dessert ToModel(DessertDetailDto dto)
        {
            return new Dessert
            {
                Id = dto.Id,
                Name = dto.Name,
                Quantity = dto.Quantity,
                TimeToPrepare = dto.TimeToPrepare,
                MainIngredients = dto.MainIngredients,
                Description = dto.Description,
                PreparationDetails = dto.PreparationDetails,
                ImageUrl = dto.ImageUrl
            };
        }
    }
}