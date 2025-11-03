namespace Infrastructure.Repository.Models;

/// <summary>
/// Options for configuring entity queries, including change tracking and soft-deleted entity inclusion.
/// This record is immutable to prevent accidental modification of shared instances.
/// </summary>
/// <remarks>
/// This record provides pre-configured options for common query scenarios and allows
/// customization of query behavior. The static properties provide convenient presets:
/// <list type="bullet">
/// <item><description><see cref="Default"/> - Standard read-only query (no tracking, exclude soft-deleted)</description></item>
/// <item><description><see cref="Tracking"/> - Query with change tracking enabled for updates</description></item>
/// <item><description><see cref="SoftDeleted"/> - Query that includes soft-deleted entities</description></item>
/// <item><description><see cref="All"/> - Query with all options (includes soft-deleted)</description></item>
/// <item><description><see cref="ForRestore"/> - Query for restoring soft-deleted entities (tracking + include soft-deleted)</description></item>
/// </list>
/// 
/// Usage example:
/// <code>
/// // Standard read-only query
/// var entities = await repository.GetAllAsync(QueryOptions.Default);
/// 
/// // Query with tracking for updates
/// var entity = await repository.GetByIdAsync(id, QueryOptions.Tracking);
/// 
/// // Query including soft-deleted entities
/// var allEntities = await repository.GetAllAsync(QueryOptions.SoftDeleted);
/// 
/// // Create custom options
/// var customOptions = new QueryOptions(trackChanges: true, includeSoftDeleted: false);
/// </code>
/// </remarks>
public record QueryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryOptions"/> record.
    /// </summary>
    /// <param name="trackChanges">Whether to enable change tracking for the query.</param>
    /// <param name="includeSoftDeleted">Whether to include soft-deleted entities in the query.</param>
    public QueryOptions(bool trackChanges = false, bool includeSoftDeleted = false)
    {
        TrackChanges = trackChanges;
        IncludeSoftDeleted = includeSoftDeleted;
    }

    /// <summary>
    /// Gets whether change tracking should be enabled for the query.
    /// When true, entities will be tracked by EF Core's change tracker for automatic change detection.
    /// When false, entities will use AsNoTracking for improved read-only performance.
    /// </summary>
    /// <remarks>
    /// Tracking should be enabled when:
    /// <list type="bullet">
    /// <item><description>You plan to modify and save the entity</description></item>
    /// <item><description>You need to detect changes made to the entity</description></item>
    /// </list>
    /// 
    /// Tracking should be disabled when:
    /// <list type="bullet">
    /// <item><description>You only need to read the data</description></item>
    /// <item><description>Performance is critical and you're dealing with large result sets</description></item>
    /// </list>
    /// </remarks>
    public bool TrackChanges { get; }
    
    /// <summary>
    /// Gets whether soft-deleted entities should be included in the query.
    /// When true, query filters for soft-deleted entities are ignored.
    /// When false, soft-deleted entities are excluded from results by default.
    /// </summary>
    /// <remarks>
    /// This option allows you to query soft-deleted entities when needed,
    /// such as for administrative purposes or when restoring deleted entities.
    /// </remarks>
    public bool IncludeSoftDeleted { get; }

    /// <summary>
    /// Default query options: no change tracking, exclude soft-deleted entities.
    /// Suitable for most read-only queries.
    /// </summary>
    public static QueryOptions Default => new(false, false);

    /// <summary>
    /// Query options with change tracking enabled: tracks changes, excludes soft-deleted entities.
    /// Suitable for queries where entities will be modified and saved.
    /// </summary>
    public static QueryOptions Tracking => new(true, false);
    
    /// <summary>
    /// Query options for including soft-deleted entities: no change tracking, includes soft-deleted entities.
    /// Suitable for administrative queries that need to see deleted data.
    /// </summary>
    public static QueryOptions SoftDeleted => new(false, true);

    /// <summary>
    /// Query options with all features: no change tracking, includes soft-deleted entities.
    /// Suitable for read-only queries that need to see all data including deleted entities.
    /// </summary>
    public static QueryOptions All => new(false, true);

    /// <summary>
    /// Query options for restoring entities: change tracking enabled, includes soft-deleted entities.
    /// Suitable for restore operations where soft-deleted entities need to be loaded, modified, and saved.
    /// </summary>
    public static QueryOptions ForRestore => new(true, true);
}