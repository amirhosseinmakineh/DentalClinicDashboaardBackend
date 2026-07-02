using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DomainRole = DentalDashboard.Domain.Models.Role;
using DomainUser = DentalDashboard.Domain.Models.User;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth.Helpers;

public static class AuthResponseFactory
{
    public static LoginResponse CreateLoginResponse(DomainUser user, IReadOnlyCollection<DomainRole> roles, string token)
    {
        var roleNames = roles
            .Select(role => role.RoleName)
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Distinct()
            .ToList();

        var primaryRole = ResolvePrimaryRole(roleNames);

        return new LoginResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = CreateFullName(user.FirstName, user.LastName),
            PhoneNumber = user.PhoneNumber,
            Role = primaryRole,
            Roles = roleNames,
            ConsultantProfileId = user.ConsultantProfile?.Id,
            IsCompleteProfile = user.IsCompleteProfile,
            DefaultDashboard = ResolveDashboardKey(primaryRole),
            DefaultDashboardRoute = ResolveDashboardRoute(primaryRole),
            DashboardAccess = CreateDashboardAccess(roleNames),
            Token = token
        };
    }

    public static AuthenticatedUserResponse CreateAuthenticatedUserResponse(
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        IReadOnlyCollection<string> roles)
    {
        var roleNames = roles
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Distinct()
            .ToList();
        var primaryRole = ResolvePrimaryRole(roleNames);

        return new AuthenticatedUserResponse
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            FullName = CreateFullName(firstName, lastName),
            PhoneNumber = phoneNumber,
            Role = primaryRole,
            Roles = roleNames,
            DefaultDashboard = ResolveDashboardKey(primaryRole),
            DefaultDashboardRoute = ResolveDashboardRoute(primaryRole),
            DashboardAccess = CreateDashboardAccess(roleNames)
        };
    }

    private static IReadOnlyCollection<DashboardAccessResponse> CreateDashboardAccess(IReadOnlyCollection<string> roles)
    {
        return roles
            .Select(role => new DashboardAccessResponse
            {
                Role = role,
                Dashboard = ResolveDashboardKey(role),
                Route = ResolveDashboardRoute(role)
            })
            .ToList();
    }

    private static string CreateFullName(string firstName, string lastName)
    {
        return $"{firstName} {lastName}".Trim();
    }

    private static string ResolvePrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains("Admin"))
            return "Admin";

        if (roles.Contains("Consultant"))
            return "Consultant";

        if (roles.Contains("Secretary"))
            return "Secretary";

        if (roles.Contains("Patient"))
            return "Patient";

        if (roles.Contains("NormalUser"))
            return "NormalUser";

        if (roles.Contains("User"))
            return "User";

        return roles.FirstOrDefault() ?? string.Empty;
    }

    private static string ResolveDashboardKey(string role)
    {
        return role switch
        {
            "Admin" => "AdminDashboard",
            "Consultant" => "ConsultantDashboard",
            "Secretary" => "SecretaryDashboard",
            "Patient" => "PatientDashboard",
            "NormalUser" => "UserDashboard",
            "User" => "UserDashboard",
            _ => "UserDashboard"
        };
    }

    private static string ResolveDashboardRoute(string role)
    {
        return role switch
        {
            "Admin" => "/admin/dashboard",
            "Consultant" => "/consultant/dashboard",
            "Secretary" => "/secretary/dashboard",
            "Patient" => "/patient/dashboard",
            "NormalUser" => "/dashboard",
            "User" => "/dashboard",
            _ => "/dashboard"
        };
    }
}
