using Blog_API_Auth.Models;
using Blog_API_Auth.Models.Dtos;
using Blog_API_Auth.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Blog_API_Auth.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiExplorerSettings(GroupName = "users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private ApiResponse _apiResponse;
        private string _secretKey;
        private string _infoFromTkn;

        public UsersController(IUserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
            _apiResponse = new();
            _secretKey = _config.GetValue<string>("AppSettings:Secret");
            _infoFromTkn = "";
        }

        [HttpPost]
        public IActionResult Register([FromBody] UserRegisterDto userReg)
        {
            try
            {
                var user = _userRepo.Register(userReg, userReg.Password);

                if (user.UserEmail != userReg.UserEmail)
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add(user.UserEmail);
                    return BadRequest(_apiResponse);
                }

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = user;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return BadRequest(_apiResponse);
            }
        }

        [HttpPost]
        public IActionResult Login([FromBody] UserLoginDto userLogin)
        {
            try
            {
                var loggedUser = _userRepo.Login(userLogin.UserEmail, userLogin.Password);

                if (loggedUser == null)
                {
                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("No hay usuario con ese email");
                    return Unauthorized(_apiResponse);
                }

                if (loggedUser.UserEmail != userLogin.UserEmail)
                {
                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add(loggedUser.UserEmail);
                    return BadRequest(_apiResponse);
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, loggedUser.UserId.ToString()),
                    new Claim(ClaimTypes.Name, loggedUser.UserEmail.ToString())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddDays(1),
                    SigningCredentials = credentials
                };

                var tokenHandler = new JwtSecurityTokenHandler();

                var token = tokenHandler.CreateToken(tokenDescriptor);

                var user = new UserLoginReponse
                {
                    UserName = loggedUser.UserName,
                    Token = tokenHandler.WriteToken(token)
                };

                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Result = user;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return Ok(_apiResponse);
            }
        }

        [HttpGet]
        public IActionResult CheckRole()
        {
            try
            {
                if (GetInfoFromTkn())
                {
                    string[] data = _infoFromTkn.Split(",");

                    var id = data[0];
                    var email = data[1];

                    if (_userRepo.CheckRole(email))
                    {
                        _apiResponse.StatusCode = HttpStatusCode.OK;
                        _apiResponse.IsSuccess = true;
                        _apiResponse.Result = email;
                        return Ok(_apiResponse);
                    }

                    _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.IsSuccess = false;
                    _apiResponse.ErrorMessages.Add("Usuario no autorizado");
                    _apiResponse.Result = email;
                    return Ok(_apiResponse);
                }

                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(_infoFromTkn);
                return BadRequest(_apiResponse);

            } catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _apiResponse);
            }
        }

        [HttpGet]
        public IActionResult GetIdFromToken()
        {
            try
            {
                if (GetInfoFromTkn())
                {
                    string[] data = _infoFromTkn.Split(",");

                    var id = data[0];
                    var email = data[1];

                    _apiResponse.StatusCode = HttpStatusCode.OK;
                    _apiResponse.IsSuccess = true;
                    _apiResponse.Result = id;
                    return Ok(_apiResponse);
                }

                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(_infoFromTkn);
                return StatusCode(500, _apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add(ex.Message);
                return BadRequest(_apiResponse);
            }
        }

        private bool GetInfoFromTkn()
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);
                var token = HttpContext.Request.Headers["Authorization"];

                token = token.ToString().Substring(7);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var UserId = jwtToken.Claims.First(x => x.Type == "nameid").Value;
                var UserEmail = jwtToken.Claims.First(x => x.Type == "unique_name").Value;

                _infoFromTkn = UserId + "," + UserEmail;

                return true;
            } 
            catch (Exception ex)
            {
                _infoFromTkn = ex.Message;
                return false;
            }
        }
    }
}
