using Barsoft.SignalRHub.Application.DTOs;
using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barsoft.SignalRHub.SignalRHub.Controllers;

/// <summary>
/// Authentication controller
/// Handles user login and JWT token generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// Validates credentials and returns JWT token
    /// </summary>
    /// <param name="request">Login request with userCode and password</param>
    /// <returns>Login response with JWT token and user info</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.UserCode) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with empty credentials");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "UserCode and Password are required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // Get user by user code
            var user = await _userRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent user: {UserCode}", request.UserCode);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Invalid credentials",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Verify password using BCrypt
            bool isPasswordValid = PasswordHasher.VerifyPassword(request.Password, user.Password);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login attempt with invalid password for user: {UserCode}", request.UserCode);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Invalid credentials",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Check if user is active
            if (!user.Aktif)
            {
                _logger.LogWarning("Login attempt with inactive user: {UserCode}", request.UserCode);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "User account is inactive",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user);

            // Map to UserDto
            var userDto = new UserDto
            {
                Id = user.Id,
                UserCode = user.UserCode,
                Description = user.Description,
                IsAdmin = user.Admin,
                SubeIds = UserDto.ParseSubeIds(user.SubeIds),
                Telefon = user.Telefon
            };

            // Create response
            var response = new LoginResponseDto
            {
                Token = token,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddMinutes(480) // Default from JwtSettings
            };

            _logger.LogInformation("User logged in successfully: {UserCode}, UserId: {UserId}", user.UserCode, user.Id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {UserCode}", request.UserCode);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get current authenticated user info
    /// Requires valid JWT token
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        // Get user ID from JWT token
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return Unauthorized();
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            UserCode = user.UserCode,
            Description = user.Description,
            IsAdmin = user.Admin,
            SubeIds = UserDto.ParseSubeIds(user.SubeIds),
            Telefon = user.Telefon
        };

        return Ok(userDto);
    }
}
