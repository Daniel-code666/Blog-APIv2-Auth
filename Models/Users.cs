using System.ComponentModel.DataAnnotations;

namespace Blog_API_Auth.Models
{
    public class Users
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        public string UserName { get; set; }

        public byte[] PasswordHash { get; set; }

        public byte[] PasswordSalt { get; set; }

        public int UserRolId { get; set; }

        public DateTime UserCreatedAt { get; set; } 
    }
}
