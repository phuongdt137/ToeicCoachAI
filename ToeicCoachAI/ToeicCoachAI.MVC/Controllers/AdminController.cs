using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToeicCoachAI.Model.DTOs.Admin;
using ToeicCoachAI.Model.Enums;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.MVC.Controllers
{
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // =========================================================================
        // PHASE 1: OVERVIEW & SKILL TAGS + AI RULES (UC-16)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetDashboardSummaryAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new AdminDashboardSummaryDto());
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> SkillTags(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetSkillTagManagementPageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new SkillTagManagementPageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSkillTag(SaveSkillTagRequestDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["AdminError"] = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(SkillTags));
            }

            var result = await _adminService.SaveSkillTagAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(SkillTags));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkillTag(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminService.DeleteSkillTagAsync(id, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(SkillTags));
        }

        // =========================================================================
        // PHASE 2: DUAL-AI GENERATION & BLIND VALIDATION PIPELINE (UC-17, UC-18)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> AiPipeline(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetAiPipelinePageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new AiPipelinePageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerAiJob(TriggerAiJobRequestDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || model.TargetSkillTagId == Guid.Empty)
            {
                TempData["AdminError"] = "Vui lòng chọn Nhãn Kỹ năng (SkillTag) và số lượng cụm câu hỏi hợp lệ.";
                return RedirectToAction(nameof(AiPipeline));
            }

            var result = await _adminService.TriggerAiGenerationJobAsync(model, JobTriggerType.ADMIN_MANUAL, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(AiPipeline));
        }

        // =========================================================================
        // PHASE 3: QUARANTINE QUEUE & QUESTION BANK (UC-19, UC-20)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Quarantine(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetQuarantinePageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new QuarantinePageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuarantine(ReviewQuarantineRequestDto model, CancellationToken cancellationToken)
        {
            var result = await _adminService.ApproveAndPublishQuarantineGroupAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(Quarantine));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeGroupStatus(
            Guid groupId,
            QuestionGroupStatus status,
            string returnAction = "Quarantine",
            CancellationToken cancellationToken = default)
        {
            var result = await _adminService.ChangeQuestionGroupStatusAsync(groupId, status, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(returnAction);
        }

        [HttpGet]
        public async Task<IActionResult> QuestionBank(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetQuestionBankPageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new QuestionBankPageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCuratedQuestion(CreateCuratedQuestionRequestDto model, CancellationToken cancellationToken)
        {
            var result = await _adminService.CreateCuratedQuestionGroupAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(QuestionBank));
        }

        // =========================================================================
        // PHASE 4: MOCK TEST BLUEPRINTS & CREDIT / SUBSCRIPTION SETTINGS (UC-21, UC-24)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Blueprints(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetBlueprintManagementPageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new BlueprintManagementPageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBlueprint(SaveBlueprintRequestDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["AdminError"] = "Thông tin Khung Đề thi chưa hợp lệ.";
                return RedirectToAction(nameof(Blueprints));
            }

            var result = await _adminService.SaveBlueprintAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(Blueprints));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAssembleBlueprint(Guid blueprintId, CancellationToken cancellationToken)
        {
            var result = await _adminService.AutoAssembleBlueprintAsync(blueprintId, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(Blueprints));
        }

        [HttpGet]
        public async Task<IActionResult> Settings(CancellationToken cancellationToken)
        {
            var result = await _adminService.GetAdminSettingsPageAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["AdminError"] = result.Message;
                return View(new AdminSettingsPageDto());
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCreditRule(CreditRuleAdminDto model, CancellationToken cancellationToken)
        {
            var result = await _adminService.UpdateCreditRuleAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSubscriptionPlan(SubscriptionPlanAdminDto model, CancellationToken cancellationToken)
        {
            var result = await _adminService.SaveSubscriptionPlanAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["AdminSuccess"] = result.Message;
            }
            else
            {
                TempData["AdminError"] = result.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> AudioProxy([FromQuery] string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text)) return NoContent();
            var cleanText = text.Length > 220 ? text[..220] : text;
            var encoded = Uri.EscapeDataString(cleanText);
            var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=en&q={encoded}";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    return File(stream, "audio/mpeg", enableRangeProcessing: true);
                }
            }
            catch
            {
                // Fallback gracefully
            }

            return NoContent();
        }
    }
}
