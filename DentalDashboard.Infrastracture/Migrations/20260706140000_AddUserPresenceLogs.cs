using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPresenceLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'UserPresenceLogs', N'U') IS NULL
                BEGIN
                    CREATE TABLE UserPresenceLogs (
                        Id bigint NOT NULL IDENTITY(1,1),
                        UserId uniqueidentifier NOT NULL,
                        EventType int NOT NULL,
                        OccurredAt datetime2 NOT NULL,
                        Description nvarchar(500) NULL,
                        CreatedAt datetime2 NOT NULL,
                        UpdatedAt datetime2 NULL,
                        IsDeleted bit NOT NULL CONSTRAINT DF_UserPresenceLogs_IsDeleted DEFAULT 0,
                        DeletedAt datetime2 NULL,
                        CONSTRAINT PK_UserPresenceLogs PRIMARY KEY (Id),
                        CONSTRAINT FK_UserPresenceLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id)
                    );

                    CREATE INDEX IX_UserPresenceLogs_UserId ON UserPresenceLogs(UserId);
                    CREATE INDEX IX_UserPresenceLogs_OccurredAt ON UserPresenceLogs(OccurredAt);
                    CREATE INDEX IX_UserPresenceLogs_UserId_OccurredAt ON UserPresenceLogs(UserId, OccurredAt);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'UserPresenceLogs', N'U') IS NOT NULL
                    DROP TABLE UserPresenceLogs;
                """);
        }
    }
}
