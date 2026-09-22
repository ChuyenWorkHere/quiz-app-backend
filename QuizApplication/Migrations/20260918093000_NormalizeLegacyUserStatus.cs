using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuizApplication.Data;

#nullable disable

namespace QuizApplication.Migrations
{
    [DbContext(typeof(QuizDbContext))]
    [Migration("20260918093000_NormalizeLegacyUserStatus")]
    public partial class NormalizeLegacyUserStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Before account locking existed, RegisterAsync did not assign IsActive.
            // SQL Server therefore stored the enum default (LOCKED = 0) for normal users.
            migrationBuilder.Sql("UPDATE Users SET IsActive = 1 WHERE IsActive = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous zero values cannot be restored safely because they did not
            // represent an intentional lock.
        }
    }
}
