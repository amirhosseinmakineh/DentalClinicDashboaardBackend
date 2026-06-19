using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Security.Generator;
using DentalDashboard.Utilities.Hasher;
using Microsoft.EntityFrameworkCore;

public class LoginCommandHandler : ICommandHandler<LoginCommand, object>
{
    private readonly IUserRepository userRepository;
    private readonly ITokenGenerator tokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository)
    {
        this.userRepository = userRepository;
        this.tokenGenerator = tokenGenerator;
    }

    public async Task<Result<object>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetAll()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.ConsultantProfile)
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken);

        if (user is null)
        {
            return Result<object>.Failure(
                "کاربری با این مشخصات یافت نشد");
        }

        if (!user.IsActive)
        {
            return Result<object>.Failure(
                "حساب کاربری غیرفعال است");
        }

        var isValidPassword = PasswordHasher.VerifyPassword(
            command.PasswordHash,
            user.PasswordHash);

        if (!isValidPassword)
        {
            return Result<object>.Failure(
                "رمز عبور اشتباه است");
        }

        var userRolesList = user.UserRoles
            .Where(x => !x.IsDeleted && x.Role != null && !x.Role.IsDeleted)
            .Select(x => x.Role)
            .DistinctBy(x => x.Id)
            .ToList();

        if (!userRolesList.Any())
        {
            return Result<object>.Failure(
                "برای این کاربر هیچ نقشی ثبت نشده است");
        }

        var roleNames = userRolesList
            .Select(x => x.RoleName)
            .ToList();

        var primaryRole = ResolvePrimaryRole(roleNames);
        var token = tokenGenerator.GenerateToken(
            user,
            userRolesList);

        return Result<object>.Success(
            new
            {
                UserId = user.Id,
                user.FirstName,
                user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                user.PhoneNumber,
                Role = primaryRole,
                Roles = roleNames,
                ConsultantProfileId = user.ConsultantProfile?.Id,
                DefaultDashboard = ResolveDashboardKey(primaryRole),
                DefaultDashboardRoute = ResolveDashboardRoute(primaryRole),
                DashboardAccess = roleNames.Select(role => new
                {
                    Role = role,
                    Dashboard = ResolveDashboardKey(role),
                    Route = ResolveDashboardRoute(role)
                }).ToList(),
                Token = token
            },
            "ورود با موفقیت انجام شد");
    }

    private static string ResolvePrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains("Admin"))
            return "Admin";

        if (roles.Contains("Consultant"))
            return "Consultant";

        if (roles.Contains("Patient"))
            return "Patient";

        if (roles.Contains("NormalUser"))
            return "NormalUser";

        if (roles.Contains("User"))
            return "User";

        return roles.First();
    }

    private static string ResolveDashboardKey(string role)
    {
        return role switch
        {
            "Admin" => "AdminDashboard",
            "Consultant" => "ConsultantDashboard",
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
            "Patient" => "/patient/dashboard",
            "NormalUser" => "/dashboard",
            "User" => "/dashboard",
            _ => "/dashboard"
        };
    }
}
