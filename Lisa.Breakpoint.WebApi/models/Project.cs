using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi.Models
{
    public class Project
    {
        public string Number { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Organization  { get; set; }
        public string CurrentVersion { get; set; }
        public IList<Member> Members  { get; set; }
    }

    public class ProjectPost
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string CurrentVersion { get; set; }

        [Required]
        public IList<Member> Members { get; set; }
    }
}