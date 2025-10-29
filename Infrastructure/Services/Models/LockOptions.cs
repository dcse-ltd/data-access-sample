namespace Infrastructure.Services.Models;

public class LockOptions
{
    public bool IncludeChildren { get; set; }
    public int MaxDepth { get; set; } = 1;
}