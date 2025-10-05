using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory user store
var users = new ConcurrentDictionary<int, User>();
var nextId = 1;

// GET: Retrieve all users or a specific user by ID
app.MapGet("/users", () => users.Values);

app.MapGet("/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound()
);

// POST: Add a new user
app.MapPost("/users", (UserDto newUser) =>
{
    var user = new User
    {
        Id = nextId++,
        Name = newUser.Name,
        Email = newUser.Email
    };
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
});

// PUT: Update an existing user
app.MapPut("/users/{id:int}", (int id, UserDto updatedUser) =>
{
    if (!users.ContainsKey(id))
        return Results.NotFound();

    var user = users[id];
    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound();
});

app.Run();

// Models
record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

record UserDto(string Name, string Email);
