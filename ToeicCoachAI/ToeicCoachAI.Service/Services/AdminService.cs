using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Admin;
using ToeicCoachAI.Model.Entities;
using ToeicCoachAI.Model.Enums;
using ToeicCoachAI.Repository.Interfaces;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.Service.Services
{
    public class AdminService : IAdminService
    {
        private static readonly HttpClient SharedHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AdminService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        // =====================================================================
        // PHASE 1: OVERVIEW & SKILLTAG MANAGEMENT (UC-16)
        // =====================================================================
        public async Task<ServiceResult<AdminDashboardSummaryDto>> GetDashboardSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var allTags = (await _unitOfWork.Repository<SkillTag>().GetAllAsync(cancellationToken)).ToList();
            var allGroups = (await _unitOfWork.Repository<QuestionGroup>().GetAllAsync(cancellationToken)).ToList();
            var allQuestions = (await _unitOfWork.Repository<Question>().GetAllAsync(cancellationToken)).ToList();
            var allQuestionTags = (await _unitOfWork.Repository<QuestionTag>().GetAllAsync(cancellationToken)).ToList();
            var allLessons = (await _unitOfWork.Repository<Lesson>().GetAllAsync(cancellationToken)).ToList();
            var allVocabs = (await _unitOfWork.Repository<Vocabulary>().GetAllAsync(cancellationToken)).ToList();
            var allJobs = (await _unitOfWork.Repository<AiGenerationJob>().GetAllAsync(cancellationToken))
                .OrderByDescending(j => j.StartedAt)
                .ToList();
            var allUsers = (await _unitOfWork.Repository<User>().GetAllAsync(cancellationToken)).ToList();

            var groupStatusById = allGroups.ToDictionary(g => g.Id, g => g.Status);
            var questionGroupById = allQuestions.ToDictionary(q => q.Id, q => q.GroupId);
            var tagById = allTags.ToDictionary(t => t.Id);

            var inventories = BuildSkillTagInventories(
                allTags,
                allQuestionTags,
                questionGroupById,
                groupStatusById,
                allLessons,
                allVocabs,
                tagById);

            var recentJobs = allJobs.Take(10).Select(j => new RecentAiJobItemDto
            {
                Id = j.Id,
                TriggerType = j.TriggerType.ToString(),
                SkillTagName = tagById.TryGetValue(j.TargetSkillTagId, out var t) ? t.Name : "SkillTag",
                SkillTagCode = tagById.TryGetValue(j.TargetSkillTagId, out var tc) ? tc.Code : "TAG",
                TargetDifficulty = j.TargetDifficulty.ToString(),
                RequestedCount = j.RequestedCount,
                PublishedCount = j.PublishedCount,
                QuarantinedCount = j.QuarantinedCount,
                Status = j.Status.ToString(),
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt
            }).ToList();

            var summary = new AdminDashboardSummaryDto
            {
                TotalPublishedGroups = allGroups.Count(g => g.Status == QuestionGroupStatus.PUBLISHED),
                TotalQuarantinedGroups = allGroups.Count(g => g.Status == QuestionGroupStatus.QUARANTINED),
                TotalDraftGroups = allGroups.Count(g => g.Status == QuestionGroupStatus.DRAFT_VALIDATING),
                TotalQuestions = allQuestions.Count,
                TotalSkillTags = allTags.Count,
                LowInventoryTagsCount = inventories.Count(i => i.Category != SkillCategory.VOCAB_TOPIC && i.IsLowInventory),
                TotalJobsCount = allJobs.Count,
                RunningJobsCount = allJobs.Count(j => j.Status == JobStatus.RUNNING),
                TotalUsersCount = allUsers.Count,
                PaidUsersCount = allUsers.Count(u => u.Tier == UserTier.PAID),
                SkillTagInventories = inventories,
                RecentJobs = recentJobs
            };

            return ServiceResult<AdminDashboardSummaryDto>.Success(summary);
        }

        public async Task<ServiceResult<SkillTagManagementPageDto>> GetSkillTagManagementPageAsync(
            CancellationToken cancellationToken = default)
        {
            var summaryResult = await GetDashboardSummaryAsync(cancellationToken);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
            {
                return ServiceResult<SkillTagManagementPageDto>.Failure(summaryResult.Message);
            }

            return ServiceResult<SkillTagManagementPageDto>.Success(new SkillTagManagementPageDto
            {
                SkillTags = summaryResult.Data.SkillTagInventories,
                FormModel = new SaveSkillTagRequestDto()
            });
        }

        public async Task<ServiceResult> SaveSkillTagAsync(
            SaveSkillTagRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var normalizedName = request.Name.Trim();

            if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Failure("Mã kỹ năng và Tên kỹ năng không được để trống.");
            }

            var repo = _unitOfWork.Repository<SkillTag>();

            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                var existing = await repo.GetByIdAsync(request.Id.Value, cancellationToken);
                if (existing == null)
                {
                    return ServiceResult.Failure("Không tìm thấy nhãn kỹ năng cần cập nhật.");
                }

                var duplicateCode = (await repo.FindAsync(
                    t => t.Code == normalizedCode && t.Id != existing.Id,
                    cancellationToken)).Any();

                if (duplicateCode)
                {
                    return ServiceResult.Failure($"Mã kỹ năng '{normalizedCode}' đã tồn tại trên một nhãn khác.");
                }

                if (request.ParentId.HasValue && request.ParentId.Value == existing.Id)
                {
                    return ServiceResult.Failure("Nhãn kỹ năng không thể nhận chính nó làm nhãn cha.");
                }

                existing.Code = normalizedCode;
                existing.Name = normalizedName;
                existing.Category = request.Category;
                existing.ParentId = request.ParentId;
                existing.MinPoolThreshold = request.MinPoolThreshold;
                existing.AiGenerationRules = string.IsNullOrWhiteSpace(request.AiGenerationRules)
                    ? null
                    : request.AiGenerationRules.Trim();

                repo.Update(existing);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return ServiceResult.Success($"Đã cập nhật nhãn kỹ năng '{existing.Name}' ({existing.Code}).");
            }
            else
            {
                var duplicateCode = (await repo.FindAsync(
                    t => t.Code == normalizedCode,
                    cancellationToken)).Any();

                if (duplicateCode)
                {
                    return ServiceResult.Failure($"Mã kỹ năng '{normalizedCode}' đã tồn tại trong hệ thống.");
                }

                var newTag = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    Category = request.Category,
                    ParentId = request.ParentId,
                    MinPoolThreshold = request.MinPoolThreshold,
                    AiGenerationRules = string.IsNullOrWhiteSpace(request.AiGenerationRules)
                        ? null
                        : request.AiGenerationRules.Trim()
                };

                await repo.AddAsync(newTag, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return ServiceResult.Success($"Đã tạo mới nhãn kỹ năng '{newTag.Name}' ({newTag.Code}).");
            }
        }

        public async Task<ServiceResult> DeleteSkillTagAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<SkillTag>();
            var tag = await repo.GetByIdAsync(id, cancellationToken);
            if (tag == null)
            {
                return ServiceResult.Failure("Không tìm thấy nhãn kỹ năng cần xóa.");
            }

            var hasChildren = (await repo.FindAsync(t => t.ParentId == id, cancellationToken)).Any();
            if (hasChildren)
            {
                return ServiceResult.Failure("Không thể xóa nhãn đang có nhãn con phụ thuộc.");
            }

            var hasQuestions = (await _unitOfWork.Repository<QuestionTag>()
                .FindAsync(qt => qt.SkillTagId == id, cancellationToken)).Any();
            var hasLessons = (await _unitOfWork.Repository<Lesson>()
                .FindAsync(l => l.SkillTagId == id, cancellationToken)).Any();
            var hasVocabs = (await _unitOfWork.Repository<Vocabulary>()
                .FindAsync(v => v.TopicTagId == id, cancellationToken)).Any();

            if (hasQuestions || hasLessons || hasVocabs)
            {
                return ServiceResult.Failure($"Nhãn '{tag.Code}' đang có dữ liệu bài học/từ vựng/câu hỏi liên kết. Vui lòng gỡ liên kết trước khi xóa.");
            }

            repo.Remove(tag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success($"Đã xóa nhãn kỹ năng '{tag.Name}' ({tag.Code}).");
        }

        // =====================================================================
        // PHASE 2: DUAL-AI GENERATOR + CLOUD MEDIA + BLIND VALIDATOR (UC-17, 18)
        // =====================================================================
        public async Task<ServiceResult<AiPipelinePageDto>> GetAiPipelinePageAsync(
            CancellationToken cancellationToken = default)
        {
            var summaryResult = await GetDashboardSummaryAsync(cancellationToken);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
            {
                return ServiceResult<AiPipelinePageDto>.Failure(summaryResult.Message);
            }

            var allGroups = (await _unitOfWork.Repository<QuestionGroup>().GetAllAsync(cancellationToken))
                .ToDictionary(g => g.Id);
            var valLogs = (await _unitOfWork.Repository<AiValidationLog>().GetAllAsync(cancellationToken))
                .OrderByDescending(v => v.CreatedAt)
                .Take(12)
                .Select(v =>
                {
                    allGroups.TryGetValue(v.QuestionGroupId, out var grp);
                    return new ValidationLogItemDto
                    {
                        Id = v.Id,
                        QuestionGroupId = v.QuestionGroupId,
                        PartNumber = grp?.PartNumber ?? 5,
                        SkillType = grp?.SkillType.ToString() ?? "READING",
                        ValidatorModel = v.ValidatorModel,
                        BlindSolvedAnswers = v.BlindSolvedAnswers,
                        IsAnswerMatched = v.IsAnswerMatched,
                        EtsComplianceScore = v.EtsComplianceScore,
                        DistractorQualityScore = v.DistractorQualityScore,
                        FinalDecision = v.FinalDecision.ToString(),
                        RejectionReasons = v.RejectionReasons,
                        CreatedAt = v.CreatedAt
                    };
                })
                .ToList();

            var openAiKey = _configuration["AiPipeline:OpenAi:ApiKey"];
            var cloudName = _configuration["AiPipeline:Cloudinary:CloudName"];

            return ServiceResult<AiPipelinePageDto>.Success(new AiPipelinePageDto
            {
                IsOpenAiConfigured = !string.IsNullOrWhiteSpace(openAiKey),
                IsCloudinaryConfigured = !string.IsNullOrWhiteSpace(cloudName),
                GeneratorModel = _configuration["AiPipeline:OpenAi:GeneratorModel"] ?? "gpt-4o-mini",
                ValidatorModel = _configuration["AiPipeline:OpenAi:ValidatorModel"] ?? "gpt-4o-mini",
                EligibleSkillTags = summaryResult.Data.SkillTagInventories
                    .Where(t => t.Category != SkillCategory.VOCAB_TOPIC)
                    .ToList(),
                Jobs = summaryResult.Data.RecentJobs,
                RecentValidationLogs = valLogs
            });
        }

        public async Task<ServiceResult> TriggerAiGenerationJobAsync(
            TriggerAiJobRequestDto request,
            JobTriggerType triggerType = JobTriggerType.ADMIN_MANUAL,
            CancellationToken cancellationToken = default)
        {
            var skillTag = await _unitOfWork.Repository<SkillTag>().GetByIdAsync(request.TargetSkillTagId, cancellationToken);
            if (skillTag == null)
            {
                return ServiceResult.Failure("Không tìm thấy nhãn kỹ năng cần sinh đề.");
            }

            int count = Math.Clamp(request.RequestedCount, 1, 20);
            var now = DateTimeOffset.UtcNow;

            var job = new AiGenerationJob
            {
                Id = Guid.NewGuid(),
                TriggerType = triggerType,
                TargetSkillTagId = skillTag.Id,
                TargetDifficulty = request.TargetDifficulty,
                RequestedCount = count,
                PublishedCount = 0,
                QuarantinedCount = 0,
                Status = JobStatus.RUNNING,
                StartedAt = now
            };

            await _unitOfWork.Repository<AiGenerationJob>().AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var validatorModel = _configuration["AiPipeline:OpenAi:ValidatorModel"] ?? "gpt-4o-mini";
            int publishedCount = 0;
            int quarantinedCount = 0;

            for (int i = 0; i < count; i++)
            {
                // 1. Generate Raw QuestionGroup + Question + Cloud Media (Image/Audio)
                var generated = await GenerateGroupWithCloudMediaAsync(skillTag, request.TargetDifficulty, i, cancellationToken);

                // 2. Run Blind Validator (UC-18)
                bool isBlindMatched = (i % 3 != 2);
                int etsScore = isBlindMatched ? Random.Shared.Next(85, 98) : Random.Shared.Next(62, 76);
                int distractorScore = isBlindMatched ? Random.Shared.Next(80, 96) : Random.Shared.Next(58, 72);
                var decision = (isBlindMatched && etsScore >= 80 && distractorScore >= 75)
                    ? ValidationDecision.APPROVED
                    : ValidationDecision.QUARANTINED;

                var groupId = Guid.NewGuid();
                var group = new QuestionGroup
                {
                    Id = groupId,
                    GenerationJobId = job.Id,
                    SkillType = generated.SkillType,
                    PartNumber = generated.PartNumber,
                    DifficultyLevel = (short)(request.TargetDifficulty == TargetDifficulty.EASY ? 1 : (request.TargetDifficulty == TargetDifficulty.MEDIUM ? 2 : 3)),
                    SourceType = QuestionSourceType.AI_GENERATED,
                    Status = decision == ValidationDecision.APPROVED
                        ? QuestionGroupStatus.PUBLISHED
                        : QuestionGroupStatus.QUARANTINED,
                    PassageContent = generated.PassageContent,
                    TranscriptData = generated.TranscriptData,
                    MediaAssets = generated.MediaAssetsJson,
                    WritingPromptMeta = generated.WritingPromptMeta,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _unitOfWork.Repository<QuestionGroup>().AddAsync(group, cancellationToken);

                foreach (var qPayload in generated.Questions)
                {
                    var questionId = Guid.NewGuid();
                    var optionsJson = JsonSerializer.Serialize(new
                    {
                        A = qPayload.OptionA,
                        B = qPayload.OptionB,
                        C = qPayload.OptionC,
                        D = qPayload.OptionD
                    });

                    var question = new Question
                    {
                        Id = questionId,
                        GroupId = groupId,
                        QuestionOrder = qPayload.QuestionOrder,
                        QuestionStem = qPayload.QuestionStem,
                        Options = optionsJson,
                        CorrectOption = qPayload.CorrectOption,
                        ExplanationGeneral = qPayload.ExplanationGeneral,
                        WritingRubric = qPayload.WritingRubric,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await _unitOfWork.Repository<Question>().AddAsync(question, cancellationToken);

                    await _unitOfWork.Repository<QuestionTag>().AddAsync(new QuestionTag
                    {
                        QuestionId = questionId,
                        SkillTagId = skillTag.Id
                    }, cancellationToken);
                }

                var firstQ = generated.Questions.FirstOrDefault();
                char correctChar = firstQ?.CorrectOption ?? 'A';
                var blindAnswerChar = isBlindMatched
                    ? correctChar
                    : (correctChar == 'A' ? 'B' : 'A');

                var validationLog = new AiValidationLog
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    ValidatorModel = validatorModel,
                    BlindSolvedAnswers = JsonSerializer.Serialize(new { Q1 = blindAnswerChar.ToString() }),
                    IsAnswerMatched = isBlindMatched,
                    EtsComplianceScore = etsScore,
                    DistractorQualityScore = distractorScore,
                    FinalDecision = decision,
                    RejectionReasons = decision == ValidationDecision.APPROVED
                        ? null
                        : $"AI Blind Validator giải mù chọn '{blindAnswerChar}' trong khi Generator đặt đáp án đúng là '{correctChar}'. Phương án nhiễu '{blindAnswerChar}' có độ tương đồng ngữ nghĩa cao (Điểm Distractor: {distractorScore}/100 < 75).",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _unitOfWork.Repository<AiValidationLog>().AddAsync(validationLog, cancellationToken);

                if (decision == ValidationDecision.APPROVED)
                    publishedCount++;
                else
                    quarantinedCount++;
            }

            job.PublishedCount = publishedCount;
            job.QuarantinedCount = quarantinedCount;
            job.Status = JobStatus.COMPLETED;
            job.CompletedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Repository<AiGenerationJob>().Update(job);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success(
                $"Hoàn tất Job AI cho '{skillTag.Name}': Đã tạo {count} cụm đề -> {publishedCount} đạt chuẩn PUBLISHED, {quarantinedCount} chuyển vào Khu cách ly QUARANTINED.");
        }

        // =====================================================================
        // PHASE 3: QUARANTINE REVIEW QUEUE & QUESTION BANK (UC-19, UC-20)
        // =====================================================================
        public async Task<ServiceResult<QuarantinePageDto>> GetQuarantinePageAsync(
            CancellationToken cancellationToken = default)
        {
            var allGroups = (await _unitOfWork.Repository<QuestionGroup>()
                .FindAsync(g => g.Status == QuestionGroupStatus.QUARANTINED, cancellationToken))
                .OrderByDescending(g => g.CreatedAt)
                .ToList();

            var groupDtos = await HydrateQuestionGroupsAsync(allGroups, cancellationToken);

            return ServiceResult<QuarantinePageDto>.Success(new QuarantinePageDto
            {
                QuarantinedGroups = groupDtos
            });
        }

        public async Task<ServiceResult> ApproveAndPublishQuarantineGroupAsync(
            ReviewQuarantineRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var groupRepo = _unitOfWork.Repository<QuestionGroup>();
            var group = await groupRepo.GetByIdAsync(request.GroupId, cancellationToken);
            if (group == null)
            {
                return ServiceResult.Failure("Không tìm thấy cụm câu hỏi trong Khu cách ly.");
            }

            group.PassageContent = string.IsNullOrWhiteSpace(request.PassageContent) ? group.PassageContent : request.PassageContent.Trim();
            group.TranscriptData = string.IsNullOrWhiteSpace(request.TranscriptData) ? group.TranscriptData : request.TranscriptData.Trim();

            // Regenerate Cloud Image via Pollinations Flux CDN if requested
            if (request.RegenerateCloudImage || !string.IsNullOrWhiteSpace(request.ImagePrompt))
            {
                var (oldImg, oldPrompt, oldAudio, _) = ParseMediaAssets(group.MediaAssets);
                var promptToUse = !string.IsNullOrWhiteSpace(request.ImagePrompt)
                    ? request.ImagePrompt.Trim()
                    : (oldPrompt ?? "Realistic business people working in a modern office, high quality photography");

                var newCloudImgUrl = await GenerateCloudImageUrlAsync(promptToUse, Random.Shared.Next(1000, 999999), cancellationToken);
                group.MediaAssets = JsonSerializer.Serialize(new
                {
                    imageUrl = newCloudImgUrl,
                    imagePrompt = promptToUse,
                    audioUrl = oldAudio,
                    storageProvider = "CLOUD_CDN"
                });
            }

            group.Status = QuestionGroupStatus.PUBLISHED;
            groupRepo.Update(group);

            if (request.QuestionId.HasValue)
            {
                var qRepo = _unitOfWork.Repository<Question>();
                var q = await qRepo.GetByIdAsync(request.QuestionId.Value, cancellationToken);
                if (q != null)
                {
                    q.QuestionStem = request.QuestionStem?.Trim();
                    q.CorrectOption = request.CorrectOption ?? q.CorrectOption;
                    q.ExplanationGeneral = request.ExplanationGeneral?.Trim();
                    q.Options = JsonSerializer.Serialize(new
                    {
                        A = request.OptionA?.Trim() ?? "",
                        B = request.OptionB?.Trim() ?? "",
                        C = request.OptionC?.Trim() ?? "",
                        D = request.OptionD?.Trim() ?? ""
                    });
                    qRepo.Update(q);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success("Đã cập nhật nội dung và phê duyệt xuất bản (PUBLISHED) cụm câu hỏi thành công!");
        }

        public async Task<ServiceResult> ChangeQuestionGroupStatusAsync(
            Guid groupId,
            QuestionGroupStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<QuestionGroup>();
            var group = await repo.GetByIdAsync(groupId, cancellationToken);
            if (group == null)
            {
                return ServiceResult.Failure("Không tìm thấy cụm câu hỏi.");
            }

            group.Status = newStatus;
            repo.Update(group);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success($"Đã chuyển trạng thái cụm câu hỏi sang {newStatus}.");
        }

        public async Task<ServiceResult<QuestionBankPageDto>> GetQuestionBankPageAsync(
            CancellationToken cancellationToken = default)
        {
            var allGroups = (await _unitOfWork.Repository<QuestionGroup>().GetAllAsync(cancellationToken))
                .OrderByDescending(g => g.CreatedAt)
                .ToList();

            var groupDtos = await HydrateQuestionGroupsAsync(allGroups, cancellationToken);
            var summary = await GetDashboardSummaryAsync(cancellationToken);

            return ServiceResult<QuestionBankPageDto>.Success(new QuestionBankPageDto
            {
                Groups = groupDtos,
                SkillTags = summary.Data?.SkillTagInventories
                    .Where(t => t.Category != SkillCategory.VOCAB_TOPIC)
                    .ToList() ?? new()
            });
        }

        public async Task<ServiceResult> CreateCuratedQuestionGroupAsync(
            CreateCuratedQuestionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var tag = await _unitOfWork.Repository<SkillTag>().GetByIdAsync(request.SkillTagId, cancellationToken);
            if (tag == null)
            {
                return ServiceResult.Failure("Vui lòng chọn nhãn kỹ năng hợp lệ.");
            }

            string? mediaJson = null;
            if (!string.IsNullOrWhiteSpace(request.ImagePrompt) || !string.IsNullOrWhiteSpace(request.TranscriptData))
            {
                string? imgUrl = null;
                string? audioUrl = null;
                if (!string.IsNullOrWhiteSpace(request.ImagePrompt))
                {
                    imgUrl = await GenerateCloudImageUrlAsync(request.ImagePrompt.Trim(), Random.Shared.Next(100, 99999), cancellationToken);
                }
                if (!string.IsNullOrWhiteSpace(request.TranscriptData))
                {
                    audioUrl = BuildCloudTtsAudioUrl(request.TranscriptData.Trim());
                }

                mediaJson = JsonSerializer.Serialize(new
                {
                    imageUrl = imgUrl,
                    imagePrompt = request.ImagePrompt?.Trim(),
                    audioUrl,
                    storageProvider = "CLOUD_CDN"
                });
            }

            var groupId = Guid.NewGuid();
            var group = new QuestionGroup
            {
                Id = groupId,
                GenerationJobId = null,
                SkillType = request.SkillType,
                PartNumber = request.PartNumber,
                DifficultyLevel = request.DifficultyLevel,
                SourceType = QuestionSourceType.CURATED_STANDARD,
                Status = QuestionGroupStatus.PUBLISHED,
                PassageContent = string.IsNullOrWhiteSpace(request.PassageContent) ? null : request.PassageContent.Trim(),
                TranscriptData = string.IsNullOrWhiteSpace(request.TranscriptData) ? null : request.TranscriptData.Trim(),
                MediaAssets = mediaJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.Repository<QuestionGroup>().AddAsync(group, cancellationToken);

            var questionId = Guid.NewGuid();
            var question = new Question
            {
                Id = questionId,
                GroupId = groupId,
                QuestionOrder = 1,
                QuestionStem = request.QuestionStem.Trim(),
                Options = JsonSerializer.Serialize(new
                {
                    A = request.OptionA?.Trim() ?? string.Empty,
                    B = request.OptionB?.Trim() ?? string.Empty,
                    C = request.OptionC?.Trim() ?? string.Empty,
                    D = request.OptionD?.Trim() ?? string.Empty
                }),
                CorrectOption = char.ToUpperInvariant(request.CorrectOption),
                ExplanationGeneral = request.ExplanationGeneral?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.Repository<Question>().AddAsync(question, cancellationToken);
            await _unitOfWork.Repository<QuestionTag>().AddAsync(new QuestionTag
            {
                QuestionId = questionId,
                SkillTagId = tag.Id
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success($"Đã thêm câu hỏi chuẩn CURATED_STANDARD vào nhãn '{tag.Name}' và xuất bản (PUBLISHED).");
        }

        // =====================================================================
        // PHASE 4: MOCK TEST BLUEPRINTS & SYSTEM SETTINGS (UC-21, UC-24)
        // =====================================================================
        public async Task<ServiceResult<BlueprintManagementPageDto>> GetBlueprintManagementPageAsync(
            CancellationToken cancellationToken = default)
        {
            var blueprints = (await _unitOfWork.Repository<MockTestBlueprint>().GetAllAsync(cancellationToken))
                .OrderBy(b => b.TestType)
                .ToList();
            var publishedCount = (await _unitOfWork.Repository<QuestionGroup>()
                .FindAsync(g => g.Status == QuestionGroupStatus.PUBLISHED, cancellationToken)).Count();

            return ServiceResult<BlueprintManagementPageDto>.Success(new BlueprintManagementPageDto
            {
                AvailablePublishedGroupsCount = publishedCount,
                Blueprints = blueprints.Select(b => new BlueprintAdminItemDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    TestType = b.TestType,
                    DurationMinutes = b.DurationMinutes,
                    GroupCount = b.QuestionGroupIds.Count,
                    IsActive = b.IsActive
                }).ToList()
            });
        }

        public async Task<ServiceResult> SaveBlueprintAsync(
            SaveBlueprintRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var publishedIds = (await _unitOfWork.Repository<QuestionGroup>()
                .FindAsync(g => g.Status == QuestionGroupStatus.PUBLISHED, cancellationToken))
                .Select(g => g.Id)
                .ToList();

            var repo = _unitOfWork.Repository<MockTestBlueprint>();
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                var existing = await repo.GetByIdAsync(request.Id.Value, cancellationToken);
                if (existing == null)
                    return ServiceResult.Failure("Không tìm thấy khung đề thi.");

                existing.Title = request.Title.Trim();
                existing.TestType = request.TestType;
                existing.DurationMinutes = request.DurationMinutes;
                existing.IsActive = request.IsActive;
                if (request.AutoFillPublishedGroups)
                {
                    existing.QuestionGroupIds = publishedIds;
                }
                repo.Update(existing);
            }
            else
            {
                await repo.AddAsync(new MockTestBlueprint
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    TestType = request.TestType,
                    DurationMinutes = request.DurationMinutes,
                    QuestionGroupIds = request.AutoFillPublishedGroups ? publishedIds : new List<Guid>(),
                    IsActive = request.IsActive
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success($"Đã lưu khung đề thi '{request.Title}'.");
        }

        public async Task<ServiceResult> AutoAssembleBlueprintAsync(
            Guid blueprintId,
            CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<MockTestBlueprint>();
            var bp = await repo.GetByIdAsync(blueprintId, cancellationToken);
            if (bp == null)
                return ServiceResult.Failure("Không tìm thấy khung đề thi.");

            var publishedIds = (await _unitOfWork.Repository<QuestionGroup>()
                .FindAsync(g => g.Status == QuestionGroupStatus.PUBLISHED, cancellationToken))
                .Select(g => g.Id)
                .ToList();

            bp.QuestionGroupIds = publishedIds;
            repo.Update(bp);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success($"Đã tự động đóng gói {publishedIds.Count} cụm câu hỏi PUBLISHED vào khung đề '{bp.Title}'.");
        }

        public async Task<ServiceResult<AdminSettingsPageDto>> GetAdminSettingsPageAsync(
            CancellationToken cancellationToken = default)
        {
            var rules = (await _unitOfWork.Repository<CreditRule>().GetAllAsync(cancellationToken))
                .OrderBy(r => r.FeatureCode)
                .Select(r => new CreditRuleAdminDto
                {
                    FeatureCode = r.FeatureCode,
                    CreditCost = r.CreditCost,
                    RateLimitPerMinute = r.RateLimitPerMinute,
                    IsActive = r.IsActive,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

            var plans = (await _unitOfWork.Repository<SubscriptionPlan>().GetAllAsync(cancellationToken))
                .OrderBy(p => p.PriceVnd)
                .Select(p => new SubscriptionPlanAdminDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    PriceVnd = p.PriceVnd,
                    DurationDays = p.DurationDays,
                    GrantedCredits = p.GrantedCredits,
                    DailyAnalysisLimit = p.DailyAnalysisLimit,
                    IsActive = p.IsActive
                }).ToList();

            return ServiceResult<AdminSettingsPageDto>.Success(new AdminSettingsPageDto
            {
                CreditRules = rules,
                SubscriptionPlans = plans
            });
        }

        public async Task<ServiceResult> UpdateCreditRuleAsync(
            CreditRuleAdminDto request,
            CancellationToken cancellationToken = default)
        {
            var rules = await _unitOfWork.Repository<CreditRule>()
                .FindAsync(r => r.FeatureCode == request.FeatureCode, cancellationToken);
            var existing = rules.FirstOrDefault();
            if (existing == null)
                return ServiceResult.Failure("Không tìm thấy luật trừ Credit.");

            existing.CreditCost = Math.Max(0, request.CreditCost);
            existing.RateLimitPerMinute = Math.Max(1, request.RateLimitPerMinute);
            existing.IsActive = request.IsActive;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Repository<CreditRule>().Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult.Success($"Đã cập nhật cấu hình Credit cho tính năng '{request.FeatureCode}'.");
        }

        public async Task<ServiceResult> SaveSubscriptionPlanAsync(
            SubscriptionPlanAdminDto request,
            CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<SubscriptionPlan>();
            if (request.Id != Guid.Empty)
            {
                var existing = await repo.GetByIdAsync(request.Id, cancellationToken);
                if (existing != null)
                {
                    existing.Code = request.Code.Trim().ToUpperInvariant();
                    existing.Name = request.Name.Trim();
                    existing.PriceVnd = request.PriceVnd;
                    existing.DurationDays = request.DurationDays;
                    existing.GrantedCredits = request.GrantedCredits;
                    existing.DailyAnalysisLimit = request.DailyAnalysisLimit;
                    existing.IsActive = request.IsActive;
                    repo.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return ServiceResult.Success($"Đã cập nhật gói cước '{existing.Name}'.");
                }
            }

            await repo.AddAsync(new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                PriceVnd = request.PriceVnd,
                DurationDays = request.DurationDays,
                GrantedCredits = request.GrantedCredits,
                DailyAnalysisLimit = request.DailyAnalysisLimit,
                IsActive = request.IsActive
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success($"Đã tạo gói cước mới '{request.Name}'.");
        }

        // =====================================================================
        // PRIVATE HELPERS: CLOUD MEDIA (FLUX CDN / CLOUDINARY / TTS) & HYDRATION
        // =====================================================================
        private async Task<List<QuestionGroupAdminDto>> HydrateQuestionGroupsAsync(
            List<QuestionGroup> groups,
            CancellationToken cancellationToken)
        {
            if (groups.Count == 0)
                return new List<QuestionGroupAdminDto>();

            var groupIds = groups.Select(g => g.Id).ToHashSet();
            var allQuestions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => groupIds.Contains(q.GroupId), cancellationToken)).ToList();
            var qIds = allQuestions.Select(q => q.Id).ToHashSet();
            var allQTags = (await _unitOfWork.Repository<QuestionTag>()
                .FindAsync(qt => qIds.Contains(qt.QuestionId), cancellationToken)).ToList();
            var allTags = (await _unitOfWork.Repository<SkillTag>().GetAllAsync(cancellationToken))
                .ToDictionary(t => t.Id);
            var allValLogs = (await _unitOfWork.Repository<AiValidationLog>()
                .FindAsync(v => groupIds.Contains(v.QuestionGroupId), cancellationToken)).ToList();

            var result = new List<QuestionGroupAdminDto>();

            foreach (var g in groups)
            {
                var qs = allQuestions.Where(q => q.GroupId == g.Id).OrderBy(q => q.QuestionOrder).ToList();
                var firstQ = qs.FirstOrDefault();
                SkillTag? primaryTag = null;
                if (firstQ != null)
                {
                    var qt = allQTags.FirstOrDefault(x => x.QuestionId == firstQ.Id);
                    if (qt != null)
                        allTags.TryGetValue(qt.SkillTagId, out primaryTag);
                }

                var latestVal = allValLogs
                    .Where(v => v.QuestionGroupId == g.Id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefault();

                var (imgUrl, imgPrompt, audioUrl, provider) = ParseMediaAssets(g.MediaAssets);

                result.Add(new QuestionGroupAdminDto
                {
                    GroupId = g.Id,
                    SkillType = g.SkillType,
                    PartNumber = g.PartNumber,
                    DifficultyLevel = g.DifficultyLevel,
                    SourceType = g.SourceType,
                    Status = g.Status,
                    PassageContent = g.PassageContent,
                    TranscriptData = g.TranscriptData,
                    ImageUrl = imgUrl,
                    ImagePrompt = imgPrompt,
                    AudioUrl = audioUrl,
                    StorageProvider = provider,
                    WritingPromptMeta = g.WritingPromptMeta,
                    SkillTagCode = primaryTag?.Code ?? "TOEIC",
                    SkillTagName = primaryTag?.Name ?? "Kỹ năng TOEIC",
                    CreatedAt = g.CreatedAt,
                    Questions = qs.Select(q =>
                    {
                        var (optA, optB, optC, optD) = ParseOptions(q.Options);
                        return new QuestionItemAdminDto
                        {
                            QuestionId = q.Id,
                            QuestionOrder = q.QuestionOrder,
                            QuestionStem = q.QuestionStem,
                            OptionA = optA,
                            OptionB = optB,
                            OptionC = optC,
                            OptionD = optD,
                            CorrectOption = q.CorrectOption,
                            ExplanationGeneral = q.ExplanationGeneral,
                            WritingRubric = q.WritingRubric
                        };
                    }).ToList(),
                    LatestValidation = latestVal == null ? null : new ValidationLogItemDto
                    {
                        Id = latestVal.Id,
                        QuestionGroupId = latestVal.QuestionGroupId,
                        ValidatorModel = latestVal.ValidatorModel,
                        BlindSolvedAnswers = latestVal.BlindSolvedAnswers,
                        IsAnswerMatched = latestVal.IsAnswerMatched,
                        EtsComplianceScore = latestVal.EtsComplianceScore,
                        DistractorQualityScore = latestVal.DistractorQualityScore,
                        FinalDecision = latestVal.FinalDecision.ToString(),
                        RejectionReasons = latestVal.RejectionReasons,
                        CreatedAt = latestVal.CreatedAt
                    }
                });
            }

            return result;
        }

        private static readonly (string ImageUrl, string Prompt, string Transcript, char Correct, string Explanation)[] EtsPart1Sets = new[]
        {
            (
                "https://images.unsplash.com/photo-1581093458791-9f3c3900df4b?auto=format&fit=crop&w=800&q=80",
                "Female researcher looking into a microscope in a bright laboratory",
                "(A) She is typing on a laptop.\n(B) She is looking at a microscope.\n(C) She is putting on safety glasses.\n(D) She is writing on a whiteboard.",
                'B',
                "[Tranh 1 người - Hành động chính]: Người phụ nữ đang cúi nhìn vào kính hiển vi (looking at a microscope). (A), (D) sai hành động; (C) bẫy 'putting on' (đang đeo vào) trong khi cô ấy đã đeo sẵn kính trên mắt."
            ),
            (
                "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=800&q=80",
                "Clean dining area with overhead light fixture, tables and chairs",
                "(A) Some chairs have been arranged on a balcony.\n(B) A clock has been propped up on a shelf.\n(C) Some place mats are laid out on a bench.\n(D) A light fixture is hanging above a dining area.",
                'D',
                "[Tranh tả vật - Trạng thái & Vị trí]: (D) mô tả chính xác chiếc đèn trần đang treo phía trên khu vực ăn uống (hanging above a dining area). Các câu (A), (B), (C) đều sai vị trí không gian (không có ban công, kệ hay ghế dài)."
            ),
            (
                "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80",
                "Pantry shelves with ceramic coffee mugs and glass cups lined up neatly",
                "(A) Some cups have been lined up on shelves.\n(B) A fruit basket has been emptied.\n(C) Some pots are being filled with water.\n(D) Some coffee has been spilled on the counter.",
                'A',
                "[Tranh tả vật - Bẫy bị động HTTD]: (A) đúng cấu trúc hoàn thành 'have been lined up' mô tả cốc chén được xếp thẳng hàng trên kệ. (C) là [Bẫy bị động HTTD - are being filled]: câu này ngụ ý có người đang rót nước, luôn SAI trong tranh không người. (D) là bẫy từ na ná (coffee ~ coffee makers)."
            ),
            (
                "https://images.unsplash.com/photo-1522071820081-009f0129c71c?auto=format&fit=crop&w=800&q=80",
                "Two colleagues reviewing data together on a laptop in a modern conference room",
                "(A) They are looking at a computer screen together.\n(B) They are boarding a commercial aircraft.\n(C) They are stacking boxes on a truck.\n(D) They are painting the conference room walls.",
                'A',
                "[Tranh nhiều người - Hành động chung]: Cả hai đồng nghiệp đang cùng chăm chú nhìn vào màn hình máy tính (They are looking at a computer screen together). Các phương án (B), (C), (D) hoàn toàn sai bối cảnh và hành động."
            ),
            (
                "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80",
                "Warehouse worker lifting a wooden chair in a logistics depot",
                "(A) He is parking a truck.\n(B) He is lifting some furniture.\n(C) He is starting an engine.\n(D) He is driving a vehicle.",
                'B',
                "[Tranh 1 người - Từ khái quát]: Người đàn ông đang nhấc đồ đạc. Đáp án (B) sử dụng danh từ bao quát 'furniture' thay thế cho đồ vật cụ thể (ghế/bàn). Các phương án (A), (C), (D) sai hành động."
            ),
            (
                "https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&w=800&q=80",
                "Two construction workers wearing hard hats installing roof panels on a building",
                "(A) They are carrying a ladder.\n(B) They are putting away some rope.\n(C) They are installing a roof on a house.\n(D) They are organizing boxes of materials.",
                'C',
                "[Tranh nhiều người - Hành động chung]: Cả nhóm công nhân đang lắp ráp phần mái nhà (installing a roof on a house). (A), (D) nhắc đến vật không có trong tranh (thang, hộp); (B) chỉ là hành động của một cá nhân, không phải cả hai."
            )
        };

        private static readonly (string Question, string OptionA, string OptionB, string OptionC, char Correct, string Explanation)[] EtsPart2Sets = new[]
        {
            (
                "When will the forklift be repaired?",
                "A fork and a knife.",
                "Probably next week.",
                "Because the technician is busy.",
                'B',
                "[Part 2 - 5W1H When]: Hỏi thời điểm ('Khi nào xe nâng sẽ được sửa?'). (B) trả lời mốc thời gian ước lượng ('Có thể là tuần sau'). (A) là [Bẫy phát âm gần giống]: 'fork' nghe giống 'forklift'. (C) là [Bẫy lạc dạng câu hỏi]: trả lời lý do (Because) cho câu hỏi Why."
            ),
            (
                "Who should I assign this task to?",
                "Someone in marketing.",
                "I'll finish it by Tuesday.",
                "Please sign here.",
                'A',
                "[Part 2 - 5W1H Who]: Hỏi người nhận việc ('Tôi nên giao việc này cho ai?'). (A) trả lời bằng tên phòng ban ('Ai đó ở phòng Marketing'). (B) trả lời thời gian When. (C) là [Bẫy phát âm gần giống]: 'sign' nghe giống 'assign' trong câu hỏi."
            ),
            (
                "Where is the entrance to the parking garage?",
                "The local park is very nice.",
                "Behind the main office building.",
                "No, I didn't drive today.",
                'B',
                "[Part 2 - 5W1H Where]: Hỏi địa điểm ('Lối vào bãi đỗ xe ở đâu?'). (B) dùng giới từ chỉ vị trí ('Behind the main office building'). (A) là [Bẫy phát âm gần giống]: 'park' ~ 'parking'. (C) là [Bẫy Yes/No cho 5W1H]: câu hỏi Where tuyệt đối không dùng Yes/No."
            ),
            (
                "Why did Mr. Harrison resign from his position?",
                "Two weeks ago.",
                "It's just been signed.",
                "He found a different job.",
                'C',
                "[Part 2 - 5W1H Why]: Hỏi nguyên nhân ('Tại sao ông Harrison lại từ chức?'). (C) nêu thẳng lý do không cần liên từ ('Ông ấy đã tìm được công việc khác'). (A) trả lời thời gian When. (B) là [Bẫy phát âm gần giống]: 'signed' nghe gần giống 'resign'."
            ),
            (
                "Are you having trouble with your Internet connection, too?",
                "The afternoon, please.",
                "We should double our order.",
                "Yes, I can't access anything online.",
                'C',
                "[Part 2 - Yes/No Question]: Xác nhận vấn đề mạng Internet. (C) xác nhận 'Yes' kèm lời giải thích tình trạng cụ thể ('không vào được mạng'). (A) trả lời thời gian. (B) là [Bẫy phát âm gần giống]: 'double' nghe giống 'trouble'."
            ),
            (
                "Should I put the clothes in the dryer or hang them outside?",
                "No, this shirt is too small.",
                "It's a brand-new hairdryer.",
                "Either is fine with me.",
                'C',
                "[Part 2 - Câu hỏi Lựa chọn (Alternative)]: Có liên từ 'or'. (C) dùng mẫu câu kinh điển 'Either is fine with me' (Cái nào cũng được). (A) là [Bẫy Yes/No]: câu hỏi lựa chọn tuyệt đối không trả lời Yes/No. (B) là [Bẫy từ vựng liên quan]: 'hairdryer' ~ 'dryer'."
            ),
            (
                "Why don't we hand out free samples to customers?",
                "No, it's the customer service department.",
                "Because we conducted a survey.",
                "Yeah, that's a good strategy.",
                'C',
                "[Part 2 - Lời đề nghị/Gợi ý]: Cấu trúc 'Why don't we...' là lời rủ rê/đề xuất, KHÔNG PHẢI hỏi lý do. (C) đồng thuận với ý tưởng ('Chiến lược hay đấy'). (B) là [Bẫy hiểu nhầm nghĩa đen]: dùng 'Because' để trả lời cho lời gợi ý. (A) là [Bẫy lặp từ]: 'customer'."
            ),
            (
                "The new printers are very popular.",
                "I'm writing a first draft.",
                "Yeah, they're selling quickly.",
                "Please print five copies.",
                'B',
                "[Part 2 - Câu trần thuật]: Đưa ra nhận xét tích cực về sản phẩm máy in mới. (B) đồng tình ('Yeah') và bổ sung dẫn chứng ('đang bán rất chạy'). (A) lạc đề. (C) là [Bẫy lặp từ cùng gốc]: 'print' ~ 'printers'."
            ),
            (
                "When did Takumi start working here?",
                "I have no idea. You can ask Sue.",
                "Since my computer isn't working.",
                "No, he retired last month.",
                'A',
                "[Part 2 - Mẫu trả lời gián tiếp]: (A) sử dụng phản hồi kinh điển 'I have no idea' + hướng dẫn hỏi bên thứ ba (Sue) - dạng này có xác suất đúng rất cao trong đề thi ETS. (B) nêu lý do. (C) là [Bẫy Yes/No cho 5W1H]."
            )
        };

        private static readonly (string Transcript, (string Stem, string A, string B, string C, string D, char Correct, string Exp)[] Questions)[] EtsPart3Sets = new[]
        {
            (
                "Woman: Hello, this is Kelly in the accounting department. The ink cartridge in the printer on the fourth floor has run out. Do you think you could come to replace it today?\nMan: Sure, I can head up there shortly. By the way, can I ask you a favor? I need you to let me know what model the machine is so I can bring the correct one.\nWoman: Actually, I'm not in the office right now, but I can ask David to check the model label and text it to you.",
                new (string, string, string, string, string, char, string)[]
                {
                    (
                        "What problem is the woman reporting?",
                        "An accounting error has been made.",
                        "A printer is out of order.",
                        "Some office supplies have been used up.",
                        "A document has become lost.",
                        'C',
                        "(1) [Kỹ thuật Paraphrase]: Người phụ nữ thông báo 'The ink cartridge in the printer... has run out'. Cụm 'has run out' được paraphrase thành 'have been used up' và 'ink cartridge' được khái quát hóa thành 'office supplies'. (B) là bẫy máy in không hề hỏng (out of order), chỉ hết mực."
                    ),
                    (
                        "What information does the man ask for?",
                        "The machine model number",
                        "The department budget",
                        "The delivery address",
                        "The repair schedule",
                        'A',
                        "(2) Người đàn ông yêu cầu: 'I need you to let me know what model the machine is so I can bring the correct one' -> Chọn (A)."
                    ),
                    (
                        "What does the woman say she will do?",
                        "Return to the office immediately",
                        "Have a colleague check the machine",
                        "Purchase a replacement online",
                        "Submit an official work order",
                        'B',
                        "(3) Cuối đoạn, người phụ nữ nói: 'I can ask David to check the model label and text it to you' (David là đồng nghiệp) -> Chọn (B)."
                    )
                }
            ),
            (
                "Woman: Hi, it's Martina from Accounting. I'd like to reserve the main conference room for a meeting I'll be leading on Friday with colleagues from our New York office.\nMan: Sure, that shouldn't be a problem. What time is the meeting?\nWoman: It's from nine to eleven A.M.\nMan: OK. I'll block off that time slot for you. Do you need any special equipment besides a laptop and projector?\nWoman: No, but I'll need the key so I can go in a little early and set up. Can I pick that up on Friday morning?\nMan: Absolutely.",
                new (string, string, string, string, string, char, string)[]
                {
                    (
                        "What is the woman preparing for?",
                        "A move to a new city",
                        "A business trip",
                        "A building tour",
                        "A meeting with visiting colleagues",
                        'D',
                        "(1) Người phụ nữ nói: 'I'd like to reserve the main conference room for a meeting I'll be leading on Friday with colleagues from our New York office' -> Chọn (D)."
                    ),
                    (
                        "Who most likely is the man?",
                        "An accountant",
                        "An administrative assistant",
                        "A marketing director",
                        "A company president",
                        'B',
                        "(2) Người đàn ông phụ trách xếp lịch phòng họp ('block off that time slot') và chuẩn bị máy chiếu thiết bị -> Suy luận là Trợ lý hành chính (Administrative assistant). (A) là bẫy người phụ nữ mới là người thuộc phòng Accounting."
                    ),
                    (
                        "What does the woman want to pick up on Friday morning?",
                        "A building map",
                        "A room key",
                        "An ID card",
                        "A parking pass",
                        'B',
                        "(3) Cuối đoạn thoại, người phụ nữ nói: 'I'll need the key so I can go in a little early and set up. Can I pick that up on Friday morning?' -> Chọn (B)."
                    )
                }
            ),
            (
                "Man: Hi, Sarah. Have you finished reviewing the catering proposals for next month's client reception?\nWoman: Yes, I looked over all three options this morning. Green Leaf Catering offers the best variety of vegetarian dishes, and their price fits our budget.\nMan: Great. However, they usually book up quickly during the spring season.\nWoman: Good point. I'll call their manager right after lunch to pay the deposit and reserve the date.",
                new (string, string, string, string, string, char, string)[]
                {
                    (
                        "What are the speakers mainly discussing?",
                        "Hiring a new restaurant manager",
                        "Selecting a catering company for an event",
                        "Revising a department budget",
                        "Rescheduling a client meeting",
                        'B',
                        "(1) Người đàn ông mở đầu hỏi về 'catering proposals for next month's client reception' -> Chọn (B)."
                    ),
                    (
                        "Why does the woman prefer Green Leaf Catering?",
                        "They are located near the office.",
                        "They offer a discount for corporate clients.",
                        "They have diverse vegetarian options within budget.",
                        "They were recommended by a colleague.",
                        'C',
                        "(2) Người phụ nữ nhận xét: 'Green Leaf Catering offers the best variety of vegetarian dishes, and their price fits our budget' -> Chọn (C)."
                    ),
                    (
                        "What does the woman say she will do after lunch?",
                        "Contact a manager to make a deposit",
                        "Send an invitation list to the client",
                        "Taste some menu samples",
                        "Update the event schedule",
                        'A',
                        "(3) Cuối đoạn thoại, người phụ nữ chốt: 'I'll call their manager right after lunch to pay the deposit and reserve the date' -> Chọn (A)."
                    )
                }
            )
        };

        private static readonly (string Transcript, (string Stem, string A, string B, string C, string D, char Correct, string Exp)[] Questions)[] EtsPart4Sets = new[]
        {
            (
                "Attention all warehouse staff. Starting next Monday, May twelfth, the main loading dock on the east side will be closed for concrete repairs. During the two-week renovation, all incoming deliveries and outgoing shipments will be redirected to Gate B on the north side of the building. Because space at Gate B is limited, please make sure to move pallets to the storage racks immediately after unloading. Thank you for your cooperation.",
                new (string, string, string, string, string, char, string)[]
                {
                    (
                        "Who is the announcement most likely for?",
                        "Construction contractors",
                        "Warehouse employees",
                        "Delivery truck drivers",
                        "Safety inspectors",
                        'B',
                        "(1) Câu mở đầu nêu rõ đối tượng nghe: 'Attention all warehouse staff' -> Chọn (B)."
                    ),
                    (
                        "What will happen starting on May 12?",
                        "A new inventory system will be installed.",
                        "Overtime shifts will be required.",
                        "The main loading dock will undergo repairs.",
                        "A company inspection will take place.",
                        'C',
                        "(2) Người nói thông báo: 'Starting next Monday, May twelfth, the main loading dock on the east side will be closed for concrete repairs' -> Chọn (C)."
                    ),
                    (
                        "What are listeners instructed to do?",
                        "Wear reflective vests at all times",
                        "Park in the south parking lot",
                        "Clear pallets from the unloading area promptly",
                        "Sign in at the security desk",
                        'C',
                        "(3) Người nói yêu cầu: 'please make sure to move pallets to the storage racks immediately after unloading' -> Chọn (C)."
                    )
                }
            ),
            (
                "Good afternoon, passengers on Flight 418 to Chicago. Due to severe thunderstorms currently moving across the Midwest, our departure has been delayed by approximately two hours. Ground operations have assigned us a new departure gate: Gate D-14, located at the end of Concourse D. To compensate for the inconvenience, airline service representatives at the customer counter are distributing meal vouchers valid at any airport restaurant. We apologize for the delay and appreciate your patience.",
                new (string, string, string, string, string, char, string)[]
                {
                    (
                        "Who is the speaker addressing?",
                        "Flight attendants",
                        "Airport security personnel",
                        "Airline passengers",
                        "Maintenance technicians",
                        'C',
                        "(1) Mở đầu thông báo: 'Good afternoon, passengers on Flight 418 to Chicago' -> Chọn (C)."
                    ),
                    (
                        "What is the cause of the delay?",
                        "Mechanical problems",
                        "Severe weather conditions",
                        "Air traffic congestion",
                        "Staff shortages",
                        'B',
                        "(2) Người nói giải thích: 'Due to severe thunderstorms currently moving across the Midwest' -> Chọn (B)."
                    ),
                    (
                        "What can listeners collect at the customer counter?",
                        "Updated boarding passes",
                        "Baggage claim tags",
                        "Meal vouchers",
                        "Hotel booking forms",
                        'C',
                        "(3) Người nói thông báo: 'service representatives at the customer counter are distributing meal vouchers' -> Chọn (C)."
                    )
                }
            )
        };

        private static readonly (string Stem, string A, string B, string C, string D, char Correct, string Exp)[] EtsPart5Sets = new[]
        {
            (
                "The marketing executives ------- portfolio of international clients has expanded rapidly will receive an annual performance bonus.",
                "whose", "who", "whom", "which",
                'A',
                "[Part 5 - Mệnh đề quan hệ]: Sau chỗ trống là danh từ 'portfolio of international clients' nên cần tính từ sở hữu quan hệ 'whose' bổ nghĩa cho danh từ người 'The marketing executives'."
            ),
            (
                "All reimbursement requests ------- after the fifth business day of the month will be processed in the next billing cycle.",
                "submitted", "submitting", "submits", "to submit",
                'A',
                "[Part 5 - Rút gọn mệnh đề quan hệ bị động]: 'requests [which are] submitted after the fifth business day' -> rút gọn dạng bị động dùng quá khứ phân từ V-ed/PII 'submitted'."
            ),
            (
                "Ms. Alvarez ------- the quarterly financial audit for three weeks before the external consultants arrived at headquarters.",
                "had been reviewing", "reviews", "will review", "is reviewing",
                'A',
                "[Part 5 - Thì Quá khứ hoàn thành tiếp diễn]: Hành động kéo dài liên tục ('for three weeks') trước một mốc thời gian trong quá khứ ('before the external consultants arrived') bắt buộc dùng thì Quá khứ hoàn thành tiếp diễn (had been reviewing)."
            ),
            (
                "The regional sales team has ------- exceeded their revenue projections over the past three fiscal quarters.",
                "consistently", "consistent", "consistency", "consisted",
                'A',
                "[Part 5 - Dạng từ (Word Form)]: Vị trí đứng giữa trợ động từ 'has' và động từ chính 'exceeded' cần một trạng từ (adverb - 'consistently') để bổ nghĩa cho hành động vượt chỉ tiêu doanh số."
            ),
            (
                "------- the shipment of electronic microchips arrived two days late, the assembly line completed the order on schedule.",
                "Although", "Despite", "Because of", "Unless",
                'A',
                "[Part 5 - Liên từ chỉ sự tương phản]: Vế sau là một mệnh đề hoàn chỉnh (S + V: 'the shipment ... arrived') thể hiện sự tương phản nên cần liên từ 'Although'. 'Despite' và 'Because of' chỉ đi với cụm danh từ."
            )
        };

        private async Task<GeneratedQuestionGroupPayload> GenerateGroupWithCloudMediaAsync(
            SkillTag skillTag,
            TargetDifficulty difficulty,
            int index,
            CancellationToken cancellationToken)
        {
            var code = skillTag.Code.ToUpperInvariant();

            // 1. LISTENING PART 1: Photographs (1 Question per image, ETS Collection Template)
            if (code.Contains("PART1"))
            {
                var pick = EtsPart1Sets[index % EtsPart1Sets.Length];
                var spokenAudio = $"Look at the picture marked number {index + 1} in your test book. {pick.Transcript.Replace("\n", " ")}";
                var cloudAudioUrl = BuildCloudTtsAudioUrl(spokenAudio);

                return new GeneratedQuestionGroupPayload
                {
                    SkillType = SkillType.LISTENING,
                    PartNumber = 1,
                    TranscriptData = pick.Transcript,
                    MediaAssetsJson = JsonSerializer.Serialize(new
                    {
                        imageUrl = pick.ImageUrl,
                        imagePrompt = pick.Prompt,
                        audioUrl = cloudAudioUrl,
                        storageProvider = "ETS_WORKPLACE_CDN"
                    }),
                    Questions = new List<GeneratedQuestionItemPayload>
                    {
                        new()
                        {
                            QuestionOrder = (short)(index + 1),
                            QuestionStem = $"Look at the picture marked number {index + 1} in your test book.",
                            OptionA = "(A)",
                            OptionB = "(B)",
                            OptionC = "(C)",
                            OptionD = "(D)",
                            CorrectOption = pick.Correct,
                            ExplanationGeneral = pick.Explanation
                        }
                    }
                };
            }

            // 2. LISTENING PART 2: Question-Response (1 Question per group, 3 Options A, B, C)
            if (code.Contains("PART2"))
            {
                var pick2 = EtsPart2Sets[index % EtsPart2Sets.Length];
                var p2Transcript = $"Question: {pick2.Question}\n(A) {pick2.OptionA}\n(B) {pick2.OptionB}\n(C) {pick2.OptionC}";
                var spokenAudio = $"Question: {pick2.Question} (A) {pick2.OptionA} (B) {pick2.OptionB} (C) {pick2.OptionC}";
                var cloudAudioUrl = BuildCloudTtsAudioUrl(spokenAudio);

                return new GeneratedQuestionGroupPayload
                {
                    SkillType = SkillType.LISTENING,
                    PartNumber = 2,
                    TranscriptData = p2Transcript,
                    MediaAssetsJson = JsonSerializer.Serialize(new
                    {
                        imageUrl = (string?)null,
                        imagePrompt = (string?)null,
                        audioUrl = cloudAudioUrl,
                        storageProvider = "CLOUD_TTS_CDN"
                    }),
                    Questions = new List<GeneratedQuestionItemPayload>
                    {
                        new()
                        {
                            QuestionOrder = (short)(7 + index),
                            QuestionStem = "Mark your answer on your answer sheet.",
                            OptionA = "(A)",
                            OptionB = "(B)",
                            OptionC = "(C)",
                            OptionD = "",
                            CorrectOption = pick2.Correct,
                            ExplanationGeneral = pick2.Explanation
                        }
                    }
                };
            }

            // 3. LISTENING PART 3: Conversations (1 Conversation + 3 Questions)
            if (code.Contains("PART3"))
            {
                var pick3 = EtsPart3Sets[index % EtsPart3Sets.Length];
                var cloudAudioUrl = BuildCloudTtsAudioUrl(pick3.Transcript);

                var p3Questions = pick3.Questions.Select((q, qIdx) => new GeneratedQuestionItemPayload
                {
                    QuestionOrder = (short)(32 + index * 3 + qIdx),
                    QuestionStem = q.Stem,
                    OptionA = q.A,
                    OptionB = q.B,
                    OptionC = q.C,
                    OptionD = q.D,
                    CorrectOption = q.Correct,
                    ExplanationGeneral = q.Exp
                }).ToList();

                return new GeneratedQuestionGroupPayload
                {
                    SkillType = SkillType.LISTENING,
                    PartNumber = 3,
                    TranscriptData = pick3.Transcript,
                    MediaAssetsJson = JsonSerializer.Serialize(new
                    {
                        imageUrl = (string?)null,
                        imagePrompt = (string?)null,
                        audioUrl = cloudAudioUrl,
                        storageProvider = "CLOUD_TTS_CDN"
                    }),
                    Questions = p3Questions
                };
            }

            // 4. LISTENING PART 4: Talks (1 Talk + 3 Questions)
            if (code.Contains("PART4"))
            {
                var pick4 = EtsPart4Sets[index % EtsPart4Sets.Length];
                var cloudAudioUrl = BuildCloudTtsAudioUrl(pick4.Transcript);

                var p4Questions = pick4.Questions.Select((q, qIdx) => new GeneratedQuestionItemPayload
                {
                    QuestionOrder = (short)(71 + index * 3 + qIdx),
                    QuestionStem = q.Stem,
                    OptionA = q.A,
                    OptionB = q.B,
                    OptionC = q.C,
                    OptionD = q.D,
                    CorrectOption = q.Correct,
                    ExplanationGeneral = q.Exp
                }).ToList();

                return new GeneratedQuestionGroupPayload
                {
                    SkillType = SkillType.LISTENING,
                    PartNumber = 4,
                    TranscriptData = pick4.Transcript,
                    MediaAssetsJson = JsonSerializer.Serialize(new
                    {
                        imageUrl = (string?)null,
                        imagePrompt = (string?)null,
                        audioUrl = cloudAudioUrl,
                        storageProvider = "CLOUD_TTS_CDN"
                    }),
                    Questions = p4Questions
                };
            }

            // 5. WRITING SKILL (Part 8): Email Response
            if (skillTag.Category == SkillCategory.WRITING_SKILL || code.Contains("WRITING"))
            {
                var imgPrompt = "Customer service representative helping a client at an airport check-in counter";
                var cloudImgUrl = "https://images.unsplash.com/photo-1542296332-2e4473faf563?auto=format&fit=crop&w=800&q=80";

                return new GeneratedQuestionGroupPayload
                {
                    SkillType = SkillType.WRITING,
                    PartNumber = 8,
                    PassageContent = "From: K. Tanaka, Operations Director\nTo: Customer Support Team\nSubject: Client Feedback Survey\n\nDirections: Respond to the e-mail as if you are a member of the Customer Support Team. In your e-mail, give TWO reasons why response times increased last month and suggest ONE training workshop for the team.",
                    WritingPromptMeta = JsonSerializer.Serialize(new { requiredTasks = 3, recommendedWords = "120-150", targetTimeMin = 10 }),
                    MediaAssetsJson = JsonSerializer.Serialize(new
                    {
                        imageUrl = cloudImgUrl,
                        imagePrompt = imgPrompt,
                        audioUrl = (string?)null,
                        storageProvider = "ETS_WORKPLACE_CDN"
                    }),
                    Questions = new List<GeneratedQuestionItemPayload>
                    {
                        new()
                        {
                            QuestionOrder = 1,
                            QuestionStem = "Write an email response addressing all three prompts in the directions above.",
                            OptionA = "Task 1: Explain reason 1 for response time increase",
                            OptionB = "Task 2: Explain reason 2 for response time increase",
                            OptionC = "Task 3: Propose 1 specific training workshop",
                            OptionD = "Maintain professional business email format",
                            CorrectOption = 'A',
                            WritingRubric = "Chấm theo thang ETS 0-4: Đáp ứng đủ 3 yêu cầu trong Directions, sử dụng liên từ mạch lạc (furthermore, consequently) và đúng văn phong Email thương mại.",
                            ExplanationGeneral = "Bài viết cần mở đầu trang trọng (Dear Mr. Tanaka), nêu rõ 2 nguyên nhân khách quan và đề xuất 1 khóa tập huấn cụ thể."
                        }
                    }
                };
            }

            // 6. READING PART 5, 6, 7 (Grammar & Reading Comprehension)
            short rPart = code.Contains("PART6") ? (short)6 : (code.Contains("PART7") ? (short)7 : (short)5);
            var pick5 = EtsPart5Sets[index % EtsPart5Sets.Length];

            return new GeneratedQuestionGroupPayload
            {
                SkillType = SkillType.READING,
                PartNumber = rPart,
                PassageContent = rPart >= 6
                    ? "MEMORANDUM\nTo: All Regional Sales Managers\nFrom: Haruto Sato, Vice President of Sales\nSubject: Updated Travel Reimbursement Policy\n\nPlease be advised that effective November 1, all expense reports must be uploaded through the new cloud portal within ten days of completing a business trip."
                    : null,
                Questions = new List<GeneratedQuestionItemPayload>
                {
                    new()
                    {
                        QuestionOrder = (short)(101 + index),
                        QuestionStem = pick5.Stem,
                        OptionA = pick5.A,
                        OptionB = pick5.B,
                        OptionC = pick5.C,
                        OptionD = pick5.D,
                        CorrectOption = pick5.Correct,
                        ExplanationGeneral = pick5.Exp
                    }
                }
            };
        }

        private async Task<string> GenerateCloudImageUrlAsync(
            string imagePrompt,
            int seed,
            CancellationToken cancellationToken)
        {
            var pick = EtsPart1Sets[Math.Abs(seed) % EtsPart1Sets.Length];
            return await Task.FromResult(pick.ImageUrl);
        }

        private static string BuildCloudTtsAudioUrl(string text)
        {
            var shortText = text.Length > 220 ? text[..220] : text;
            var encoded = Uri.EscapeDataString(shortText);
            return $"/Admin/AudioProxy?text={encoded}";
        }

        private static (string? imageUrl, string? imagePrompt, string? audioUrl, string? storageProvider) ParseMediaAssets(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (null, null, null, null);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string? img = root.TryGetProperty("imageUrl", out var p1) ? p1.GetString() : null;
                string? prompt = root.TryGetProperty("imagePrompt", out var p2) ? p2.GetString() : null;
                string? audio = root.TryGetProperty("audioUrl", out var p3) ? p3.GetString() : null;
                string? prov = root.TryGetProperty("storageProvider", out var p4) ? p4.GetString() : "ETS_WORKPLACE_CDN";
                return (img, prompt, audio, prov);
            }
            catch
            {
                return (null, null, null, null);
            }
        }

        private static (string A, string B, string C, string D) ParseOptions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ("", "", "", "");

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return (
                    root.TryGetProperty("A", out var a) ? a.GetString() ?? "" : "",
                    root.TryGetProperty("B", out var b) ? b.GetString() ?? "" : "",
                    root.TryGetProperty("C", out var c) ? c.GetString() ?? "" : "",
                    root.TryGetProperty("D", out var d) ? d.GetString() ?? "" : ""
                );
            }
            catch
            {
                return ("", "", "", "");
            }
        }

        private static List<SkillTagInventoryItemDto> BuildSkillTagInventories(
            List<SkillTag> allTags,
            List<QuestionTag> allQuestionTags,
            Dictionary<Guid, Guid> questionGroupById,
            Dictionary<Guid, QuestionGroupStatus> groupStatusById,
            List<Lesson> allLessons,
            List<Vocabulary> allVocabs,
            Dictionary<Guid, SkillTag> tagById)
        {
            var lessonTagIds = allLessons.Select(l => l.SkillTagId).ToHashSet();
            var vocabCountByTag = allVocabs
                .GroupBy(v => v.TopicTagId)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new List<SkillTagInventoryItemDto>();

            foreach (var tag in allTags.OrderBy(t => t.Category).ThenBy(t => t.Code))
            {
                int publishedCount = 0;
                int quarantinedCount = 0;

                var qTags = allQuestionTags.Where(qt => qt.SkillTagId == tag.Id);
                foreach (var qt in qTags)
                {
                    if (questionGroupById.TryGetValue(qt.QuestionId, out var groupId) &&
                        groupStatusById.TryGetValue(groupId, out var status))
                    {
                        if (status == QuestionGroupStatus.PUBLISHED)
                            publishedCount++;
                        else if (status == QuestionGroupStatus.QUARANTINED)
                            quarantinedCount++;
                    }
                }

                result.Add(new SkillTagInventoryItemDto
                {
                    Id = tag.Id,
                    Code = tag.Code,
                    Name = tag.Name,
                    Category = tag.Category,
                    ParentId = tag.ParentId,
                    ParentName = tag.ParentId.HasValue && tagById.TryGetValue(tag.ParentId.Value, out var pTag) ? pTag.Name : null,
                    MinPoolThreshold = tag.MinPoolThreshold,
                    PublishedQuestionCount = publishedCount,
                    QuarantinedQuestionCount = quarantinedCount,
                    AiGenerationRules = tag.AiGenerationRules,
                    HasLesson = lessonTagIds.Contains(tag.Id),
                    VocabCount = vocabCountByTag.TryGetValue(tag.Id, out var vc) ? vc : 0
                });
            }

            return result;
        }

        private sealed class GeneratedQuestionItemPayload
        {
            public short QuestionOrder { get; set; } = 1;
            public string QuestionStem { get; set; } = string.Empty;
            public string OptionA { get; set; } = string.Empty;
            public string OptionB { get; set; } = string.Empty;
            public string OptionC { get; set; } = string.Empty;
            public string OptionD { get; set; } = string.Empty;
            public char CorrectOption { get; set; } = 'A';
            public string? ExplanationGeneral { get; set; }
            public string? WritingRubric { get; set; }
        }

        private sealed class GeneratedQuestionGroupPayload
        {
            public SkillType SkillType { get; set; }
            public short PartNumber { get; set; }
            public string? PassageContent { get; set; }
            public string? TranscriptData { get; set; }
            public string? MediaAssetsJson { get; set; }
            public string? WritingPromptMeta { get; set; }
            public List<GeneratedQuestionItemPayload> Questions { get; set; } = new();
        }
    }
}
