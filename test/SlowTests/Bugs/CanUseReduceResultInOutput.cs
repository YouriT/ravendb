using System.Linq;
using FastTests;
using Raven.Client.Indexes;
using Raven.Client.Indexing;
using Xunit;

namespace SlowTests.Bugs
{
    public class CanUseReduceResultInOutput : RavenNewTestBase
    {
        private class CalendarWeek
        {
            public Owner Owner { get; set; }
            public SalesAssignment[] SalesAssignments { get; set; }

            public decimal PendingCount { get; set; }
        }

        private class Owner
        {
            public string OwnerId { get; set; }
        }

        private class SalesAssignment
        {
            public string Status { get; set; }
        }

        private class MyIndex : AbstractIndexCreationTask<CalendarWeek, MyIndex.ReduceResult>
        {
            public class ReduceResult
            {
                public decimal CalendarsCount { get; set; }
                public string OwnerId { get; set; }
                public decimal SoldCount { get; set; }

                public decimal PendingCount { get; set; }
            }


            public MyIndex()
            {
                Map = calendarWeeks => from calendarWeek in calendarWeeks
                                        select new ReduceResult
                                        {
                                            OwnerId = calendarWeek.Owner.OwnerId,
                                            SoldCount = (decimal)calendarWeek.SalesAssignments.Where(x => x.Status == "Sold" || x.Status == "NotSold").Count(),
                                            CalendarsCount = 1m
                                        };

                Reduce = records => from record in records
                                    group record by record.OwnerId
                                        into g
                                        let count = g.Sum(x => x.CalendarsCount)
                                        let sold = g.Sum(x => x.SoldCount)
                                        select new ReduceResult
                                        {
                                            OwnerId = g.Key,
                                            SoldCount = sold,
                                            CalendarsCount = count
                                        };


                Stores.Add(x => x.OwnerId, FieldStorage.Yes);
                Stores.Add(x => x.CalendarsCount, FieldStorage.Yes);
            }
        }

        [Fact]
        public void CanCreateIndex()
        {
            using(var store = GetDocumentStore())
            {
                new MyIndex().Execute(store);
            }
        }
    }
}
