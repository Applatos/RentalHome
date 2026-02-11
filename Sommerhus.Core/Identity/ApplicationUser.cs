using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Sommerhus.Core.Identity;

public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? LastName { get; set; }
    [MaxLength(20)]  public string? Phone { get; set; }
    [MaxLength(500)] public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
