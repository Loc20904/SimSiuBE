using Microsoft.EntityFrameworkCore;
using ViettalAPI.Data;
using ViettalAPI.Models;

namespace ViettalAPI.Services
{
    public interface IPaymentExpirationService
    {
        Task ReleaseExpiredPaymentsAsync();
    }

    public class PaymentExpirationService : IPaymentExpirationService
    {
        private readonly ViettalDbContext _context;

        public PaymentExpirationService(ViettalDbContext context)
        {
            _context = context;
        }

        public async Task ReleaseExpiredPaymentsAsync()
        {
            var now = DateTime.UtcNow;
            var expiredPayments = await _context.PaymentTransactions
                .Where(payment => payment.Status == PaymentStatus.Pending && payment.ExpiredAt <= now)
                .ToListAsync();

            if (expiredPayments.Count == 0)
            {
                return;
            }

            foreach (var payment in expiredPayments)
            {
                payment.Status = PaymentStatus.Expired;
                payment.UpdatedAt = now;

                var sim = await _context.Sims.FindAsync(payment.SimId);
                if (sim == null || sim.Status != SimStatus.Reserved)
                {
                    continue;
                }

                var hasActivePaymentOrOrder = await _context.PaymentTransactions.AnyAsync(other =>
                    other.Id != payment.Id &&
                    other.SimId == payment.SimId &&
                    (other.Status == PaymentStatus.Pending || other.Status == PaymentStatus.Paid));

                if (!hasActivePaymentOrOrder)
                {
                    sim.Status = SimStatus.Available;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
