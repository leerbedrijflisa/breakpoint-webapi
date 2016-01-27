﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lisa.Breakpoint.WebApi
{
    public class Report
    {
        public string Title { get; set; }
        public string Number { get; set; }
        public string Project { get; set; }
        public string Organization { get; set; }
        public string StepByStep { get; set; }
        public string Expectation { get; set; }
        public string WhatHappened { get; set; }
        public string Reporter { get; set; }
        public DateTime Reported { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string Version { get; set; }
        public AssignedTo AssignedTo { get; set; }
        public IList<Comment> Comments { get; set; }
        public IList<string> Platforms { get; set; }
    }

    public class ReportPost
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string StepByStep { get; set; }

        [Required]
        public string Expectation { get; set; }

        [Required]
        public string WhatHappened { get; set; }

        [Required]
        public string Reporter { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Priority { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public AssignedTo AssignedTo { get; set; }
        public IList<string> Platforms { get; set; }
    }

    public class AssignedTo
    {
        [Required]
        public string Type { get; set; }
        [Required]
        public string Value { get; set; }
    }

    public class Comment
    {
        public DateTime Posted { get; set; }
        [Required]
        public string   Author { get; set; }
        [Required]
        public string   Text { get; set; }
    }

    public class Filter
    {
        public Filter (string type, string value)
        {
            Type = type;
            Value = value;
        }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}