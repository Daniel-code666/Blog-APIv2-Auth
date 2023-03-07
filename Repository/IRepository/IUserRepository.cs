using Blog_API_Auth.Models.Dtos;
using Blog_API_Auth.Models;

namespace Blog_API_Auth.Repository.IRepository
{
    public interface IUserRepository
    {
        Users Register(UserRegisterDto userReg, string password);

        Users Login(string email, string password);

        bool CheckRole(string email);
    }
}
