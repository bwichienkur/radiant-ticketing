using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence;

public sealed class ReportingDbContext : EnhancementHubDbContext, IReportingDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
