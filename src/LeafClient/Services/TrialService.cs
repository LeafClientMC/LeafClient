using System;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class TrialService
    {
        public enum BannerKind
        {
            None,
            EligibleNotDismissed,
            ActiveTrial,
            TrialEndedNeedsCta,
        }

        public sealed record EligibilityState(
            bool Eligible,
            string? Reason,
            long? MsUntilEligible);

        public sealed record GrantOutcome(
            bool Granted,
            string? Reason,
            string? LeafPlusTier,
            string? LeafPlusPeriodEnd);

        public static async Task<EligibilityState?> EvaluateAsync(string accessToken)
        {
            string deviceHash;
            try { deviceHash = HwidService.GetDeviceHash(); }
            catch { return null; }

            var resp = await LeafApiService.CheckTrialEligibilityAsync(accessToken, deviceHash);
            if (resp == null) return null;
            return new EligibilityState(resp.Eligible, resp.Reason, resp.MsUntilEligible);
        }

        public static async Task<GrantOutcome?> GrantAsync(string accessToken)
        {
            string deviceHash;
            try { deviceHash = HwidService.GetDeviceHash(); }
            catch { return null; }

            var resp = await LeafApiService.GrantTrialAsync(accessToken, deviceHash);
            if (resp == null) return null;
            return new GrantOutcome(
                resp.Granted,
                resp.Reason,
                resp.LeafPlusTier,
                resp.LeafPlusPeriodEnd);
        }

        public static BannerKind ResolveBannerKind(
            LeafApiBalance? balance,
            bool trialPopupDismissed,
            string? trialEndedSeenForUserId,
            string? currentUserId,
            EligibilityState? eligibility)
        {
            if (balance == null) return BannerKind.None;

            if (balance.IsTrial && balance.IsLeafPlus)
                return BannerKind.ActiveTrial;

            if (balance.HadLeafPlusTrial && !balance.IsLeafPlus)
            {
                if (string.IsNullOrEmpty(currentUserId))
                    return BannerKind.None;
                if (!string.Equals(trialEndedSeenForUserId, currentUserId, StringComparison.Ordinal))
                    return BannerKind.TrialEndedNeedsCta;
                return BannerKind.None;
            }

            if (balance.IsLeafPlus) return BannerKind.None;
            if (balance.HadLeafPlusTrial) return BannerKind.None;
            if (trialPopupDismissed) return BannerKind.None;
            if (eligibility == null) return BannerKind.None;
            if (!eligibility.Eligible) return BannerKind.None;
            return BannerKind.EligibleNotDismissed;
        }

        public static string FriendlyIneligibilityMessage(string? reason)
        {
            return reason switch
            {
                "already_consumed_by_account" => "Your account has already used the free trial.",
                "already_consumed_by_device" => "This device has already used a free trial on another account.",
                "account_too_new" => "Your account is too new - try again in a few hours.",
                "currently_subscribed" => "You're already subscribed to Leaf+.",
                "subscribed_via_lemonsqueezy" => "Trials are only available to new accounts.",
                "invalid_device_hash" => "Could not verify this device. Please try again.",
                "rate_limited" => "Too many trial attempts. Please try again later.",
                _ => "Trial unavailable right now. Please try again later.",
            };
        }

        public static int RemainingDaysFromIso(string? periodEndIso)
        {
            if (string.IsNullOrWhiteSpace(periodEndIso)) return 0;
            if (!DateTime.TryParse(periodEndIso, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var end))
                return 0;
            var diff = end.ToUniversalTime() - DateTime.UtcNow;
            if (diff.TotalSeconds <= 0) return 0;
            return (int)Math.Ceiling(diff.TotalDays);
        }
    }
}
