using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.DTOs.Dashboard
{
    public class StudentDashboardSummaryDto
    {
        // User Profile & Credit Info
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserTier CurrentTier { get; set; }
        public bool IsPaidTier => CurrentTier == UserTier.PAID;
        public int CreditBalance { get; set; }

        // Paid Subscription Info (UC-12, UC-13)
        public string SubscriptionPlanName { get; set; } = "Gói Miễn Phí (Free Starter)";
        public int SubscriptionDaysRemaining { get; set; }
        public int DailyAnalysisUsed { get; set; }
        public int DailyAnalysisLimit { get; set; }
        public List<string> SuggestedRoadmapSteps { get; set; } = new();

        // Target & Estimated Scores (Listening, Reading, Writing)
        public int TargetListening { get; set; }
        public int TargetReading { get; set; }
        public int TargetWriting { get; set; }
        public int TotalTargetLR => TargetListening + TargetReading;
        public DateOnly? TargetDeadline { get; set; }

        public int EstimatedListening { get; set; }
        public int EstimatedReading { get; set; }
        public int EstimatedWriting { get; set; }
        public int TotalEstimatedLR => EstimatedListening + EstimatedReading;
        public int ScoreDeltaFromLastWeek { get; set; }
        public int RemainingScoreGap => Math.Max(0, TotalTargetLR - TotalEstimatedLR);

        // Study & Mistake Stats
        public int WeeklyStudyMinutes { get; set; }
        public int StreakDays { get; set; }
        public int ActiveMistakeCount { get; set; }
        public int DueVocabCount { get; set; }
        public int CompletedLessonsCount { get; set; }
        public int TotalLessonsCount { get; set; }

        // AI Priority Recommendation
        public PriorityLessonRecommendationDto PriorityRecommendation { get; set; } = new();

        // Weak Skill Tags (Top 3-4 for Priority Box)
        public List<SkillMasteryItemDto> WeakSkillTags { get; set; } = new();

        // All Part Masteries (Listening Part 1-4, Reading Part 5-7, Writing)
        public List<PartMasteryDto> PartMasteries { get; set; } = new();

        // Weekly Checklist & Tasks
        public List<DailyTaskItemDto> TodayTasks { get; set; } = new();

        // Latest AI Competency Report (UC-11)
        public CompetencyReportBriefDto? LatestAiReport { get; set; }
    }

    public class PriorityLessonRecommendationDto
    {
        public Guid? SkillTagId { get; set; }
        public Guid? LessonId { get; set; }
        public string TagCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int QuestionCount { get; set; } = 10;
        public int EstimatedMinutes { get; set; } = 15;
        public string DifficultyLabel { get; set; } = "Trung bình";
        public double CurrentMasteryPercent { get; set; }
        public double TargetMasteryPercent { get; set; } = 75;
    }

    public class SkillMasteryItemDto
    {
        public Guid SkillTagId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public double MasteryPercent { get; set; }
        public int TotalAttempts { get; set; }
        public int MistakeCount { get; set; }
    }

    public class PartMasteryDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartTitle { get; set; } = string.Empty;
        public string PartSubtitle { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty; // LISTENING, READING, WRITING
        public string IconClass { get; set; } = string.Empty;
        public double MasteryPercent { get; set; }
        public int TopicCount { get; set; }
        public bool IsPriority { get; set; }
        public int CreditCost { get; set; }
    }

    public class DailyTaskItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string BadgeText { get; set; } = string.Empty;
    }

    public class CompetencyReportBriefDto
    {
        public Guid ReportId { get; set; }
        public string OverallSummary { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string TriggerSource { get; set; } = string.Empty;
    }

    public class TestHubPageDto
    {
        public StudentDashboardSummaryDto Summary { get; set; } = new();
        public List<BlueprintItemDto> Blueprints { get; set; } = new();
        public List<RecentSessionItemDto> RecentSessions { get; set; } = new();
    }

    public class BlueprintItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int QuestionCount { get; set; }
        public string DifficultyLabel { get; set; } = "Chuẩn ETS";
        public bool IsCompleted { get; set; }
    }

    public class RecentSessionItemDto
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class VocabHubPageDto
    {
        public StudentDashboardSummaryDto Summary { get; set; } = new();
        public int MasteredCount { get; set; }
        public int LearningCount { get; set; }
        public int NewOrDueCount { get; set; }
        public List<VocabCardItemDto> VocabItems { get; set; } = new();
        public List<MistakeCardItemDto> ActiveMistakes { get; set; } = new();
    }

    public class VocabCardItemDto
    {
        public Guid Id { get; set; }
        public string Word { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string MeaningVi { get; set; } = string.Empty;
        public string ExampleEn { get; set; } = string.Empty;
        public string ExampleVi { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public int FamiliarityLevel { get; set; } // 0: Mới, 1-3: Đang học, 4-5: Đã thuộc
        public string StatusBadge => FamiliarityLevel >= 4 ? "Đã thuộc" : (FamiliarityLevel > 0 ? "Đang học" : "Mới");
        public string StatusClass => FamiliarityLevel >= 4 ? "mastered" : (FamiliarityLevel > 0 ? "learning" : "new-word");
    }

    public class MistakeCardItemDto
    {
        public Guid MistakeId { get; set; }
        public string SkillTagName { get; set; } = string.Empty;
        public int MistakeCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastWrongAgo { get; set; } = string.Empty;
    }

    public class SubscriptionAndWalletPageDto
    {
        public StudentDashboardSummaryDto Summary { get; set; } = new();
        public List<SubscriptionPlanItemDto> Plans { get; set; } = new();
        public List<CreditTransactionItemDto> Transactions { get; set; } = new();
    }

    public class SubscriptionPlanItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long PriceVnd { get; set; }
        public int DurationDays { get; set; }
        public int GrantedCredits { get; set; }
        public int DailyAnalysisLimit { get; set; }
        public bool IsPopular { get; set; }
    }

    public class CreditTransactionItemDto
    {
        public Guid Id { get; set; }
        public string TxType { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int BalanceAfter { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentOrderCode { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
