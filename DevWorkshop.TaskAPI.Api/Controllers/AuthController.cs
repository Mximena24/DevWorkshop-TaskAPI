using DevWorkshop.TaskAPI.Application.DTOs.Auth;
using DevWorkshop.TaskAPI.Application.DTOs.Common;
using DevWorkshop.TaskAPI.Application.DTOs.Users;
using DevWorkshop.TaskAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevWorkshop.TaskAPI.Api.Controllers;

/// <summary>
/// Controlador para la gestión de autenticación y autorización
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario mediante email y contraseña
    /// </summary>
    /// <param name="loginDto">Credenciales de autenticación</param>
    /// <returns>Token JWT y datos del usuario si las credenciales son válidas</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Intento de autenticación para email: {Email}", loginDto.Email);

            // Validar modelo de entrada
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Datos de autenticación inválidos", errors));
            }

            // Procesar autenticación
            var authResponse = await _authService.LoginAsync(loginDto);

            if (authResponse == null)
            {
                _logger.LogWarning("Credenciales inválidas para email: {Email}", loginDto.Email);
                return Unauthorized(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Credenciales inválidas"));
            }

            _logger.LogInformation("Autenticación exitosa para email: {Email}", loginDto.Email);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                authResponse,
                "Autenticación exitosa"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la autenticación para email: {Email}", loginDto.Email);
            return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResponse(
                "Error interno del servidor durante la autenticación"));
        }
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema
    /// </summary>
    /// <param name="createUserDto">Datos del usuario a registrar</param>
    /// <returns>Token JWT y datos del usuario recién registrado</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            _logger.LogInformation("Intento de registro para email: {Email}", createUserDto.Email);

            // Validar modelo de entrada
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Datos de registro inválidos", errors));
            }

            // Verificar disponibilidad del email
            var emailExists = await _userService.EmailExistsAsync(createUserDto.Email);
            if (emailExists)
            {
                _logger.LogWarning("Intento de registro con email existente: {Email}", createUserDto.Email);
                return Conflict(ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Ya existe un usuario registrado con este email"));
            }

            // Crear usuario
            var createdUser = await _userService.CreateUserAsync(createUserDto);

            // Generar token automáticamente
            var token = _authService.GenerateJwtToken(createdUser.UserId, createdUser.Email, createdUser.RoleName);
            var expirationTime = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes());

            var authResponse = new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expirationTime,
                User = new UserInfo
                {
                    UserId = createdUser.UserId,
                    FullName = createdUser.FullName,
                    Email = createdUser.Email
                }
            };

            _logger.LogInformation("Registro exitoso para email: {Email}", createUserDto.Email);
            return CreatedAtAction(nameof(Register), ApiResponse<AuthResponseDto>.SuccessResponse(
                authResponse,
                "Usuario registrado exitosamente"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación durante el registro para email: {Email}", createUserDto.Email);
            return Conflict(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el registro para email: {Email}", createUserDto.Email);
            return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResponse(
                "Error interno del servidor durante el registro"));
        }
    }

    /// <summary>
    /// Cierra la sesión del usuario actual invalidando su token
    /// </summary>
    /// <returns>Confirmación de cierre de sesión exitoso</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("No se pudo obtener el ID del usuario desde el token");
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Token inválido o usuario no identificado"));
            }

            _logger.LogInformation("Intento de logout para usuario: {UserId}", userId);

            var logoutResult = await _authService.LogoutAsync(userId);

            if (!logoutResult)
            {
                _logger.LogWarning("Fallo en el logout para usuario: {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "Error al procesar el cierre de sesión"));
            }

            _logger.LogInformation("Logout exitoso para usuario: {UserId}", userId);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { message = "Sesión cerrada exitosamente" },
                "Cierre de sesión exitoso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el logout");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "Error interno del servidor durante el cierre de sesión"));
        }
    }

    /// <summary>
    /// Endpoint de verificación de autenticación
    /// </summary>
    /// <returns>Información del usuario autenticado</returns>
    [HttpGet("test-auth")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public ActionResult<ApiResponse<object>> TestAuth()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var emailClaim = User.FindFirst(ClaimTypes.Email);
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        var userInfo = new
        {
            UserId = userIdClaim?.Value,
            Email = emailClaim?.Value,
            Role = roleClaim?.Value,
            Message = "Token válido - Usuario autenticado correctamente"
        };

        return Ok(ApiResponse<object>.SuccessResponse(userInfo, "Autenticación verificada"));
    }

    /// <summary>
    /// Método auxiliar para obtener los minutos de expiración del JWT
    /// </summary>
    private int GetJwtExpirationMinutes()
    {
        return 60; // Valor por defecto
    }
}