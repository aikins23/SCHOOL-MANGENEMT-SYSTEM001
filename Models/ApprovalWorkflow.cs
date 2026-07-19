using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// A reusable engine for handling approvals (e.g., Performance Reports, Weekly Output).
    /// </summary>
    public class ApprovalWorkflow
    {
        public int WorkflowId { get; set; }

        /// <summary>
        /// Identifies the type of entity being approved (e.g., "PerformanceReport", "WeeklyOutputReport").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the specific report being approved.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Overall status of the workflow (e.g., "Draft", "Submitted", "Approved", "Rejected").
        /// </summary>
        public string CurrentStatus { get; set; } = "Draft";

        public Guid? SchoolId { get; set; }
        public Guid? SyncId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    }

    public class ApprovalStep
    {
        public int StepId { get; set; }
        public int WorkflowId { get; set; }
        public int StepOrder { get; set; }

        /// <summary>
        /// The role required to approve this step (e.g., "Headmaster", "Director").
        /// </summary>
        public string ApproverRole { get; set; } = string.Empty;

        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovalDate { get; set; }

        /// <summary>
        /// Action taken (e.g., "Approved", "Rejected", "Pending").
        /// </summary>
        public string Action { get; set; } = "Pending";

        public string Comments { get; set; } = string.Empty;
    }
}
