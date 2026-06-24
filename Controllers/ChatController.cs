using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViettalAPI.Data;
using ViettalAPI.Services;

namespace ViettalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ViettalDbContext _context;
        private readonly IGeminiService _geminiService;

        public ChatController(ViettalDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpGet("models")]
        [AllowAnonymous]
        public async Task<IActionResult> GetModels()
        {
            var result = await _geminiService.ListModelsAsync();
            return Content(result, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> PostChat([FromBody] ChatRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });
            }

            // 1. Get authenticated user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Người dùng chưa xác thực." });
            }

            // 2. Fetch User Profile
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });
            }

            // 3. Fetch User's Orders
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 4. Fetch some available Sims to recommend
            var availableSims = await _context.Sims
                .Where(s => s.Status == Models.SimStatus.Available)
                .Take(12)
                .ToListAsync();

            // 5. Construct Context Injection System Instruction
            var systemInstruction = BuildSystemInstruction(user, orders, availableSims);

            // 6. Call Gemini Service
            var reply = await _geminiService.ChatAsync(systemInstruction, request.Message.Trim());

            return Ok(new ChatResponseDto { Reply = reply });
        }

        private string BuildSystemInstruction(
            Models.AppUser user, 
            List<Models.SimOrder> orders, 
            List<Models.BeautifulSim> availableSims)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Bạn là Trợ lý ảo AI thông minh và nhiệt tình của ứng dụng 'Viettal Sim Đẹp' (Hệ thống phân phối sim số đẹp trực tuyến).");
            sb.AppendLine("Nhiệm vụ của bạn là hỗ trợ khách hàng tra cứu thông tin đơn hàng, giải đáp thắc mắc dịch vụ và tư vấn lựa chọn SIM số đẹp phù hợp.");
            sb.AppendLine();
            
            sb.AppendLine("--- THÔNG TIN KHÁCH HÀNG ĐANG TRÒ CHUYỆN ---");
            sb.AppendLine($"- Họ và tên: {user.FullName}");
            sb.AppendLine($"- Email: {user.Email}");
            sb.AppendLine($"- Số điện thoại: {user.Phone}");
            sb.AppendLine();

            sb.AppendLine("--- DANH SÁCH ĐƠN HÀNG CỦA KHÁCH HÀNG NÀY ---");
            if (orders.Count == 0)
            {
                sb.AppendLine("(Khách hàng chưa có đơn hàng nào)");
            }
            else
            {
                foreach (var order in orders)
                {
                    sb.AppendLine($"- Đơn hàng mã: {order.Id} | SIM ID đặt mua: {order.SimId} | Người nhận: {order.ReceiverName} ({order.ReceiverPhone}) | Địa chỉ: {order.Address} | Tổng tiền: {order.TotalPrice:N0} VNĐ | Trạng thái: {order.Status} | Ngày tạo: {order.CreatedAt:dd/MM/yyyy HH:mm} (UTC)");
                    if (!string.IsNullOrEmpty(order.Note))
                    {
                        sb.AppendLine($"  Ghi chú đơn hàng: {order.Note}");
                    }
                }
            }
            sb.AppendLine();

            sb.AppendLine("--- DANH SÁCH SIM SỐ ĐẸP ĐANG MỞ BÁN (AVAILABLE) ---");
            sb.AppendLine("CHỈ được gợi ý các SIM có trong danh sách này. TUYỆT ĐỐI không tự bịa ra số SIM khác.");
            if (availableSims.Count == 0)
            {
                sb.AppendLine("(Hiện tại kho SIM đang tạm hết hàng)");
            }
            else
            {
                foreach (var sim in availableSims)
                {
                    sb.AppendLine($"- SIM: {sim.PhoneNumber} | Nhà mạng: {sim.Carrier} | Loại: {sim.Type} | Giá: {sim.Price:N0} VNĐ | Ý nghĩa: {sim.Meaning} | Chi tiết: {sim.Description}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("--- HƯỚNG DẪN ỨNG XỬ & NGUYÊN TẮC TRẢ LỜI ---");
            sb.AppendLine("1. Xưng hô lịch sự: Gọi khách hàng là Anh/Chị hoặc gọi trực tiếp bằng tên của họ ('Khách', 'Anh Khách', v.v. dựa trên FullName). Tự xưng là 'Trợ lý Viettal' hoặc 'Em'.");
            sb.AppendLine("2. Khi khách hàng hỏi về đơn hàng: Hãy kiểm tra danh sách đơn hàng trên và tóm tắt trạng thái đơn hàng của họ bằng tiếng Việt dễ hiểu (ví dụ: 'Đơn hàng mã ORD-xxx của anh/chị hiện đang ở trạng thái Chờ xử lý...').");
            sb.AppendLine("3. Khi khách hàng muốn tư vấn SIM: Hãy tìm hiểu nhu cầu của họ (nhà mạng mong muốn, khoảng giá, hoặc loại SIM như tam hoa, tứ quý, thần tài, lộc phát...) và chủ động trích xuất các số SIM phù hợp từ DANH SÁCH SIM ĐANG MỞ BÁN ở trên để giới thiệu, giải thích ý nghĩa phong thủy tốt lành của SIM đó.");
            sb.AppendLine("4. Hướng dẫn đặt mua: Nhắc nhở khách hàng rằng họ có thể tìm kiếm số SIM này trong mục 'Kho SIM' hoặc click chọn SIM nổi bật ngay trên ứng dụng và nhấn nút 'Đặt mua' để tiến hành đặt hàng trực tiếp.");
            sb.AppendLine("5. Chính sách chung của Viettal: Giao SIM miễn phí toàn quốc, hỗ trợ đăng ký chính chủ miễn phí tại nhà khi giao SIM.");
            sb.AppendLine("6. Định dạng câu trả lời: Sử dụng Markdown (in đậm, danh sách gạch đầu dòng) để câu trả lời dễ đọc, rõ ràng.");
            sb.AppendLine("7. Giới hạn câu trả lời: Trả lời ngắn gọn, tập trung vào câu hỏi, tránh lan man.");

            return sb.ToString();
        }
    }

    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
    }
}
