using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ViettalAPI.Migrations
{
    /// <inheritdoc />
    public partial class Initdatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SimId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalPrice = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sims",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Meaning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "Address", "CreatedAt", "Note", "ReceiverName", "ReceiverPhone", "SimId", "Status", "TotalPrice", "UserId" },
                values: new object[,]
                {
                    { "ORD-1001", "Thành phố Hồ Chí Minh", new DateTime(2026, 6, 15, 9, 47, 41, 834, DateTimeKind.Utc).AddTicks(9772), "Giao hàng giờ hành chính", "Nguyễn Văn Khách", "0909000000", "sim-002", "Pending", 28500000, "user-customer" },
                    { "ORD-1002", "Hà Nội", new DateTime(2026, 6, 13, 13, 47, 41, 834, DateTimeKind.Utc).AddTicks(9782), "", "Nguyễn Văn Khách", "0909000000", "sim-004", "Completed", 9600000, "user-customer" }
                });

            migrationBuilder.InsertData(
                table: "Sims",
                columns: new[] { "Id", "Carrier", "Description", "Meaning", "PhoneNumber", "Price", "Status", "Type" },
                values: new object[,]
                {
                    { "sim-001", "Mobifone", "Số dễ nhớ, phù hợp kinh doanh và xây dựng thương hiệu cá nhân.", "Dãy 8 tượng trưng cho phát tài, phát lộc.", "0909 888 888", 125000000, "Available", "Sim lục quý" },
                    { "sim-002", "Viettel", "Cân bằng giữa độ đẹp, ngân sách và tính dễ đọc.", "Cặp 68 và 86 mang ý nghĩa lộc phát luân chuyển.", "0986 686 868", 28500000, "Available", "Sim lộc phát" },
                    { "sim-003", "Vinaphone", "Phù hợp chủ shop, tư vấn viên và người làm dịch vụ cần số dễ nhớ.", "Tam hoa 3 tạo cảm giác chắc chắn, bền vững.", "0912 333 333", 42000000, "Available", "Sim tam hoa" },
                    { "sim-004", "Vietnamobile", "Một lựa chọn cá nhân hóa, dễ nhớ khi giới thiệu.", "Gắn với năm sinh 1999, dễ tạo dấu ấn cá nhân.", "0888 197 1999", 9600000, "Sold", "Sim năm sinh" },
                    { "sim-005", "Viettel", "Phù hợp người kinh doanh cần số hotline nổi bật.", "Tứ quý 4 tạo nhịp số đều, chắc và rất dễ ghi nhớ.", "0901 444 444", 65000000, "Available", "Sim tứ quý" },
                    { "sim-006", "Mobifone", "Dãy số có nhịp đọc đẹp, giá vừa phải.", "Cặp 79 tượng trưng cho thần tài, may mắn trong công việc.", "0937 797 979", 18500000, "Available", "Sim thần tài" },
                    { "sim-007", "Gmobile", "Số đẹp cho nhu cầu cá nhân hoặc cửa hàng nhỏ.", "Cụm 555 lặp lại tạo cảm giác cân bằng và dễ nhớ.", "0999 555 555", 32000000, "Sold", "Sim tam hoa" },
                    { "sim-008", "Vinaphone", "Số có nhịp đọc mềm, dễ giới thiệu qua điện thoại.", "Cặp 168 gợi ý nghĩa sinh lộc phát.", "0918 168 168", 22000000, "Available", "Sim lộc phát" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "Password", "Phone", "Role" },
                values: new object[,]
                {
                    { "user-admin", "admin@simdep.vn", "Quản trị viên", "$2a$11$vWiZj4kZj7CkpUvV/aonDuJAnDd1p/N5k4ydIi3J4MFpbElYTbXM6", "0909999999", "Admin" },
                    { "user-customer", "customer@simdep.vn", "Nguyễn Văn Khách", "$2a$11$.TWYGrbFs7kG64fcbVW/oOxyWwMk3IyuCY91EaWLXTPeiMASS7cRu", "0909000000", "Customer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Sims");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
