using CareerSEA.Contracts.Requests;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Services;
using CareerSEA.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class AuthServiceTests
{
    private readonly ITestOutputHelper _output;

    public AuthServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Register fails when the username already exists")]
    public async Task RegisterAsync_ShouldFail_WhenUserAlreadyExists()
    {
        _output.WriteLine("Arrange: Creating test database with an existing user named 'alpha'.");

        using var db = TestDbFactory.Create();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "alpha",
            Name = "A",
            LastName = "B",
            PasswordHash = "dummyhash"
        });

        await db.SaveChangesAsync();

        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting to register another user with the same username.");

        var result = await service.RegisterAsync(new SignupRequest
        {
            UserName = "alpha",
            Name = "A",
            LastName = "B",
            Password = "123456"
        });

        _output.WriteLine($"Assert: Registration should fail. Actual message = {result.Message}");

        Assert.False(result.Status);
        Assert.Equal("User already exists", result.Message);
    }

    [Fact(DisplayName = "Register creates a new user and hashes the password")]
    public async Task RegisterAsync_ShouldCreateUser_AndHashPassword()
    {
        _output.WriteLine("Arrange: Creating empty test database.");

        using var db = TestDbFactory.Create();
        var service = new AuthService(db);

        _output.WriteLine("Act: Registering a new user with username 'alpha'.");

        var result = await service.RegisterAsync(new SignupRequest
        {
            UserName = "alpha",
            Name = "Alphan",
            LastName = "Algul",
            Password = "Secret123!"
        });

        var user = db.Users.Single(u => u.UserName == "alpha");

        _output.WriteLine("Assert: Registration should succeed and password should be hashed.");
        _output.WriteLine($"Saved user: {user.UserName}, Name: {user.Name} {user.LastName}");

        Assert.True(result.Status);
        Assert.Equal("Alphan", user.Name);
        Assert.Equal("Algul", user.LastName);
        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual("Secret123!", user.PasswordHash);
    }
    
    /*
    [Fact(DisplayName = "Register fails when first name is empty")]
    public async Task RegisterAsync_ShouldFail_WhenFirstNameIsEmpty()
    {
        _output.WriteLine("Arrange: Creating empty test database.");

        using var db = TestDbFactory.Create();
        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting registration with empty first name.");

        var result = await service.RegisterAsync(new SignupRequest
        {
            UserName = "alpha",
            Name = "",
            LastName = "B",
            Password = "123456"
        });

        _output.WriteLine($"Assert: Registration should fail. Actual status = {result.Status}, message = {result.Message}");

        Assert.False(result.Status);
    }
    /*
    /*
    [Fact(DisplayName = "Register fails when last name is empty")]
    public async Task RegisterAsync_ShouldFail_WhenLastNameIsEmpty()
    {
        _output.WriteLine("Arrange: Creating empty test database.");

        using var db = TestDbFactory.Create();
        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting registration with empty last name.");

        var result = await service.RegisterAsync(new SignupRequest
        {
            UserName = "alpha",
            Name = "A",
            LastName = "",
            Password = "123456"
        });

        _output.WriteLine($"Assert: Registration should fail. Actual status = {result.Status}, message = {result.Message}");

        Assert.False(result.Status);
    }
    */
    /*
    [Fact(DisplayName = "Register fails when password is shorter than 6 characters")]
    public async Task RegisterAsync_ShouldFail_WhenPasswordIsLessThan6Characters()
    {
        _output.WriteLine("Arrange: Creating empty test database.");

        using var db = TestDbFactory.Create();
        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting registration with a password shorter than 6 characters.");

        var result = await service.RegisterAsync(new SignupRequest
        {
            UserName = "alpha",
            Name = "A",
            LastName = "B",
            Password = "12345"
        });

        _output.WriteLine($"Assert: Registration should fail. Actual status = {result.Status}, message = {result.Message}");

        Assert.False(result.Status);
    }
    */

    [Fact(DisplayName = "Login fails when the user does not exist")]
    public async Task LoginAsync_ShouldFail_WhenUserDoesNotExist()
    {
        _output.WriteLine("Arrange: Creating empty test database.");

        using var db = TestDbFactory.Create();
        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting login for a missing user.");

        var result = await service.LoginAsync(new LoginRequest
        {
            UserName = "missing",
            Password = "x"
        });

        _output.WriteLine($"Assert: Login should fail. Actual message = {result?.Message}");

        Assert.NotNull(result);
        Assert.False(result!.Status);
        Assert.Equal("Wrong Username or Password", result.Message);
    }

    [Fact(DisplayName = "Login fails when the password is incorrect")]
    public async Task LoginAsync_ShouldFail_WhenPasswordIsWrong()
    {
        _output.WriteLine("Arrange: Creating a user with a known correct password.");

        using var db = TestDbFactory.Create();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alpha",
            Name = "A",
            LastName = "B"
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "CorrectPassword");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        _output.WriteLine("Act: Attempting login with the wrong password.");

        var result = await service.LoginAsync(new LoginRequest
        {
            UserName = "alpha",
            Password = "WrongPassword"
        });

        _output.WriteLine($"Assert: Login should fail. Actual message = {result?.Message}");

        Assert.NotNull(result);
        Assert.False(result!.Status);
        Assert.Equal("Wrong Username or Password", result.Message);
    }

    [Fact(DisplayName = "Login returns a token and stores a refresh token when credentials are valid")]
    public async Task LoginAsync_ShouldReturnToken_AndSaveRefreshToken_WhenCredentialsAreValid()
    {
        _output.WriteLine("Arrange: Creating a user with a correct hashed password.");

        using var db = TestDbFactory.Create();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alpha",
            Name = "A",
            LastName = "B"
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "CorrectPassword");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        _output.WriteLine("Act: Logging in with valid credentials.");

        var result = await service.LoginAsync(new LoginRequest
        {
            UserName = "alpha",
            Password = "CorrectPassword"
        });

        var savedUser = db.Users.Single(u => u.UserName == "alpha");

        _output.WriteLine("Assert: Login should succeed and refresh token should be saved.");
        _output.WriteLine($"Refresh token present: {!string.IsNullOrWhiteSpace(savedUser.RefreshToken)}");

        Assert.NotNull(result);
        Assert.True(result!.Status);
        Assert.Equal("Successful", result.Message);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.RefreshToken));
        Assert.True(savedUser.RefreshTokenExpiry > DateTime.UtcNow);
    }
}