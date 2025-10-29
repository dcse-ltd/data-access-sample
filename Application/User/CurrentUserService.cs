using Infrastructure.Services.Interfaces;

namespace Application.User;

public class CurrentUserService : ICurrentUserService
{
    private static readonly Guid DefaultUserId = Guid.Parse("76c6e414-9fbe-4d74-bae5-402cbd26ac76"); 
    public Guid UserId => DefaultUserId;
    public string GetUserName(Guid userId) => "dave";
}