using System.ComponentModel.DataAnnotations;

namespace Blog_API_Auth.Models.Dtos
{
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        public string UserEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
