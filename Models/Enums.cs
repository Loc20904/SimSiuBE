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
        Sold
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }
}
