using System;
using System.Collections.Generic;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public static class DefaultTimetablePeriods
    {
        public static List<TimePeriod> Create()
        {
            return new List<TimePeriod>
            {
                Fixed("Silence Hour", "07:15", "07:45", 1),
                Fixed("Assembly / Registration", "07:45", "08:00", 2),
                Teaching("Period 1", "08:00", "09:00", 3),
                Teaching("Period 2", "09:00", "10:00", 4),
                Fixed("First Break", "10:00", "10:30", 5),
                Teaching("Period 3", "10:30", "11:30", 6),
                Teaching("Period 4", "11:30", "12:30", 7),
                Fixed("Lunch Time", "12:30", "13:00", 8),
                Teaching("Period 5", "13:00", "13:45", 9),
                Fixed("Second Break", "13:45", "14:00", 10),
                Teaching("Period 6", "14:00", "14:45", 11),
                Fixed("Closing", "14:45", "15:00", 12)
            };
        }

        private static TimePeriod Teaching(string name, string start, string end, int order)
        {
            return Period(name, start, end, false, order);
        }

        private static TimePeriod Fixed(string name, string start, string end, int order)
        {
            return Period(name, start, end, true, order);
        }

        private static TimePeriod Period(string name, string start, string end, bool isBreak, int order)
        {
            return new TimePeriod
            {
                PeriodName = name,
                StartTime = TimeSpan.Parse(start),
                EndTime = TimeSpan.Parse(end),
                IsBreak = isBreak,
                SortOrder = order
            };
        }
    }
}
