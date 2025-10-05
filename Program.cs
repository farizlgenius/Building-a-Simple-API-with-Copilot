using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



// Replace with your secure key
var jwtKey = "b7f3e2c9a1d44e0f8c6a9d3f5e2b7c1a";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

// ✅ FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<UserDtoValidator>();
builder.Services.AddAuthorization();

var app = builder.Build();

// 📋 Request/Response Logging Middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Incoming Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"Outgoing Response: {context.Response.StatusCode}");
});

// 🛑 Global Error Handling Middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        await context.Response.WriteAsJsonAsync(new { error = error?.Message });
    });
});

// 🔐 Enable Authentication
app.UseAuthentication();
app.UseAuthorization();

// 🧠 In-memory store
var users = new ConcurrentDictionary<int, User>();
var nextId = 1;

// 🔐 Secure endpoints with [Authorize]
app.MapGet("/users", () => Results.Ok(users.Values))
    .RequireAuthorization();

// Token generator endpoint
app.MapPost("/token", () =>
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, "user"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenString });
});

app.MapGet("/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { error = $"User with ID {id} not found." })
).RequireAuthorization();

app.MapPost("/users", async (UserDto newUser, [FromServices] IValidator<UserDto> validator) =>
{
    var validationResult = await validator.ValidateAsync(newUser);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

    var user = new User
    {
        Id = nextId++,
        Name = newUser.Name,
        Email = newUser.Email
    };
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
}).RequireAuthorization();

app.MapPut("/users/{id:int}", async (int id, UserDto updatedUser, [FromServices] IValidator<UserDto> validator) =>
{
    if (!users.ContainsKey(id))
        return Results.NotFound(new { error = $"User with ID {id} not found." });

    var validationResult = await validator.ValidateAsync(updatedUser);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

    var user = users[id];
    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
}).RequireAuthorization();

app.MapDelete("/users/{id:int}", (int id) =>
    users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"User with ID {id} not found." })
).RequireAuthorization();

app.Run();

// 📦 Models
public record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public record UserDto(string Name, string Email);

// ✅ Validator
public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be valid.");
    }
}
