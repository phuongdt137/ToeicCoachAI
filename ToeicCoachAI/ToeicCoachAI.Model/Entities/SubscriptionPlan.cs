namespace ToeicCoachAI.Model.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long PriceVnd { get; set; }
        public int DurationDays { get; set; }
        public int GrantedCredits { get; set; }
        public int DailyAnalysisLimit { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}
