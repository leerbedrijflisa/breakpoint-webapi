using System;
using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi
{
    public class ReportComparer : IComparer<Report>
    {
        public int Compare(Report a, Report b)
        {
            if (PriorityToValue(a.Priority) > PriorityToValue(b.Priority))
            {
                return 1;
            }
            else if (PriorityToValue(a.Priority) == PriorityToValue(b.Priority))
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private int PriorityToValue(string priority)
        {
            switch (priority)
            {
                case Priorities.FixImmediately:
                    return 1;
                case Priorities.FixBeforeRelease:
                    return 2;
                case Priorities.FixForNextRelease:
                    return 3;
                case Priorities.FixWhenever:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
