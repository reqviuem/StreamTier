using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using Subscription = StreamTier.API.Models.Subscription;
using SubscriptionService = Stripe.SubscriptionService;

namespace StreamTier.API;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IUSerService _userService;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ISubscriptionService _subscriptionService;
    private readonly CustomerService _customerService;


    public AuthenticationController(CustomerService customerService, IUSerService userService, SignInManager<User> signInManager,
        IConfiguration configuration, ISubscriptionService subscriptionService)
    {
        _userService = userService;
        _signInManager = signInManager;
        _configuration = configuration;
        _subscriptionService = subscriptionService;
        _customerService = customerService;
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

        var user = new User { UserName = registerRequestDto.Email, Email = registerRequestDto.Email};
        
        var result = await _userService.CreateAsync(user, registerRequestDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        
        //Create user with basic free plan at the registering phase

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var customerCreateOptions = new CustomerCreateOptions()
        {
            Email = user.Email,
            Name = user.Email
        };
        
        var customer = await _customerService.CreateAsync(customerCreateOptions);


        var subscriptionCreateOptions = new SubscriptionCreateOptions()
        {
            Customer = customer.Id,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = "price_1U57gt5B4xOxmiDEcyhXD7Nw" },
            },

            Metadata = new Dictionary<string, string>
            {
                { "userId", user.Id }
            }
        };

        var subscriptionService = new SubscriptionService();
        var stripeSubscription = await subscriptionService.CreateAsync(subscriptionCreateOptions);

        user.StripeCustomerId = customer.Id;


        var subscription = new CreateSubscriptionDto
        {
            UserId = user.Id,
            CreatedAt = stripeSubscription.Created,
            CurrentPeriodStart = stripeSubscription.Items.Data[0].CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.Items.Data[0].CurrentPeriodEnd,
            PlanId = "FreePlan",
            Status = Status.Active,
            StripeCustomerId = user.StripeCustomerId,
            StripeSubscriptionId = stripeSubscription.Id
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
}