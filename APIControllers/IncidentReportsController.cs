using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using renjibackend.Data;
using renjibackend.DTO;
using renjibackend.Models;
using renjibackend.Services;
using renjibackend.Utility;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;



namespace renjibackend.APIControllers
{
    [ApiController]
    [Route("api/reports")]
    public class IncidentReportsController: ControllerBase
    {

        private readonly RenjiDbContext db;
        private Response response = new Response();

        public IncidentReportsController(RenjiDbContext _db)
        {
            this.db = _db;
        }


        [HttpPost("post")]
        [Authorize]
        public async Task<IActionResult> PostNewReport([FromBody] NewReport report)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    response.success = false;
                    response.message = "Model State is Invalid";
                    response.details = ModelState;
                    return BadRequest(response);
                }

                int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                string accidentTypeName = await db.Accidents.Where(u => u.Id == report.AccidentTypeId).Select(n => n.Name).FirstOrDefaultAsync() ?? "";
                int departmentId = await db.Users.Where(u => u.Id == userID).Select(n => n.DepartmentId).FirstOrDefaultAsync();

                Debug.WriteLine(accidentTypeName);
                var newReport = new IncidentReport
                {   
                    Title = report.Title,
                    Description = report.Description,
                    Location = report.Location,
                    ReportedDate = DateTime.UtcNow,
                    ReportedBy = userID,
                    LastUpdated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = 10, 
                    AttachmentPath = "",
                    AccidentId = report.AccidentTypeId,
                    DepartmentId = departmentId,
                };
       

                db.IncidentReports.Add(newReport);
                await db.SaveChangesAsync();

                response.success = true;
                response.message = "New Reports Added Successfully";
                return Ok(response);

            }
            catch (Exception err)
            {
                response.success = false;
                response.message = "Internal Server Error";
                response.details = err.Message;

                return StatusCode(500, response);
            }
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetReports(int userID)
        {

            Debug.WriteLine(userID);
            if (userID == 0)
            {
                response.success = false;
                response.message = "No User ID";
                return BadRequest(response);
            }

            var reports = await db.IncidentReports.Select(n => new {
                ID = n.Id,
                Type = db.Accidents.Where(u => u.Id == n.AccidentId).Select(n => n.Name).FirstOrDefault(),
                Location = n.Location,
                ReportedDate = n.ReportedDate,
                Status = n.Status == 10 ? "Open" :
                         n.Status == 20 ? "In Progress" :
                         n.Status == 30 ? "Resolved" : ""
            }).ToListAsync();

            int noOfOpenReports = await db.IncidentReports.Where(u => u.Status == 10).CountAsync();
            int noOfInProgressReports = await db.IncidentReports.Where(u => u.Status == 20).CountAsync();
            int noOfResolvedReports = await db.IncidentReports.Where(u => u.Status == 30).CountAsync();

            Debug.WriteLine(noOfOpenReports);

            var reportCounts = new { open = noOfOpenReports, inProgress = noOfInProgressReports, resolved = noOfResolvedReports };

            response.success = true;
            response.message = "Success";
            response.details = new { data = reports, reportCounts };

            return Ok(response);
        }

        // Number of Accidents per Type (For Bar Chart and Pie Chart)
        [HttpGet("summaryreports-1")]
        public async Task<IActionResult> GetSummaryReports_1()
        {
            var query1 = from ir in db.IncidentReports
                         join a in db.Accidents
                         on ir.AccidentId equals a.Id
                         group ir by a.Name into g
                         select new
                          {
                              x = g.Key, // Accident Types
                              y = g.Count() // Total per Accident Types
                          };

            var result1 = await query1.OrderByDescending(x => x.y).ToListAsync();

            var totalCount = db.IncidentReports.Count();

            var query2 = from ir in db.IncidentReports
                         join a in db.Accidents
                         on ir.AccidentId equals a.Id
                         group ir by a.Name into g
                         select new
                         {
                             label = g.Key, // Accident Type
                             value = g.Count(),
                             percentage = ((double)g.Count() / totalCount * 100).ToString("0.0") + "%"
                         };

            var result2 = await query2.ToListAsync();

            response.success = true;
            response.message = "Success";
            response.details = new { data1 = result1, data2 = result2 };

            return Ok(response);
        }


        // Daily Incident Trends by Accident Type
        [HttpGet("summaryreports-2")]
        public IActionResult GetSummaryReports_2()
        {
            var queryResult = db.IncidentReports
                .Join(db.Accidents,
                      ir => ir.AccidentId,
                      a => a.Id,
                      (ir, a) => new { ir.ReportedDate, AccidentType = a.Name })
                .AsEnumerable() // move data to memory
                .GroupBy(x => new {
                    x.AccidentType,
                    ReportHour = new DateTime(x.ReportedDate.Year, x.ReportedDate.Month, x.ReportedDate.Day, x.ReportedDate.Hour, 0, 0)
                })
                .Select(g => new {
                    g.Key.AccidentType,
                    ReportHour = g.Key.ReportHour,
                    TotalIncidents = g.Count()
                })
                .OrderBy(x => x.ReportHour)
                .ThenBy(x => x.AccidentType)
                .ToList();


            var chartData = queryResult
                .GroupBy(x => x.AccidentType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { x = x.ReportHour, y = x.TotalIncidents }).ToList()
                );

            response.success = true;
            response.message = "Success";
            response.details = new { data = chartData };

            return Ok(response);
        }



        [HttpPost("actionplan")]
        public async Task<IActionResult> PostActionPlan([FromBody] NewActionPlan.ActionPlanDto actionPlan)
        {  

            try
            {
                if (!ModelState.IsValid)
                {
                    response.success = false;
                    response.message = "Model State is Invalid";
                    response.details = ModelState;
                    return BadRequest(response);
                }

                var newActionPlan = new ActionPlan
                {
                    IncidentReportId = actionPlan.IncidentReportID,
                    ActionDetail = actionPlan.Form.ActionDescription,
                    MaintenanceStaffId = actionPlan.Form.PersonInCharge,
                    DueDate = actionPlan.Form.TargetDate,
                    ActionType = actionPlan.Form.ActionTypes,
                    Priority = actionPlan.Form.Priority,
                    Status = 10, // 10 - In Progress, 20 - Resolved
                };

                db.ActionPlans.Add(newActionPlan);
                

                var incidentReportRecord = await db.IncidentReports.Where(u => u.Id == actionPlan.IncidentReportID).FirstOrDefaultAsync();
                
                if (incidentReportRecord != null)
                {
                    incidentReportRecord.Status = 20; // Change status to In Progress
                }

                await db.SaveChangesAsync();

                response.success = true;
                response.message = "Action Plan Added Successfully";

                return Ok(response);

            }
            catch (Exception err)
            {
                response.success = false;
                response.message = "Internal Server Error";
                response.details = err.Message;

                return StatusCode(500, response);
            }
            
        }
      }
}
