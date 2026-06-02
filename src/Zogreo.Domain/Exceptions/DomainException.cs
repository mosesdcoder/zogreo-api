namespace Zogreo.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public class InvalidStateTransitionException(string from, string to)
    : DomainException($"Cannot transition from {from} to {to}.") { }
