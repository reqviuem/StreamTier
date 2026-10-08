namespace StreamTier.API.Exceptions;

/// <summary>
/// A Stripe webhook event that can never be processed successfully, so retrying it is pointless.
/// The controller logs it and acknowledges the event; any other exception results in a 500 so Stripe retries.
/// </summary>
public class PermanentWebhookException : Exception
{
    public PermanentWebhookException(string message) : base(message)
    {
    }

    public PermanentWebhookException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
