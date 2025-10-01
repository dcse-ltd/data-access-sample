using DataAccess.Entity.Models;

namespace DataAccess.Entity.Interfaces;

public interface ILockableEntity
{
    public LockInfo LockInfo { get; set; }
    void Lock(Guid userId);
    void Unlock();
}