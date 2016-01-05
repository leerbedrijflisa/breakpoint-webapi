using Lisa.Breakpoint.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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

        public static IList<DateTime> CheckReported(string reported)
        {
            reported = reported.ToLower();
            int date = 0;
            DateTime filterDay = DateTime.Today;
            DateTime filterDayTwo = DateTime.Today.AddDays(1);

            //Filters out all the characters and white spaces
            string unparsedDate = Regex.Match(reported, @"\d+").Value;
            if (unparsedDate != "")
            {
                if (!int.TryParse(unparsedDate, out date))
                {
                    filterDay = DateTime.MinValue.AddDays(1);
                }

            }

            DateTime d1 = new DateTime(1970, 1, 1);
            TimeSpan span = DateTime.Today - d1;
            if (date > span.TotalDays)
            {
                filterDay = DateTime.MinValue.AddDays(1);
                date = 0;
            }

            if (reported == "today")
            {
                //Breaks the if so it won't give errors
            }
            else if (reported == "yesterday")
            {
                //Subtracts 1 day to get the date of yesterday
                filterDay = filterDay.AddDays(-1);
                filterDayTwo = filterDayTwo.AddDays(-1);
            }
            else if (Regex.IsMatch(reported, @"^\d+\W+days\W+ago$"))
            {
                //Subtracts the amount of days you entered on both so you get 1 day
                filterDay = filterDay.AddDays(-date);
                filterDayTwo = filterDayTwo.AddDays(-date);
            }
            else if (Regex.IsMatch(reported, @"^last\W+\d+\W+days$"))
            {
                //Sutracts the amount of days so you can filter between 25 days ago and tomorrow
                filterDay = filterDay.AddDays(-date);
            }
            else if (_monthNames.Any(reported.Contains) && date >= 1970 && date <= DateTime.Today.Year) //Gets the date of a certain year
            {
                       //Replaces the numbers in the string so it won't give errors
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                if (_monthNames.Contains(reported))
                {
                    filterDay = new DateTime(date, _monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = _calculateFilterDayTwo(reported, filterDay);
                }
                else
                {
                    filterDay = DateTime.MinValue.AddDays(1);
                }
            }
            else if (date >= 1970 && date <= DateTime.Today.Year)
            {
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                if (reported != string.Empty)
                {
                    filterDay = DateTime.MinValue.AddDays(1);
                }
                else
                {
                    filterDay = new DateTime(date, 1, 1);
                    filterDayTwo = filterDay.AddYears(1);
                }
            }
            else if (_monthNames.Contains(reported))
            {
                //If the month is below or equal to the current month, get the month of this year
                if ((_monthNames.IndexOf(reported) + 1) <= DateTime.Today.Month)
                {
                    filterDay = new DateTime(filterDay.Year, _monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = _calculateFilterDayTwo(reported, filterDay);
                }
                else
                {
                    filterDay = new DateTime(filterDay.AddYears(-1).Year, _monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = _calculateFilterDayTwo(reported, filterDay);
                }
            }
            else
            {
                filterDay = DateTime.MinValue.AddDays(1);
            }
            IList<DateTime> dateTimes = new DateTime[2] { filterDay, filterDayTwo };
            return dateTimes;
        }

        private static DateTime _calculateFilterDayTwo(string reported, DateTime filterDayTwo)
        {
            if (reported == _monthNames[11])
            {
                filterDayTwo = new DateTime(filterDayTwo.AddYears(1).Year, 1, 1);
            }
            else
            {
                filterDayTwo = new DateTime(filterDayTwo.Year, _monthNames.IndexOf(reported) + 2, 1);
            };

            return filterDayTwo;
        }

        private static readonly IList<string> _monthNames = new string[12] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
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
