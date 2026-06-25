using System.ComponentModel.DataAnnotations;

namespace ViettalAPI.Models
{
    public class PaymentTransaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? OrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string SimId { get; set; } = string.Empty;

        [Required]
        public PaymentProvider Provider { get; set; } = PaymentProvider.PayOS;

        [Required]
        public long PayOsOrderCode { get; set; }

        [MaxLength(100)]
        public string? PaymentLinkId { get; set; }

        [MaxLength(1000)]
        public string? CheckoutUrl { get; set; }

        [MaxLength(4000)]
        public string? QrCode { get; set; }

        [Required]
        public int Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(100)]
        public string? PayOsReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        public DateTime ExpiredAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? RawWebhookJson { get; set; }

        public SimOrder? Order { get; set; }
    }
}
