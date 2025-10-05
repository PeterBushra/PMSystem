using Jobick.Models;
using Microsoft.EntityFrameworkCore;
using Jobick.Services.Interfaces;

namespace Jobick.Services;

/// <summary>
/// Provides user-related operations like login and lookup.
/// </summary>
public class UserService : IUserService
{
    private readonly AdhmmcPmContext _context;

    public UserService(AdhmmcPmContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Attempts to log in a user by email and password.
    /// Returns the User if credentials are valid, otherwise null.
    /// NOTE: In production, store password hashes and compare hashed values.
    /// </summary>
    public async Task<User?> LoginAsync(string email, string password)
    {
        // You may want to hash the password before comparing in production
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
    }

    /// <summary>
    /// Retrieves a user by id.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }
}