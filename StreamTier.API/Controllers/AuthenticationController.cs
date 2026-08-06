using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;


    public AuthenticationController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
    public async Task<IActionResult> Register(RegisterRequest registerRequest)
    {
        if (!CheckEmailAddress(registerRequest.Email))
        {
            return BadRequest("Structure of email address is wrong!");
        }

        var user = new User { UserName = registerRequest.Email, Email = registerRequest.Email };

        var result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Created();
    }

    [HttpGet]
    [Route("users")]
    public async Task<IActionResult> GetAllUSers()
    {
        var users = await _userManager.Users.ToListAsync();

        return Ok(users);
    }
}