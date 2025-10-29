namespace Infrastructure.Repository.Models;

public class QueryOptions
{
    public bool TrackChanges { get; set; }
    public bool IncludeSoftDeleted { get; set; }

    public static QueryOptions Default => new()
    {
        TrackChanges = false,
        IncludeSoftDeleted = false,
    };

    public static QueryOptions Tracking => new()
    {
        TrackChanges = true,
        IncludeSoftDeleted = false
    };
    
    public static QueryOptions SoftDeleted => new()
    {
        TrackChanges = false,
        IncludeSoftDeleted = true
    };

    public static QueryOptions All => new()
    {
        TrackChanges = false,
        IncludeSoftDeleted = true
    };

    public static QueryOptions ForRestore => new()
    {
        TrackChanges = true,
        IncludeSoftDeleted = true
    };
}