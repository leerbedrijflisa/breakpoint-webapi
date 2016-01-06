using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi.Models
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
        public const string WontfixApproved = "wontFixApproved";
        public const string Closed = "closed";

        public static Error InvalidValueError = new Error(1208, new { field = "status", value = string.Format("{0}, {1}, {2}, {3}, {4}", Open, Fixed, WontFix, WontfixApproved, Closed) });

        public static readonly IEnumerable<string> List = new string[] { Open, Fixed, WontFix, WontfixApproved, Closed };
    }

    public static class Groups
    {
        public const string Developers = "developers";
        public const string Testers = "testers";
        public const string Managers = "managers";

        public static readonly IEnumerable<string> List = new[] { Managers, Developers, Testers };
    }
}
