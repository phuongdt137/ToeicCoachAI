using Microsoft.EntityFrameworkCore;
using ToeicCoachAI.Model.Entities;

namespace ToeicCoachAI.Repository.Data
{
    public class ToeicCoachAiDbContext : DbContext
    {
        public ToeicCoachAiDbContext(DbContextOptions<ToeicCoachAiDbContext> options)
            : base(options)
        {
        }

        // Module 1: Identity & Billing
        public DbSet<User> Users { get; set; }
        public DbSet<GuestSession> GuestSessions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<CreditRule> CreditRules { get; set; }

        // Module 2: Taxonomy & Curriculum
        public DbSet<SkillTag> SkillTags { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }
        public DbSet<Vocabulary> Vocabularies { get; set; }
        public DbSet<UserVocabProgress> UserVocabProgresses { get; set; }

        // Module 3: Question Bank & Dual-Agent Pipeline
        public DbSet<AiGenerationJob> AiGenerationJobs { get; set; }
        public DbSet<QuestionGroup> QuestionGroups { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionTag> QuestionTags { get; set; }
        public DbSet<AiValidationLog> AiValidationLogs { get; set; }

        // Module 4: Practice, Mock Test, Writing & Contextual Chat
        public DbSet<MockTestBlueprint> MockTestBlueprints { get; set; }
        public DbSet<PracticeSession> PracticeSessions { get; set; }
        public DbSet<UserAttempt> UserAttempts { get; set; }
        public DbSet<WritingSubmission> WritingSubmissions { get; set; }
        public DbSet<ContextualChatThread> ContextualChatThreads { get; set; }
        public DbSet<ContextualChatMessage> ContextualChatMessages { get; set; }

        // Module 5: Mistakes & Adaptive Analytics
        public DbSet<UserMistake> UserMistakes { get; set; }
        public DbSet<UserSkillProficiency> UserSkillProficiencies { get; set; }
        public DbSet<AiCompetencyReport> AiCompetencyReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================================
            // MODULE 1: IDENTITY & BILLING
            // =================================================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", t =>
                {
                    t.HasCheckConstraint("CK_users_credit_balance", "[CreditBalance] >= 0");
                });
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Tier).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.CreditBalance).HasDefaultValue(0);
            });

            modelBuilder.Entity<GuestSession>(entity =>
            {
                entity.ToTable("guest_sessions");
                entity.HasKey(e => e.AnonymousToken);
                entity.Property(e => e.AnonymousToken).HasMaxLength(128);

                entity.HasOne(e => e.TestSession)
                    .WithMany(s => s.GuestSessions)
                    .HasForeignKey(e => e.TestSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ClaimedByUser)
                    .WithMany(u => u.ClaimedGuestSessions)
                    .HasForeignKey(e => e.ClaimedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("subscription_plans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.ToTable("user_subscriptions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.PaymentOrderCode).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.PaymentOrderCode).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Plan)
                    .WithMany(p => p.UserSubscriptions)
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CreditTransaction>(entity =>
            {
                entity.ToTable("credit_transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TxType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.FeatureCode).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.PaymentOrderCode).HasMaxLength(100);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CreditTransactions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CreditRule>(entity =>
            {
                entity.ToTable("credit_rules");
                entity.HasKey(e => e.FeatureCode);
                entity.Property(e => e.FeatureCode).HasConversion<string>().HasMaxLength(50);
            });

            // =================================================================
            // MODULE 2: TAXONOMY & CURRICULUM
            // =================================================================
            modelBuilder.Entity<SkillTag>(entity =>
            {
                entity.ToTable("skill_tags");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.MinPoolThreshold).HasDefaultValue(50);

                entity.HasOne(e => e.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.ToTable("lessons");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SkillTagId).IsUnique();
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(300).IsRequired();
                entity.HasIndex(e => e.Slug).IsUnique();

                entity.HasOne(e => e.SkillTag)
                    .WithOne(t => t.Lesson)
                    .HasForeignKey<Lesson>(e => e.SkillTagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserLessonProgress>(entity =>
            {
                entity.ToTable("user_lesson_progress");
                entity.HasKey(e => new { e.UserId, e.LessonId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.LessonProgresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lesson)
                    .WithMany(l => l.UserProgresses)
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Vocabulary>(entity =>
            {
                entity.ToTable("vocabularies");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Word).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Phonetic).HasMaxLength(150);
                entity.Property(e => e.Pos).HasMaxLength(50).IsRequired();
                entity.Property(e => e.MeaningVi).HasMaxLength(500).IsRequired();
                entity.Property(e => e.AudioUrl).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);

                entity.HasOne(e => e.TopicTag)
                    .WithMany(t => t.Vocabularies)
                    .HasForeignKey(e => e.TopicTagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserVocabProgress>(entity =>
            {
                entity.ToTable("user_vocab_progress");
                entity.HasKey(e => new { e.UserId, e.VocabId });
                entity.Property(e => e.EaseFactor).HasDefaultValue(2.5);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.VocabProgresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Vocabulary)
                    .WithMany(v => v.UserProgresses)
                    .HasForeignKey(e => e.VocabId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =================================================================
            // MODULE 3: QUESTION BANK & DUAL-AGENT PIPELINE
            // =================================================================
            modelBuilder.Entity<AiGenerationJob>(entity =>
            {
                entity.ToTable("ai_generation_jobs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TriggerType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.TargetDifficulty).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(e => e.TargetSkillTag)
                    .WithMany(t => t.GenerationJobs)
                    .HasForeignKey(e => e.TargetSkillTagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<QuestionGroup>(entity =>
            {
                entity.ToTable("question_groups");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SkillType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.SourceType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

                // Fast Unseen Question Pool Selection Index
                entity.HasIndex(e => new { e.Status, e.PartNumber, e.DifficultyLevel });

                entity.HasOne(e => e.GenerationJob)
                    .WithMany(j => j.QuestionGroups)
                    .HasForeignKey(e => e.GenerationJobId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("questions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CorrectOption).HasMaxLength(1);

                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Questions)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionTag>(entity =>
            {
                entity.ToTable("question_tags");
                entity.HasKey(e => new { e.QuestionId, e.SkillTagId });

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.QuestionTags)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SkillTag)
                    .WithMany(t => t.QuestionTags)
                    .HasForeignKey(e => e.SkillTagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AiValidationLog>(entity =>
            {
                entity.ToTable("ai_validation_logs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ValidatorModel).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FinalDecision).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(e => e.QuestionGroup)
                    .WithMany(g => g.ValidationLogs)
                    .HasForeignKey(e => e.QuestionGroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =================================================================
            // MODULE 4: PRACTICE, MOCK TEST, WRITING & CONTEXTUAL CHAT
            // =================================================================
            modelBuilder.Entity<MockTestBlueprint>(entity =>
            {
                entity.ToTable("mock_test_blueprints");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.TestType).HasConversion<string>().HasMaxLength(50);
            });

            modelBuilder.Entity<PracticeSession>(entity =>
            {
                entity.ToTable("practice_sessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionMode).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PracticeSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Blueprint)
                    .WithMany(b => b.PracticeSessions)
                    .HasForeignKey(e => e.BlueprintId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserAttempt>(entity =>
            {
                entity.ToTable("user_attempts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SelectedOption).HasMaxLength(1);

                // Index for NOT EXISTS unseen question lookup
                entity.HasIndex(e => new { e.UserId, e.QuestionId });

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.Attempts)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Attempts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.Attempts)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WritingSubmission>(entity =>
            {
                entity.ToTable("writing_submissions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GradingStatus).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.RawScore).HasPrecision(3, 1);

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.WritingSubmissions)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.WritingSubmissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.WritingSubmissions)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreditTransaction)
                    .WithMany(t => t.WritingSubmissions)
                    .HasForeignKey(e => e.CreditTxId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContextualChatThread>(entity =>
            {
                entity.ToTable("contextual_chat_threads");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ChatThreads)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.ChatThreads)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.ChatThreads)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Attempt)
                    .WithMany(a => a.ChatThreads)
                    .HasForeignKey(e => e.AttemptId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContextualChatMessage>(entity =>
            {
                entity.ToTable("contextual_chat_messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SenderRole).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(e => e.Thread)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(e => e.ThreadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreditTransaction)
                    .WithMany(tx => tx.ChatMessages)
                    .HasForeignKey(e => e.CreditTxId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =================================================================
            // MODULE 5: MISTAKES & ADAPTIVE ANALYTICS
            // =================================================================
            modelBuilder.Entity<UserMistake>(entity =>
            {
                entity.ToTable("user_mistakes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LastWrongOption).HasMaxLength(1);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

                entity.HasIndex(e => new { e.UserId, e.QuestionId }).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.Status, e.PrimarySkillTagId, e.NextReviewAt });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Mistakes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Question)
                    .WithMany(q => q.Mistakes)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PrimarySkillTag)
                    .WithMany(t => t.PrimaryMistakes)
                    .HasForeignKey(e => e.PrimarySkillTagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserSkillProficiency>(entity =>
            {
                entity.ToTable("user_skill_proficiency");
                entity.HasKey(e => new { e.UserId, e.SkillTagId });
                entity.Property(e => e.AccuracyRate).HasPrecision(5, 2);
                entity.Property(e => e.AvgTimeSec).HasPrecision(6, 2);
                entity.Property(e => e.ProficiencyScore).HasPrecision(5, 2);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.SkillProficiencies)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SkillTag)
                    .WithMany(t => t.UserProficiencies)
                    .HasForeignKey(e => e.SkillTagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AiCompetencyReport>(entity =>
            {
                entity.ToTable("ai_competency_reports");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TriggerSource).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CompetencyReports)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreditTransaction)
                    .WithMany(tx => tx.CompetencyReports)
                    .HasForeignKey(e => e.CreditTxId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
