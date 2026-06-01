using AspireApp1.Server.DTO;

namespace AspireApp1.Server.Models
{
    public record SyncOperation(
        string Type,
        DessertDetailDto? Payload,
        int? TargetId,
        string Id
    );
}
