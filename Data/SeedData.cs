using System;
using System.Collections.Generic;
using System.Linq;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeownersSubdivision.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if the database is already seeded
                if (context.Users.Any())
                {
                    return; // Database has been seeded
                }

                // Add homeowners
                var homeowners = new List<Homeowner>
                {
                    new Homeowner
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        Address = "123 Main St",
                        Email = "john.doe@example.com",
                        Phone = "555-123-4567",
                        LotNumber = "A101",
                        BlockNumber = "B1",
                        JoinDate = DateTime.Parse("2023-01-15")
                    },
                    new Homeowner
                    {
                        FirstName = "Jane",
                        LastName = "Smith",
                        Address = "456 Oak Ave",
                        Email = "jane.smith@example.com",
                        Phone = "555-987-6543",
                        LotNumber = "A102",
                        BlockNumber = "B1",
                        JoinDate = DateTime.Parse("2023-02-20")
                    },
                    new Homeowner
                    {
                        FirstName = "Michael",
                        LastName = "Johnson",
                        Address = "789 Pine Rd",
                        Email = "michael.johnson@example.com",
                        Phone = "555-456-7890",
                        LotNumber = "A103",
                        BlockNumber = "B1",
                        JoinDate = DateTime.Parse("2023-03-10")
                    },
                    new Homeowner
                    {
                        FirstName = "Sarah",
                        LastName = "Williams",
                        Address = "101 Cedar Ln",
                        Email = "sarah.williams@example.com",
                        Phone = "555-789-0123",
                        LotNumber = "A201",
                        BlockNumber = "B2",
                        JoinDate = DateTime.Parse("2023-04-05")
                    },
                    new Homeowner
                    {
                        FirstName = "David",
                        LastName = "Brown",
                        Address = "202 Elm St",
                        Email = "david.brown@example.com",
                        Phone = "555-234-5678",
                        LotNumber = "A202",
                        BlockNumber = "B2",
                        JoinDate = DateTime.Parse("2023-05-12")
                    }
                };

                context.Homeowners.AddRange(homeowners);
                context.SaveChanges();

                // Add admin and staff users first
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@homeowners.com",
                    Password = "admin123", // In production, this should be hashed
                    FirstName = "System",
                    LastName = "Admin",
                    PhoneNumber = "555-111-0000",
                    Role = UserRole.Administrator,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2023-01-01")
                };

                var staffUser = new User
                {
                    Username = "staff",
                    Email = "staff@homeowners.com",
                    Password = "staff123", // In production, this should be hashed
                    FirstName = "Staff",
                    LastName = "Member",
                    PhoneNumber = "555-222-0000",
                    Role = UserRole.Staff,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2023-01-01")
                };

                context.Users.AddRange(adminUser, staffUser);
                context.SaveChanges();

                // Add homeowner users
                var users = new List<User>();
                for (int i = 0; i < homeowners.Count; i++)
                {
                    var homeowner = homeowners[i];
                    var user = new User
                    {
                        Username = homeowner.FirstName.ToLower() + homeowner.LastName.ToLower(),
                        Email = homeowner.Email,
                        Password = "password123", // In production, this should be hashed
                        FirstName = homeowner.FirstName,
                        LastName = homeowner.LastName,
                        PhoneNumber = homeowner.Phone,
                        Role = UserRole.Homeowner,
                        IsActive = true,
                        CreatedAt = homeowner.JoinDate,
                        HomeownerId = homeowner.Id
                    };
                    users.Add(user);
                }

                context.Users.AddRange(users);
                context.SaveChanges();

                // Add events
                var events = new List<Event>
                {
                    new Event
                    {
                        Title = "Annual Community Meeting",
                        Description = "Yearly meeting to discuss community updates and plans",
                        Location = "Community Center",
                        StartDate = DateTime.Parse("2023-12-15 18:00:00"),
                        EndDate = DateTime.Parse("2023-12-15 20:00:00"),
                        CreatedBy = adminUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new Event
                    {
                        Title = "Summer Barbecue",
                        Description = "Annual summer gathering for all homeowners",
                        Location = "Community Park",
                        StartDate = DateTime.Parse("2023-07-22 12:00:00"),
                        EndDate = DateTime.Parse("2023-07-22 16:00:00"),
                        CreatedBy = staffUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-60)
                    },
                    new Event
                    {
                        Title = "Holiday Decoration Contest",
                        Description = "Contest for the best decorated home for the holidays",
                        Location = "Throughout the subdivision",
                        StartDate = DateTime.Parse("2023-12-01 00:00:00"),
                        EndDate = DateTime.Parse("2023-12-24 23:59:59"),
                        CreatedBy = adminUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-90)
                    }
                };

                context.Events.AddRange(events);
                context.SaveChanges();

                // Add announcements
                var announcements = new List<Announcement>
                {
                    new Announcement
                    {
                        Title = "Road Maintenance",
                        Content = "Road maintenance will be performed on Oak St and Pine Rd from June 10-12. Please avoid parking on these streets during this time.",
                        IsPublished = true,
                        PublishDate = DateTime.Parse("2023-06-01"),
                        ExpireDate = DateTime.Parse("2023-06-13"),
                        CreatedBy = adminUser.Id
                    },
                    new Announcement
                    {
                        Title = "Pool Opening",
                        Content = "The community pool will open for the season on May 28. Hours will be 9AM-8PM daily.",
                        IsPublished = true,
                        PublishDate = DateTime.Parse("2023-05-15"),
                        ExpireDate = DateTime.Parse("2023-05-29"),
                        CreatedBy = staffUser.Id
                    },
                    new Announcement
                    {
                        Title = "New Community Website",
                        Content = "We are pleased to announce the launch of our new community website. Please register to access all features.",
                        IsPublished = true,
                        PublishDate = DateTime.Parse("2023-04-01"),
                        CreatedBy = adminUser.Id
                    }
                };

                context.Announcements.AddRange(announcements);
                context.SaveChanges();

                // Add facilities
                var facilities = new List<Facility>
                {
                    new Facility
                    {
                        Name = "Main Function Hall",
                        Type = FacilityType.FunctionHall,
                        Description = "Large indoor event space for community gatherings and private events",
                        MaxCapacity = 150,
                        HourlyRate = 50.00m,
                        OpeningTime = new TimeSpan(9, 0, 0), // 9:00 AM
                        ClosingTime = new TimeSpan(22, 0, 0), // 10:00 PM
                        IsActive = true
                    },
                    new Facility
                    {
                        Name = "Swimming Pool",
                        Type = FacilityType.SwimmingPool,
                        Description = "Outdoor swimming pool with lap lanes and recreational area",
                        MaxCapacity = 50,
                        HourlyRate = 25.00m,
                        OpeningTime = new TimeSpan(8, 0, 0), // 8:00 AM
                        ClosingTime = new TimeSpan(20, 0, 0), // 8:00 PM
                        IsActive = true
                    },
                    new Facility
                    {
                        Name = "Basketball Court",
                        Type = FacilityType.SportsCourt,
                        Description = "Full-sized basketball court with lighting for evening games",
                        MaxCapacity = 20,
                        HourlyRate = 15.00m,
                        OpeningTime = new TimeSpan(7, 0, 0), // 7:00 AM
                        ClosingTime = new TimeSpan(21, 0, 0), // 9:00 PM
                        IsActive = true
                    },
                    new Facility
                    {
                        Name = "Tennis Court",
                        Type = FacilityType.SportsCourt,
                        Description = "Professional tennis court with proper surface and net",
                        MaxCapacity = 4,
                        HourlyRate = 20.00m,
                        OpeningTime = new TimeSpan(7, 0, 0), // 7:00 AM
                        ClosingTime = new TimeSpan(21, 0, 0), // 9:00 PM
                        IsActive = true
                    }
                };

                context.Facilities.AddRange(facilities);
                context.SaveChanges();

                // Add maintenance requests
                var maintenanceRequests = new List<ServiceRequest>
                {
                    new ServiceRequest
                    {
                        HomeownerId = homeowners[0].Id,
                        Title = "Street Light Out",
                        Description = "The street light in front of my house (123 Main St) is not working",
                        Status = ServiceRequestStatus.Completed,
                        Priority = Priority.Medium,
                        SubmissionDate = DateTime.Parse("2023-05-10"),
                        CompletionDate = DateTime.Parse("2023-05-15"),
                        AssignedToId = staffUser.Id
                    },
                    new ServiceRequest
                    {
                        HomeownerId = homeowners[1].Id,
                        Title = "Pothole on Oak Ave",
                        Description = "There is a large pothole developing on Oak Ave near house #456",
                        Status = ServiceRequestStatus.InProgress,
                        Priority = Priority.High,
                        SubmissionDate = DateTime.Parse("2023-06-01"),
                        AssignedToId = staffUser.Id
                    },
                    new ServiceRequest
                    {
                        HomeownerId = homeowners[2].Id,
                        Title = "Playground Equipment Damage",
                        Description = "The slide at the community playground appears to be cracked",
                        Status = ServiceRequestStatus.New,
                        Priority = Priority.Medium,
                        SubmissionDate = DateTime.Parse("2023-06-05")
                    },
                    new ServiceRequest
                    {
                        HomeownerId = homeowners[3].Id,
                        Title = "Fallen Tree Branch",
                        Description = "A large tree branch has fallen on the walking path behind Cedar Ln",
                        Status = ServiceRequestStatus.Completed,
                        Priority = Priority.Low,
                        SubmissionDate = DateTime.Parse("2023-05-20"),
                        CompletionDate = DateTime.Parse("2023-05-21"),
                        AssignedToId = staffUser.Id
                    }
                };

                context.ServiceRequests.AddRange(maintenanceRequests);
                context.SaveChanges();
            }
        }
    }
} 