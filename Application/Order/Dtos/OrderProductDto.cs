namespace Application.Order.Dtos;

public class OrderProductDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
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
}