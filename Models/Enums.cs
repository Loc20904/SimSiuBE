namespace ViettalAPI.Models
{
    public enum UserRole
    {
        Customer,
        Admin
    }

    public enum SimStatus
    {
        Available,
        Reserved,
        Sold
    }

    public enum OrderStatus
    {
        Pending,
        PendingPayment,
        Paid,
        Confirmed,
        Completed,
        PaymentExpired,
        Cancelled
    }

    public enum PaymentProvider
    {
        PayOS
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Cancelled,
        Expired,
        Failed
    }
}
