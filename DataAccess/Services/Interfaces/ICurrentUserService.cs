namespace DataAccess.Services.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string GetUserName(Guid userId);
}