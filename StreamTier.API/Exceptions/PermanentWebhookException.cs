namespace StreamTier.API.Exceptions;

/// PermanentWebhookException used to prevent pointless retrying.
public class PermanentWebhookException : Exception
{
    public PermanentWebhookException(string message) : base(message)
    {
    }

    public PermanentWebhookException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
