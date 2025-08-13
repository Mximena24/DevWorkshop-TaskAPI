using DevWorkshop.TaskAPI.Application.DTOs.Common;
using DevWorkshop.TaskAPI.Application.DTOs.Users;
using DevWorkshop.TaskAPI.Application.Interfaces;
using DevWorkshop.TaskAPI.Application.Services;
using DevWorkshop.TaskAPI.Domain.Entities;
using DevWorkshop.TaskAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DevWorkshop.TaskAPI.Api.Controllers;

/// <summary>
/// Controlador para la gestión de usuarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    // TODO: ESTUDIANTE - Inyectar IUserService y ILogger
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        // TODO: ESTUDIANTE - Configurar las dependencias inyectadas
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("getAll")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();

            var response = new ApiResponse<IEnumerable<UserDto>>
            {
                Success = true,
                Message = "Usuarios obtenidos correctamente",
                Data = users
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la lista de usuarios.");

            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "Ocurrió un error interno al obtener los usuarios"
            };

            return StatusCode(500, errorResponse);
        }
    }


    [HttpGet("getById/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
    {
        try
        {
            // 1. Validar que el ID sea válido (> 0)
            if (id <= 0)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "El email no es válido"
                });


            // 2. Llamar al servicio para buscar el usuario
            var user = await _userService.GetUserByIdAsync(id);

            // 3. Si no existe, retornar NotFound
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Usuario no encontrado",
                    Data = null
                });
            }

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario encontrado correctamente",
                Data = user
            });
        }
        catch (Exception ex)
        {
            // 5. Manejar excepciones
            _logger.LogError(ex, "Error al obtener el usuario con ID {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Error interno: {ex.Message}",
                Data = null
            });
        }
    }


    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)] // Conflict - email ya existe
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        // TODO: ESTUDIANTE - Implementar lógica del controlador
        try
        {
            // Validar el modelo de entrada
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Intento de crear usuario con datos inválidos: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                return BadRequest(ApiResponse<UserDto>.ErrorResponse(
                    "Datos de entrada inválidos",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            // Crear el usuario usando el servicio
            var user = await _userService.CreateUserAsync(createUserDto);

            // Crear respuesta exitosa
            var response = ApiResponse<UserDto>.SuccessResponse(
                user,
                "Usuario creado correctamente"
            );

            _logger.LogInformation("Usuario creado exitosamente: {Email} con ID: {UserId}",
                user.Email, user.UserId);

            // Retornar 201 Created con la ubicación del recurso creado
            return CreatedAtAction(
                nameof(GetUserById),
                new { id = user.UserId },
                response
            );
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email ya está en uso"))
        {
            // Manejar conflicto de emails duplicados
            _logger.LogWarning("Conflicto al crear usuario: {Message}", ex.Message);

            return Conflict(ApiResponse<UserDto>.ErrorResponse(
                "Email duplicado",
                ex.Message
            ));
        }
        catch (AutoMapper.AutoMapperMappingException ex)
        {
            // Manejar errores de mapeo de AutoMapper
            _logger.LogError(ex, "Error de mapeo AutoMapper al crear usuario: {Email}", createUserDto?.Email ?? "Unknown");

            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse(
                "Error de configuración",
                "Error en el mapeo de datos. Contacte al administrador."
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al crear usuario: {Email}", createUserDto?.Email ?? "Unknown");

            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse(
                "Error interno del servidor",
                $"Ocurrió un error inesperado: {ex.Message}"
            ));
        }
    }


    [HttpPut("update/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            // 1. Validar que el ID sea válido
            if (id <= 0)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "El ID de usuario no es válido.",
                    Data = null
                });

            // 2. Validar el modelo si tiene datos
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "El modelo de datos no es válido.",
                    Data = null
                });

            // 3. Verificar que el usuario existe
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Usuario no encontrado.",
                    Data = null
                });

            // 4. Si se actualiza email, verificar que no esté en uso
            if (!string.IsNullOrEmpty(updateUserDto.Email))
            {
                var emailExists = await _userService.EmailExistsAsync(updateUserDto.Email, id);
                if (emailExists)
                    return Conflict(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "El email ya está en uso por otro usuario.",
                        Data = null
                    });
            }

            // 5. Llamar al servicio para actualizar
            var updatedUser = await _userService.UpdateUserAsync(id, updateUserDto);

            // 6. Retornar Ok con el usuario actualizado
            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario actualizado correctamente",
                Data = updatedUser
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el usuario con ID {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Ocurrió un error interno al actualizar el usuario.",
                Data = null
            });
        }
    }


    [HttpDelete("delete/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id <= 0)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "El ID de usuario no es válido.",
                Data = null
            });

        var deleted = await _userService.DeleteUserAsync(id);
        var response = ApiResponse<bool>.SuccessResponse(
                true,
                "Usuario eliminado correctamente"
            );
        if (deleted)
            return CreatedAtAction(
                nameof(GetUserById),
                response
            );

        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Message = "Usuario no encontrado.",
            Data = null
        });
    }


    [HttpGet("getByEmail/{email}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "El formato del email no es válido.",
                Data = null
            });

        var user = await _userService.GetUserByEmailAsync(email);

        if (user != null)
            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario encontrado.",
                Data = user
            });

        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Message = "Usuario no encontrado.",
            Data = null
        });
    }

    [HttpGet("getByRole/{roleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsersByRole(int roleId)
    {
        if (roleId <= 0)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "El ID de rol debe ser un número positivo.",
                Data = null
            });

        var users = await _userService.GetUsersByRoleAsync(roleId);

        return Ok(new ApiResponse<IEnumerable<UserDto>>
        {
            Success = true,
            Message = "Usuarios obtenidos correctamente.",
            Data = users
        });
    }


    [HttpGet("getStatistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> GetUserStatistics()
    {
        try
        {
            var statistics = await _userService.GetUserStatisticsAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Estadísticas obtenidas correctamente.",
                Data = statistics
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Error interno: {ex.Message}",
                Data = null
            });
        }
    }
}