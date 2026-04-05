using Chesster.Api.Data;
using Chesster.Api.Models;
using Chesster.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chesster.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> FindOrCreateOAuthUserAsync(string provider, string providerKey, string email, string username);
    Task LinkOAuthProviderAsync(Guid userId, string provider, string providerKey);
    bool ValidatePassword(User user, string password);
}

public class AuthService : IAuthService
{
    private readonly ChessterDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(ChessterDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email already registered");

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new InvalidOperationException("Username already taken");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || user.PasswordHash == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> FindOrCreateOAuthUserAsync(string provider, string providerKey, string email, string username)
    {
        var existingLogin = await _context.UserLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey);

        if (existingLogin != null) return existingLogin.User;

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            await LinkOAuthProviderAsync(existingUser.Id, provider, providerKey);
            return existingUser;
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await LinkOAuthProviderAsync(user.Id, provider, providerKey);
        return user;
    }

    public async Task LinkOAuthProviderAsync(Guid userId, string provider, string providerKey)
    {
        var existingLogin = await _context.UserLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey);

        if (existingLogin != null) return;

        var login = new UserLogin
        {
            UserId = userId,
            Provider = provider,
            ProviderKey = providerKey
        };

        _context.UserLogins.Add(login);
        await _context.SaveChangesAsync();
    }

    public bool ValidatePassword(User user, string password)
    {
        return user.PasswordHash != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}