using System;
using System.Collections.Generic;

namespace renjibackend.Models;

public partial class IncidentReport
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime ReportedDate { get; set; }

    public int ReportedBy { get; set; }

    public string Location { get; set; } = null!;

    public string AttachmentPath { get; set; } = null!;

    public DateTime LastUpdated { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? AccidentId { get; set; }

    public int Status { get; set; }

    public int? DepartmentId { get; set; }
}
