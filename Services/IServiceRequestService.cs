using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.ViewModels;
using Microsoft.AspNetCore.Http;

namespace HomeownersSubdivision.Services
{
    public interface IServiceRequestService
    {
        Task<List<ServiceRequest>> GetAllServiceRequestsAsync(
            ServiceRequestFilterViewModel? filter = null, 
            int page = 1, 
            int pageSize = 10);
            
        Task<int> GetServiceRequestsCountAsync(ServiceRequestFilterViewModel? filter = null);
        
        Task<ServiceRequest> GetServiceRequestByIdAsync(int id);
        
        Task<List<ServiceRequest>> GetServiceRequestsByHomeownerIdAsync(int homeownerId);
        
        Task<ServiceRequest> CreateServiceRequestAsync(
            ServiceRequest request, 
            IFormFile? image = null);
            
        Task<ServiceRequest> UpdateServiceRequestAsync(
            ServiceRequest request, 
            string? staffNotes = null, 
            IFormFile? newImage = null);
            
        Task<bool> DeleteServiceRequestAsync(int id);
        
        Task<bool> AssignServiceRequestAsync(int requestId, int staffId);
        
        Task<bool> ChangeServiceRequestStatusAsync(
            int requestId, 
            ServiceRequestStatus newStatus, 
            string? notes = null);
            
        Task<List<ServiceRequestUpdate>> GetServiceRequestUpdatesAsync(int requestId);
        
        Task SaveServiceRequestImageAsync(int requestId, IFormFile image);
    }
    
    public class ServiceRequestHistoryUpdate
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedById { get; set; }
        public required string Notes { get; set; }
        public ServiceRequestStatus OldStatus { get; set; }
        public ServiceRequestStatus NewStatus { get; set; }
    }
} 