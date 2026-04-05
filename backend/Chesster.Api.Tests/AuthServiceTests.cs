using Chesster.Api.Models;
using Chesster.Api.Models.DTOs;
using Chesster.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Chesster.Api.Tests;

public class AuthServiceTests : TestBase
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_dbContext, _mockJwtService.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest("testuser", "test@example.com", "password123");
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-token");
        result.User.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        user.Should().NotBeNull();
        user.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsException()
    {
        // Arrange
        var existingUser = new User { Username = "existing", Email = "test@example.com", PasswordHash = "hash" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest("newuser", "test@example.com", "password123");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyExists_ThrowsException()
    {
        // Arrange
        var existingUser = new User { Username = "testuser", Email = "existing@example.com", PasswordHash = "hash" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest("testuser", "new@example.com", "password123");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = hashedPassword };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest("test@example.com", password);
        _mockJwtService.Setup(x => x.GenerateToken(user)).Returns("test-token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-token");
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "password123");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword") };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest("test@example.com", "wrongpassword");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User { Username = "testuser", Email = "test@example.com" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.GetUserByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindOrCreateOAuthUserAsync_ExistingLogin_ReturnsUser()
    {
        // Arrange
        var user = new User { Username = "testuser", Email = "test@example.com" };
        _dbContext.Users.Add(user);
        var login = new UserLogin { UserId = user.Id, Provider = "google", ProviderKey = "key123" };
        _dbContext.UserLogins.Add(login);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.FindOrCreateOAuthUserAsync("google", "key123", "test@example.com", "testuser");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task FindOrCreateOAuthUserAsync_ExistingUser_LinksAndReturnsUser()
    {
        // Arrange
        var user = new User { Username = "testuser", Email = "test@example.com" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.FindOrCreateOAuthUserAsync("google", "key123", "test@example.com", "testuser");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);

        var linkedLogin = await _dbContext.UserLogins.FirstOrDefaultAsync(l => l.Provider == "google" && l.ProviderKey == "key123");
        linkedLogin.Should().NotBeNull();
        linkedLogin.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task FindOrCreateOAuthUserAsync_NewUser_CreatesAndLinks()
    {
        // Act
        var result = await _authService.FindOrCreateOAuthUserAsync("google", "key123", "new@example.com", "newuser");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("new@example.com");
        result.Username.Should().Be("newuser");

        var login = await _dbContext.UserLogins.FirstOrDefaultAsync(l => l.Provider == "google" && l.ProviderKey == "key123");
        login.Should().NotBeNull();
        login.UserId.Should().Be(result.Id);
    }

    [Fact]
    public async Task LinkOAuthProviderAsync_NewLink_AddsLogin()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _authService.LinkOAuthProviderAsync(userId, "google", "key123");

        // Assert
        var login = await _dbContext.UserLogins.FirstOrDefaultAsync(l => l.Provider == "google" && l.ProviderKey == "key123");
        login.Should().NotBeNull();
        login.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task LinkOAuthProviderAsync_ExistingLink_DoesNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var login = new UserLogin { UserId = userId, Provider = "google", ProviderKey = "key123" };
        _dbContext.UserLogins.Add(login);
        await _dbContext.SaveChangesAsync();

        // Act
        await _authService.LinkOAuthProviderAsync(userId, "google", "key123");

        // Assert
        var logins = await _dbContext.UserLogins.Where(l => l.Provider == "google" && l.ProviderKey == "key123").ToListAsync();
        logins.Should().HaveCount(1);
    }

    [Fact]
    public void ValidatePassword_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { PasswordHash = hashedPassword };

        // Act
        var result = _authService.ValidatePassword(user, password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = new User { PasswordHash = hashedPassword };

        // Act
        var result = _authService.ValidatePassword(user, "wrongpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_NullPasswordHash_ReturnsFalse()
    {
        // Arrange
        var user = new User { PasswordHash = null };

        // Act
        var result = _authService.ValidatePassword(user, "password");

        // Assert
        result.Should().BeFalse();
    }
}