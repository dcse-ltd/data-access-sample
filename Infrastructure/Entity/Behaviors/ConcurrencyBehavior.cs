namespace Infrastructure.Entity.Behaviors;

public class ConcurrencyBehavior
{
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}