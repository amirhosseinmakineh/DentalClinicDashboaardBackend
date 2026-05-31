using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Security.Generator;
using DentalDashboard.Utilities.Hasher;

public class LoginCommandHandler : ICommandHandler<LoginCommand, object>
{
    private readonly IUserRepository userRepository;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IUserRoleRepository userRoleRepository;
    private readonly IRoleRepository roleRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository)
    {
        this.userRepository = userRepository;
        this.tokenGenerator = tokenGenerator;
        this.userRoleRepository = userRoleRepository;
        this.roleRepository = roleRepository;
    }

    public async Task<Result<object>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync();

        var user = users.FirstOrDefault(
            x => x.PhoneNumber == command.PhoneNumber);

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

        var userRoles = await userRoleRepository.GetAllAsync();

        var roles = await roleRepository.GetAllAsync();

        var userRolesList =
            (from ur in userRoles
             join r in roles
                 on ur.RoleId equals r.Id
             where ur.UserId == user.Id
             select r)
            .ToList();

        var token = tokenGenerator.GenerateToken(
            user,
            userRolesList);

        return Result<object>.Success(
            new
            {
                UserId = user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                Roles = userRolesList
                    .Select(x => x.RoleName)
                    .ToList(),
                Token = token
            },
            "ورود با موفقیت انجام شد");
    }
}