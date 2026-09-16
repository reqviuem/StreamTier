using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StreamTier.API.Data;
using StreamTier.API.Models;
using StreamTier.API.Services;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.PlanService;
using StreamTier.API.Services.StripeService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using StreamTier.API.Services.WebHookService;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();

builder.Services.AddScoped<IStripeService, StripeService>();

builder.Services.AddScoped<IWebHookService, WebHookService>();

builder.Services.AddScoped<IUSerService, UserService>();

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddScoped<IInvoiceService, InvoiceService>();


builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:Issuer"];
        options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:Audience"];
        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)); 
        
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();