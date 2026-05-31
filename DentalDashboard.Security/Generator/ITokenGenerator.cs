using DentalDashboard.Domain.Models;
using System.Security.Claims;
namespace DentalDashboard.Security.Generator
{
    public interface ITokenGenerator
    {
        string GenerateToken(User user, List<Role> roles);
        ClaimsPrincipal ValidateToken(string token);
    }
}
