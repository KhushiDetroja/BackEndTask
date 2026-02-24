using System.ComponentModel.DataAnnotations;

namespace BackendTask.DTOs;

public class CreateUserDTO
{
    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("MANAGER|SUPPORT|USER", ErrorMessage = "Role must be MANAGER, SUPPORT or USER.")]
    public string Role { get; set; }
    
}