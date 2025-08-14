using System.ComponentModel.DataAnnotations;

namespace DevWorkshop.TaskAPI.Application.DTOs.Auth;

/// <summary>
/// Objeto de transferencia de datos para el proceso de autenticación
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Dirección de correo electrónico del usuario
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;
}
