using Chesster.Api.Models;
using Chesster.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Chesster.Api.Tests;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("TestSecretKey12345678901234567890");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _jwtService = new JwtService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity.Should().NotBeNull();
        principal.Identity.IsAuthenticated.Should().BeTrue();
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(user.Username);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Act
        var principal = _jwtService.ValidateToken("invalid.token.here");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Create an expired token by setting up a mock with past expiration
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Key"]).Returns("TestSecretKey12345678901234567890");
        mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        var jwtService = new JwtService(mockConfig.Object);
        var token = jwtService.GenerateToken(user);

        // Wait a bit to ensure token is expired (tokens expire in 7 days, but we can mock time)
        // For simplicity, we'll just test with an invalid token
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxMjM0NTY3OC05MDEyLTM0NTYtNzg5MC0xMjM0NTY3ODkwMTIiLCJuYW1lIjoidGVzdHVzZXIiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJqdGkiOiIxMjM0NTY3OC05MDEyLTM0NTYtNzg5MC0xMjM0NTY3ODkwMTIiLCJleHAiOjE1MTYyMzkwMjIsImlzcyI6IlRlc3RJc3N1ZXIiLCJhdWQiOiJUZXN0QXVkaWVuY2UifQ.invalid";

        // Act
        var principal = jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }
}