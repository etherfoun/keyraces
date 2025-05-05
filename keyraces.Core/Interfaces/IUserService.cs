namespace keyraces.Core.Interfaces
{
    public interface IUserService
    {
        Task<string> RegisterAsync(string name, string email, string password);
        Task<bool> ValidateLoginAsync(string email, string password);
    }
}
