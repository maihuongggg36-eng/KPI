using KPI.Domain.Entities.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPI.Infrastructure.Data.Configurations;

public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("evaluations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SelfTotalScore).HasPrecision(5, 2);
        builder.Property(e => e.ManagerTotalScore).HasPrecision(5, 2);
        builder.Property(e => e.FinalScore).HasPrecision(5, 2);
        builder.Property(e => e.FinalClassification).HasMaxLength(50);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(e => e.AssignmentId).IsUnique();
        builder.HasIndex(e => e.EmployeeId).HasDatabaseName("idx_eval_emp");
        builder.HasIndex(e => e.PeriodId).HasDatabaseName("idx_eval_period");
        builder.HasIndex(e => e.Status).HasDatabaseName("idx_eval_status");

        builder.HasOne(e => e.Assignment)
            .WithOne()
            .HasForeignKey<Evaluation>(e => e.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Period)
            .WithMany()
            .HasForeignKey(e => e.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EvaluationDetailConfiguration : IEntityTypeConfiguration<EvaluationDetail>
{
    public void Configure(EntityTypeBuilder<EvaluationDetail> builder)
    {
        builder.ToTable("evaluation_details");
        builder.HasKey(ed => ed.Id);
        builder.Property(ed => ed.ActualValue).HasPrecision(15, 2);
        builder.Property(ed => ed.CompletionRate).HasPrecision(7, 2);
        builder.Property(ed => ed.SelfScore).HasPrecision(5, 2);
        builder.Property(ed => ed.ManagerScore).HasPrecision(5, 2);
        builder.Property(ed => ed.ScoreGap).HasPrecision(5, 2);

        builder.HasIndex(ed => ed.AssignmentDetailId).IsUnique();

        builder.HasOne(ed => ed.Evaluation)
            .WithMany(e => e.EvaluationDetails)
            .HasForeignKey(ed => ed.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ed => ed.AssignmentDetail)
            .WithOne()
            .HasForeignKey<EvaluationDetail>(ed => ed.AssignmentDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CivilServantClassificationConfiguration : IEntityTypeConfiguration<CivilServantClassification>
{
    public void Configure(EntityTypeBuilder<CivilServantClassification> builder)
    {
        builder.ToTable("civil_servant_classifications");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ProposedRank).HasMaxLength(50).IsRequired();
        builder.Property(c => c.FinalizedRank).HasMaxLength(50).IsRequired();

        builder.HasIndex(c => c.EvaluationId).IsUnique();

        builder.HasOne(c => c.Evaluation)
            .WithOne(e => e.CivilServantClassification)
            .HasForeignKey<CivilServantClassification>(c => c.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CivilServant)
            .WithMany()
            .HasForeignKey(c => c.CivilServantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ApprovedByUser)
            .WithMany()
            .HasForeignKey(c => c.ApprovedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PeerReviewConfiguration : IEntityTypeConfiguration<PeerReview>
{
    public void Configure(EntityTypeBuilder<PeerReview> builder)
    {
        builder.ToTable("peer_reviews");
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.BehaviorScore).HasPrecision(5, 2);

        builder.HasOne(pr => pr.Evaluation)
            .WithMany(e => e.PeerReviews)
            .HasForeignKey(pr => pr.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Reviewer)
            .WithMany()
            .HasForeignKey(pr => pr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EvaluationWorkflowConfiguration : IEntityTypeConfiguration<EvaluationWorkflow>
{
    public void Configure(EntityTypeBuilder<EvaluationWorkflow> builder)
    {
        builder.ToTable("evaluation_workflows");
        builder.HasKey(ew => ew.Id);
        builder.Property(ew => ew.CurrentStep).HasMaxLength(50).IsRequired();
        builder.Property(ew => ew.Status).HasMaxLength(30).IsRequired();

        builder.HasIndex(ew => ew.EvaluationId).IsUnique();

        builder.HasOne(ew => ew.Evaluation)
            .WithOne(e => e.Workflow)
            .HasForeignKey<EvaluationWorkflow>(ew => ew.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("workflow_steps");
        builder.HasKey(ws => ws.Id);
        builder.Property(ws => ws.StepName).HasMaxLength(100).IsRequired();
        builder.Property(ws => ws.ActionTaken).HasMaxLength(30);

        builder.HasOne(ws => ws.Workflow)
            .WithMany(w => w.WorkflowSteps)
            .HasForeignKey(ws => ws.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ws => ws.RequiredRole)
            .WithMany()
            .HasForeignKey(ws => ws.RequiredRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.AssignedActor)
            .WithMany()
            .HasForeignKey(ws => ws.AssignedActorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class WorkflowHistoryConfiguration : IEntityTypeConfiguration<WorkflowHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowHistory> builder)
    {
        builder.ToTable("workflow_history");
        builder.HasKey(wh => wh.Id);
        builder.Property(wh => wh.FromState).HasMaxLength(50).IsRequired();
        builder.Property(wh => wh.ToState).HasMaxLength(50).IsRequired();

        builder.HasOne(wh => wh.Evaluation)
            .WithMany(e => e.WorkflowHistories)
            .HasForeignKey(wh => wh.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wh => wh.Actor)
            .WithMany()
            .HasForeignKey(wh => wh.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EvaluationDisputeConfiguration : IEntityTypeConfiguration<EvaluationDispute>
{
    public void Configure(EntityTypeBuilder<EvaluationDispute> builder)
    {
        builder.ToTable("evaluation_disputes");
        builder.HasKey(ed => ed.Id);
        builder.Property(ed => ed.Reason).IsRequired();
        builder.Property(ed => ed.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(ed => ed.Evaluation)
            .WithMany(e => e.Disputes)
            .HasForeignKey(ed => ed.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ed => ed.Appellant)
            .WithMany()
            .HasForeignKey(ed => ed.AppellantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ed => ed.Resolver)
            .WithMany()
            .HasForeignKey(ed => ed.ResolverId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
