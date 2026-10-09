using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecretaryAccessScheduleAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretaryAccessScheduleAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecretarySaleServices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecretaryReward = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretarySaleServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponseLog = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleteProfile = table.Column<bool>(type: "bit", nullable: false),
                    AvatarImageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PushNotificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecretaryType = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    WorkStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    WorkEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleteProfile = table.Column<bool>(type: "bit", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    LastOnlineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOfflineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LimitNumber = table.Column<int>(type: "int", nullable: true),
                    PreferredLeadSourceType = table.Column<int>(type: "int", nullable: true),
                    ConsultantRole = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                    table.CheckConstraint("CK_ConsultantProfiles_PreferredLeadSourceType", "[PreferredLeadSourceType] IS NULL OR [PreferredLeadSourceType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ConsultantProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CounterpartyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpenseCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadAssignmentSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentSourceType = table.Column<int>(type: "int", nullable: false),
                    UpdatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAssignmentSettings", x => x.Id);
                    table.CheckConstraint("CK_LeadAssignmentSettings_Singleton", "[Id] = 1");
                    table.CheckConstraint("CK_LeadAssignmentSettings_SourceType", "[AssignmentSourceType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_LeadAssignmentSettings_Users_UpdatedByAdminId",
                        column: x => x.UpdatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientFinancialCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Services = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrePaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstallmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuaranteeDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuaranteeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuaranteeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GuaranteeChequeRegistration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsultantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgreementType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFinancialCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientFinancialCases_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientFinancialCases_Users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientWallets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientWallets", x => x.Id);
                    table.CheckConstraint("CK_PatientWallets_Balance", "[Balance] >= 0");
                    table.ForeignKey(
                        name: "FK_PatientWallets_Users_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecretaryAccessPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretaryAccessPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretaryAccessPermissions_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecretaryAccessSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretaryAccessSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretaryAccessSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecretarySales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecretaryReward = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretarySales", x => x.Id);
                    table.CheckConstraint("CK_SecretarySales_SalePrice", "[SalePrice] > 0");
                    table.CheckConstraint("CK_SecretarySales_SecretaryReward", "[SecretaryReward] > 0");
                    table.ForeignKey(
                        name: "FK_SecretarySales_SecretarySaleServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SecretarySaleServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecretarySales_Users_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecretarySales_Users_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecretarySales_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecretaryWallets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretaryWallets", x => x.Id);
                    table.CheckConstraint("CK_SecretaryWallets_Balance", "[Balance] >= 0");
                    table.ForeignKey(
                        name: "FK_SecretaryWallets_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPresenceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPresenceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPresenceLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsultantProfileId = table.Column<long>(type: "bigint", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LeadAssignmentState = table.Column<int>(type: "int", nullable: false),
                    ConsultantProfileId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    CallDeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresThreeMinuteCall = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    ReportDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallInitiatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallResult = table.Column<int>(type: "int", nullable: true),
                    SmsSent = table.Column<bool>(type: "bit", nullable: false),
                    PatientCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PatientRegion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttendanceProbabilityPercent = table.Column<int>(type: "int", nullable: true),
                    SecondaryPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PickUp = table.Column<bool>(type: "bit", nullable: false),
                    DispatchLevel = table.Column<int>(type: "int", nullable: false),
                    LastDispatchAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByConsultantAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureReason = table.Column<int>(type: "int", nullable: true),
                    ClosureDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAssignments_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatientCheques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientFinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SayadNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCheques_PatientFinancialCases_PatientFinancialCaseId",
                        column: x => x.PatientFinancialCaseId,
                        principalTable: "PatientFinancialCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientDebts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientFinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDebts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDebts_PatientFinancialCases_PatientFinancialCaseId",
                        column: x => x.PatientFinancialCaseId,
                        principalTable: "PatientFinancialCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientFinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientFinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientFinancialTransactions_PatientFinancialCases_PatientFinancialCaseId",
                        column: x => x.PatientFinancialCaseId,
                        principalTable: "PatientFinancialCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientFinancialTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientPromissoryNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientFinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPromissoryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientPromissoryNotes_PatientFinancialCases_PatientFinancialCaseId",
                        column: x => x.PatientFinancialCaseId,
                        principalTable: "PatientFinancialCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecretaryWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretarySaleId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretaryWalletTransactions", x => x.Id);
                    table.CheckConstraint("CK_SecretaryWalletTransactions_Amount", "[Amount] <> 0");
                    table.ForeignKey(
                        name: "FK_SecretaryWalletTransactions_SecretarySales_SecretarySaleId",
                        column: x => x.SecretarySaleId,
                        principalTable: "SecretarySales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecretaryWalletTransactions_SecretaryWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "SecretaryWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecretaryWalletTransactions_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileNumber = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFiles", x => x.Id);
                    table.CheckConstraint("CK_PatientFiles_FileNumber_Positive", "[FileNumber] > 0");
                    table.ForeignKey(
                        name: "FK_PatientFiles_LeadAssignments_PatientReferenceId",
                        column: x => x.PatientReferenceId,
                        principalTable: "LeadAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientFiles_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadAssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    ConsultantProfileId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservationAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DoctorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ReservationType = table.Column<int>(type: "int", nullable: false),
                    DentalServices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientReceivedService = table.Column<bool>(type: "bit", nullable: true),
                    AttendanceConfirmationStatus = table.Column<int>(type: "int", nullable: false),
                    ConsultantAttendanceConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsultantSaysPatientAttended = table.Column<bool>(type: "bit", nullable: true),
                    ConsultantAttendanceNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecretaryReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecretaryApprovedConsultantConfirmation = table.Column<bool>(type: "bit", nullable: true),
                    SecretaryReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConsultantRewardEligibleAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsultantRewardApprovedByAdmin = table.Column<bool>(type: "bit", nullable: true),
                    ConsultantRewardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ConsultantRewardReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsultantRewardReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecretaryAnnouncementStatus = table.Column<int>(type: "int", nullable: true),
                    SecretaryAnnouncement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecretaryFollowUpContacted = table.Column<bool>(type: "bit", nullable: true),
                    SecretaryAnnouncementUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecretaryAnnouncementUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAttendanceScoreApplied = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceScoreValue = table.Column<int>(type: "int", nullable: true),
                    AttendanceScoreAppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttendancePrediction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCanceled = table.Column<bool>(type: "bit", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InitialReservationAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_LeadAssignments_LeadAssignmentId",
                        column: x => x.LeadAssignmentId,
                        principalTable: "LeadAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientReferrals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerPatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferredLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferredPhoneNumber = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeadAssignmentId = table.Column<long>(type: "bigint", nullable: true),
                    ReservationId = table.Column<long>(type: "bigint", nullable: true),
                    RewardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientReferrals_LeadAssignments_LeadAssignmentId",
                        column: x => x.LeadAssignmentId,
                        principalTable: "LeadAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientReferrals_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientReferrals_Users_ReferrerPatientUserId",
                        column: x => x.ReferrerPatientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientReferrals_Users_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientReferrals_Users_SecretaryUserId",
                        column: x => x.SecretaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientReferralId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientWalletTransactions", x => x.Id);
                    table.CheckConstraint("CK_PatientWalletTransactions_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PatientWalletTransactions_PatientReferrals_PatientReferralId",
                        column: x => x.PatientReferralId,
                        principalTable: "PatientReferrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientWalletTransactions_PatientWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "PatientWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientWalletTransactions_Users_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LeadAssignmentSettings",
                columns: new[] { "Id", "AssignmentSourceType", "CreatedAt", "DeletedAt", "IsDeleted", "UpdatedAt", "UpdatedByAdminId" },
                values: new object[] { 1L, 1, new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ConsultantProfileId",
                table: "Attendances",
                column: "ConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_UserId",
                table: "ConsultantProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Title",
                table: "ExpenseCategories",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CreatedByUserId",
                table: "FinancialTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_ExpenseCategoryId",
                table: "FinancialTransactions",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_TransactionDate",
                table: "FinancialTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_Type_ExpenseCategoryId",
                table: "FinancialTransactions",
                columns: new[] { "Type", "ExpenseCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_AssignmentType_LeadAssignmentState_ConsultantProfileId",
                table: "LeadAssignments",
                columns: new[] { "AssignmentType", "LeadAssignmentState", "ConsultantProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_CallDeadlineAt",
                table: "LeadAssignments",
                column: "CallDeadlineAt");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_ConsultantProfileId",
                table: "LeadAssignments",
                column: "ConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_IsDeleted_LeadAssignmentState_ConsultantProfileId_CreatedAt",
                table: "LeadAssignments",
                columns: new[] { "IsDeleted", "LeadAssignmentState", "ConsultantProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_PhoneNumber",
                table: "LeadAssignments",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_ReportSubmittedAt",
                table: "LeadAssignments",
                column: "ReportSubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignmentSettings_UpdatedByAdminId",
                table: "LeadAssignmentSettings",
                column: "UpdatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCheques_PatientFinancialCaseId_DueDate_Status",
                table: "PatientCheques",
                columns: new[] { "PatientFinancialCaseId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientDebts_PatientFinancialCaseId_Status_DueDate",
                table: "PatientDebts",
                columns: new[] { "PatientFinancialCaseId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientDebts_SourceType_SourceId",
                table: "PatientDebts",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_FileNumber",
                table: "PatientFiles",
                column: "FileNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_PatientReferenceId",
                table: "PatientFiles",
                column: "PatientReferenceId",
                unique: true,
                filter: "[PatientReferenceId] IS NOT NULL AND [SourceType] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_PhoneNumber",
                table: "PatientFiles",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_SecretaryUserId",
                table: "PatientFiles",
                column: "SecretaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_SecretaryUserId_PhoneNumber",
                table: "PatientFiles",
                columns: new[] { "SecretaryUserId", "PhoneNumber" },
                unique: true,
                filter: "[SecretaryUserId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFiles_SourceType",
                table: "PatientFiles",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFinancialCases_CreatedByUserId",
                table: "PatientFinancialCases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFinancialCases_PatientId",
                table: "PatientFinancialCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFinancialTransactions_CreatedByUserId",
                table: "PatientFinancialTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFinancialTransactions_PatientFinancialCaseId",
                table: "PatientFinancialTransactions",
                column: "PatientFinancialCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFinancialTransactions_SourceType_SourceId_Type",
                table: "PatientFinancialTransactions",
                columns: new[] { "SourceType", "SourceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_UserId",
                table: "PatientProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientPromissoryNotes_PatientFinancialCaseId_DueDate_Status",
                table: "PatientPromissoryNotes",
                columns: new[] { "PatientFinancialCaseId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_LeadAssignmentId",
                table: "PatientReferrals",
                column: "LeadAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_ReferredPhoneNumber",
                table: "PatientReferrals",
                column: "ReferredPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_ReferrerPatientUserId_CreatedAt",
                table: "PatientReferrals",
                columns: new[] { "ReferrerPatientUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_ReservationId",
                table: "PatientReferrals",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_ReviewedByAdminId",
                table: "PatientReferrals",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_SecretaryUserId",
                table: "PatientReferrals",
                column: "SecretaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReferrals_Status_CreatedAt",
                table: "PatientReferrals",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientWallets_PatientUserId",
                table: "PatientWallets",
                column: "PatientUserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PatientWalletTransactions_PatientReferralId_TransactionType",
                table: "PatientWalletTransactions",
                columns: new[] { "PatientReferralId", "TransactionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientWalletTransactions_PatientUserId_CreatedAt",
                table: "PatientWalletTransactions",
                columns: new[] { "PatientUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientWalletTransactions_WalletId",
                table: "PatientWalletTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ConsultantProfileId_ReservationAt_IsCanceled",
                table: "Reservations",
                columns: new[] { "ConsultantProfileId", "ReservationAt", "IsCanceled" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ConsultantRewardApprovedByAdmin_SecretaryReviewedAt",
                table: "Reservations",
                columns: new[] { "ConsultantRewardApprovedByAdmin", "SecretaryReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_LeadAssignmentId_IsCanceled",
                table: "Reservations",
                columns: new[] { "LeadAssignmentId", "IsCanceled" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OwnerType_OwnerUserId_CreatedAt",
                table: "Reservations",
                columns: new[] { "OwnerType", "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PatientUserId",
                table: "Reservations",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationType_ReservationAt",
                table: "Reservations",
                columns: new[] { "ReservationType", "ReservationAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_SecretaryAnnouncementStatus",
                table: "Reservations",
                column: "SecretaryAnnouncementStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_SecretaryAnnouncementUserId_SecretaryAnnouncementUpdatedAt",
                table: "Reservations",
                columns: new[] { "SecretaryAnnouncementUserId", "SecretaryAnnouncementUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAccessPermissions_SecretaryUserId_DayOfWeek_PermissionType",
                table: "SecretaryAccessPermissions",
                columns: new[] { "SecretaryUserId", "DayOfWeek", "PermissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAccessScheduleAudits_SecretaryUserId",
                table: "SecretaryAccessScheduleAudits",
                column: "SecretaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAccessSchedules_UserId_DayOfWeek",
                table: "SecretaryAccessSchedules",
                columns: new[] { "UserId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySales_PatientUserId",
                table: "SecretarySales",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySales_ReviewedByAdminId",
                table: "SecretarySales",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySales_SecretaryUserId_CreatedAt",
                table: "SecretarySales",
                columns: new[] { "SecretaryUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySales_ServiceId",
                table: "SecretarySales",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySales_Status_CreatedAt",
                table: "SecretarySales",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretarySaleServices_Title",
                table: "SecretarySaleServices",
                column: "Title",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryWallets_SecretaryUserId",
                table: "SecretaryWallets",
                column: "SecretaryUserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryWalletTransactions_SecretarySaleId_TransactionType",
                table: "SecretaryWalletTransactions",
                columns: new[] { "SecretarySaleId", "TransactionType" },
                unique: true,
                filter: "[SecretarySaleId] IS NOT NULL AND [TransactionType] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryWalletTransactions_SecretaryUserId_CreatedAt",
                table: "SecretaryWalletTransactions",
                columns: new[] { "SecretaryUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryWalletTransactions_WalletId",
                table: "SecretaryWalletTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPresenceLogs_OccurredAt",
                table: "UserPresenceLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPresenceLogs_UserId",
                table: "UserPresenceLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPresenceLogs_UserId_OccurredAt",
                table: "UserPresenceLogs",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "LeadAssignmentSettings");

            migrationBuilder.DropTable(
                name: "PatientCheques");

            migrationBuilder.DropTable(
                name: "PatientDebts");

            migrationBuilder.DropTable(
                name: "PatientFiles");

            migrationBuilder.DropTable(
                name: "PatientFinancialTransactions");

            migrationBuilder.DropTable(
                name: "PatientProfiles");

            migrationBuilder.DropTable(
                name: "PatientPromissoryNotes");

            migrationBuilder.DropTable(
                name: "PatientWalletTransactions");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "SecretaryAccessPermissions");

            migrationBuilder.DropTable(
                name: "SecretaryAccessScheduleAudits");

            migrationBuilder.DropTable(
                name: "SecretaryAccessSchedules");

            migrationBuilder.DropTable(
                name: "SecretaryWalletTransactions");

            migrationBuilder.DropTable(
                name: "ServiceLogs");

            migrationBuilder.DropTable(
                name: "UserPresenceLogs");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "PatientFinancialCases");

            migrationBuilder.DropTable(
                name: "PatientReferrals");

            migrationBuilder.DropTable(
                name: "PatientWallets");

            migrationBuilder.DropTable(
                name: "SecretarySales");

            migrationBuilder.DropTable(
                name: "SecretaryWallets");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "SecretarySaleServices");

            migrationBuilder.DropTable(
                name: "LeadAssignments");

            migrationBuilder.DropTable(
                name: "ConsultantProfiles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
