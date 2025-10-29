namespace Application.Order.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public string? LockedBy { get; set; }
    public Guid? LockedByUserId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public Guid ModifiedByUserId { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IEnumerable<OrderProductDto> OrderProducts { get; set; } = new List<OrderProductDto>();   
}