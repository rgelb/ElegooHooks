namespace ElegooLink.Events;

public sealed class ElegooLinkException : Exception
{
    public ElegooLinkException(string message, int? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public int? ErrorCode { get; }
}
