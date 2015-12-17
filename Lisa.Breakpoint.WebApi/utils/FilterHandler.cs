using Lisa.Breakpoint.WebApi.Models;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Lisa.Breakpoint.WebApi.utils
{
    public static class FilterHandler
    {
        public static IQueryable<Report> ApplyFilters(this IQueryable<Report> reports, params Filter[] filters)
        {
            Expression<Func<Report, bool>> outerPredicate = r => r.Number != string.Empty;
            Expression<Func<Report, bool>> innerPredicate = r => r.Number == "-1";

            foreach (Filter filter in filters)
            {
                if (filter.Type == "title")
                {
                    if (filter.Value != "")
                    {
                        outerPredicate = outerPredicate.And(WhereTitleStartsWith(filter.Value));
                    }
                }
                else if (filter.Type == "priority")
                {
                    if (filter.Value != "all")
                    {
                        outerPredicate = outerPredicate.And(WherePriority((Priority)Enum.Parse(typeof(Priority), filter.Value)));
                    }
                }
                else if (filter.Type == "status")
                {
                    if (filter.Value != "all")
                    {
                        outerPredicate = outerPredicate.And(WhereStatus(filter.Value));
                    }
                }
                else if (filter.Type == "group")
                {
                    if (filter.Value == "none")
                    {
                        outerPredicate = outerPredicate.And(WhereNoGroups());
                    }
                    else if (filter.Value == "all")
                    {
                        innerPredicate = innerPredicate.Or(WhereAllGroups());
                    }
                    else
                    {
                        innerPredicate = innerPredicate.Or(WhereGroup(filter.Value));
                    }
                }
                else if (filter.Type == "member")
                {
                    if (filter.Value == "none")
                    {
                        outerPredicate = outerPredicate.And(WhereNoMembers());
                    }
                    else if (filter.Value == "all")
                    {
                        innerPredicate = innerPredicate.Or(WhereAllMembers());
                    }
                    else
                    {
                        innerPredicate = innerPredicate.Or(WhereMember(filter.Value));
                    }
                }
                else if (filter.Type == "reporter")
                {
                    if (filter.Value != "all")
                    {
                        outerPredicate = outerPredicate.And(WhereReporter(filter.Value));
                    }
                }
            }

            reports = reports.Where(outerPredicate.And(innerPredicate));

            return reports;
        }

        private static Expression<Func<Report, bool>> WhereVersion(string term)
        {
            return r => r.Version == term;
        }
        private static Expression<Func<Report, bool>> WhereTitleStartsWith(string term)
        {
            return r => r.Title.StartsWith(term.ToString());
        }
        private static Expression<Func<Report, bool>> WhereGroup(string term)
        {
            return r => r.AssignedTo.Value == term && r.AssignedTo.Type == "group";
        }
        private static Expression<Func<Report, bool>> WhereMember(string term)
        {
            return r => r.AssignedTo.Value == term && r.AssignedTo.Type == "person";
        }
        private static Expression<Func<Report, bool>> WhereReporter(string term)
        {
            return r => r.Reporter == term;
        }
        private static Expression<Func<Report, bool>> WhereStatus(string term)
        {
            return r => r.Status == term.Replace("%20", " ");
        }
        private static Expression<Func<Report, bool>> WhereNoGroups()
        {
            return r => r.AssignedTo.Type != "group";
        }
        private static Expression<Func<Report, bool>> WhereNoMembers()
        {
            return r => r.AssignedTo.Type != "person";
        }
        private static Expression<Func<Report, bool>> WhereAllGroups()
        {
            return r => r.AssignedTo.Type == "group";
        }
        private static Expression<Func<Report, bool>> WhereAllMembers()
        {
            return r => r.AssignedTo.Type == "person";
        }
        private static Expression<Func<Report, bool>> WherePriority(Priority priority)
        {
            return r => r.Priority == priority;
        }
        private static Expression<Func<Report, bool>> WhereReportedAfter(DateTime dateTime)
        {
            return r => r.Reported > dateTime;
        }
        private static Expression<Func<Report, bool>> WhereReportedBefore(DateTime dateTime)
        {
            return r => r.Reported < dateTime;
        }
        private static Expression<Func<Report, bool>> WhereReportedOn(DateTime dateTime)
        {
            return r => r.Reported == dateTime;
        }
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, expr2.Body), expr1.Parameters);
        }
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, expr2.Body), expr1.Parameters);
        }
    }
}
