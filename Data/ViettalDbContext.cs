using Microsoft.EntityFrameworkCore;
using ViettalAPI.Models;

namespace ViettalAPI.Data
{
    public class ViettalDbContext : DbContext
    {
        public ViettalDbContext(DbContextOptions<ViettalDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<BeautifulSim> Sims => Set<BeautifulSim>();
        public DbSet<SimOrder> Orders => Set<SimOrder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role, Status, OrderStatus to be stored as strings in DB
            modelBuilder.Entity<AppUser>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<BeautifulSim>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<SimOrder>()
                .Property(o => o.Status)
                .HasConversion<string>();

            // Seed Users (Customer password: '123456', Admin password: 'admin123')
            var customerHash = BCrypt.Net.BCrypt.HashPassword("123456");
            var adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = "user-customer",
                    FullName = "Nguyễn Văn Khách",
                    Email = "customer@simdep.vn",
                    Phone = "0909000000",
                    Role = UserRole.Customer,
                    Password = customerHash
                },
                new AppUser
                {
                    Id = "user-admin",
                    FullName = "Quản trị viên",
                    Email = "admin@simdep.vn",
                    Phone = "0909999999",
                    Role = UserRole.Admin,
                    Password = adminHash
                }
            );

            // Seed BeautifulSims
            modelBuilder.Entity<BeautifulSim>().HasData(
                new BeautifulSim
                {
                    Id = "sim-001",
                    PhoneNumber = "0909 888 888",
                    Carrier = "Mobifone",
                    Type = "Sim lục quý",
                    Price = 125000000,
                    Meaning = "Dãy 8 tượng trưng cho phát tài, phát lộc.",
                    Status = SimStatus.Available,
                    Description = "Số dễ nhớ, phù hợp kinh doanh và xây dựng thương hiệu cá nhân."
                },
                new BeautifulSim
                {
                    Id = "sim-002",
                    PhoneNumber = "0986 686 868",
                    Carrier = "Viettel",
                    Type = "Sim lộc phát",
                    Price = 28500000,
                    Meaning = "Cặp 68 và 86 mang ý nghĩa lộc phát luân chuyển.",
                    Status = SimStatus.Available,
                    Description = "Cân bằng giữa độ đẹp, ngân sách và tính dễ đọc."
                },
                new BeautifulSim
                {
                    Id = "sim-003",
                    PhoneNumber = "0912 333 333",
                    Carrier = "Vinaphone",
                    Type = "Sim tam hoa",
                    Price = 42000000,
                    Meaning = "Tam hoa 3 tạo cảm giác chắc chắn, bền vững.",
                    Status = SimStatus.Available,
                    Description = "Phù hợp chủ shop, tư vấn viên và người làm dịch vụ cần số dễ nhớ."
                },
                new BeautifulSim
                {
                    Id = "sim-004",
                    PhoneNumber = "0888 197 1999",
                    Carrier = "Vietnamobile",
                    Type = "Sim năm sinh",
                    Price = 9600000,
                    Meaning = "Gắn với năm sinh 1999, dễ tạo dấu ấn cá nhân.",
                    Status = SimStatus.Sold,
                    Description = "Một lựa chọn cá nhân hóa, dễ nhớ khi giới thiệu."
                },
                new BeautifulSim
                {
                    Id = "sim-005",
                    PhoneNumber = "0901 444 444",
                    Carrier = "Viettel",
                    Type = "Sim tứ quý",
                    Price = 65000000,
                    Meaning = "Tứ quý 4 tạo nhịp số đều, chắc và rất dễ ghi nhớ.",
                    Status = SimStatus.Available,
                    Description = "Phù hợp người kinh doanh cần số hotline nổi bật."
                },
                new BeautifulSim
                {
                    Id = "sim-006",
                    PhoneNumber = "0937 797 979",
                    Carrier = "Mobifone",
                    Type = "Sim thần tài",
                    Price = 18500000,
                    Meaning = "Cặp 79 tượng trưng cho thần tài, may mắn trong công việc.",
                    Status = SimStatus.Available,
                    Description = "Dãy số có nhịp đọc đẹp, giá vừa phải."
                },
                new BeautifulSim
                {
                    Id = "sim-007",
                    PhoneNumber = "0999 555 555",
                    Carrier = "Gmobile",
                    Type = "Sim tam hoa",
                    Price = 32000000,
                    Meaning = "Cụm 555 lặp lại tạo cảm giác cân bằng và dễ nhớ.",
                    Status = SimStatus.Sold,
                    Description = "Số đẹp cho nhu cầu cá nhân hoặc cửa hàng nhỏ."
                },
                new BeautifulSim
                {
                    Id = "sim-008",
                    PhoneNumber = "0918 168 168",
                    Carrier = "Vinaphone",
                    Type = "Sim lộc phát",
                    Price = 22000000,
                    Meaning = "Cặp 168 gợi ý nghĩa sinh lộc phát.",
                    Status = SimStatus.Available,
                    Description = "Số có nhịp đọc mềm, dễ giới thiệu qua điện thoại."
                }
            );

            // Seed Orders
            modelBuilder.Entity<SimOrder>().HasData(
                new SimOrder
                {
                    Id = "ORD-1001",
                    UserId = "user-customer",
                    SimId = "sim-002",
                    ReceiverName = "Nguyễn Văn Khách",
                    ReceiverPhone = "0909000000",
                    Address = "Thành phố Hồ Chí Minh",
                    TotalPrice = 28500000,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddHours(-4),
                    Note = "Giao hàng giờ hành chính"
                },
                new SimOrder
                {
                    Id = "ORD-1002",
                    UserId = "user-customer",
                    SimId = "sim-004",
                    ReceiverName = "Nguyễn Văn Khách",
                    ReceiverPhone = "0909000000",
                    Address = "Hà Nội",
                    TotalPrice = 9600000,
                    Status = OrderStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Note = ""
                }
            );
        }
    }
}
