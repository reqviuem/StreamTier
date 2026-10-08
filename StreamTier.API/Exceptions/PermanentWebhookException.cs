namespace StreamTier.API.Exceptions;

/// PermanentWebhookException used to prevent pointless retrying.
/// The controller logs it and acknowledges the event; any other exception results in a 500 so Stripe retries.

public class PermanentWebhookException : Exception
{
    public PermanentWebhookException(string message) : base(message)
    {
    }

    public PermanentWebhookException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
