using System.ComponentModel.DataAnnotations;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Unified Feature DTO for both list and details views.
/// </summary>
public sealed record FeatureDto(
    Guid Id,
    string Name,
    string Key,
    FeatureValueType ValueType,
    FeatureCategory Category,
    bool IsSearchable,
    string? Options = null,
    string? Unit = null,
    string? IconUrl = null,
    string? Description = null);

/// <summary>
/// Feature create/update DTO.
/// </summary>
public sealed record UpsertFeatureDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(100)] string? NameEn,
    [Required, MaxLength(50)] string Key,
    [Required] string ValueType,
    string? Category = null,
    bool IsSearchable = true,
    [MaxLength(500)] string? Options = null,
    [MaxLength(20)] string? Unit = null,
    [MaxLength(500)] string? IconUrl = null);
