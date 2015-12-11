﻿using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi.Models
{
    public class Member : User
    {
        public string Role { get; set; }
    }

    public class MemberPost : UserPost
    {
        [Required]
        public string Role { get; set; }
    }
}