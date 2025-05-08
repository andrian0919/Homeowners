using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.ViewModels
{
    public class ServiceRequestListViewModel
    {
        public List<ServiceRequestViewModel> ServiceRequests { get; set; } = new List<ServiceRequestViewModel>();
        public ServiceRequestFilterViewModel Filter { get; set; } = new ServiceRequestFilterViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }

    public class ServiceRequestViewModel
    {
        public int Id { get; set; }
        
        public required string Title { get; set; }
        
        public required string Description { get; set; }
        
        public ServiceRequestStatus Status { get; set; }
        
        public Priority Priority { get; set; }
        
        public ServiceRequestType RequestType { get; set; }
        
        public DateTime SubmissionDate { get; set; }
        
        public DateTime? CompletionDate { get; set; }
        
        public int HomeownerId { get; set; }
        
        public required string HomeownerName { get; set; }
        
        public int? AssignedToId { get; set; }
        
        public required string AssignedToName { get; set; }
        
        public DateTime? LastUpdated { get; set; }
        
        public string StatusDisplayName => Status.ToString();
        
        public string PriorityDisplayName => Priority.ToString();
        
        public string RequestTypeDisplayName => RequestType.ToString();
        
        public string? ImageUrl { get; set; }
    }

    public class CreateServiceRequestViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 5)]
        public required string Title { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public required string Description { get; set; }
        
        [Required]
        public ServiceRequestType RequestType { get; set; }
        
        [Required]
        public Priority Priority { get; set; }
        
        // For file upload
        public IFormFile? Image { get; set; }
    }

    public class EditServiceRequestViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 5)]
        public required string Title { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public required string Description { get; set; }
        
        [Required]
        public ServiceRequestType RequestType { get; set; }
        
        [Required]
        public Priority Priority { get; set; }
        
        [Required]
        public ServiceRequestStatus Status { get; set; }
        
        public int? AssignedToId { get; set; }
        
        public string? StaffNotes { get; set; }
        
        public List<SelectListItem>? StaffMembers { get; set; }
        
        public string? ImageUrl { get; set; }
        
        // For file upload
        public IFormFile? NewImage { get; set; }
    }

    public class ServiceRequestDetailsViewModel
    {
        public required ServiceRequestViewModel ServiceRequest { get; set; }
        public List<ServiceRequestUpdateViewModel> Updates { get; set; } = new List<ServiceRequestUpdateViewModel>();
    }

    public class ServiceRequestUpdateViewModel
    {
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedByName { get; set; }
        public required string Notes { get; set; }
        public ServiceRequestStatus OldStatus { get; set; }
        public ServiceRequestStatus NewStatus { get; set; }
    }

    public class ServiceRequestFilterViewModel
    {
        public ServiceRequestStatus? Status { get; set; }
        public ServiceRequestType? Type { get; set; }
        public Priority? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool MyRequestsOnly { get; set; }
    }
} 