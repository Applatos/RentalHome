using System.ComponentModel.DataAnnotations;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Admin;

public sealed class ChangeStatusDto
{
    [Required(ErrorMessage = "Target status is required.")]
    public EntityStatus Target { get; set; }
}
