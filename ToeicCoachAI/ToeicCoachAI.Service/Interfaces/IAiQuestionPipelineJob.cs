namespace ToeicCoachAI.Service.Interfaces
{
    public interface IAiQuestionPipelineJob
    {
        /// <summary>
        /// UC-17 &amp; UC-18: Quét độ phủ kho đề (Question Pool) và kích hoạt luồng Dual-Agent (Generator + Validator + TTS).
        /// </summary>
        Task ExecuteAutoRefillAsync(CancellationToken cancellationToken = default);
    }
}
