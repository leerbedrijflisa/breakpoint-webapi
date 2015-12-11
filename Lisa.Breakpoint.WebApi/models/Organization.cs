using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi.Models
{
    public class Organization
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string Slug { get; set; }
        public IList<string> Members { get; set; }
        public IList<string> Platforms { get; set; }
    }

    public class OrganizationPost
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public IList<string> Members { get; set; }
    }
}