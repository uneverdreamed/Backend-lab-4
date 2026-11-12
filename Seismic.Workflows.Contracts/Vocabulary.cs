namespace Seismic.Workflows.Contracts;

public static class SeverityLevels
{
    public const string Strong = "strong";
    public const string Moderate = "moderate";
    public const string Light = "light";
}

public static class ProcessingStages
{
    public const string Started = "started";
    public const string Classifying = "classifying";
    public const string CollectingData = "collecting-data";
    public const string AwaitingConfirmation = "awaiting-confirmation";
    public const string Escalating = "escalating";
    public const string Notifying = "notifying";
    public const string Publishing = "publishing";
    public const string Completed = "completed";
    public const string Rejected = "rejected";
}

public static class ProcessingOutcomes
{
    public const string Published = "published";
    public const string PublishedUnconfirmed = "published-unconfirmed";
    public const string Rejected = "rejected";
}

public static class ConfirmationStatuses
{
    public const string NotRequired = "not-required";
    public const string Confirmed = "confirmed";
    public const string TimeoutEscalated = "timeout-escalated";
}
