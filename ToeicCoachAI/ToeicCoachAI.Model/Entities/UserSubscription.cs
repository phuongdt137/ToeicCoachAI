using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class UserSubscription
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PlanId { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PENDING;
        public string PaymentOrderCode { get; set; } = string.Empty;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        public User User { get; set; } = null!;
        public SubscriptionPlan Plan { get; set; } = null!;
    }
}
