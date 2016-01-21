using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi
{
    public static class Priorities
    {
        public const string FixImmediately = "immediately";
        public const string FixBeforeRelease = "beforerelease";
        public const string FixForNextRelease = "nextrelease";
        public const string FixWhenever = "whenever";

        public static Error InvalidValueError = new Error(1208, new { field = "priority", value = string.Format("{0}, {1}, {2}, {3}", FixImmediately, FixBeforeRelease, FixForNextRelease, FixWhenever) });

        public static readonly IEnumerable<string> List = new string[] { FixImmediately, FixBeforeRelease, FixForNextRelease, FixWhenever };
    }

    public static class Statuses
    {
        public const string Open = "open";
        public const string Fixed = "fixed";
        public const string WontFix = "wontFix";
        public const string WontFixApproved = "wontFixApproved";
        public const string Closed = "closed";

        public static Error InvalidValueError = new Error(1208, new { field = "status", value = string.Format("{0}, {1}, {2}, {3}, {4}", Open, Fixed, WontFix, WontFixApproved, Closed) });

        public static readonly IEnumerable<string> List = new string[] { Open, Fixed, WontFix, WontFixApproved, Closed };
    }

    public static class ProjectGroups
    {
        public const string Developers = "developer";
        public const string Testers = "tester";
        public const string Managers = "manager";

        public static readonly IEnumerable<string> List = new[] { Managers, Developers, Testers };
    }

    public static class OrganizationGroups
    {
        public const string Managers = "manager";
        public const string Member = "member";

        public static readonly IEnumerable<string> List = new[] { Managers, Member };
    }
}
