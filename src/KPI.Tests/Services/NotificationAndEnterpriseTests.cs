using FluentAssertions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Entities.Reporting;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KPI.Tests.Services;

public class MockRealtimeNotifier : IRealtimeNotifier
{
    public List<NotificationDto> SentToUsers { get; } = new();
    public List<NotificationDto> Broadcasts { get; } = new();

    public Task SendNotificationToUserAsync(long userId, NotificationDto notification)
    {
        SentToUsers.Add(notification);
        return Task.CompletedTask;
    }

    public Task SendNotificationToAllAsync(NotificationDto notification)
    {
        Broadcasts.Add(notification);
        return Task.CompletedTask;
    }
}

public class NotificationAndEnterpriseTests
{
    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KpiDbContext(options);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnCorrectUnreadAndList()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockNotifier = new MockRealtimeNotifier();
        var emailService = new EmailService(context, new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);
        var notifService = new NotificationService(context, mockNotifier, emailService, NullLogger<NotificationService>.Instance);

        var notif1 = new Notification { Id = 1, RecipientId = 10, Title = "Thông báo 1", Content = "Nội dung 1", IsRead = false, CreatedAt = DateTime.UtcNow };
        var notif2 = new Notification { Id = 2, RecipientId = 10, Title = "Thông báo 2", Content = "Nội dung 2", IsRead = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var notifOther = new Notification { Id = 3, RecipientId = 99, Title = "Khác", Content = "Khác", IsRead = false };
        context.Notifications.AddRange(notif1, notif2, notifOther);
        await context.SaveChangesAsync();

        // Act
        var result = await notifService.GetUserNotificationsAsync(10, limit: 10);

        // Assert
        result.UnreadCount.Should().Be(1);
        result.RecentNotifications.Should().HaveCount(2);
        result.RecentNotifications.First().Id.Should().Be(1);
        result.RecentNotifications.First().TimeAgo.Should().Be("Vừa xong");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkReadAndSetTimestamp()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockNotifier = new MockRealtimeNotifier();
        var emailService = new EmailService(context, new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);
        var notifService = new NotificationService(context, mockNotifier, emailService, NullLogger<NotificationService>.Instance);

        var notif = new Notification { Id = 1, RecipientId = 10, Title = "Test", Content = "Content", IsRead = false };
        context.Notifications.Add(notif);
        await context.SaveChangesAsync();

        // Act
        var success = await notifService.MarkAsReadAsync(1, 10);

        // Assert
        success.Should().BeTrue();
        var updated = await context.Notifications.FindAsync(1L);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldUpdateAllUnreadItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockNotifier = new MockRealtimeNotifier();
        var emailService = new EmailService(context, new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);
        var notifService = new NotificationService(context, mockNotifier, emailService, NullLogger<NotificationService>.Instance);

        context.Notifications.AddRange(
            new Notification { Id = 1, RecipientId = 5, Title = "T1", IsRead = false },
            new Notification { Id = 2, RecipientId = 5, Title = "T2", IsRead = false },
            new Notification { Id = 3, RecipientId = 5, Title = "T3", IsRead = true }
        );
        await context.SaveChangesAsync();

        // Act
        var updatedCount = await notifService.MarkAllAsReadAsync(5);

        // Assert
        updatedCount.Should().Be(2);
        var allRead = await context.Notifications.Where(n => n.RecipientId == 5).AllAsync(n => n.IsRead);
        allRead.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAndSendAsync_ShouldSaveAndTriggerSignalR()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockNotifier = new MockRealtimeNotifier();
        var emailService = new EmailService(context, new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);
        var notifService = new NotificationService(context, mockNotifier, emailService, NullLogger<NotificationService>.Instance);

        var req = new CreateNotificationRequest
        {
            RecipientId = 12,
            Title = "Giao KPI",
            Content = "Bạn được giao KPI mới",
            Type = NotificationType.Info,
            TargetUrl = "/KpiPlanning",
            SendEmail = true,
            RecipientEmail = "user12@napa.edu.vn"
        };

        // Act
        var dto = await notifService.CreateAndSendAsync(req);

        // Assert
        dto.Should().NotBeNull();
        dto.Title.Should().Be("Giao KPI");
        mockNotifier.SentToUsers.Should().HaveCount(1);
        mockNotifier.SentToUsers.First().Title.Should().Be("Giao KPI");

        var dbNotif = await context.Notifications.FirstOrDefaultAsync(n => n.RecipientId == 12);
        dbNotif.Should().NotBeNull();

        var queuedEmail = await context.EmailQueues.FirstOrDefaultAsync(e => e.ToEmail == "user12@napa.edu.vn");
        queuedEmail.Should().NotBeNull();
        queuedEmail!.Subject.Should().Contain("Giao KPI");
    }

    [Fact]
    public async Task EmailService_EnqueueAndProcess_ShouldSendMockEmail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var inMemorySettings = new Dictionary<string, string?> {
            {"SmtpSettings:Host", "mock-smtp"},
            {"SmtpSettings:Port", "587"}
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var emailService = new EmailService(context, config, NullLogger<EmailService>.Instance);

        // Act 1: Enqueue
        var queueId = await emailService.EnqueueEmailAsync("director@napa.vn", "Tiêu đề test", "<p>Nội dung</p>");
        queueId.Should().BeGreaterThan(0);

        // Act 2: Process Pending
        var sentCount = await emailService.ProcessPendingEmailsAsync(batchSize: 10);
        sentCount.Should().Be(1);

        var sentRecord = await context.EmailQueues.FindAsync(queueId);
        sentRecord!.Status.Should().Be(EmailStatus.Sent);
        sentRecord.SentAt.Should().NotBeNull();

        // Act 3: Logs
        var logs = await emailService.GetEmailLogsAsync(10);
        logs.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProfileService_GetUserProfileAndChangePassword_ShouldWork()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var profileService = new ProfileService(context, NullLogger<ProfileService>.Instance);

        var user = new User
        {
            Id = 100,
            Username = "canbo_test",
            Email = "canbo@napa.edu.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123")
        };
        context.Users.Add(user);

        var dept = new Department { Id = 1, Name = "Phòng Tổ chức Cán bộ", TreePath = "1" };
        var pos = new Position { Id = 1, Name = "Chuyên viên chính" };
        context.Departments.Add(dept);
        context.Positions.Add(pos);

        var emp = new EmployeeProfile
        {
            Id = 1,
            UserId = 100,
            DepartmentId = 1,
            PositionId = 1,
            FullName = "Nguyễn Văn Thử Nghiệm",
            StaffCode = "CB-0100"
        };
        context.EmployeeProfiles.Add(emp);
        await context.SaveChangesAsync();

        // Act 1: Get Profile
        var profile = await profileService.GetUserProfileAsync(100);
        profile.FullName.Should().Be("Nguyễn Văn Thử Nghiệm");
        profile.DepartmentName.Should().Be("Phòng Tổ chức Cán bộ");

        // Act 2: Change Password Wrong
        var (failOk, failMsg) = await profileService.ChangePasswordAsync(100, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        });
        failOk.Should().BeFalse();

        // Act 3: Change Password Success
        var (ok, msg) = await profileService.ChangePasswordAsync(100, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass@123",
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        });
        ok.Should().BeTrue();

        var updatedUser = await context.Users.FindAsync(100L);
        BCrypt.Net.BCrypt.Verify("NewPass@123", updatedUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SearchService_QuickSearch_ShouldFindEmployeesAndKpis()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var searchService = new SearchService(context);

        var dept = new Department { Id = 1, Code = "KH", Name = "Khoa Hành chính học", TreePath = "1" };
        var pos = new Position { Id = 1, Name = "Giảng viên chính" };
        context.Departments.Add(dept);
        context.Positions.Add(pos);

        var emp = new EmployeeProfile
        {
            Id = 1,
            UserId = 1,
            DepartmentId = 1,
            PositionId = 1,
            FullName = "TS. Trần Văn Khoa",
            StaffCode = "GV-001"
        };
        context.EmployeeProfiles.Add(emp);

        var kpi = new KpiMasterItem
        {
            Id = 1,
            Code = "KPI_NCKH_01",
            Name = "Công bố bài báo khoa học Scopus",
            CategoryId = 1
        };
        context.KpiMasterItems.Add(kpi);
        await context.SaveChangesAsync();

        // Act 1: Search Employee
        var searchResult1 = await searchService.QuickSearchAsync("Trần Văn Khoa", 1, "DIRECTOR");
        searchResult1.Items.Should().Contain(i => i.Title == "TS. Trần Văn Khoa");

        // Act 2: Search KPI
        var searchResult2 = await searchService.QuickSearchAsync("Scopus", 1, "DIRECTOR");
        searchResult2.Items.Should().Contain(i => i.Title == "Công bố bài báo khoa học Scopus");

        // Act 3: Empty query -> Returns quick actions & system pages
        var defaultResult = await searchService.QuickSearchAsync("", 1, "DIRECTOR");
        defaultResult.Items.Should().NotBeEmpty();
    }
}
