using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public enum TimeStepSource
    {
        Online,
        Theorical,
        Offline
    }

    public class TimeStep
    {
        public Step Step { get; set; }
        public DateTime Date { get; set; }
        public TimeStepSource Source { get; set; }

        public override string ToString()
        {
            return $"{Date} at {Step}";
        }
    }

    public class TimeTable
    {
        private const int stepsEnumerationLimit = 65536;

        public Route Route { get; set; }

        public TimeSpan?[,] WeekTable { get; set; }
        public TimeSpan?[,] FridayTable { get; set; }
        public TimeSpan?[,] SaturdayTable { get; set; }
        public TimeSpan?[,] SundayTable { get; set; }
        public TimeSpan?[,] HolidaysTable { get; set; }

        public IEnumerable<TimeStep> GetStepsFromStep(Step step, DateTime date, bool nextDays = false)
        {
            int stepIndex = Route.Steps.IndexOf(step);
            if (stepIndex == -1)
                throw new Exception("The specified step is not part of this route");

            // Determine reference day
            DayOfWeek referenceDay = date.AddHours(-3).DayOfWeek; // FIXME: Ugly trick
            DateTime referenceDate = date.DayOfWeek == referenceDay ? date.Date : date.Date.AddDays(-1);
            TimeSpan referenceDiff = date - referenceDate;

            // Find table and first index
            TimeSpan?[,] table = GetTableFromDay(referenceDay);
            int index = Enumerable.Range(0, table.GetLength(0)).TakeWhile(i => table[i, stepIndex] == null || table[i, stepIndex] < referenceDiff).Count();

            // Enumerate results
            for (int n = 0; n < stepsEnumerationLimit; n++)
            {
                if (index >= table.GetLength(0))
                {
                    if (!nextDays)
                        break;

                    referenceDate = referenceDate.AddDays(1);
                    table = GetTableFromDay(referenceDate.DayOfWeek);
                    index = 0;

                    continue;
                }

                TimeSpan? time = table[index, stepIndex];
                if (time != null)
                    yield return new TimeStep() { Step = step, Date = referenceDate.Add(time.Value), Source = TimeStepSource.Offline };

                index++;
            }
        }

        private TimeSpan?[,] GetTableFromDay(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Friday: return FridayTable;
                case DayOfWeek.Saturday: return SaturdayTable;
                case DayOfWeek.Sunday: return SundayTable;
                default: return WeekTable;
            }
        }
    }
}