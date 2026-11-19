using System.Text.Json.Serialization;

namespace SurubDirect.Catalog.Api.Clients;

internal sealed record InventoryStockRow(
    [property: JsonRequired] Guid VariantId,
    [property: JsonRequired] long OnHand,
    [property: JsonRequired] long Reserved,
    [property: JsonRequired] long Available);
