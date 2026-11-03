namespace SurubDirect.Ordering.Contracts;
public sealed record AdminOrderDetailDto(DemoOrderDto Order, ReservationPlanDto Reservation, string? CancellationReason, string? CancelledBy, DateTimeOffset? CancellationRequestedAt, DateTimeOffset? CancelledAt, OrderProcessingDto Processing, PaymentSimulationDto PaymentSimulation, ShipmentDto Shipment, OrderReturnDto Return);
