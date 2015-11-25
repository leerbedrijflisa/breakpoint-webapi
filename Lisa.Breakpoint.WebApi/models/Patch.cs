namespace Lisa.Breakpoint.WebApi.Models
{
    public class Patch
    {
        public string Action { get; set; }
        public string Field { get; set; }
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
