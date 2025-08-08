using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public class PlanService
    {
        public int SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public int ServiceId { get; set; }
        public virtual Service Service { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}