using AutoMapper;
using DevWorkshop.TaskAPI.Application.DTOs.Users;
using DevWorkshop.TaskAPI.Application.Interfaces;
using DevWorkshop.TaskAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DevWorkshop.TaskAPI.Application.Services;

/// <summary>
/// Servicio para la gestión de usuarios
/// </summary>
public class UserService : IUserService
{
    // TODO: ESTUDIANTE - Inyectar dependencias necesarias (DbContext, AutoMapper, Logger)
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;


    public UserService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UserService> logger)
    {
        // TODO: ESTUDIANTE - Configurar las dependencias inyectadas
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// TODO: ESTUDIANTE - Implementar la obtención de todos los usuarios activos
    /// 
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo todos los usuarios activos");

            // 1. Consultar la base de datos para obtener usuarios donde IsActive = true
            var users = (await _unitOfWork.Users.GetAllAsync())
                .ToList();

            // 2. Mapear las entidades User a UserDto usando AutoMapper
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            // 3. Retornar la lista de usuarios
            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la lista de usuarios activos");
            throw;
        }
    }


    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Buscando usuario por ID: {UserId}", userId);

            // 1. Buscar el usuario en la base de datos por UserId
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            // 2. Verificar que el usuario existe y está activo
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                return null;
            }

            // 3. Mapear la entidad a UserDto
            var userDto = _mapper.Map<UserDto>(user);

            // 4. Retornar el usuario o null si no existe
            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando usuario por ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Buscando usuario por email: {Email}", email);

            // 1. Buscar el usuario en la base de datos por Email
            var user = (await _unitOfWork.Users.GetAllAsync())
                .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            // 2. Verificar que el usuario existe y está activo
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado con email: {Email}", email);
                return null;
            }

            // 3. Mapear la entidad a UserDto
            var userDto = _mapper.Map<UserDto>(user);

            // 4. Retornar el usuario o null si no existe
            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando usuario por email: {Email}", email);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(int roleId)
    {
        try
        {
            _logger.LogInformation("Obteniendo usuarios por RoleId: {RoleId}", roleId);

            // Obtener usuarios activos con el rol especificado
            var users = await _unitOfWork.Users.FindAsync(u => u.RoleId == roleId);

            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuarios por RoleId: {RoleId}", roleId);
            throw;
        }
    }


    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {   // TODO: ESTUDIANTE - Implementar lógica
        // 1. Validar que el email no esté en uso
        try
        {
            _logger.LogInformation("Iniciando creación de usuario con email: {Email}", createUserDto.Email);
            var emailformat = createUserDto.Email.Trim().ToLower();
            var validuser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == emailformat);
            if (validuser != null)
            {
                throw new InvalidOperationException("Usuario ya creado");
            }

            // 2. Hashear la contraseña usando BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

            // 3. Crear una nueva entidad User con los datos del DTO
            var user = _mapper.Map<User>(createUserDto);
            user.Email = emailformat;
            user.PasswordHash = passwordHash;
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            user.LastTokenIssueAt = DateTime.Now;
            user.RoleId = 4;

            // 4. Guardar en la base de datos
            var createdUser = await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Usuario creado exitosamente con ID: {UserId}", createdUser.UserId);

            // 5. Mapear a DTO y retornar
            return _mapper.Map<UserDto>(createdUser);
        }

        catch (Exception ex)
        {
            throw;
        }

    }
    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            var emailFormat = email.Trim().ToLower();
            _logger.LogInformation("Verificando si el email {Email} ya existe", emailFormat);

            var users = await _unitOfWork.Users.GetAllAsync();
            return users.Any(u => u.Email == emailFormat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando email: {Email}", email);
            throw;
        }
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        try
        {
            _logger.LogInformation("Iniciando actualización del usuario con ID {UserId}", id);

            // 1. Buscar el usuario existente por ID
            var user = await _unitOfWork.Users.GetByIdAsync(id);

            // 2. Verificar que el usuario existe
            if (user == null)
            {
                _logger.LogWarning("No se encontró el usuario con ID {UserId}", id);
                return null;
            }

            // 3. Si se actualiza el email, validar que no esté en uso por otro usuario
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
            {
                var emailExists = await EmailExistsAsync(updateUserDto.Email);
                if (emailExists && !string.Equals(user.Email, updateUserDto.Email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("El email {Email} ya está en uso", updateUserDto.Email);
                    throw new InvalidOperationException("El correo ya está en uso por otro usuario.");
                }
                user.Email = updateUserDto.Email.Trim().ToLower();
            }

            // 4. Actualizar solo los campos que no sean null en el DTO
            if (!string.IsNullOrWhiteSpace(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName.Trim();

            if (updateUserDto.RoleId.HasValue)
                user.RoleId = updateUserDto.RoleId;

            if (updateUserDto.TeamId.HasValue)
                user.TeamId = updateUserDto.TeamId;

            // 5. Establecer UpdatedAt = DateTime.UtcNow
            user.UpdatedAt = DateTime.UtcNow;

            // 6. Guardar cambios en la base de datos
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // 7. Mapear y retornar el usuario actualizado
            _logger.LogInformation("Usuario con ID {UserId} actualizado correctamente", id);
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando el usuario con ID {UserId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Iniciando eliminación lógica del usuario con ID {UserId}", userId);

            // 1. Buscar el usuario por ID
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            // 2. Verificar que el usuario existe
            if (user == null)
            {
                _logger.LogWarning("No se encontró el usuario con ID {UserId}", userId);
                return false;
            }

            // 4. Establecer UpdatedAt = DateTime.UtcNow
            user.UpdatedAt = DateTime.UtcNow;

            // 5. Guardar cambios en la base de datos
            _unitOfWork.Users.Remove(user);
            await _unitOfWork.SaveChangesAsync();

            // 6. Retornar true si se eliminó correctamente
            _logger.LogInformation("Usuario con ID {UserId} eliminado lógicamente correctamente", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando lógicamente el usuario con ID {UserId}", userId);
            throw;
        }
    }


    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        try
        {
            _logger.LogInformation("Verificando si el email {Email} ya existe", email);

            // 1. Buscar usuarios con el email especificado
            var users = await _unitOfWork.Users.GetAllAsync();

            // 2. Si se proporciona excludeUserId, excluir ese usuario de la búsqueda
            if (excludeUserId.HasValue)
            {
                users = users.Where(u => u.UserId != excludeUserId.Value).ToList();
            }

            // 3. Retornar true si existe algún usuario con ese email
            return users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando si existe el email {Email}", email);
            throw;
        }
    }
}