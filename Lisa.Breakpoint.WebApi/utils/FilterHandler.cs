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
            Expression<Func<Report, bool>> filterPredicate = null;

            foreach (Filter filter in filters)
            {
                if (filter.Type == FilterTypes.Title)
                {
                    // Add onto the existing expression, or create a new expression if the expression is not created yet
                    Expression<Func<Report, bool>> expression = r => r.Title.StartsWith(filter.Value);
                    filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                }
                else if (filter.Type == FilterTypes.Version)
                {
                    if (filter.Value.Contains(","))
                    {
                        var versions = filter.Value.Split(',');

                        // Create a new lambda expression which will contain all versions to allow
                        Expression<Func<Report, bool>> versionpredicate = null;

                        // Add the versions to filter on to the lambda expression
                        foreach (var version in versions)
                        {
                            Expression<Func<Report, bool>> expression = r => r.Version == version;
                            versionpredicate = versionpredicate != null ? versionpredicate.Or(expression) : expression;
                        }

                        // Add the built version filter expression to the main expression
                        filterPredicate = filterPredicate != null ? filterPredicate.And(versionpredicate) : versionpredicate;
                    }
                    else
                    {
                        Expression<Func<Report, bool>> expression = r => r.Version == filter.Value;
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                }
                else if (filter.Type == FilterTypes.Priority)
                {
                    if (filter.Value.Contains(","))
                    {
                        var priorities = filter.Value.Split(',');

                        foreach (var priority in priorities)
                        {
                            if (!Priorities.List.Contains(priority))
                            {
                                ErrorHandler.Add(Priorities.InvalidValueError);
                                return reports;
                            }

                            Expression<Func<Report, bool>> expression = r => r.Priority == priority;
                            filterPredicate = filterPredicate != null ? filterPredicate.Or(expression) : expression;
                        }
                    }
                    else
                    {
                        if (!Priorities.List.Contains(filter.Value))
                        {
                            ErrorHandler.Add(Priorities.InvalidValueError);
                            return reports;
                        }

                        Expression<Func<Report, bool>> expression = r => r.Priority == filter.Value;
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                }
                else if (filter.Type == FilterTypes.Status)
                {
                    if (filter.Value.Contains(","))
                    {
                        var values = filter.Value.Split(',');
                        Expression<Func<Report, bool>> statuspredicate = null;
                        foreach (var value in values)
                        {
                            if (!Statuses.List.Contains(value))
                            {
                                ErrorHandler.Add(Statuses.InvalidValueError);
                                return reports;
                            }

                            Expression<Func<Report, bool>> expression = r => r.Status == value;
                            statuspredicate = statuspredicate != null ? statuspredicate.Or(expression) : expression;
                        }

                        filterPredicate = filterPredicate != null ? filterPredicate.Or(statuspredicate) : statuspredicate;
                    }
                    else
                    {
                        if (!Statuses.List.Contains(filter.Value))
                        {
                            ErrorHandler.Add(Statuses.InvalidValueError);
                            return reports;
                        }

                        Expression<Func<Report, bool>> expression = r => r.Status == filter.Value;
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                }
                else if (filter.Type == FilterTypes.AssignedTo)
                {
                    // Filter to all reports that are assigned to a group
                    if (filter.Value == "group")
                    {
                        Expression<Func<Report, bool>> expression = r => r.AssignedTo.Type == "group";
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                    // Filter to all reports that are assigned to a person
                    else if (filter.Value == "member")
                    {
                        Expression<Func<Report, bool>> expression = r => r.AssignedTo.Type == "person";
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                    // Use multiple filter values
                    else if (filter.Value.Contains(','))
                    {
                        var values = filter.Value.Split(',');
                        Expression<Func<Report, bool>> assigneepredicate = null;

                        foreach (var value in values)
                        {
                            Expression<Func<Report, bool>> expression = r => r.AssignedTo.Value == value;
                            assigneepredicate = assigneepredicate != null ? assigneepredicate.Or(expression) : expression;
                        }

                        filterPredicate = filterPredicate != null ? filterPredicate.And(assigneepredicate) : assigneepredicate;
                    }
                    else
                    {
                        Expression<Func<Report, bool>> expression = r => r.AssignedTo.Value == filter.Value;
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                }
                else if (filter.Type == FilterTypes.Reporter)
                {
                    Expression<Func<Report, bool>> expression = r => r.Reporter == filter.Value;
                    filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                }
                else if (filter.Type == FilterTypes.Reported)
                {
                    // Translate filter string to first and last datetime
                    var dateTimes = _CheckReported(filter.Value);

                    // Check datetime range validity
                    if (dateTimes == null)
                    {
                        ErrorHandler.Add(new Error(1207, new { field = "reported", value = filter.Value }));
                    }
                    else
                    {
                        // Add filter to predicate
                        Expression<Func<Report, bool>> expression = r => r.Reported.Date >= dateTimes[0] && r.Reported.Date < dateTimes[1];
                        filterPredicate = filterPredicate != null ? filterPredicate.And(expression) : expression;
                    }
                }
            }
            if (ErrorHandler.HasErrors)
            {
                return null;
            }
            // Apply filters
            reports = reports.Where(filterPredicate);

            return reports;
        }

        /// <summary>
        /// Converts the textual representation to a start and end date to filter between
        /// </summary>
        /// <param name="reported">The reported date filter value</param>
        /// <returns>
        /// A list of two dates, where the first is the start day to filter between,
        /// and the second is the last day to filter between.
        /// Returns null if an error occurred.
        /// </returns>
        private static IList<DateTime> _CheckReported(string reported)
        {
            reported = reported.ToLower();

            // Contains either a year to be filtering reports for, or the amount of days to take reports from.
            int date = 0;
            DateTime startFilterDay = DateTime.Today;
            DateTime endFilterDay = DateTime.Today.AddDays(1);

            //Filters out all the characters and white spaces
            string unparsedDate = Regex.Match(reported, @"\d+").Value;
            if (unparsedDate != "")
            {
                if (!int.TryParse(unparsedDate, out date))
                {
                    return null;
                }
            }

            DateTime minValue = new DateTime(1970, 1, 1);
            TimeSpan span = DateTime.Today - minValue;
            if (date > span.TotalDays)
            {
                return null;
            }

            if (reported == "today")
            {
                //Breaks the if so it won't give errors
            }
            else if (reported == "yesterday")
            {
                //Subtracts 1 day to get the date of yesterday
                startFilterDay = startFilterDay.AddDays(-1);
                endFilterDay = endFilterDay.AddDays(-1);
            }
            else if (Regex.IsMatch(reported, @"^\d+\W+days\W+ago$"))
            {
                //Subtracts the amount of days you entered on both so you get 1 day
                startFilterDay = startFilterDay.AddDays(-date);
                endFilterDay = endFilterDay.AddDays(-date);
            }
            else if (Regex.IsMatch(reported, @"^last\W+\d+\W+days$"))
            {
                //Sutracts the amount of days so you can filter between 25 days ago and tomorrow
                startFilterDay = startFilterDay.AddDays(-date);
            }
            else if (_monthNames.Any(reported.Contains) && date >= 1970 && date <= DateTime.Today.Year) //Gets the date of a certain year
            {
                //Replaces the numbers in the string so it won't give errors
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                if (_monthNames.Contains(reported))
                {
                    if (_monthNames.IndexOf(reported) + 1 > DateTime.Today.Month && date == DateTime.Today.Year)
                    {
                        return null;
                    }
                    else
                    {
                        startFilterDay = new DateTime(date, _monthNames.IndexOf(reported) + 1, 1);
                        endFilterDay = _calculateEndFilterDay(reported, startFilterDay);
                    }
                }
                else
                {
                    return null;
                }
            }
            else if (date >= 1970 && date <= DateTime.Today.Year)
            {
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                if (reported != string.Empty)
                {
                    return null;
                }
                else
                {
                    startFilterDay = new DateTime(date, 1, 1);
                    endFilterDay = startFilterDay.AddYears(1);
                }
            }
            else if (_monthNames.Contains(reported))
            {
                //If the month is below or equal to the current month, get the month of this year
                if ((_monthNames.IndexOf(reported) + 1) <= DateTime.Today.Month)
                {
                    startFilterDay = new DateTime(startFilterDay.Year, _monthNames.IndexOf(reported) + 1, 1);
                    endFilterDay = _calculateEndFilterDay(reported, startFilterDay);
                }
                else
                {
                    startFilterDay = new DateTime(startFilterDay.AddYears(-1).Year, _monthNames.IndexOf(reported) + 1, 1);
                    endFilterDay = _calculateEndFilterDay(reported, startFilterDay);
                }
            }
            else
            {
                return null;
            }
            IList<DateTime> dateTimes = new DateTime[2] { startFilterDay, endFilterDay };
            return dateTimes;
        }

        private static DateTime _calculateEndFilterDay(string reported, DateTime endFilterDay)
        {
            if (reported == _monthNames[11])
            {
                endFilterDay = new DateTime(endFilterDay.AddYears(1).Year, 1, 1);
            }
            else
            {
                endFilterDay = new DateTime(endFilterDay.Year, _monthNames.IndexOf(reported) + 2, 1);
            };

            return endFilterDay;
        }

        private static readonly IList<string> _monthNames = new string[12] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> originalExpression, Expression<Func<T, bool>> additionalExpression)
        {
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(originalExpression.Body, additionalExpression.Body), originalExpression.Parameters);
        }
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> originalExpression, Expression<Func<T, bool>> additionalExpression)
        {
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(originalExpression.Body, additionalExpression.Body), originalExpression.Parameters);
        }
    }

    public static class FilterTypes
    {
        public const string Title = "title";
        public const string Reporter = "reporter";
        public const string Reported = "reported";
        public const string Status = "status";
        public const string Priority = "priority";
        public const string Version = "version";
        public const string Assignee = "member";
        public const string AssignedGroup = "group";
        public const string AssignedTo = "assignedto";
    }
}
