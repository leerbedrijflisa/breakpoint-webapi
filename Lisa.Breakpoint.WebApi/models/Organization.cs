using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi
{
    public class Organization
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public IList<string> Members { get; set; }
        internal IList<string> Platforms { get; set; }
        internal string Number { get; set; }
    }

    public class OrganizationPost
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public IList<string> Members { get; set; }
    }
}