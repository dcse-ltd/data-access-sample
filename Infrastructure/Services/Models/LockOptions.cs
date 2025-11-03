namespace Infrastructure.Services.Models;

/// <summary>
/// Options for controlling entity locking behavior, including cascading locks to child entities.
/// </summary>
public class LockOptions
{
    /// <summary>
    /// Gets or sets whether to include child entities in locking operations.
    /// When true, locks will cascade to child entities marked with <see cref="Infrastructure.Entity.Attributes.CascadeLockAttribute"/>.
    /// </summary>
    public bool IncludeChildren { get; set; }
    
    private int _maxDepth = 1;
    
    /// <summary>
    /// Gets or sets the maximum depth to traverse when locking child entities recursively.
    /// Must be greater than 0. Default is 1 (lock direct children only).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set => _maxDepth = value > 0 
            ? value 
            : throw new ArgumentOutOfRangeException(nameof(MaxDepth), "MaxDepth must be greater than 0");
    }
}