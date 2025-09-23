namespace Sommerhus.Api.Dtos.Admin.Areas;

public record AreaDetailDto(Guid Id, string Name, string? Description, List<string> Images);
