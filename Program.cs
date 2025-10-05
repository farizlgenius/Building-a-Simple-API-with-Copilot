using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<UserDtoValidator>();

var app = builder.Build();

// Global exception handler
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

// In-memory store
var users = new ConcurrentDictionary<int, User>();
var nextId = 1;

// GET all users
app.MapGet("/users", () => Results.Ok(users.Values));

// GET user by ID
app.MapGet("/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { error = $"User with ID {id} not found." })
);

// POST create user with validation
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
});

// PUT update user with validation
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
});

// DELETE user
app.MapDelete("/users/{id:int}", (int id) =>
    users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"User with ID {id} not found." })
);

app.Run();

// Models
record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public record UserDto(string Name, string Email);

// Validator
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
