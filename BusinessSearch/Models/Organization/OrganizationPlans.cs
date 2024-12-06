using System;

namespace BusinessSearch.Models.Organization
{
    public enum OrganizationPlan
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Unlimited = 3
    }

    public static class OrganizationPlanExtensions
    {
        public static int GetDailySearchLimit(this OrganizationPlan plan)
            => plan switch
            {
                OrganizationPlan.Bronze => 100,
                OrganizationPlan.Silver => 300,
                OrganizationPlan.Gold => 500,
                OrganizationPlan.Unlimited => int.MaxValue,
                _ => throw new ArgumentException($"Unknown plan type: {plan}")
            };

        public static int GetDailySearchResultsLimit(this OrganizationPlan plan)
            => plan switch
            {
                OrganizationPlan.Bronze => 100,
                OrganizationPlan.Silver => 300,
                OrganizationPlan.Gold => 500,
                OrganizationPlan.Unlimited => int.MaxValue,
                _ => throw new ArgumentException($"Unknown plan type: {plan}")
            };

        public static string GetDisplayName(this OrganizationPlan plan)
            => plan switch
            {
                OrganizationPlan.Bronze => "Bronze (100 searches/day)",
                OrganizationPlan.Silver => "Silver (300 searches/day)",
                OrganizationPlan.Gold => "Gold (500 searches/day)",
                OrganizationPlan.Unlimited => "Unlimited Searches",
                _ => throw new ArgumentException($"Unknown plan type: {plan}")
            };
    }
}