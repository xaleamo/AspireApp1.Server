namespace AspireApp1.Server.DTO
{
    public class DessertDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string TimeToPrepare { get; set; } = "";
        public string MainIngredients { get; set; } = "";
        public string Description { get; set; } = "";
        public string PreparationDetails { get; set; } = "";
        public string? ImageUrl { get; set; }

        public static DessertDetailDto From(DessertDtoInput dto)
        {
            return new DessertDetailDto
            {
                Id = -1,
                Name = dto.Name ?? string.Empty,
                Quantity = dto.Quantity ?? string.Empty,
                TimeToPrepare = dto.TimeToPrepare ?? string.Empty,
                MainIngredients = dto.MainIngredients ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                PreparationDetails = dto.PreparationDetails ?? string.Empty,
                ImageUrl = dto.ImageUrl
            };
        }
    }
    public class DessertDtoInput
    {
        public string Name { get; set; } 
        public string Quantity { get; set; }
        public string TimeToPrepare { get; set; }
        public string? MainIngredients { get; set; } = "";
        public string? Description { get; set; } = "";
        public string? PreparationDetails { get; set; } = "";
        public string? ImageUrl { get; set; }
    }
    public class DessertSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string TimeToPrepare { get; set; } = "";
        public string? ImageUrl { get; set; }
    }

    public class PagedResultDto<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
