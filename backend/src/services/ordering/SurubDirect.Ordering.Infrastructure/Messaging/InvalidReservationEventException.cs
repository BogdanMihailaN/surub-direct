namespace SurubDirect.Ordering.Infrastructure;
public sealed class InvalidReservationEventException : Exception
{
    public InvalidReservationEventException() : base("Invalid reservation event.")
    {
    }
}
