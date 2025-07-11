﻿using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSAR.Models
{
    public class FormData
    {
        [Key]
        public int RequestId { get; set; }
        //change to CreatedBy
        [Required, ForeignKey(nameof(User))]
        public string UserId { get; set; }
        public Double RequestNumber { get; set; }
        public CaseStudy CaseStudy { get; set; } //nav
        public ICollection<History> Histories { get; set; }

        //[Required, ForeignKey(nameof(Manager))]
        //public string ManagerId { get; set; }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public string? Field1 { get; set; }
        public string? Field2 { get; set; }
        public string? Field3 { get; set; }
        public string? Depend { get; set; }
        public string? Field4 { get; set; }
        public string? Field5 { get; set; }
        public string? Field6 { get; set; }
        public string? RepeatLimit { get; set; }
        public string? Fees { get; set; }
        public string? Cities { get; set; }
        public string? TargetAudience { get; set; }
        public string? DepName { get; set; }
        public string? ExpectedOutput1 { get; set; }
        public string? ExpectedOutput2 { get; set; }
        public string? ApprovedTemplate { get; set; }
        public string? DetailedInfo { get; set; }
        public string? RequiredConditions { get; set; }

        public string? Workflow { get; set; }
        public string? UploadsRequired { get; set; }
        public string? Documents { get; set; }

        public string? Timeline { get; set; }
        public string? SystemNeeded { get; set; }
        public string? Cities2 { get; set; } // to avoid collision with existing Cities
        public string? DepartmentHeadName { get; set; }
        public string? AdditionalNotes { get; set; } //
        public string? FilePath { get; set; } // Store file name or path

        [NotMapped]

        public IFormFile? Attachment1 { get; set; }

        [NotMapped]
        public IFormFile? Attachment2 { get; set; }

        [NotMapped]
        public IFormFile? Attachment3 { get; set; }

        [NotMapped]
        public IFormFile? WorkflowFile { get; set; }

        [NotMapped]
        public IFormFile? UploadsRequiredFile { get; set; }

        [NotMapped]
        public IFormFile? DocumentsFile { get; set; }

        //notes
        public string? SectionNotes { get; set; }
        public string? DepartmentNotes { get; set; }

        public User User { get; set; }
        //public User Manager { get; set; }
        public RequestActions? RequestActions { get; set; }


        //Metadata

        public ICollection<AttachmentMetadata> Attachments { get; set; } = new List<AttachmentMetadata>();
        public ICollection<DescriptionEntry> Descriptions { get; set; } = new List<DescriptionEntry>();
    }
}
