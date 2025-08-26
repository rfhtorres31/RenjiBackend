using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using renjibackend.Data;
using renjibackend.DTO;
using renjibackend.Models;
using System.Diagnostics;
using System.Linq;


namespace renjibackend.APIControllers
{
    [ApiController]
    [Route("api/actionplan")]
    public class ActionPlanController : ControllerBase
    {
        private readonly RenjiDbContext db;
        private Response response = new Response();

        public ActionPlanController(RenjiDbContext _db)
        {
            this.db = _db;
        }


        [HttpPost("post")]
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
                    Status = 10, // 10 - In Progress, 20 - Finished
                };

                db.ActionPlans.Add(newActionPlan);
                await db.SaveChangesAsync();

                var incidentReportRecord = await db.IncidentReports.Where(u => u.Id == actionPlan.IncidentReportID).FirstOrDefaultAsync();

                if (incidentReportRecord != null)
                {
                    incidentReportRecord.Status = 20; // Change status to In Progress
                    incidentReportRecord.ActionPlanId = newActionPlan.Id;
                    await db.SaveChangesAsync();
                }

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

        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetActionPlan()
        {

            var query = await db.IncidentReports
                              .Include(i => i.ActionPlan)
                              .Include(i => i.Department)
                              .Include(i => i.Accident)
                              .Where(u => u.ActionPlanId != null)
                              .Select(n => new
                              { 
                                ActionID = n.ActionPlan != null ? n.ActionPlan.Id : 0,
                                IncidentReportID = n.Id,
                                ActionDetail = n.ActionPlan != null ? n.ActionPlan.ActionDetail ?? "" : "",
                                IncidentReportTitle = n.Title,
                                Location = n.Location,
                                Priority = n.ActionPlan != null ? n.ActionPlan.Priority == 10 ? "Low" :
                                           n.ActionPlan.Priority == 20 ? "Moderate" :
                                           n.ActionPlan.Priority == 30 ? "High" : "" : "", 
                                ReportedDate = n.ReportedDate,
                                ActionType = n.ActionPlan != null ? n.ActionPlan.ActionType == 10 ? "Corrective" :
                                             n.ActionPlan.ActionType == 20 ? "Preventive" :
                                             n.ActionPlan.ActionType == 30 ? "Mitigation" :
                                             n.ActionPlan.ActionType == 40 ? "Containment" :
                                             n.ActionPlan.ActionType == 50 ? "Monitoring" :
                                             n.ActionPlan.ActionType == 60 ? "Administrative" : "" : "",
                                CreatedAt = n.ActionPlan != null ? n.ActionPlan.CreatedAt : (DateTime?)null,
                                MaintenanceTeam = n.ActionPlan != null ? db.MaintenanceTeams.Where(u => u.Id == n.ActionPlan.MaintenanceStaffId).Select(n => n.Name).FirstOrDefault() : "",
                                AccidentType = n.Accident.Name,
                                Status = n.ActionPlan!= null ? n.ActionPlan.Status == 10 ? "In Progress" :
                                                               n.ActionPlan.Status == 20 ? "Pending" :
                                                               n.ActionPlan.Status == 30 ? "Completed" :
                                                               n.ActionPlan.Status == 30 ? "Cancelled" : "" : "", 
                              }).ToListAsync();

            response.success = true;
            response.message = "Successfully Retrieved Records";
            response.details = new { data = query };

            return Ok(response);
        }

     }
}
