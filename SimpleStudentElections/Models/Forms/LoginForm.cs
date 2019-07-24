using System.ComponentModel.DataAnnotations;

namespace SimpleStudentElections.Models.Forms
{
    public class LoginForm
    {
        [Required] public string Username { get; set; }
    }
}