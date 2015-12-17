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

        public static IList<DateTime> CheckReported(string reported)
        {
            string errorReport = reported;
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
                    ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
                    filterDay = DateTime.MinValue.AddDays(1);
                }

            }

            DateTime d1 = new DateTime(1970, 1, 1);
            TimeSpan span = DateTime.Today - d1;
            if (date > span.TotalDays)
            {
                ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
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
            else if (Regex.IsMatch(reported, @"(\d+\W+days\W+ago)"))
            {
                //Subtracts the amount of days you entered on both so you get 1 day
                filterDay = filterDay.AddDays(-date);
                filterDayTwo = filterDayTwo.AddDays(-date);
            }
            else if (Regex.IsMatch(reported, @"last\W+\d+\W+days"))
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
                    if (_monthNames.IndexOf(reported) + 1 <= DateTime.Today.Month)
                    {
                        filterDay = new DateTime(date, _monthNames.IndexOf(reported) + 1, 1);
                        filterDayTwo = _calculateFilterDayTwo(reported, filterDay);
                    }
                    else
                    {
                        ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
                        filterDay = DateTime.MinValue.AddDays(1);
                    }
                }
                else
                {
                    ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
                    filterDay = DateTime.MinValue.AddDays(1);
                }
            }
            else if (date >= 1970 && date <= DateTime.Today.Year)
            {
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                if (reported != string.Empty)
                {
                    ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
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
                ErrorHandler.Add(new Error(1207, new { field = "reported", value = errorReport }));
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
}
