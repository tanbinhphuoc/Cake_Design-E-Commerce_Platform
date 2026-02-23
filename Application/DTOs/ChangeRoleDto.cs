using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ChangeRoleDto
    {
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;
    }
}
