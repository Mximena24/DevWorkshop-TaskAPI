using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevWorkshop.TaskAPI.Application.DTOs.Auth;

/// <summary>
/// Información básica del usuario para respuestas de autenticación
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Identificador único del usuario
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de correo electrónico
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
