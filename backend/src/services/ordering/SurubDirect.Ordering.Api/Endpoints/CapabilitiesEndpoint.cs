namespace SurubDirect.Ordering.Api.Endpoints;

public static class CapabilitiesEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/capabilities", () => Results.Ok(new { checkoutEnabled = false, status = "cart-preview", cartPreviewEnabled = true }));
}
