using Infrastructure.Context;
using Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Tests.Helpers;

public class TestDbContext : BaseAppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options, Assembly.GetExecutingAssembly())
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<TestLockableEntity> TestLockableEntities => Set<TestLockableEntity>();
    public DbSet<TestAuditableEntity> TestAuditableEntities => Set<TestAuditableEntity>();
    public DbSet<TestSoftDeletableEntity> TestSoftDeletableEntities => Set<TestSoftDeletableEntity>();
    public DbSet<TestConcurrencyEntity> TestConcurrencyEntities => Set<TestConcurrencyEntity>();
    public DbSet<TestParentEntity> TestParentEntities => Set<TestParentEntity>();
}

