namespace SurubDirect.Identity.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        CsrfEndpoint.Map(app);
        LoginEndpoint.Map(app);
        LogoutEndpoint.Map(app);
        CurrentUserEndpoint.Map(app);
        SessionEndpoint.Map(app, app.Configuration);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
