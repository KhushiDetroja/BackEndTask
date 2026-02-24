using System;
using System.Collections.Generic;

namespace BackendTask.Models;

public partial class TicketStatusLog
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string OldStatus { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
