using DevWorkshop.TaskAPI.Application.DTOs.Auth;
using DevWorkshop.TaskAPI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DevWorkshop.TaskAPI.Application.Services;

/// <summary>
/// Implementación del servicio de autenticación
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserService userService, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario con email y contraseña
    /// </summary>
    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        try
        {
            //_logger.LogInformation("Iniciando proceso de autenticación para email: {Email}", loginDto.Email);

            //// Buscar usuario por email
            //var user = await _userService.GetUserByEmailAsync(loginDto.Email);
            //if (user == null)
            //{
            //    _logger.LogWarning("Usuario no encontrado con email: {Email}", loginDto.Email);
            //    return null;
            //}

            //// Obtener entidad completa para verificar contraseña
            //var userEntity = await _userService.GetUserEntityByEmailAsync(loginDto.Email);
            //if (userEntity == null)
            //{
            //    _logger.LogWarning("Entidad de usuario no encontrada para email: {Email}", loginDto.Email);
            //    return null;
            //}

            //// Verificar contraseña
            //if (!VerifyPassword(loginDto.Password, userEntity.PasswordHash))
            //{
            //    _logger.LogWarning("Contraseña incorrecta para usuario: {Email}", loginDto.Email);
            //    return null;
            //}

            //// Generar token JWT
            //var token = GenerateJwtToken(user.UserId, user.Email, user.RoleName);
            //var expirationTime = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes());

            //// Crear respuesta de autenticación
            //var authResponse = new AuthResponseDto
            //{
            //    Token = token,
            //    ExpiresAt = expirationTime,
            //    User = new UserInfo
            //    {
            //        UserId = user.UserId,
            //        FullName = user.FullName,
            //        Email = user.Email
            //    }
            //};

            _logger.LogInformation("Autenticación exitosa para usuario: {Email}", loginDto.Email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el proceso de autenticación para email: {Email}", loginDto.Email);
            return null;
        }
    }

    /// <summary>
    /// Verifica si una contraseña es válida comparándola con su hash
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar contraseña");
            return false;
        }
    }

    /// <summary>
    /// Genera un hash seguro de contraseña usando BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar hash de contraseña");
            throw;
        }
    }

    /// <summary>
    /// Genera un token JWT para un usuario autenticado
    /// </summary>
    public string GenerateJwtToken(int userId, string email, string? roleName = null)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"]!);

            // Crear claims del usuario
            var claimsList = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("jti", Guid.NewGuid().ToString()),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Agregar rol si está disponible
            if (!string.IsNullOrEmpty(roleName))
            {
                claimsList.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var claims = claimsList.ToArray();

            // Configurar token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            _logger.LogInformation("Token JWT generado exitosamente para usuario: {UserId}", userId);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar token JWT para usuario: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Cierra la sesión de un usuario invalidando su token
    /// </summary>
    public async Task<bool> LogoutAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Iniciando proceso de logout para usuario: {UserId}", userId);

            //var user = await _userService.GetUserEntityByIdAsync(userId);
            //if (user == null)
            //{
            //    _logger.LogWarning("Usuario no encontrado para logout: {UserId}", userId);
            //    return false;
            //}

            //// Actualizar timestamp para invalidar tokens anteriores
            //user.LastTokenIssueAt = DateTime.UtcNow;
            //await _userService.UpdateUserEntityAsync(user);

            _logger.LogInformation("Logout exitoso para usuario: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el logout para usuario: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Obtiene los minutos de expiración del JWT desde la configuración
    /// </summary>
    private int GetJwtExpirationMinutes()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        return int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");
    }
}

