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
        public IList<string> Browsers { get; set; }
        public IList<Group>  Groups   { get; set; }
        public IList<Member> Members  { get; set; }
    }

    public class ProjectPost
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string CurrentVersion { get; set; }
        public IList<string> Browsers { get; set; }

        [Required]
        public IList<Group> Groups { get; set; }

        [Required]
        public IList<Member> Members { get; set; }
    }

    public class Group
    {
        public Group()
        {
            Disabled = false;
        }
        [Required]
        public int    Level { get; set; }

        [Required]
        public string Name  { get; set; }
        public bool   Disabled { get; set; }
    }
}