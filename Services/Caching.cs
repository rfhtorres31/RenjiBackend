using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using renjibackend.Data;
using System.Text.Json;



namespace renjibackend.Services
{
    public class Caching
    {
        public class BarChart1Dto
        {
            public string y { get; set; }  
            public int x { get; set; }   
        }

        public class PieChartDto
        {
            public string label { get; set; }  
            public int value { get; set; }
            public string percentage { get; set; }
        }

        public class ActionPlanDto
        {
            public string label { get; set; }
            public int value { get; set; }
            public string percentage { get; set; }
        }

        public class IncidentActionDto
        {
            public int ActionID { get; set; }
            public int IncidentReportID { get; set; }
            public string ActionDetail { get; set; }
            public string IncidentReportTitle { get; set; }
            public string Location { get; set; }
            public string Priority { get; set; }
            public DateTime? DueDate { get; set; }
            public string ActionType { get; set; }
            public string MaintenanceTeam { get; set; }
            public string AccidentType { get; set; }
            public string Status { get; set; }
        }


        private readonly RenjiDbContext db;
        private readonly IMemoryCache cache;

        public Caching(IMemoryCache _cache, RenjiDbContext _db)
        {
            this.db = _db;
            this.cache = _cache;
        }



        public async Task<List<BarChart1Dto>> GetSummaryReportsBarChart_1Cached()
        {

            if (cache.TryGetValue("barChart", out List<BarChart1Dto> cachedData))
            {
                return cachedData;
            }
  
            var query1 = from ir in db.IncidentReports
                         join a in db.Accidents
                         on ir.AccidentId equals a.Id
                         group ir by a.Name into g
                         select new BarChart1Dto
                         {
                             y = g.Key, // Accident Types
                             x = g.Count() // Total per Accident Types
                         };

            var result1 = await query1.OrderByDescending(x => x.y).ToListAsync();

            cache.Set("barChart", result1, TimeSpan.FromSeconds(30));

            return result1;
        }



        public async Task<List<PieChartDto>> GetSummaryReportsPieChart_2Cached()
        {

            if (cache.TryGetValue("pieChart", out List<PieChartDto> cachedData))
            {
                return cachedData;
            }

            var totalCount = db.IncidentReports.Count();

            var query2 = from ir in db.IncidentReports
                         join a in db.Accidents
                         on ir.AccidentId equals a.Id
                         group ir by a.Name into g
                         select new PieChartDto
                         {
                             label = g.Key,
                             value = g.Count(),
                             percentage = ((double)g.Count() / totalCount * 100).ToString("0.0") + "%"
                         };

            var result2 = await query2.ToListAsync();

            cache.Set("pieChart", result2, TimeSpan.FromSeconds(30));

            return result2;
        }

        public async Task<List<IncidentActionDto>> GetActionPlanCaching()
        {

            if (cache.TryGetValue("cachedData", out List<IncidentActionDto> cachedData))
            {
                return cachedData;
            }

            var query = await db.IncidentReports
                              .Include(i => i.ActionPlan)
                              .Include(i => i.Department)
                              .Include(i => i.Accident)
                              .Where(u => u.ActionPlanId != null && u.ActionPlan.Status != 30)
                              .Select(n => new IncidentActionDto
                              {
                                  ActionID = n.ActionPlan != null ? n.ActionPlan.Id : 0,
                                  IncidentReportID = n.Id,
                                  ActionDetail = n.ActionPlan != null ? n.ActionPlan.ActionDetail ?? "" : "",
                                  IncidentReportTitle = n.Title,
                                  Location = n.Location,
                                  Priority = n.ActionPlan != null ? n.ActionPlan.Priority == 10 ? "Low" :
                                           n.ActionPlan.Priority == 20 ? "Moderate" :
                                           n.ActionPlan.Priority == 30 ? "High" : "" : "",
                                  DueDate = n.ActionPlan.DueDate,
                                  ActionType = n.ActionPlan != null ? n.ActionPlan.ActionType == 10 ? "Corrective" :
                                             n.ActionPlan.ActionType == 20 ? "Preventive" :
                                             n.ActionPlan.ActionType == 30 ? "Mitigation" :
                                             n.ActionPlan.ActionType == 40 ? "Containment" :
                                             n.ActionPlan.ActionType == 50 ? "Monitoring" :
                                             n.ActionPlan.ActionType == 60 ? "Administrative" : "" : "",
                                  MaintenanceTeam = n.ActionPlan != null ? db.MaintenanceTeams.Where(u => u.Id == n.ActionPlan.MaintenanceStaffId).Select(n => n.Name).FirstOrDefault() : "",
                                  AccidentType = n.Accident.Name,
                                  Status = n.ActionPlan != null ? n.ActionPlan.Status == 10 ? "In Progress" :
                                                               n.ActionPlan.Status == 20 ? "Pending" :
                                                               n.ActionPlan.Status == 30 ? "Completed" :
                                                               n.ActionPlan.Status == 40 ? "Cancelled" : "" : "",
                              }).ToListAsync();


            cache.Set("cachedData", query, TimeSpan.FromSeconds(30));

            return query;
        }




    }
}
