using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Auth;
using ToeicCoachAI.Model.DTOs.Dashboard;
using ToeicCoachAI.Model.Entities;
using ToeicCoachAI.Model.Enums;
using ToeicCoachAI.Repository.Interfaces;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.Service.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<StudentDashboardSummaryDto>> GetStudentDashboardAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<StudentDashboardSummaryDto>.Failure("Không tìm thấy thông tin học viên.");
            }

            var allTags = (await _unitOfWork.Repository<SkillTag>().GetAllAsync(cancellationToken)).ToList();
            var proficiencies = (await _unitOfWork.Repository<UserSkillProficiency>()
                .FindAsync(p => p.UserId == userId, cancellationToken)).ToList();
            var mistakes = (await _unitOfWork.Repository<UserMistake>()
                .FindAsync(m => m.UserId == userId && m.Status != MistakeStatus.MASTERED, cancellationToken)).ToList();
            var completedLessons = (await _unitOfWork.Repository<UserLessonProgress>()
                .FindAsync(l => l.UserId == userId && l.IsCompleted, cancellationToken)).Count();
            var totalLessons = (await _unitOfWork.Repository<Lesson>()
                .GetAllAsync(cancellationToken)).Count();
            var sessions = (await _unitOfWork.Repository<PracticeSession>()
                .FindAsync(s => s.UserId == userId && s.Status == PracticeSessionStatus.SUBMITTED, cancellationToken))
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();
            var reports = (await _unitOfWork.Repository<AiCompetencyReport>()
                .FindAsync(r => r.UserId == userId, cancellationToken))
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            var dueVocabs = (await _unitOfWork.Repository<UserVocabProgress>()
                .FindAsync(v => v.UserId == userId && v.NextReviewAt <= DateTimeOffset.UtcNow, cancellationToken))
                .Count();

            int estListening = 310;
            int estReading = 275;
            int estWriting = 140;

            int totalTargetLr = user.TargetScoreLr.HasValue && user.TargetScoreLr.Value > 0 ? user.TargetScoreLr.Value : 750;
            int targetListening = totalTargetLr / 2 + 25;
            int targetReading = totalTargetLr - targetListening;
            int targetWriting = user.TargetScoreW.HasValue && user.TargetScoreW.Value > 0 ? user.TargetScoreW.Value : 160;

            var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[^1] : user.FullName;
            var initials = nameParts.Length >= 2
                ? $"{char.ToUpperInvariant(nameParts[0][0])}{char.ToUpperInvariant(nameParts[^1][0])}"
                : (user.FullName.Length >= 2 ? user.FullName[..2].ToUpperInvariant() : "TC");

            var partMasteries = BuildPartMasteries(proficiencies, allTags);
            var weakSkills = BuildWeakSkillTags(proficiencies, allTags);

            var topWeak = weakSkills.FirstOrDefault();
            var priorityRec = new PriorityLessonRecommendationDto
            {
                TagCode = topWeak?.Code ?? "PART5_REL_CLAUSE",
                Title = topWeak?.Name ?? "Part 5: Mệnh đề quan hệ (Relative Clauses)",
                Reason = "Bạn thường nhầm đại từ quan hệ who, which, that và mệnh đề quan hệ rút gọn trong câu phức.",
                QuestionCount = user.Tier == UserTier.PAID ? 15 : 10,
                EstimatedMinutes = 15,
                DifficultyLabel = user.Tier == UserTier.PAID ? "Thích ứng sâu (PRO Adaptive AI)" : "Cơ bản (Free Starter)",
                CurrentMasteryPercent = topWeak?.MasteryPercent ?? 42,
                TargetMasteryPercent = 75
            };

            var latestReport = reports.FirstOrDefault();
            bool isPaid = user.Tier == UserTier.PAID;

            // Load active subscription & plan if present
            var activeSub = (await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.ACTIVE, cancellationToken))
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefault();

            SubscriptionPlan? currentPlan = null;
            int remainingDays = 0;
            if (activeSub != null)
            {
                currentPlan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(activeSub.PlanId, cancellationToken);
                if (activeSub.ExpiresAt.HasValue)
                {
                    remainingDays = Math.Max(0, (int)Math.Ceiling((activeSub.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays));
                }
            }
            else if (isPaid)
            {
                remainingDays = 30;
            }

            // Count today's AI usage from CreditTransactions
            var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
            var todayUsedCount = (await _unitOfWork.Repository<CreditTransaction>()
                .FindAsync(tx => tx.UserId == userId && tx.CreatedAt >= todayStart && tx.TxType == CreditTxType.COMMIT, cancellationToken))
                .Count();

            var summary = new StudentDashboardSummaryDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                FirstName = firstName,
                Initials = initials,
                Email = user.Email,
                CurrentTier = user.Tier,
                CreditBalance = user.CreditBalance,
                SubscriptionPlanName = currentPlan?.Name ?? (isPaid ? "Gói PRO Coaching 30 Ngày" : "Gói Miễn Phí (Free Starter)"),
                SubscriptionDaysRemaining = remainingDays,
                DailyAnalysisUsed = todayUsedCount,
                DailyAnalysisLimit = currentPlan?.DailyAnalysisLimit ?? (isPaid ? 50 : 3),
                SuggestedRoadmapSteps = new List<string>
                {
                    "Giai đoạn 1 (Tuần 1-2): Nắm vững Mệnh đề quan hệ & Rút gọn mệnh đề Part 5 (tăng độ chính xác từ 42% -> 75%).",
                    "Giai đoạn 2 (Tuần 3-5): Luyện kỹ thuật bắt từ khóa Paraphrasing trong Part 4 & Part 7 (tốc độ < 45 giây/câu).",
                    "Giai đoạn 3 (Tuần 6-8): Luyện viết Email thương mại Q6-7 & Essay Q8 với AI chấm chữa chi tiết từng câu."
                },
                TargetListening = targetListening,
                TargetReading = targetReading,
                TargetWriting = targetWriting,
                TargetDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(56)),
                EstimatedListening = estListening,
                EstimatedReading = estReading,
                EstimatedWriting = estWriting,
                ScoreDeltaFromLastWeek = 25,
                WeeklyStudyMinutes = sessions.Count > 0 ? sessions.Count * 35 : 260,
                StreakDays = 7,
                ActiveMistakeCount = mistakes.Count > 0 ? mistakes.Count : 6,
                DueVocabCount = dueVocabs > 0 ? dueVocabs : 10,
                CompletedLessonsCount = completedLessons > 0 ? completedLessons : 9,
                TotalLessonsCount = totalLessons > 0 ? Math.Max(totalLessons, 32) : 32,
                PriorityRecommendation = priorityRec,
                WeakSkillTags = weakSkills,
                PartMasteries = partMasteries,
                TodayTasks = new List<DailyTaskItemDto>
                {
                    new()
                    {
                        Title = "Luyện Smart Practice: Mệnh đề quan hệ",
                        DurationText = "15 phút",
                        ActionUrl = "/Dashboard/Practice",
                        IsCompleted = false,
                        BadgeText = "Miễn phí"
                    },
                    new()
                    {
                        Title = "Ôn 10 từ vựng SRS đến hạn hôm nay",
                        DurationText = "5 phút",
                        ActionUrl = "/Dashboard/Vocabulary",
                        IsCompleted = false,
                        BadgeText = "SRS"
                    },
                    new()
                    {
                        Title = "Luyện viết Email thương mại (AI chấm điểm)",
                        DurationText = "10 phút",
                        ActionUrl = "/Dashboard/Practice#writing-section",
                        IsCompleted = false,
                        BadgeText = "3 Credits"
                    }
                },
                LatestAiReport = latestReport != null
                    ? new CompetencyReportBriefDto
                    {
                        ReportId = latestReport.Id,
                        OverallSummary = latestReport.AiAnalysisDetail ?? "Báo cáo chẩn đoán năng lực AI đã cập nhật.",
                        CreatedAt = latestReport.CreatedAt,
                        TriggerSource = latestReport.TriggerSource.ToString()
                    }
                    : new CompetencyReportBriefDto
                    {
                        ReportId = Guid.Empty,
                        OverallSummary = "Kỹ năng Listening Part 1-3 ổn định (61%), cần tập trung lấp lỗ hổng Ngữ pháp Part 5 (Mệnh đề quan hệ - 42%) và cải thiện cấu trúc câu phức trong Writing Email.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                        TriggerSource = ReportTriggerSource.DIAGNOSTIC_TEST.ToString()
                    }
            };

            return ServiceResult<StudentDashboardSummaryDto>.Success(summary);
        }

        public async Task<ServiceResult<SubscriptionAndWalletPageDto>> GetSubscriptionAndWalletPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var summaryResult = await GetStudentDashboardAsync(userId, cancellationToken);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
            {
                return ServiceResult<SubscriptionAndWalletPageDto>.Failure(summaryResult.Message);
            }

            var plans = (await _unitOfWork.Repository<SubscriptionPlan>()
                .FindAsync(p => p.IsActive, cancellationToken))
                .OrderBy(p => p.PriceVnd)
                .Select(p => new SubscriptionPlanItemDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    PriceVnd = p.PriceVnd,
                    DurationDays = p.DurationDays,
                    GrantedCredits = p.GrantedCredits,
                    DailyAnalysisLimit = p.DailyAnalysisLimit,
                    IsPopular = p.Code == "PRO_MONTHLY"
                })
                .ToList();

            var transactions = (await _unitOfWork.Repository<CreditTransaction>()
                .FindAsync(tx => tx.UserId == userId, cancellationToken))
                .OrderByDescending(tx => tx.CreatedAt)
                .Take(15)
                .Select(tx => new CreditTransactionItemDto
                {
                    Id = tx.Id,
                    TxType = tx.TxType.ToString(),
                    FeatureName = tx.FeatureCode.HasValue
                        ? tx.FeatureCode.Value.ToString()
                        : (tx.TxType == CreditTxType.SUBSCRIPTION_GRANT ? "Cấp phát gói cước / Đăng ký" : "Giao dịch Ví AI Credits"),
                    Amount = tx.Amount,
                    BalanceAfter = tx.BalanceAfter,
                    Status = tx.Status.ToString(),
                    PaymentOrderCode = tx.PaymentOrderCode ?? "-",
                    CreatedAt = tx.CreatedAt
                })
                .ToList();

            return ServiceResult<SubscriptionAndWalletPageDto>.Success(new SubscriptionAndWalletPageDto
            {
                Summary = summaryResult.Data,
                Plans = plans,
                Transactions = transactions
            });
        }

        public async Task<ServiceResult<AuthUserResponseDto>> PurchaseSubscriptionPlanAsync(
            Guid userId,
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<AuthUserResponseDto>.Failure("Không tìm thấy tài khoản học viên.");
            }

            var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId, cancellationToken);
            if (plan == null || !plan.IsActive)
            {
                return ServiceResult<AuthUserResponseDto>.Failure("Gói cước không tồn tại hoặc đã ngừng cung cấp.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTimeOffset.UtcNow;
                var orderCode = $"ORD-{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

                // 1. Expire any existing active subscriptions
                var existingSubs = await _unitOfWork.Repository<UserSubscription>()
                    .FindAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.ACTIVE, cancellationToken);
                foreach (var oldSub in existingSubs)
                {
                    oldSub.Status = SubscriptionStatus.EXPIRED;
                    _unitOfWork.Repository<UserSubscription>().Update(oldSub);
                }

                // 2. Create new ACTIVE UserSubscription (UC-12)
                var newSub = new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlanId = plan.Id,
                    Status = SubscriptionStatus.ACTIVE,
                    PaymentOrderCode = orderCode,
                    StartedAt = now,
                    ExpiresAt = now.AddDays(plan.DurationDays)
                };
                await _unitOfWork.Repository<UserSubscription>().AddAsync(newSub, cancellationToken);

                // 3. Upgrade User Tier to PAID and grant AI Credits
                user.Tier = UserTier.PAID;
                user.CreditBalance += plan.GrantedCredits;
                user.UpdatedAt = now;
                _unitOfWork.Users.Update(user);

                // 4. Record CreditTransaction Ledger (UC-13)
                var creditTx = new CreditTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TxType = CreditTxType.SUBSCRIPTION_GRANT,
                    FeatureCode = null,
                    Amount = plan.GrantedCredits,
                    BalanceAfter = user.CreditBalance,
                    ReferenceId = newSub.Id,
                    PaymentOrderCode = orderCode,
                    Status = CreditTxStatus.COMPLETED,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.Repository<CreditTransaction>().AddAsync(creditTx, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ServiceResult<AuthUserResponseDto>.Success(new AuthUserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Tier = user.Tier,
                    CreditBalance = user.CreditBalance
                }, $"Đăng ký thành công {plan.Name}! Đã cộng +{plan.GrantedCredits} AI Credits vào ví (Mã GD: {orderCode}).");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ServiceResult<TestHubPageDto>> GetTestHubPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var summaryResult = await GetStudentDashboardAsync(userId, cancellationToken);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
            {
                return ServiceResult<TestHubPageDto>.Failure(summaryResult.Message);
            }

            var blueprints = (await _unitOfWork.Repository<MockTestBlueprint>()
                .FindAsync(b => b.IsActive, cancellationToken)).ToList();

            var sessions = (await _unitOfWork.Repository<PracticeSession>()
                .FindAsync(s => s.UserId == userId, cancellationToken))
                .OrderByDescending(s => s.StartedAt)
                .Take(5)
                .ToList();

            var blueprintDtos = blueprints.Select(b => new BlueprintItemDto
            {
                Id = b.Id,
                Title = b.Title,
                TestType = b.TestType.ToString(),
                DurationMinutes = b.DurationMinutes,
                QuestionCount = b.TestType == BlueprintTestType.FULL_MOCK_LR ? 200 : (b.TestType == BlueprintTestType.PAID_DIAGNOSTIC ? 100 : (b.TestType == BlueprintTestType.FULL_MOCK_WRITING ? 8 : 40)),
                DifficultyLabel = b.TestType == BlueprintTestType.GUEST_PLACEMENT ? "Thích ứng trình độ" : "Chuẩn ETS 2026",
                IsCompleted = sessions.Any(s => s.BlueprintId == b.Id && s.Status == PracticeSessionStatus.SUBMITTED)
            }).ToList();

            var recentSessionDtos = sessions.Select(s => new RecentSessionItemDto
            {
                SessionId = s.Id,
                Title = s.SessionMode == PracticeSessionMode.DIAGNOSTIC_TEST
                    ? "Kiểm tra chẩn đoán đầu vào"
                    : (s.SessionMode == PracticeSessionMode.MOCK_TEST ? "Thi thử TOEIC Mock Test" : "Luyện tập thông minh Part 5"),
                Mode = s.SessionMode.ToString(),
                TotalScore = 585,
                MaxScore = 990,
                TimeAgo = "Gần đây"
            }).ToList();

            if (recentSessionDtos.Count == 0)
            {
                recentSessionDtos.AddRange(new[]
                {
                    new RecentSessionItemDto { SessionId = Guid.Empty, Title = "Kiểm tra Smart Practice Part 5", Mode = "PART_PRACTICE", TotalScore = 72, MaxScore = 100, TimeAgo = "Hôm qua" },
                    new RecentSessionItemDto { SessionId = Guid.Empty, Title = "Bài kiểm tra chẩn đoán đầu vào", Mode = "DIAGNOSTIC_TEST", TotalScore = 585, MaxScore = 990, TimeAgo = "3 ngày trước" }
                });
            }

            return ServiceResult<TestHubPageDto>.Success(new TestHubPageDto
            {
                Summary = summaryResult.Data,
                Blueprints = blueprintDtos,
                RecentSessions = recentSessionDtos
            });
        }

        public async Task<ServiceResult<VocabHubPageDto>> GetVocabHubPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var summaryResult = await GetStudentDashboardAsync(userId, cancellationToken);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
            {
                return ServiceResult<VocabHubPageDto>.Failure(summaryResult.Message);
            }

            var allTags = (await _unitOfWork.Repository<SkillTag>().GetAllAsync(cancellationToken))
                .ToDictionary(t => t.Id, t => t.Name);
            var vocabs = (await _unitOfWork.Repository<Vocabulary>().GetAllAsync(cancellationToken)).ToList();
            var userVocabProgress = (await _unitOfWork.Repository<UserVocabProgress>()
                .FindAsync(vp => vp.UserId == userId, cancellationToken))
                .ToDictionary(vp => vp.VocabId, vp => (int)vp.SrsLevel);

            var vocabCards = new List<VocabCardItemDto>();
            int index = 0;
            int[] defaultLevels = { 2, 0, 4, 0, 4, 4, 2, 2, 0, 2, 0, 4 };

            foreach (var v in vocabs)
            {
                int level = userVocabProgress.TryGetValue(v.Id, out var userLvl)
                    ? userLvl
                    : defaultLevels[index % defaultLevels.Length];
                index++;

                vocabCards.Add(new VocabCardItemDto
                {
                    Id = v.Id,
                    Word = v.Word,
                    PartOfSpeech = string.IsNullOrWhiteSpace(v.Pos) ? "n." : v.Pos,
                    Phonetic = v.Phonetic ?? "",
                    MeaningVi = v.MeaningVi,
                    ExampleEn = v.ExampleEn ?? "",
                    ExampleVi = v.ExampleVi ?? "",
                    TopicName = allTags.TryGetValue(v.TopicTagId, out var tName) ? tName : "Văn phòng",
                    FamiliarityLevel = level
                });
            }

            return ServiceResult<VocabHubPageDto>.Success(new VocabHubPageDto
            {
                Summary = summaryResult.Data,
                MasteredCount = vocabCards.Count(c => c.FamiliarityLevel >= 4),
                LearningCount = vocabCards.Count(c => c.FamiliarityLevel >= 1 && c.FamiliarityLevel <= 3),
                NewOrDueCount = vocabCards.Count(c => c.FamiliarityLevel == 0),
                VocabItems = vocabCards,
                ActiveMistakes = new List<MistakeCardItemDto>
                {
                    new() { SkillTagName = "Part 5: Đại từ quan hệ (who / which / whose)", MistakeCount = 4, Status = "LEARNING", LastWrongAgo = "Hôm qua" },
                    new() { SkillTagName = "Part 4: Câu hỏi suy luận người nói (Inference)", MistakeCount = 3, Status = "LEARNING", LastWrongAgo = "2 ngày trước" },
                    new() { SkillTagName = "Part 7: Tìm thông tin đồng nghĩa (Paraphrasing)", MistakeCount = 2, Status = "LEARNING", LastWrongAgo = "4 ngày trước" }
                }
            });
        }

        private static List<SkillMasteryItemDto> BuildWeakSkillTags(
            List<UserSkillProficiency> proficiencies,
            List<SkillTag> allTags)
        {
            if (proficiencies.Count > 0)
            {
                var tagDict = allTags.ToDictionary(t => t.Id);
                return proficiencies
                    .OrderBy(p => p.ProficiencyScore)
                    .Take(3)
                    .Select(p =>
                    {
                        tagDict.TryGetValue(p.SkillTagId, out var tag);
                        return new SkillMasteryItemDto
                        {
                            SkillTagId = p.SkillTagId,
                            Code = tag?.Code ?? "SKILL",
                            Name = tag?.Name ?? "Kỹ năng trọng tâm",
                            Category = tag?.Category.ToString() ?? "GRAMMAR",
                            Subtitle = "Ưu tiên khắc phục lỗ hổng kiến thức",
                            MasteryPercent = Math.Round((double)p.ProficiencyScore, 0),
                            TotalAttempts = p.TotalAttempts
                        };
                    })
                    .ToList();
            }

            return new List<SkillMasteryItemDto>
            {
                new()
                {
                    Code = "PART5_REL_CLAUSE",
                    Name = "Part 5 – Ngữ pháp",
                    Category = "GRAMMAR",
                    Subtitle = "Mệnh đề quan hệ, thì hiện tại hoàn thành",
                    MasteryPercent = 42,
                    TotalAttempts = 24,
                    MistakeCount = 6
                },
                new()
                {
                    Code = "PART4_TALKS",
                    Name = "Part 4 – Bài nói ngắn",
                    Category = "PART_SKILL",
                    Subtitle = "Nghe thông tin chi tiết & suy luận",
                    MasteryPercent = 54,
                    TotalAttempts = 18,
                    MistakeCount = 4
                },
                new()
                {
                    Code = "PART7_READING_COMP",
                    Name = "Part 7 – Đọc hiểu",
                    Category = "PART_SKILL",
                    Subtitle = "Email, thông báo nội bộ, tin nhắn",
                    MasteryPercent = 55,
                    TotalAttempts = 30,
                    MistakeCount = 5
                }
            };
        }

        private static List<PartMasteryDto> BuildPartMasteries(
            List<UserSkillProficiency> proficiencies,
            List<SkillTag> allTags)
        {
            return new List<PartMasteryDto>
            {
                new() { PartCode = "PART1", PartTitle = "Part 1", PartSubtitle = "Mô tả tranh", SkillCategory = "LISTENING", IconClass = "bi-image", MasteryPercent = 65, TopicCount = 6, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "PART2", PartTitle = "Part 2", PartSubtitle = "Hỏi và đáp", SkillCategory = "LISTENING", IconClass = "bi-chat-dots", MasteryPercent = 58, TopicCount = 8, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "PART3", PartTitle = "Part 3", PartSubtitle = "Hội thoại", SkillCategory = "LISTENING", IconClass = "bi-people", MasteryPercent = 61, TopicCount = 10, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "PART4", PartTitle = "Part 4", PartSubtitle = "Bài nói ngắn", SkillCategory = "LISTENING", IconClass = "bi-volume-up", MasteryPercent = 54, TopicCount = 8, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "PART5", PartTitle = "Part 5", PartSubtitle = "Hoàn thành câu", SkillCategory = "READING", IconClass = "bi-text-left", MasteryPercent = 42, TopicCount = 12, IsPriority = true, CreditCost = 0 },
                new() { PartCode = "PART6", PartTitle = "Part 6", PartSubtitle = "Hoàn thành đoạn văn", SkillCategory = "READING", IconClass = "bi-file-earmark-text", MasteryPercent = 57, TopicCount = 7, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "PART7", PartTitle = "Part 7", PartSubtitle = "Đọc hiểu", SkillCategory = "READING", IconClass = "bi-book", MasteryPercent = 55, TopicCount = 10, IsPriority = false, CreditCost = 0 },
                new() { PartCode = "WRITING_Q1_5", PartTitle = "Writing Q1–5", PartSubtitle = "Viết câu theo tranh", SkillCategory = "WRITING", IconClass = "bi-pencil-square", MasteryPercent = 68, TopicCount = 5, IsPriority = false, CreditCost = 3 },
                new() { PartCode = "WRITING_Q6_7", PartTitle = "Writing Q6–7", PartSubtitle = "Phản hồi Email công việc", SkillCategory = "WRITING", IconClass = "bi-envelope-paper", MasteryPercent = 52, TopicCount = 6, IsPriority = true, CreditCost = 3 },
                new() { PartCode = "WRITING_Q8", PartTitle = "Writing Q8", PartSubtitle = "Viết luận Opinion Essay", SkillCategory = "WRITING", IconClass = "bi-journal-richtext", MasteryPercent = 48, TopicCount = 8, IsPriority = false, CreditCost = 3 }
            };
        }
    }
}
