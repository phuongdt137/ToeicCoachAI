using System.ComponentModel.DataAnnotations;
using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.DTOs.Admin
{
    public class AdminDashboardSummaryDto
    {
        public int TotalPublishedGroups { get; set; }
        public int TotalQuarantinedGroups { get; set; }
        public int TotalDraftGroups { get; set; }
        public int TotalQuestions { get; set; }

        public int TotalSkillTags { get; set; }
        public int LowInventoryTagsCount { get; set; }
        public int TotalJobsCount { get; set; }
        public int RunningJobsCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int PaidUsersCount { get; set; }

        public List<SkillTagInventoryItemDto> SkillTagInventories { get; set; } = new();
        public List<RecentAiJobItemDto> RecentJobs { get; set; } = new();
    }

    public class SkillTagInventoryItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public SkillCategory Category { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int MinPoolThreshold { get; set; }
        public int PublishedQuestionCount { get; set; }
        public int QuarantinedQuestionCount { get; set; }
        public string? AiGenerationRules { get; set; }
        public bool HasLesson { get; set; }
        public int VocabCount { get; set; }

        public int DeficitCount => Math.Max(0, MinPoolThreshold - PublishedQuestionCount);
        public bool IsLowInventory => PublishedQuestionCount < MinPoolThreshold;
        public int FillPercentage => MinPoolThreshold > 0
            ? Math.Min(100, (int)Math.Round((double)PublishedQuestionCount / MinPoolThreshold * 100))
            : 100;
    }

    public class RecentAiJobItemDto
    {
        public Guid Id { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public string SkillTagName { get; set; } = string.Empty;
        public string SkillTagCode { get; set; } = string.Empty;
        public string TargetDifficulty { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int PublishedCount { get; set; }
        public int QuarantinedCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }

    public class SkillTagManagementPageDto
    {
        public List<SkillTagInventoryItemDto> SkillTags { get; set; } = new();
        public SaveSkillTagRequestDto FormModel { get; set; } = new();
    }

    public class SaveSkillTagRequestDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã kỹ năng (Code).")]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên nhãn kỹ năng.")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public SkillCategory Category { get; set; } = SkillCategory.PART_SKILL;

        public Guid? ParentId { get; set; }

        [Range(1, 5000, ErrorMessage = "Ngưỡng câu hỏi tối thiểu phải từ 1 đến 5000.")]
        public int MinPoolThreshold { get; set; } = 50;

        public string? AiGenerationRules { get; set; }
    }

    // =========================================================================
    // PHASE 2: AI PIPELINE (GENERATOR + CLOUD MEDIA + BLIND VALIDATOR) (UC-17, 18)
    // =========================================================================
    public class AiPipelinePageDto
    {
        public bool IsOpenAiConfigured { get; set; }
        public bool IsCloudinaryConfigured { get; set; }
        public string GeneratorModel { get; set; } = "gpt-4o-mini";
        public string ValidatorModel { get; set; } = "gpt-4o-mini";

        public List<SkillTagInventoryItemDto> EligibleSkillTags { get; set; } = new();
        public List<RecentAiJobItemDto> Jobs { get; set; } = new();
        public List<ValidationLogItemDto> RecentValidationLogs { get; set; } = new();
        public TriggerAiJobRequestDto FormModel { get; set; } = new();
    }

    public class TriggerAiJobRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn nhãn kỹ năng cần sinh đề.")]
        public Guid TargetSkillTagId { get; set; }

        [Required]
        public TargetDifficulty TargetDifficulty { get; set; } = TargetDifficulty.MEDIUM;

        [Range(1, 20, ErrorMessage = "Số lượng cụm câu hỏi mỗi lần chạy từ 1 đến 20.")]
        public int RequestedCount { get; set; } = 3;
    }

    public class ValidationLogItemDto
    {
        public Guid Id { get; set; }
        public Guid QuestionGroupId { get; set; }
        public short PartNumber { get; set; }
        public string SkillType { get; set; } = string.Empty;
        public string ValidatorModel { get; set; } = string.Empty;
        public string? BlindSolvedAnswers { get; set; }
        public bool IsAnswerMatched { get; set; }
        public int EtsComplianceScore { get; set; }
        public int DistractorQualityScore { get; set; }
        public string FinalDecision { get; set; } = string.Empty;
        public string? RejectionReasons { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    // =========================================================================
    // PHASE 3: QUARANTINE QUEUE & QUESTION BANK (UC-19, UC-20)
    // =========================================================================
    public class QuestionGroupAdminDto
    {
        public Guid GroupId { get; set; }
        public SkillType SkillType { get; set; }
        public short PartNumber { get; set; }
        public short DifficultyLevel { get; set; }
        public QuestionSourceType SourceType { get; set; }
        public QuestionGroupStatus Status { get; set; }
        public string? PassageContent { get; set; }
        public string? TranscriptData { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePrompt { get; set; }
        public string? AudioUrl { get; set; }
        public string? StorageProvider { get; set; }
        public string? WritingPromptMeta { get; set; }
        public string SkillTagCode { get; set; } = string.Empty;
        public string SkillTagName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        public List<QuestionItemAdminDto> Questions { get; set; } = new();
        public ValidationLogItemDto? LatestValidation { get; set; }
    }

    public class QuestionItemAdminDto
    {
        public Guid QuestionId { get; set; }
        public short QuestionOrder { get; set; }
        public string? QuestionStem { get; set; }
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public char? CorrectOption { get; set; }
        public string? ExplanationGeneral { get; set; }
        public string? WritingRubric { get; set; }
    }

    public class QuarantinePageDto
    {
        public List<QuestionGroupAdminDto> QuarantinedGroups { get; set; } = new();
    }

    public class ReviewQuarantineRequestDto
    {
        [Required]
        public Guid GroupId { get; set; }
        public Guid? QuestionId { get; set; }
        public string? PassageContent { get; set; }
        public string? TranscriptData { get; set; }
        public string? ImagePrompt { get; set; }
        public bool RegenerateCloudImage { get; set; }
        public string? QuestionStem { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public char? CorrectOption { get; set; }
        public string? ExplanationGeneral { get; set; }
    }

    public class QuestionBankPageDto
    {
        public List<QuestionGroupAdminDto> Groups { get; set; } = new();
        public List<SkillTagInventoryItemDto> SkillTags { get; set; } = new();
    }

    public class CreateCuratedQuestionRequestDto
    {
        [Required]
        public Guid SkillTagId { get; set; }
        public SkillType SkillType { get; set; } = SkillType.READING;
        public short PartNumber { get; set; } = 5;
        public short DifficultyLevel { get; set; } = 2;
        public string? PassageContent { get; set; }
        public string? TranscriptData { get; set; }
        public string? ImagePrompt { get; set; }
        public string QuestionStem { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public char CorrectOption { get; set; } = 'A';
        public string? ExplanationGeneral { get; set; }
    }

    // =========================================================================
    // PHASE 4: MOCK TEST BLUEPRINTS & SYSTEM SETTINGS (UC-21, UC-24)
    // =========================================================================
    public class BlueprintManagementPageDto
    {
        public List<BlueprintAdminItemDto> Blueprints { get; set; } = new();
        public int AvailablePublishedGroupsCount { get; set; }
    }

    public class BlueprintAdminItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public BlueprintTestType TestType { get; set; }
        public int DurationMinutes { get; set; }
        public int GroupCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveBlueprintRequestDto
    {
        public Guid? Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public BlueprintTestType TestType { get; set; } = BlueprintTestType.FULL_MOCK_LR;

        [Range(10, 240)]
        public int DurationMinutes { get; set; } = 120;

        public bool AutoFillPublishedGroups { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class AdminSettingsPageDto
    {
        public List<CreditRuleAdminDto> CreditRules { get; set; } = new();
        public List<SubscriptionPlanAdminDto> SubscriptionPlans { get; set; } = new();
    }

    public class CreditRuleAdminDto
    {
        public CreditFeatureCode FeatureCode { get; set; }
        public int CreditCost { get; set; }
        public int RateLimitPerMinute { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class SubscriptionPlanAdminDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long PriceVnd { get; set; }
        public int DurationDays { get; set; }
        public int GrantedCredits { get; set; }
        public int DailyAnalysisLimit { get; set; }
        public bool IsActive { get; set; }
    }
}
