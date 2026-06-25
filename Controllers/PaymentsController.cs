using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ViettalAPI.Data;
using ViettalAPI.DTOs;
using ViettalAPI.Models;
using ViettalAPI.Services;

namespace ViettalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ViettalDbContext _context;
        private readonly IPayOsService _payOsService;
        private readonly PayOsOptions _payOsOptions;
        private readonly IPaymentExpirationService _paymentExpirationService;

        public PaymentsController(
            ViettalDbContext context,
            IPayOsService payOsService,
            IOptions<PayOsOptions> payOsOptions,
            IPaymentExpirationService paymentExpirationService)
        {
            _context = context;
            _payOsService = payOsService;
            _payOsOptions = payOsOptions.Value;
            _paymentExpirationService = paymentExpirationService;
        }

        [HttpPost("payos/orders")]
        [Authorize]
        public async Task<ActionResult<CreatePayOsOrderResponse>> CreatePayOsOrder([FromBody] CreatePayOsOrderRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.SimId) ||
                string.IsNullOrWhiteSpace(request.ReceiverName) ||
                string.IsNullOrWhiteSpace(request.ReceiverPhone) ||
                string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new { message = "Vui long nhap day du thong tin dat hang." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _paymentExpirationService.ReleaseExpiredPaymentsAsync();

                var sim = await _context.Sims.FindAsync(request.SimId);
                if (sim == null)
                {
                    return BadRequest(new { message = "Khong tim thay SIM can dat mua." });
                }

                if (sim.Status != SimStatus.Available)
                {
                    return BadRequest(new { message = "SIM nay vua duoc dat mua. Vui long chon SIM khac." });
                }

                var now = DateTime.UtcNow;
                var expiredAt = now.AddMinutes(Math.Max(1, _payOsOptions.LinkExpirationMinutes));
                var payment = new PaymentTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = currentUserId,
                    SimId = sim.Id,
                    Provider = PaymentProvider.PayOS,
                    PayOsOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = sim.Price,
                    ReceiverName = request.ReceiverName.Trim(),
                    ReceiverPhone = request.ReceiverPhone.Trim(),
                    Address = request.Address.Trim(),
                    Note = request.Note.Trim(),
                    Status = PaymentStatus.Pending,
                    CreatedAt = now,
                    ExpiredAt = expiredAt,
                    UpdatedAt = now
                };

                sim.Status = SimStatus.Reserved;
                _context.PaymentTransactions.Add(payment);
                await _context.SaveChangesAsync();

                var payOsResult = await _payOsService.CreatePaymentLinkAsync(new PayOsCreatePaymentRequest
                {
                    OrderCode = payment.PayOsOrderCode,
                    Amount = payment.Amount,
                    Description = $"SIMSIU{payment.PayOsOrderCode % 1000000000}",
                    BuyerName = payment.ReceiverName,
                    BuyerPhone = payment.ReceiverPhone,
                    Items = new List<PayOsPaymentItem>
                    {
                        new()
                        {
                            Name = sim.PhoneNumber,
                            Quantity = 1,
                            Price = sim.Price
                        }
                    },
                    CancelUrl = $"{_payOsOptions.CancelUrl}?simId={Uri.EscapeDataString(payment.SimId)}&orderCode={payment.PayOsOrderCode}",
                    ReturnUrl = $"{_payOsOptions.ReturnUrl}?simId={Uri.EscapeDataString(payment.SimId)}&orderCode={payment.PayOsOrderCode}",
                    ExpiredAt = new DateTimeOffset(expiredAt).ToUnixTimeSeconds()
                });

                payment.PaymentLinkId = payOsResult.PaymentLinkId;
                payment.CheckoutUrl = payOsResult.CheckoutUrl;
                payment.QrCode = payOsResult.QrCode;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new CreatePayOsOrderResponse
                {
                    OrderId = string.Empty,
                    PayOsOrderCode = payment.PayOsOrderCode,
                    PaymentLinkId = payment.PaymentLinkId ?? string.Empty,
                    CheckoutUrl = payment.CheckoutUrl ?? string.Empty,
                    QrCode = payment.QrCode ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Loi khi tao thanh toan payOS: " + ex.Message });
            }
        }

        [HttpPost("payos/{orderCode:long}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelPayOsPayment(long orderCode)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.PayOsOrderCode == orderCode && p.UserId == currentUserId);

            if (payment == null)
            {
                return NotFound(new { message = "Khong tim thay giao dich thanh toan." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (payment.Status == PaymentStatus.Pending)
                {
                    var sim = await _context.Sims.FindAsync(payment.SimId);
                    await ReleasePaymentAsync(payment, sim, PaymentStatus.Cancelled);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { status = payment.Status.ToString() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Loi khi huy thanh toan payOS: " + ex.Message });
            }
        }

        [HttpPost("payos/{orderCode:long}/sync")]
        [Authorize]
        public async Task<IActionResult> SyncPayOsPayment(long orderCode)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.PayOsOrderCode == orderCode && p.UserId == currentUserId);

            if (payment == null)
            {
                return NotFound(new { message = "Khong tim thay giao dich thanh toan." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var linkInfo = await _payOsService.GetPaymentLinkAsync(orderCode);
                var sim = await _context.Sims.FindAsync(payment.SimId);
                payment.PaymentLinkId = string.IsNullOrWhiteSpace(linkInfo.PaymentLinkId)
                    ? payment.PaymentLinkId
                    : linkInfo.PaymentLinkId;
                payment.UpdatedAt = DateTime.UtcNow;

                var status = linkInfo.Status.Trim().ToUpperInvariant();
                if (status == "PAID")
                {
                    if (linkInfo.Amount != 0 && linkInfo.Amount != payment.Amount)
                    {
                        await ReleasePaymentAsync(payment, sim, PaymentStatus.Failed);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return BadRequest(new { message = "So tien thanh toan khong khop." });
                    }

                    await MarkPaymentPaidAsync(payment, sim, linkInfo.Transactions.FirstOrDefault()?.Reference);
                }
                else if (status == "CANCELLED" || status == "CANCELED")
                {
                    await ReleasePaymentAsync(payment, sim, PaymentStatus.Cancelled);
                }
                else if (status == "EXPIRED")
                {
                    await ReleasePaymentAsync(payment, sim, PaymentStatus.Expired);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { status = payment.Status.ToString(), orderId = payment.OrderId ?? string.Empty });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Loi khi dong bo thanh toan payOS: " + ex.Message });
            }
        }

        [HttpPost("payos/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest request)
        {
            if (!_payOsService.IsValidWebhookSignature(request.Data, request.Signature))
            {
                return BadRequest(new { message = "Chu ky webhook khong hop le." });
            }

            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.PayOsOrderCode == request.Data.OrderCode);

            if (payment == null)
            {
                return NotFound(new { message = "Khong tim thay giao dich thanh toan." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sim = await _context.Sims.FindAsync(payment.SimId);
                payment.RawWebhookJson = JsonSerializer.Serialize(request);
                payment.UpdatedAt = DateTime.UtcNow;

                if (request.Success && request.Code == "00" && request.Data.Code == "00")
                {
                    if (request.Data.Amount != payment.Amount)
                    {
                        await ReleasePaymentAsync(payment, sim, PaymentStatus.Failed);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return BadRequest(new { message = "So tien thanh toan khong khop." });
                    }

                    payment.PaymentLinkId = request.Data.PaymentLinkId;
                    await MarkPaymentPaidAsync(payment, sim, request.Data.Reference);
                }
                else if (payment.Status == PaymentStatus.Pending)
                {
                    await ReleasePaymentAsync(payment, sim, PaymentStatus.Failed);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Webhook processed." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Loi khi xu ly webhook payOS: " + ex.Message });
            }
        }

        private async Task MarkPaymentPaidAsync(PaymentTransaction payment, BeautifulSim? sim, string? reference)
        {
            if (payment.Status == PaymentStatus.Paid)
            {
                return;
            }

            payment.Status = PaymentStatus.Paid;
            payment.PayOsReference = reference;
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(payment.OrderId))
            {
                var order = new SimOrder
                {
                    Id = "ORD-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    UserId = payment.UserId,
                    SimId = payment.SimId,
                    ReceiverName = payment.ReceiverName,
                    ReceiverPhone = payment.ReceiverPhone,
                    Address = payment.Address,
                    TotalPrice = payment.Amount,
                    Status = OrderStatus.Paid,
                    CreatedAt = DateTime.UtcNow,
                    Note = payment.Note
                };

                _context.Orders.Add(order);
                payment.OrderId = order.Id;
            }

            if (sim != null)
            {
                sim.Status = SimStatus.Sold;
            }

            await Task.CompletedTask;
        }

        private async Task ReleasePaymentAsync(PaymentTransaction payment, BeautifulSim? sim, PaymentStatus status)
        {
            payment.Status = status;
            payment.UpdatedAt = DateTime.UtcNow;

            if (sim != null && sim.Status == SimStatus.Reserved)
            {
                var hasOtherActivePayment = await _context.PaymentTransactions.AnyAsync(other =>
                    other.Id != payment.Id &&
                    other.SimId == payment.SimId &&
                    (other.Status == PaymentStatus.Pending || other.Status == PaymentStatus.Paid));

                if (!hasOtherActivePayment)
                {
                    sim.Status = SimStatus.Available;
                }
            }
        }
    }
}
