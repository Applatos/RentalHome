using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record UpsertFeatureDto(
    string Name,

    [MaxLength]
    string Key,
    string ValueType,
    string? Unit,
    string? IconUrl
    );
