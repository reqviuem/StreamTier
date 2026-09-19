using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using Subscription = StreamTier.API.Models.Subscription;

namespace StreamTier.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IUSerService _userService;
    private readonly IConfiguration _configuration;
    private readonly ISubscriptionService _subscriptionService;

    public AuthenticationController(IUSerService userService,
        IConfiguration configuration, ISubscriptionService subscriptionService)
    {
        _userService = userService;
        _configuration = configuration;
        _subscriptionService = subscriptionService;
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

        var result = await _userService.CreateAsync(user, registerRequestDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var subscription = new CreateSubscriptionDto
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = null,
            PlanId = "FreePlan",
            Status = Status.Active,
            StripeCustomerId = null,
            StripeSubscriptionId = null
        };

        await _subscriptionService.Save(Subscription.FromDto(subscription));


        return Created();
    }


    [HttpGet]
    // [Authorize(Roles = "Admin")]
    [Route("users")]
    public async Task<IActionResult> GetAllUSers()
    {
        var users = await _userService.GetAllUsersAsync();

        return Ok(users);
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login(LoginRequestDto requestDto)
    {
        var user = await _userService.FindByEmailAsync(requestDto.Email);

        if (user is null || !await _userService.CheckPasswordAsync(user, requestDto.Password))
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

        return Ok(new { AccessToken = accessToken });
    }

    private bool CheckEmailAddress(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                return false;

            var parts = addr.Host.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            string tld = parts[^1];

            if (tld.Length < 2 || !tld.All(char.IsLetter))
            {
                return false;
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}