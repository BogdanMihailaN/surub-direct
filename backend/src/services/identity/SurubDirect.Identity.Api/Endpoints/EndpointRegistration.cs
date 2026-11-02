namespace SurubDirect.Identity.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        CsrfEndpoint.Map(app);
        LoginEndpoint.Map(app);
        RegisterCustomerEndpoint.Map(app);
        LogoutEndpoint.Map(app);
        RequestEmailConfirmationEndpoint.Map(app);
        RequestPasswordResetEndpoint.Map(app);
        ConfirmEmailEndpoint.Map(app);
        ResetPasswordEndpoint.Map(app);

        CurrentUserEndpoint.Map(app);
        SessionEndpoint.Map(app, app.Configuration);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
