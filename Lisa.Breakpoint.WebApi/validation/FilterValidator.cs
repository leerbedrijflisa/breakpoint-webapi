using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    public class FilterValidator
    {
        public IEnumerable<Error> ValidateFilters(IEnumerable<Filter> filters)
        {
            var errors = new List<Error>();

            foreach (var filter in filters)
            {
                if (filter.Type == FilterTypes.Priority)
                {
                    var values = filter.Value.Split(',');

                    foreach (var value in values)
                    {

                        if (!Priorities.List.Contains(value))
                        {
                            errors.Add(Priorities.InvalidValueError);
                        }
                    }
                }
                else if (filter.Type == FilterTypes.Status)
                {
                    
                    var values = filter.Value.Split(',');

                    foreach (var value in values)
                    {
                        if (!Statuses.List.Contains(value))
                        {
                            errors.Add(Statuses.InvalidValueError);
                        }
                    }
                }
                else if (filter.Type == FilterTypes.Reported)
                {
                    if (FilterHandler.CheckReported(filter.Value) == null)
                    {
                        errors.Add(new Error(1207, new { field = "reported", value = filter.Value }));
                    }
                }
            }

            return errors;
        }
    }
}
