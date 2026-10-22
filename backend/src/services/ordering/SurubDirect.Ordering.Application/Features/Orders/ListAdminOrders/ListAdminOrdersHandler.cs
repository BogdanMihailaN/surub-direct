using System.Globalization;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Application;
public sealed class ListAdminOrdersHandler(IAdminDemoOrders orders)
{
    public Task<AdminOrderPageDto> Handle(string? status, string? number, int page, CancellationToken ct)
    {
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (page is < 1 or > 100000 || status is not (null or "Confirming" or "Confirmed" or "Failed" or "Cancelling" or "Cancelled"))
            throw new BusinessException("Filtrul sau pagina nu sunt valide.");
        long? sequence = null;
        if (!string.IsNullOrWhiteSpace(number))
        {
            var value = number.Trim();
            if (value.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase))
                value = value[5..];
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                throw new BusinessException("Introdu numărul complet al comenzii, de exemplu DEMO-00000001.");
            sequence = parsed;
        }

        return orders.Read(status, sequence, page, ct);
    }
}
