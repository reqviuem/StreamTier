using Microsoft.AspNetCore.Identity;
using StreamTier.API.Models;

namespace StreamTier.API.Data.Seed;

public class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedAdminRoleAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedAdminRoleAsync()
    {
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
            _logger.LogInformation("Created Admin role.");
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@streamtier.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new User { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var password = _configuration["AdminSeed:Password"]
                           ?? throw new InvalidOperationException("AdminSeed:Password not configured.");

            var result = await _userManager.CreateAsync(adminUser, password);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            _logger.LogInformation("Assigned Admin role to {Email}.", adminEmail);
        }
    }
}