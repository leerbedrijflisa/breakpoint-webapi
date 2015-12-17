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
            bool UseOr = false;
            // The AND clauses for the filtering. The default expression will always default to true (number is never empty)
            Expression<Func<Report, bool>> outerPredicate = r => r.Number != string.Empty;
            // The OR clauses for the filtering. The default expression will always default to false (number is never -1)
            Expression<Func<Report, bool>> innerPredicate = r => r.Number == "-1";

            foreach (Filter filter in filters)
            {
                if (filter.Type == FilterTypes.Title)
                {
                    if (string.IsNullOrWhiteSpace(filter.Value))
                    {
                        outerPredicate = outerPredicate.And(r => r.Title.StartsWith(filter.Value));
                    }
                }
                else if (filter.Type == FilterTypes.Version)
                {
                    if (filter.Value.Contains(","))
                    {
                        var versions = filter.Value.Split(',');
                        Expression<Func<Report, bool>> predicate = r => r.Number == "-1";
                        foreach (var version in versions)
                        {
                            predicate = predicate.Or(r => r.Version == version);
                        }
                        outerPredicate = outerPredicate.And(predicate);
                    }
                    else
                    {
                        outerPredicate = outerPredicate.And(r => r.Version == filter.Value);
                    }
                }
                else if (filter.Type == FilterTypes.Priority)
                {
                    if (filter.Value != "all")
                    {
                        // Parse filter value to corresponding enum value to filter with
                        var priority = (Priority)Enum.Parse(typeof(Priority), filter.Value);
                        outerPredicate = outerPredicate.And(r => r.Priority == priority);
                    }
                }
                else if (filter.Type == FilterTypes.Status)
                {
                    if (filter.Value != "all")
                    {
                        outerPredicate = outerPredicate.And(r => r.Status == filter.Value.Replace("%20", " "));
                    }
                }
                else if (filter.Type == FilterTypes.Group)
                {
                    if (filter.Value == "none")
                    {
                        outerPredicate = outerPredicate.And(r => r.AssignedTo.Type != "group");
                    }
                    else if (filter.Value == "all")
                    {
                        innerPredicate = innerPredicate.Or(r => r.AssignedTo.Type == "group");
                        UseOr = true;
                    }
                    else
                    {
                        innerPredicate = innerPredicate.Or(r => r.AssignedTo.Value == filter.Value && r.AssignedTo.Type == "group");
                        UseOr = true;
                    }
                }
                else if (filter.Type == FilterTypes.Member)
                {
                    if (filter.Value == "none")
                    {
                        outerPredicate = outerPredicate.And(r => r.AssignedTo.Type != "person");
                    }
                    else if (filter.Value == "all")
                    {
                        innerPredicate = innerPredicate.Or(r => r.AssignedTo.Type == "person");
                        UseOr = true;
                    }
                    else
                    {
                        innerPredicate = innerPredicate.Or(r => r.AssignedTo.Value == filter.Value && r.AssignedTo.Type == "person");
                        UseOr = true;
                    }
                }
                else if (filter.Type == FilterTypes.Reporter)
                {
                    if (filter.Value != "all")
                    {
                        outerPredicate = outerPredicate.And(r => r.Reporter == filter.Value);
                    }
                }
            }

            // Apply filters
            reports = UseOr ? reports.Where(outerPredicate.And(innerPredicate)) : reports.Where(outerPredicate);

            return reports;
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

    public static class FilterTypes
    {
        public const string Title = "title";
        public const string Reporter = "reporter";
        public const string Status = "status";
        public const string Priority = "priority";
        public const string Version = "version";
        public const string Member = "member";
        public const string Group = "group";
    }
}
