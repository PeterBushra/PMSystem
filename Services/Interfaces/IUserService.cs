using Jobick.Models;

namespace Jobick.Services.Interfaces;

public interface IUserService
{
    System.Threading.Tasks.Task<User?> LoginAsync(string email, string password);
    System.Threading.Tasks.Task<User?> GetUserByIdAsync(int id);
}
