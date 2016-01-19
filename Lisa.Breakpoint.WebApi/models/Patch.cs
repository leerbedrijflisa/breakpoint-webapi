using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi
{
    public class Patch
    {
        [Required]
        public string Action { get; set; }

        [Required]
        public string Field { get; set; }

        [Required]
        public object Value { get; set; }
    }

    // temporary for patching the members in a project
    public class TempMemberPatch
    {
        public string Sender { get; set; }
        public string Type { get; set; }
        public string Member { get; set; }
        public string Role { get; set; }
    }
}
