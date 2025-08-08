using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OPROZ_Main.Models;
using OPROZ_Main.Services;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("user/login")]
        public async Task<IActionResult> UserLogin([FromBody] LoginApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Unauthorized(new { message = "Account is locked out" });
                }
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Check if user has User role or higher
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => new[] { "User", "Manager", "Admin", "SuperAdmin" }.Contains(r)))
            {
                return StatusCode(403, new { message = "Access denied" });
            }

            var primaryRole = roles.OrderBy(r => new[] { "SuperAdmin", "Admin", "Manager", "User" }.ToList().IndexOf(r)).First();

            var token = _jwtService.GenerateToken(user.Id, user.Email!, primaryRole, user.CompanyId);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in via API", model.Email);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = primaryRole,
                    companyId = user.CompanyId,
                    verified = user.Verified
                }
            });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Unauthorized(new { message = "Account is locked out" });
                }
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Check if user has admin roles
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => new[] { "Admin", "Manager", "Support", "SuperAdmin" }.Contains(r)))
            {
                return StatusCode(403, new { message = "Admin access required" });
            }

            var primaryRole = roles.OrderBy(r => new[] { "SuperAdmin", "Admin", "Manager", "Support" }.ToList().IndexOf(r)).First();

            var token = _jwtService.GenerateToken(user.Id, user.Email!, primaryRole, user.CompanyId);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin user {Email} logged in via API", model.Email);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = primaryRole,
                    companyId = user.CompanyId,
                    verified = user.Verified
                }
            });
        }

        [HttpPost("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            int? companyId = companyIdClaim != null ? int.Parse(companyIdClaim) : null;

            return Ok(new
            {
                valid = true,
                user = new
                {
                    id = userId,
                    email = email,
                    role = role,
                    companyId = companyId
                }
            });
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> RefreshToken()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.OrderBy(r => new[] { "SuperAdmin", "Admin", "Manager", "Support", "User" }.ToList().IndexOf(r)).FirstOrDefault() ?? "User";

            var token = _jwtService.GenerateToken(user.Id, user.Email!, primaryRole, user.CompanyId);

            return Ok(new { token });
        }
    }
}