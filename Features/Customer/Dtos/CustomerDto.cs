namespace Features.Customer.Dtos;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; }  = string.Empty;
    public string Email { get; set; }  = string.Empty;
    public string Phone { get; set; }   = string.Empty;
    public bool Locked { get; set; }
    public string? LockedBy { get; set; }
    public Guid? LockedByUserId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public Guid ModifiedByUserId { get; set; }
    public DateTime ModifiedOn { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}