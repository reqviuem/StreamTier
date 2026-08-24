using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace StreamTier.API;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;


    public AuthenticationController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    private bool CheckEmailAddress(string email)
    {
        bool isValid;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            isValid = addr.Address == email;
        }
        catch (FormatException)
        {
            isValid = false;
        }

        return isValid;
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegisterRequestDto registerRequestDto)
    {
        if (!CheckEmailAddress(registerRequestDto.Email))
        {
            return BadRequest("Structure of email address is wrong!");
        }

        var user = new User { UserName = registerRequestDto.Email, Email = registerRequestDto.Email };

        var result = await _userManager.CreateAsync(user, registerRequestDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Created();
    }

    [HttpGet]
    // [Authorize(Roles = "Admin")]
    [Route("users")]
    public async Task<IActionResult> GetAllUSers()
    {
        var users = await _userManager.Users.ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [Route("login")]

    public async Task<IActionResult> Login(LoginRequestDto requestDto)
    {
        var user = await _userManager.FindByEmailAsync(requestDto.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, requestDto.Password))
        {
            return Unauthorized();
        }

        var signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

        var credentials = new SigningCredentials(signInKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
        ];

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationInMinutes")),
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JsonWebTokenHandler();

        string accessToken = tokenHandler.CreateToken(tokenDescriptor);
        
        return Ok(new {AccessToken = accessToken});
    }
}