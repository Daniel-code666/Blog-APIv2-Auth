using Blog_API_Auth.Data;
using Blog_API_Auth.Models.Dtos;
using Blog_API_Auth.Models;
using Blog_API_Auth.Repository.IRepository;

namespace Blog_API_Auth.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;

        public UserRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Users Login(string email, string password)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.UserEmail == email);

                if (user == null)
                {
                    return null;
                }

                if (!VerificaPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    return new Users() { UserEmail = "Contraseña incorrecta" };
                }

                return user;
            }
            catch (Exception ex)
            {
                return new Users() { UserEmail = ex.Message };
            }
        }

        public Users Register(UserRegisterDto userReg, string password)
        {
            try
            {
                var userTest = _db.Users.FirstOrDefault(u => u.UserEmail == userReg.UserEmail);

                if (!CheckEmail(userReg.UserEmail))
                {
                    var user = new Users()
                    {
                        UserEmail = userReg.UserEmail,
                        UserName = userReg.UserName,
                        UserRolId = 2,
                        UserCreatedAt = DateTime.Now 
                    };

                    byte[] passwordHash, passwordSalt;

                    CrearPasswordHash(password, out passwordHash, out passwordSalt);

                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    _db.Users.Add(user);

                    Save();

                    return user;
                }
                else
                {
                    return new Users() { UserEmail = "El email ya está registrado" };
                }
            }
            catch (Exception ex)
            {
                return new Users() { UserEmail = ex.Message };
            }
        }

        public bool CheckRole(string email)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.UserEmail == email);

                return user.UserRolId == 1;
            } catch (Exception ex)
            {
                return false;
            }
        }

        private bool VerificaPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        private void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool CheckEmail(string email)
        {
            return _db.Users.Any(u => u.UserEmail == email);
        }

        private bool Save()
        {
            return _db.SaveChanges() >= 0 ? true : false;
        }
    }
}
