namespace ApiMonitor.Domain.Enums;

public enum CheckErrorType
{
    Timeout,
    DnsError,
    ConnectionError,
    UnexpectedStatusCode
}
