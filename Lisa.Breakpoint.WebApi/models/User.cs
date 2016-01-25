using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi
{
    public class User
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
    }

    public class UserPost
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string FullName { get; set; }
    }
}