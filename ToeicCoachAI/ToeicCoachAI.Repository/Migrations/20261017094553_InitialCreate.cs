using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToeicCoachAI.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_rules",
                columns: table => new
                {
                    FeatureCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditCost = table.Column<int>(type: "int", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_rules", x => x.FeatureCode);
                });

            migrationBuilder.CreateTable(
                name: "mock_test_blueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    QuestionGroupIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mock_test_blueprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinPoolThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    AiGenerationRules = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skill_tags_skill_tags_ParentId",
                        column: x => x.ParentId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PriceVnd = table.Column<long>(type: "bigint", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    GrantedCredits = table.Column<int>(type: "int", nullable: false),
                    DailyAnalysisLimit = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditBalance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TargetScoreLr = table.Column<int>(type: "int", nullable: true),
                    TargetScoreW = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.CheckConstraint("CK_users_credit_balance", "[CreditBalance] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ai_generation_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetSkillTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDifficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedCount = table.Column<int>(type: "int", nullable: false),
                    PublishedCount = table.Column<int>(type: "int", nullable: false),
                    QuarantinedCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_generation_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_generation_jobs_skill_tags_TargetSkillTagId",
                        column: x => x.TargetSkillTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<short>(type: "smallint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TheoryMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommonTraps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsFreePreview = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lessons_skill_tags_SkillTagId",
                        column: x => x.SkillTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vocabularies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phonetic = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Pos = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MeaningVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefinitionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExampleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExampleVi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WordFamily = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Collocations = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vocabularies_skill_tags_TopicTagId",
                        column: x => x.TopicTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TxType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeatureCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credit_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "practice_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlueprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FilterConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStateSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoreSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practice_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_practice_sessions_mock_test_blueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "mock_test_blueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_practice_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_skill_proficiency",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAttempts = table.Column<int>(type: "int", nullable: false),
                    CorrectAttempts = table.Column<int>(type: "int", nullable: false),
                    AccuracyRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AvgTimeSec = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    ProficiencyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_skill_proficiency", x => new { x.UserId, x.SkillTagId });
                    table.ForeignKey(
                        name: "FK_user_skill_proficiency_skill_tags_SkillTagId",
                        column: x => x.SkillTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_skill_proficiency_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenerationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SkillType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartNumber = table.Column<short>(type: "smallint", nullable: false),
                    DifficultyLevel = table.Column<short>(type: "smallint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassageContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranscriptData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MediaAssets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingPromptMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_groups_ai_generation_jobs_GenerationJobId",
                        column: x => x.GenerationJobId,
                        principalTable: "ai_generation_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_lesson_progress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_lesson_progress", x => new { x.UserId, x.LessonId });
                    table.ForeignKey(
                        name: "FK_user_lesson_progress_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_lesson_progress_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_vocab_progress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrsLevel = table.Column<short>(type: "smallint", nullable: false),
                    EaseFactor = table.Column<double>(type: "float", nullable: false, defaultValue: 2.5),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    NextReviewAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WrongCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_vocab_progress", x => new { x.UserId, x.VocabId });
                    table.ForeignKey(
                        name: "FK_user_vocab_progress_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_vocab_progress_vocabularies_VocabId",
                        column: x => x.VocabId,
                        principalTable: "vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_competency_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditTxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetricsSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedScores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiAnalysisDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedRoadmap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_competency_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_competency_reports_credit_transactions_CreditTxId",
                        column: x => x.CreditTxId,
                        principalTable: "credit_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_competency_reports_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guest_sessions",
                columns: table => new
                {
                    AnonymousToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TestSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedScoreLr = table.Column<int>(type: "int", nullable: true),
                    AiBasicFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_sessions", x => x.AnonymousToken);
                    table.ForeignKey(
                        name: "FK_guest_sessions_practice_sessions_TestSessionId",
                        column: x => x.TestSessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guest_sessions_users_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_validation_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidatorModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlindSolvedAnswers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAnswerMatched = table.Column<bool>(type: "bit", nullable: false),
                    EtsComplianceScore = table.Column<int>(type: "int", nullable: false),
                    DistractorQualityScore = table.Column<int>(type: "int", nullable: false),
                    FinalDecision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RejectionReasons = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_validation_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_validation_logs_question_groups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "question_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionOrder = table.Column<short>(type: "smallint", nullable: false),
                    QuestionStem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ExplanationGeneral = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionExplanations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingRubric = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsageStats = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_question_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "question_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_tags",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_tags", x => new { x.QuestionId, x.SkillTagId });
                    table.ForeignKey(
                        name: "FK_question_tags_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_tags_skill_tags_SkillTagId",
                        column: x => x.SkillTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    TimeSpentSec = table.Column<int>(type: "int", nullable: false),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_attempts_practice_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_attempts_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_attempts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_mistakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimarySkillTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WrongCount = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveCorrectCount = table.Column<int>(type: "int", nullable: false),
                    LastWrongOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextReviewAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_mistakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_mistakes_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_mistakes_skill_tags_PrimarySkillTagId",
                        column: x => x.PrimarySkillTagId,
                        principalTable: "skill_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_mistakes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "writing_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    TimeSpentSec = table.Column<int>(type: "int", nullable: false),
                    GradingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawScore = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    AiEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditTxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_writing_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_writing_submissions_credit_transactions_CreditTxId",
                        column: x => x.CreditTxId,
                        principalTable: "credit_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_writing_submissions_practice_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_writing_submissions_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_writing_submissions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contextual_chat_threads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contextual_chat_threads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contextual_chat_threads_practice_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contextual_chat_threads_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contextual_chat_threads_user_attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "user_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contextual_chat_threads_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contextual_chat_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditTxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contextual_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contextual_chat_messages_contextual_chat_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "contextual_chat_threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contextual_chat_messages_credit_transactions_CreditTxId",
                        column: x => x.CreditTxId,
                        principalTable: "credit_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_competency_reports_CreditTxId",
                table: "ai_competency_reports",
                column: "CreditTxId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_competency_reports_UserId",
                table: "ai_competency_reports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_generation_jobs_TargetSkillTagId",
                table: "ai_generation_jobs",
                column: "TargetSkillTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_validation_logs_QuestionGroupId",
                table: "ai_validation_logs",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_messages_CreditTxId",
                table: "contextual_chat_messages",
                column: "CreditTxId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_messages_ThreadId",
                table: "contextual_chat_messages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_threads_AttemptId",
                table: "contextual_chat_threads",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_threads_QuestionId",
                table: "contextual_chat_threads",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_threads_SessionId",
                table: "contextual_chat_threads",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_contextual_chat_threads_UserId",
                table: "contextual_chat_threads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_UserId_CreatedAt",
                table: "credit_transactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_sessions_ClaimedByUserId",
                table: "guest_sessions",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_guest_sessions_TestSessionId",
                table: "guest_sessions",
                column: "TestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_SkillTagId",
                table: "lessons",
                column: "SkillTagId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lessons_Slug",
                table: "lessons",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practice_sessions_BlueprintId",
                table: "practice_sessions",
                column: "BlueprintId");

            migrationBuilder.CreateIndex(
                name: "IX_practice_sessions_UserId",
                table: "practice_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_question_groups_GenerationJobId",
                table: "question_groups",
                column: "GenerationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_question_groups_Status_PartNumber_DifficultyLevel",
                table: "question_groups",
                columns: new[] { "Status", "PartNumber", "DifficultyLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_question_tags_SkillTagId",
                table: "question_tags",
                column: "SkillTagId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_GroupId",
                table: "questions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_tags_Code",
                table: "skill_tags",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_tags_ParentId",
                table: "skill_tags",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Code",
                table: "subscription_plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_attempts_QuestionId",
                table: "user_attempts",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_attempts_SessionId",
                table: "user_attempts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_attempts_UserId_QuestionId",
                table: "user_attempts",
                columns: new[] { "UserId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_lesson_progress_LessonId",
                table: "user_lesson_progress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_user_mistakes_PrimarySkillTagId",
                table: "user_mistakes",
                column: "PrimarySkillTagId");

            migrationBuilder.CreateIndex(
                name: "IX_user_mistakes_QuestionId",
                table: "user_mistakes",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_mistakes_UserId_QuestionId",
                table: "user_mistakes",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_mistakes_UserId_Status_PrimarySkillTagId_NextReviewAt",
                table: "user_mistakes",
                columns: new[] { "UserId", "Status", "PrimarySkillTagId", "NextReviewAt" });

            migrationBuilder.CreateIndex(
                name: "IX_user_skill_proficiency_SkillTagId",
                table: "user_skill_proficiency",
                column: "SkillTagId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_PaymentOrderCode",
                table: "user_subscriptions",
                column: "PaymentOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_PlanId",
                table: "user_subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_UserId",
                table: "user_subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_vocab_progress_VocabId",
                table: "user_vocab_progress",
                column: "VocabId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vocabularies_TopicTagId",
                table: "vocabularies",
                column: "TopicTagId");

            migrationBuilder.CreateIndex(
                name: "IX_writing_submissions_CreditTxId",
                table: "writing_submissions",
                column: "CreditTxId");

            migrationBuilder.CreateIndex(
                name: "IX_writing_submissions_QuestionId",
                table: "writing_submissions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_writing_submissions_SessionId",
                table: "writing_submissions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_writing_submissions_UserId",
                table: "writing_submissions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_competency_reports");

            migrationBuilder.DropTable(
                name: "ai_validation_logs");

            migrationBuilder.DropTable(
                name: "contextual_chat_messages");

            migrationBuilder.DropTable(
                name: "credit_rules");

            migrationBuilder.DropTable(
                name: "guest_sessions");

            migrationBuilder.DropTable(
                name: "question_tags");

            migrationBuilder.DropTable(
                name: "user_lesson_progress");

            migrationBuilder.DropTable(
                name: "user_mistakes");

            migrationBuilder.DropTable(
                name: "user_skill_proficiency");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "user_vocab_progress");

            migrationBuilder.DropTable(
                name: "writing_submissions");

            migrationBuilder.DropTable(
                name: "contextual_chat_threads");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "vocabularies");

            migrationBuilder.DropTable(
                name: "credit_transactions");

            migrationBuilder.DropTable(
                name: "user_attempts");

            migrationBuilder.DropTable(
                name: "practice_sessions");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "mock_test_blueprints");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "question_groups");

            migrationBuilder.DropTable(
                name: "ai_generation_jobs");

            migrationBuilder.DropTable(
                name: "skill_tags");
        }
    }
}
