using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ToeicCoachAI.Model.Entities;
using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Repository.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ToeicCoachAiDbContext context)
        {
            var now = DateTimeOffset.UtcNow;

            // 0. Seed Default SYSTEM_ADMIN Account (admin@toeiccoach.ai / Admin@123)
            var adminEmail = "admin@toeiccoach.ai";
            if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
            {
                context.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    PasswordHash = HashPasswordPbkdf2("Admin@123"),
                    FullName = "System Administrator",
                    Role = UserRole.SYSTEM_ADMIN,
                    Tier = UserTier.PAID,
                    CreditBalance = 9999,
                    TargetScoreLr = 990,
                    TargetScoreW = 200,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await context.SaveChangesAsync();
            }

            // 1. Seed Credit Rules (UC-24)
            if (!await context.CreditRules.AnyAsync())
            {
                context.CreditRules.AddRange(
                    new CreditRule { FeatureCode = CreditFeatureCode.CONTEXT_CHAT, CreditCost = 1, RateLimitPerMinute = 15, IsActive = true, UpdatedAt = now },
                    new CreditRule { FeatureCode = CreditFeatureCode.WRITING_GRADE, CreditCost = 3, RateLimitPerMinute = 5, IsActive = true, UpdatedAt = now },
                    new CreditRule { FeatureCode = CreditFeatureCode.COMPETENCY_REPORT, CreditCost = 5, RateLimitPerMinute = 3, IsActive = true, UpdatedAt = now }
                );
                await context.SaveChangesAsync();
            }

            // 2. Seed Subscription Plans (UC-12, UC-24)
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                context.SubscriptionPlans.AddRange(
                    new SubscriptionPlan
                    {
                        Id = Guid.NewGuid(),
                        Code = "PRO_MONTHLY",
                        Name = "Gói PRO Coaching 30 Ngày",
                        PriceVnd = 199000,
                        DurationDays = 30,
                        GrantedCredits = 300,
                        DailyAnalysisLimit = 50,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.NewGuid(),
                        Code = "INTENSIVE_90D",
                        Name = "Gói Intensive Cấp Tốc 90 Ngày",
                        PriceVnd = 499000,
                        DurationDays = 90,
                        GrantedCredits = 1000,
                        DailyAnalysisLimit = 100,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. Seed SkillTags (UC-16)
            if (!await context.SkillTags.AnyAsync())
            {
                var tagP1 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART1_PHOTO",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 1: Mô tả tranh (Photographs)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 1 (Photographs): 1 tranh = 1 câu hỏi. Đề thi in stem: 'Look at the picture marked number X in your test book.' và 4 lựa chọn (A), (B), (C), (D). Audio phát 4 câu mô tả.
Phân loại 3 dạng tranh:
1. Tranh 1 người: Chung chủ ngữ, tập trung vào hành động (V-ing), tư thế, vị trí. Sử dụng từ khái quát thay cho vật cụ thể (tool, equipment, furniture, vehicle).
2. Tranh nhiều người: Phải nghe rõ chủ ngữ (They, Some of the people, One of the men/women) để phân biệt hành động chung cả nhóm vs hành động riêng từng cá nhân.
3. Tranh tả vật/cảnh (không có người): Cấu trúc 'There is/are + N + P', 'N + is/are + adj/P', 'N + have/has been + V_PII'.
BẪY BẮT BUỘC:
- Bẫy bị động HTTD ('is/are being + V_PII'): LUÔN SAI trong tranh tả vật vì ngụ ý có người đang thực hiện hành động.
- Bẫy từ vựng phát âm na ná (Near-homophone): coffee makers ~ coffee, bookshelf ~ book, flowerpot ~ flower.
- Bẫy hành động vs trạng thái: 'putting on' (đang mặc vào) vs 'wearing' (đang mặc sẵn trên người)."
                };
                var tagP2 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART2_QA",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 2: Hỏi và đáp (Question-Response)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 2 (Question-Response): 25 câu hỏi (Q7 - Q31). Đề thi KHÔNG in câu hỏi và đáp án (chỉ in 'Mark your answer on your answer sheet.'). CHỈ CÓ 3 LỰA CHỌN (A), (B), (C) - không có lựa chọn (D).
Phân loại dạng câu hỏi:
1. 5W1H (Who, When, Where, Why, What, How): BẪY LOẠI NGAY nếu đáp án bắt đầu bằng 'Yes' hoặc 'No'.
2. Yes/No & Tag Questions: Trả lời 'Yes/No + giải thích cụ thể', hoặc phản hồi gián tiếp xác nhận tình trạng.
3. Alternative Questions ('...or...'): TUYỆT ĐỐI KHÔNG dùng Yes/No. Chọn A, chọn B, hoặc cả hai / sao cũng được ('Either is fine with me', 'Whichever you prefer', 'Actually, I don't care').
4. Statements (Câu trần thuật): Phản hồi cảm ơn, đồng tình ('Yeah, they're selling quickly'), hoặc đính chính thông tin.
5. Suggestions ('Why don't we...'): BẪY trả lời 'Because...' (hiểu nhầm lời rủ rê thành câu hỏi lý do).
BẪY BẮT BUỘC:
- Bẫy phát âm gần giống (Near-homophone / Sound-alike): sign ~ assign, cart ~ cartridge, tell ~ teller, fork ~ forklift, double ~ trouble, tax ~ taxi, signed ~ resign.
- Bẫy lặp từ khác nghĩa: room (phòng vs chỗ trống), key (chìa khóa vs then chốt).
- Mẫu đáp án gián tiếp xác suất đúng cao: 'I have no idea / You can ask Sue', 'The schedule is being revised'."
                };
                var tagP3 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART3_CONV",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 3: Đoạn hội thoại (Conversations)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 3 (Conversations): 13 đoạn hội thoại, mỗi đoạn gắn liền với CHÙM ĐÚNG 3 CÂU HỎI liên tiếp (Q32-34, Q35-37...). Câu hỏi và cả 4 lựa chọn (A, B, C, D) ĐƯỢC IN trên đề thi.
Cấu trúc kịch bản: 4-6 lượt thoại công sở (General Office, Facility, Personnel, Shopping, Catering/Events, Order & Delivery).
QUY TẮC TUẦN TỰ 3 CÂU HỎI (TRIAD):
- Câu 1: Chủ đề / Mục đích / Địa điểm ('What are the speakers discussing?', 'Where does the conversation take place?') -> Nằm ở 1-2 câu đầu.
- Câu 2: Chi tiết cụ thể / Sự cố / Danh tính người nói ('What problem does the woman mention?', 'Who most likely is the man?') -> Nằm ở giữa đoạn.
- Câu 3: Đề nghị / Yêu cầu / Hành động tiếp theo ('What will the man probably do next?', 'What does the woman suggest?') -> Nằm ở cuối đoạn.
BẪY BẮT BUỘC:
- Kỹ thuật Paraphrase: Đáp án đúng dùng từ đồng nghĩa / danh từ chung (vd: 'has run out' -> 'have been used up').
- Bẫy lặp từ khóa: Phương án nhiễu bê nguyên từ khóa trong bài nhưng ghép sai quan hệ ngữ pháp.
- Bắt buộc trích dẫn bằng chứng dòng thoại (1), (2), (3) trong phần giải thích."
                };
                var tagP4 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART4_TALKS",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 4: Bài nói ngắn (Short Talks)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 4 (Short Talks): 10 bài nói ngắn, mỗi bài chùm đúng 3 câu hỏi (Q71-73...). Bài độc thoại 90-130 từ (thông báo nội bộ kho bãi/văn phòng, tin nhắn thoại, thông báo sân bay/nhà ga).
Tuần tự 3 câu hỏi:
- Câu 1: Đối tượng người nghe / Mục đích thông báo ('Who is the announcement for?', 'What is the speaker announcing?').
- Câu 2: Chi tiết mốc thời gian, lý do sửa chữa/hoãn chuyến, hoặc quy định mới.
- Câu 3: Hành động người nghe được yêu cầu thực hiện ('What are listeners asked to do?').
Bẫy lặp từ cục bộ trong khi ngữ cảnh toàn câu sai lệch so với bài nói."
                };
                var tagP5Rel = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART5_REL_CLAUSE",
                    Category = SkillCategory.GRAMMAR,
                    Name = "Part 5: Mệnh đề quan hệ (Relative Clauses)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 5 Grammar: Câu đơn bối cảnh kinh doanh có 1 chỗ trống kiểm tra đại từ quan hệ (who, whom, whose, which, that) hoặc rút gọn mệnh đề quan hệ dạng chủ động (V-ing) / bị động (V-ed/PII).
Yêu cầu:
- Bẫy cấu trúc xen giữa danh từ chính và đại từ quan hệ.
- Phân biệt chặt chẽ whose + N (sở hữu) vs who + V.
- Duy nhất 1 đáp án đúng hoàn toàn ngữ pháp, giải thích rõ thành phần câu."
                };
                var tagP5Tense = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART5_TENSES",
                    Category = SkillCategory.GRAMMAR,
                    Name = "Part 5: Thì động từ & Hòa hợp chủ vị",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 5 Grammar: Kiểm tra thì động từ và sự hòa hợp chủ ngữ - vị ngữ trong văn cảnh doanh nghiệp quốc tế.
Dấu hiệu nhận biết: By the time, since, over the past decade, before + past simple.
Bẫy cụm giới từ xen giữa (Prepositional Phrase): Chủ ngữ số ít ngăn cách bởi cụm giới từ dài với các danh từ số nhiều đi kèm làm nhiễu dạng số của động từ."
                };
                var tagP6 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART6_TEXT_COMP",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 6: Hoàn thành đoạn văn (Text Completion)",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 6: Đoạn văn thương mại (Email, Memo, Thông cáo báo chí, Hướng dẫn nội bộ) 130-160 từ gồm 4 chỗ trống [1], [2], [3], [4].
Bắt buộc gồm:
- 1 câu hỏi từ vựng (Vocabulary in context)
- 1 câu hỏi ngữ pháp (Thì động từ / Liên từ liên kết however, therefore, in addition)
- 1 câu hỏi dạng từ (Word form)
- 1 câu hỏi điền cả câu văn hoàn chỉnh (Sentence Insertion) đòi hỏi mạch logic trước sau."
                };
                var tagP7 = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "PART7_READING_COMP",
                    Category = SkillCategory.PART_SKILL,
                    Name = "Part 7: Đọc hiểu Email & Thông báo",
                    MinPoolThreshold = 50,
                    AiGenerationRules = @"Chuẩn ETS TOEIC Part 7: Văn bản đọc hiểu đơn hoặc kép (Single/Double Passages: Email trao đổi công việc, thông báo sự kiện, hóa đơn kèm thư yêu cầu).
Chùm 3-4 câu hỏi:
- Câu 1: Mục đích chính của tài liệu (Main purpose / Why is Mr. X writing?)
- Câu 2-3: Thông tin chi tiết, điều kiện áp dụng, chính sách công ty
- Câu 4: Suy luận hành động tiếp theo hoặc câu hỏi từ đồng nghĩa (In the email, the word '...' is closest in meaning to...)
Áp dụng kỹ thuật Paraphrase triệt để giữa văn bản và đáp án đúng."
                };
                var tagWritingEmail = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "WRITING_EMAIL",
                    Category = SkillCategory.WRITING_SKILL,
                    Name = "Writing Part 2: Phản hồi Email công việc",
                    MinPoolThreshold = 30,
                    AiGenerationRules = @"Chuẩn TOEIC Writing Questions 6-7 (Respond to an Email):
Đề bài cung cấp 1 email công việc nhận được kèm chỉ dẫn (Directions) yêu cầu phản hồi thực hiện chính xác 3 nhiệm vụ cụ thể (vd: giải thích 2 lý do và đề xuất 1 giải pháp).
Tiêu chí chấm điểm ETS (Thang 0-4):
- Hoàn thành đầy đủ cả 3 nhiệm vụ (Task Fulfillment)
- Sử dụng liên từ chuyển ý mạch lạc (Furthermore, As a result, Consequently)
- Đúng văn phong và định dạng email thương mại trang trọng."
                };
                var tagWritingEssay = new SkillTag
                {
                    Id = Guid.NewGuid(),
                    Code = "WRITING_ESSAY",
                    Category = SkillCategory.WRITING_SKILL,
                    Name = "Writing Part 3: Viết luận nêu quan điểm (Opinion Essay)",
                    MinPoolThreshold = 30,
                    AiGenerationRules = "Tạo câu hỏi nghị luận công sở/xã hội yêu cầu viết bài luận tối thiểu 300 từ nêu quan điểm kèm tiêu chí chấm điểm Rubric (Grammar, Vocabulary, Organization, Task Fulfillment)."
                };

                var tagVocabOffice = new SkillTag { Id = Guid.NewGuid(), Code = "VOCAB_OFFICE", Category = SkillCategory.VOCAB_TOPIC, Name = "Văn phòng", MinPoolThreshold = 50, AiGenerationRules = "Chủ đề từ vựng tiếng Anh văn phòng, họp hành, hành chính nhân sự." };
                var tagVocabFinance = new SkillTag { Id = Guid.NewGuid(), Code = "VOCAB_FINANCE", Category = SkillCategory.VOCAB_TOPIC, Name = "Tài chính", MinPoolThreshold = 50, AiGenerationRules = "Chủ đề từ vựng kế toán, ngân sách, hóa đơn, đầu tư." };
                var tagVocabLogistics = new SkillTag { Id = Guid.NewGuid(), Code = "VOCAB_LOGISTICS", Category = SkillCategory.VOCAB_TOPIC, Name = "Logistics", MinPoolThreshold = 50, AiGenerationRules = "Chủ đề từ vựng chuỗi cung ứng, kho bãi, vận chuyển hàng hóa." };
                var tagVocabHr = new SkillTag { Id = Guid.NewGuid(), Code = "VOCAB_HR", Category = SkillCategory.VOCAB_TOPIC, Name = "Tuyển dụng", MinPoolThreshold = 50, AiGenerationRules = "Chủ đề từ vựng tuyển dụng, phỏng vấn, phúc lợi nhân viên." };

                context.SkillTags.AddRange(
                    tagP1, tagP2, tagP3, tagP4,
                    tagP5Rel, tagP5Tense, tagP6, tagP7,
                    tagWritingEmail, tagWritingEssay,
                    tagVocabOffice, tagVocabFinance, tagVocabLogistics, tagVocabHr
                );
                await context.SaveChangesAsync();

                // 4. Seed Lessons (UC-03, UC-16)
                context.Lessons.AddRange(
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        SkillTagId = tagP5Rel.Id,
                        PartNumber = 5,
                        Title = "Part 5: Mệnh đề quan hệ & Rút gọn mệnh đề",
                        Slug = "part-5-menh-de-quan-he",
                        TheoryMarkdown = "### 1. Đại từ quan hệ trong TOEIC Part 5\nPhân biệt **who, whom, whose, which, that** và dấu hiệu nhận biết mệnh đề quan hệ rút gọn (V-ing / V3-ed).",
                        OrderIndex = 1,
                        IsFreePreview = true
                    },
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        SkillTagId = tagP4.Id,
                        PartNumber = 4,
                        Title = "Part 4: Chiến thuật bắt từ khóa Bài nói ngắn",
                        Slug = "part-4-chien-thuat-bai-noi-ngan",
                        TheoryMarkdown = "### Kỹ thuật đọc trước câu hỏi (Previewing)\nCách nắm bắt ý chính ngay trong 2 câu đầu tiên của đoạn thông báo hoặc tin nhắn thoại.",
                        OrderIndex = 2,
                        IsFreePreview = true
                    },
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        SkillTagId = tagWritingEmail.Id,
                        PartNumber = 8,
                        Title = "Writing: Cấu trúc chuẩn Email thương mại đạt điểm tối đa",
                        Slug = "writing-cau-truc-email-thuong-mai",
                        TheoryMarkdown = "### Khung dàn ý 4 phần cho TOEIC Writing Q6-7\nCách mở đầu trang trọng, trả lời đủ 3 yêu cầu trong đề bài và kết thư chuyên nghiệp.",
                        OrderIndex = 3,
                        IsFreePreview = false
                    }
                );

                // 5. Seed Vocabularies (UC-05, UC-16)
                context.Vocabularies.AddRange(
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabOffice.Id, Word = "negotiate", Pos = "v.", Phonetic = "/nɪˈɡoʊ.ʃi.eɪt/", MeaningVi = "Đàm phán, thương lượng", ExampleEn = "We need to negotiate a better contract with the supplier.", ExampleVi = "Chúng ta cần đàm phán một hợp đồng tốt hơn với nhà cung cấp." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabFinance.Id, Word = "invoice", Pos = "n.", Phonetic = "/ˈɪn.vɔɪs/", MeaningVi = "Hóa đơn thanh toán", ExampleEn = "Please settle the attached invoice within 15 business days.", ExampleVi = "Vui lòng thanh toán hóa đơn đính kèm trong vòng 15 ngày làm việc." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabOffice.Id, Word = "deadline", Pos = "n.", Phonetic = "/ˈded.laɪn/", MeaningVi = "Hạn chót hoàn thành", ExampleEn = "The deadline for submitting the quarterly report is Friday.", ExampleVi = "Hạn chót nộp báo cáo quý là thứ Sáu." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabFinance.Id, Word = "reimburse", Pos = "v.", Phonetic = "/ˌriː.ɪmˈbɝːs/", MeaningVi = "Hoàn trả chi phí (công tác phí)", ExampleEn = "The company will reimburse all travel expenses upon receipt.", ExampleVi = "Công ty sẽ hoàn trả mọi chi phí đi lại khi có biên lai." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabLogistics.Id, Word = "inventory", Pos = "n.", Phonetic = "/ˈɪn.vən.tɔːr.i/", MeaningVi = "Hàng tồn kho, bảng kiểm kê", ExampleEn = "Our warehouse staff conducts a full inventory check every month.", ExampleVi = "Nhân viên kho của chúng tôi thực hiện kiểm kê toàn bộ hàng tồn mỗi tháng." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabOffice.Id, Word = "colleague", Pos = "n.", Phonetic = "/ˈkɑː.liːɡ/", MeaningVi = "Đồng nghiệp", ExampleEn = "She discussed the marketing proposal with her colleagues.", ExampleVi = "Cô ấy đã thảo luận đề xuất tiếp thị với các đồng nghiệp." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabFinance.Id, Word = "quarterly", Pos = "adj.", Phonetic = "/ˈkwɔːr.t̬ɚ.li/", MeaningVi = "Hàng quý (3 tháng 1 lần)", ExampleEn = "Quarterly revenue exceeded our initial projections by 12%.", ExampleVi = "Doanh thu hàng quý vượt dự kiến ban đầu của chúng tôi 12%." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabOffice.Id, Word = "agenda", Pos = "n.", Phonetic = "/əˈdʒen.də/", MeaningVi = "Chương trình nghị sự, nội dung cuộc họp", ExampleEn = "Budget allocation is the first item on today's meeting agenda.", ExampleVi = "Phân bổ ngân sách là mục đầu tiên trong chương trình họp hôm nay." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabFinance.Id, Word = "renew", Pos = "v.", Phonetic = "/rɪˈnuː/", MeaningVi = "Gia hạn (hợp đồng, thẻ thành viên)", ExampleEn = "Tenants must notify the landlord 30 days in advance to renew the lease.", ExampleVi = "Người thuê phải báo trước 30 ngày để gia hạn hợp đồng thuê." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabHr.Id, Word = "applicant", Pos = "n.", Phonetic = "/ˈæp.lɪ.kənt/", MeaningVi = "Ứng viên xin việc", ExampleEn = "All qualified applicants will be invited for a second interview.", ExampleVi = "Tất cả ứng viên đủ điều kiện sẽ được mời phỏng vấn vòng hai." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabLogistics.Id, Word = "shipment", Pos = "n.", Phonetic = "/ˈʃɪp.mənt/", MeaningVi = "Lô hàng gửi đi, việc vận chuyển", ExampleEn = "The shipment of electronic components was delayed due to weather.", ExampleVi = "Lô hàng linh kiện điện tử bị trễ do thời tiết." },
                    new Vocabulary { Id = Guid.NewGuid(), TopicTagId = tagVocabOffice.Id, Word = "attend", Pos = "v.", Phonetic = "/əˈtend/", MeaningVi = "Tham dự (cuộc họp, hội thảo)", ExampleEn = "Over 200 sales representatives attended the annual conference.", ExampleVi = "Hơn 200 đại diện kinh doanh đã tham dự hội nghị thường niên." }
                );

                // 6. Seed MockTestBlueprints (UC-01, UC-06, UC-21)
                context.MockTestBlueprints.AddRange(
                    new MockTestBlueprint
                    {
                        Id = Guid.NewGuid(),
                        Title = "Bài Kiểm Tra Chẩn Đoán Đầu Vào (Placement Test)",
                        TestType = BlueprintTestType.GUEST_PLACEMENT,
                        DurationMinutes = 35,
                        QuestionGroupIds = new List<Guid>(),
                        IsActive = true
                    },
                    new MockTestBlueprint
                    {
                        Id = Guid.NewGuid(),
                        Title = "Bài Kiểm Tra Chẩn Đoán Chuyên Sâu (Paid Diagnostic)",
                        TestType = BlueprintTestType.PAID_DIAGNOSTIC,
                        DurationMinutes = 60,
                        QuestionGroupIds = new List<Guid>(),
                        IsActive = true
                    },
                    new MockTestBlueprint
                    {
                        Id = Guid.NewGuid(),
                        Title = "Đề Thi Thử TOEIC Chuẩn ETS 2026 - Test 01 (Full 200 Câu L&R)",
                        TestType = BlueprintTestType.FULL_MOCK_LR,
                        DurationMinutes = 120,
                        QuestionGroupIds = new List<Guid>(),
                        IsActive = true
                    },
                    new MockTestBlueprint
                    {
                        Id = Guid.NewGuid(),
                        Title = "Đề Thi Thử TOEIC Writing Toàn Diện - Test 01 (Q1-Q8)",
                        TestType = BlueprintTestType.FULL_MOCK_WRITING,
                        DurationMinutes = 60,
                        QuestionGroupIds = new List<Guid>(),
                        IsActive = true
                    }
                );

                await context.SaveChangesAsync();
            }
            else
            {
                // Backfill AiGenerationRules on existing SkillTags if null or outdated
                var existingTags = await context.SkillTags.ToListAsync();
                bool hasTagUpdates = false;
                foreach (var t in existingTags)
                {
                    var newRule = GetDefaultRuleForTagCode(t.Code);
                    if (!string.IsNullOrWhiteSpace(newRule) && (string.IsNullOrWhiteSpace(t.AiGenerationRules) || t.AiGenerationRules.Length < 160))
                    {
                        t.AiGenerationRules = newRule;
                        hasTagUpdates = true;
                    }
                }
                if (hasTagUpdates)
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        private static string GetDefaultRuleForTagCode(string code) => code switch
        {
            "PART1_PHOTO" => @"Chuẩn ETS TOEIC Part 1 (Photographs): 1 tranh = 1 câu hỏi. Đề thi in stem: 'Look at the picture marked number X in your test book.' và 4 lựa chọn (A), (B), (C), (D). Audio phát 4 câu mô tả.
Phân loại 3 dạng tranh:
1. Tranh 1 người: Chung chủ ngữ, tập trung vào hành động (V-ing), tư thế, vị trí. Sử dụng từ khái quát thay cho vật cụ thể (tool, equipment, furniture, vehicle).
2. Tranh nhiều người: Phải nghe rõ chủ ngữ (They, Some of the people, One of the men/women) để phân biệt hành động chung cả nhóm vs hành động riêng từng cá nhân.
3. Tranh tả vật/cảnh (không có người): Cấu trúc 'There is/are + N + P', 'N + is/are + adj/P', 'N + have/has been + V_PII'.
BẪY BẮT BUỘC:
- Bẫy bị động HTTD ('is/are being + V_PII'): LUÔN SAI trong tranh tả vật vì ngụ ý có người đang thực hiện hành động.
- Bẫy từ vựng phát âm na ná (Near-homophone): coffee makers ~ coffee, bookshelf ~ book, flowerpot ~ flower.
- Bẫy hành động vs trạng thái: 'putting on' (đang mặc vào) vs 'wearing' (đang mặc sẵn trên người).",

            "PART2_QA" => @"Chuẩn ETS TOEIC Part 2 (Question-Response): 25 câu hỏi (Q7 - Q31). Đề thi KHÔNG in câu hỏi và đáp án (chỉ in 'Mark your answer on your answer sheet.'). CHỈ CÓ 3 LỰA CHỌN (A), (B), (C) - không có lựa chọn (D).
Phân loại dạng câu hỏi:
1. 5W1H (Who, When, Where, Why, What, How): BẪY LOẠI NGAY nếu đáp án bắt đầu bằng 'Yes' hoặc 'No'.
2. Yes/No & Tag Questions: Trả lời 'Yes/No + giải thích cụ thể', hoặc phản hồi gián tiếp xác nhận tình trạng.
3. Alternative Questions ('...or...'): TUYỆT ĐỐI KHÔNG dùng Yes/No. Chọn A, chọn B, hoặc cả hai / sao cũng được ('Either is fine with me', 'Whichever you prefer', 'Actually, I don't care').
4. Statements (Câu trần thuật): Phản hồi cảm ơn, đồng tình ('Yeah, they're selling quickly'), hoặc đính chính thông tin.
5. Suggestions ('Why don't we...'): BẪY trả lời 'Because...' (hiểu nhầm lời rủ rê thành câu hỏi lý do).
BẪY BẮT BUỘC:
- Bẫy phát âm gần giống (Near-homophone / Sound-alike): sign ~ assign, cart ~ cartridge, tell ~ teller, fork ~ forklift, double ~ trouble, tax ~ taxi, signed ~ resign.
- Bẫy lặp từ khác nghĩa: room (phòng vs chỗ trống), key (chìa khóa vs then chốt).
- Mẫu đáp án gián tiếp xác suất đúng cao: 'I have no idea / You can ask Sue', 'The schedule is being revised'.",

            "PART3_CONV" => @"Chuẩn ETS TOEIC Part 3 (Conversations): 13 đoạn hội thoại, mỗi đoạn gắn liền với CHÙM ĐÚNG 3 CÂU HỎI liên tiếp (Q32-34, Q35-37...). Câu hỏi và cả 4 lựa chọn (A, B, C, D) ĐƯỢC IN trên đề thi.
Cấu trúc kịch bản: 4-6 lượt thoại công sở (General Office, Facility, Personnel, Shopping, Catering/Events, Order & Delivery).
QUY TẮC TUẦN TỰ 3 CÂU HỎI (TRIAD):
- Câu 1: Chủ đề / Mục đích / Địa điểm ('What are the speakers discussing?', 'Where does the conversation take place?') -> Nằm ở 1-2 câu đầu.
- Câu 2: Chi tiết cụ thể / Sự cố / Danh tính người nói ('What problem does the woman mention?', 'Who most likely is the man?') -> Nằm ở giữa đoạn.
- Câu 3: Đề nghị / Yêu cầu / Hành động tiếp theo ('What will the man probably do next?', 'What does the woman suggest?') -> Nằm ở cuối đoạn.
BẪY BẮT BUỘC:
- Kỹ thuật Paraphrase: Đáp án đúng dùng từ đồng nghĩa / danh từ chung (vd: 'has run out' -> 'have been used up').
- Bẫy lặp từ khóa: Phương án nhiễu bê nguyên từ khóa trong bài nhưng ghép sai quan hệ ngữ pháp.
- Bắt buộc trích dẫn bằng chứng dòng thoại (1), (2), (3) trong phần giải thích.",

            "PART4_TALKS" => @"Chuẩn ETS TOEIC Part 4 (Short Talks): 10 bài nói ngắn, mỗi bài chùm đúng 3 câu hỏi (Q71-73...). Bài độc thoại 90-130 từ (thông báo nội bộ kho bãi/văn phòng, tin nhắn thoại, thông báo sân bay/nhà ga).
Tuần tự 3 câu hỏi:
- Câu 1: Đối tượng người nghe / Mục đích thông báo ('Who is the announcement for?', 'What is the speaker announcing?').
- Câu 2: Chi tiết mốc thời gian, lý do sửa chữa/hoãn chuyến, hoặc quy định mới.
- Câu 3: Hành động người nghe được yêu cầu thực hiện ('What are listeners asked to do?').
Bẫy lặp từ cục bộ trong khi ngữ cảnh toàn câu sai lệch so với bài nói.",

            "PART5_REL_CLAUSE" => @"Chuẩn ETS TOEIC Part 5 Grammar: Câu đơn bối cảnh kinh doanh có 1 chỗ trống kiểm tra đại từ quan hệ (who, whom, whose, which, that) hoặc rút gọn mệnh đề quan hệ dạng chủ động (V-ing) / bị động (V-ed/PII).
Yêu cầu:
- Bẫy cấu trúc xen giữa danh từ chính và đại từ quan hệ.
- Phân biệt chặt chẽ whose + N (sở hữu) vs who + V.
- Duy nhất 1 đáp án đúng hoàn toàn ngữ pháp, giải thích rõ thành phần câu.",

            "PART5_TENSES" => @"Chuẩn ETS TOEIC Part 5 Grammar: Kiểm tra thì động từ và sự hòa hợp chủ ngữ - vị ngữ trong văn cảnh doanh nghiệp quốc tế.
Dấu hiệu nhận biết: By the time, since, over the past decade, before + past simple.
Bẫy cụm giới từ xen giữa (Prepositional Phrase): Chủ ngữ số ít ngăn cách bởi cụm giới từ dài với các danh từ số nhiều đi kèm làm nhiễu dạng số của động từ.",

            "PART6_TEXT_COMP" => @"Chuẩn ETS TOEIC Part 6: Đoạn văn thương mại (Email, Memo, Thông cáo báo chí, Hướng dẫn nội bộ) 130-160 từ gồm 4 chỗ trống [1], [2], [3], [4].
Bắt buộc gồm:
- 1 câu hỏi từ vựng (Vocabulary in context)
- 1 câu hỏi ngữ pháp (Thì động từ / Liên từ liên kết however, therefore, in addition)
- 1 câu hỏi dạng từ (Word form)
- 1 câu hỏi điền cả câu văn hoàn chỉnh (Sentence Insertion) đòi hỏi mạch logic trước sau.",

            "PART7_READING_COMP" => @"Chuẩn ETS TOEIC Part 7: Văn bản đọc hiểu đơn hoặc kép (Single/Double Passages: Email trao đổi công việc, thông báo sự kiện, hóa đơn kèm thư yêu cầu).
Chùm 3-4 câu hỏi:
- Câu 1: Mục đích chính của tài liệu (Main purpose / Why is Mr. X writing?)
- Câu 2-3: Thông tin chi tiết, điều kiện áp dụng, chính sách công ty
- Câu 4: Suy luận hành động tiếp theo hoặc câu hỏi từ đồng nghĩa (In the email, the word '...' is closest in meaning to...)
Áp dụng kỹ thuật Paraphrase triệt để giữa văn bản và đáp án đúng.",

            "WRITING_EMAIL" => @"Chuẩn TOEIC Writing Questions 6-7 (Respond to an Email):
Đề bài cung cấp 1 email công việc nhận được kèm chỉ dẫn (Directions) yêu cầu phản hồi thực hiện chính xác 3 nhiệm vụ cụ thể (vd: giải thích 2 lý do và đề xuất 1 giải pháp).
Tiêu chí chấm điểm ETS (Thang 0-4):
- Hoàn thành đầy đủ cả 3 nhiệm vụ (Task Fulfillment)
- Sử dụng liên từ chuyển ý mạch lạc (Furthermore, As a result, Consequently)
- Đúng văn phong và định dạng email thương mại trang trọng.",

            "WRITING_ESSAY" => @"Tạo câu hỏi nghị luận công sở/xã hội yêu cầu viết bài luận tối thiểu 300 từ nêu quan điểm kèm tiêu chí chấm điểm Rubric (Grammar, Vocabulary, Organization, Task Fulfillment).",
            _ => "Sinh câu hỏi chuẩn định dạng ETS TOEIC Collection 2026, bối cảnh thương mại quốc tế, chỉ có duy nhất 1 đáp án đúng hoàn toàn."
        };

        private static string HashPasswordPbkdf2(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
