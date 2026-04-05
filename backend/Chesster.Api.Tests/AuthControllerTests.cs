using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Chesster.Api.Controllers;
using Chesster.Api.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Moq;

namespace Chesster.Api.Tests;

public class AuthControllerTests : TestBase
{
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(
            _mockAuthService.Object,
            _mockJwtService.Object,
            Mock.Of<IConfiguration>()
        );

        // Setup controller context for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithUser()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "password123"
        );

        var expectedResponse = new AuthResponse(
            Token: "test-token",
            User: new UserDto(Guid.NewGuid(), "newuser", "newuser@example.com", DateTime.UtcNow)
        );

        _mockAuthService
            .Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ActionResult<AuthResponse>>();
        if (result.Result is OkObjectResult okResult)
        {
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }
        else
        {
            Assert.Fail("Expected OkObjectResult");
        }
    }

    [Fact]
    public async Task Register_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "testuser",
            Email: "test@example.com",
            Password: "password123"
        );

        _mockAuthService
            .Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ActionResult<AuthResponse>>();
        if (result.Result is BadRequestObjectResult badRequestResult)
        {
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "User already exists" });
        }
        else
        {
            Assert.Fail("Expected BadRequestObjectResult");
        }
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "login@example.com",
            Password: "password123"
        );

        var expectedResponse = new AuthResponse(
            Token: "test-token",
            User: new UserDto(Guid.NewGuid(), "loginuser", "login@example.com", DateTime.UtcNow)
        );

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<ActionResult<AuthResponse>>();
        if (result.Result is OkObjectResult okResult)
        {
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }
        else
        {
            Assert.Fail("Expected OkObjectResult");
        }
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "invalid@example.com",
            Password: "wrongpassword"
        );

        _mockAuthService
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync((AuthResponse)null!);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        result.Should().BeAssignableTo<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.Value.Should().BeEquivalentTo(new { message = "Invalid email or password" });
    }

    [Fact]
    public async Task GetCurrentUser_ValidUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Models.User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Email, user.Email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockAuthService
            .Setup(x => x.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.Should().BeAssignableTo<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var userDto = okResult!.Value as UserDto;
        userDto.Should().NotBeNull();
        userDto!.Username.Should().Be("testuser");
        userDto.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCurrentUser_NoClaims_ReturnsUnauthorized()
    {
        // Arrange
        _controller.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Email, "notfound@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockAuthService
            .Setup(x => x.GetUserByEmailAsync("notfound@example.com"))
            .ReturnsAsync((Models.User)null!);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}