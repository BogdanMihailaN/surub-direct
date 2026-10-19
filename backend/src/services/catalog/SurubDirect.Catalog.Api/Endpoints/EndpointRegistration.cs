namespace SurubDirect.Catalog.Api.Endpoints;
public static class EndpointRegistration
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        GetPublicImageEndpoint.Map(app);
        ListCategoriesEndpoint.Map(app);
        GetTaxonomyEndpoint.Map(app);
        ListProductsEndpoint.Map(app);
        GetProductEndpoint.Map(app);
        VariantExistsEndpoint.Map(app, app.Configuration);
        FindPurchasingVariantEndpoint.Map(app, app.Configuration);
        SearchPurchasingVariantsEndpoint.Map(app, app.Configuration);
        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization("Admin");
        PreviewImportEndpoint.Map(admin);
        CommitImportEndpoint.Map(admin);
        GetImportTemplateEndpoint.Map(admin);
        UploadImageEndpoint.Map(admin);
        UpdateImageEndpoint.Map(admin);
        ArrangeImagesEndpoint.Map(admin);
        DeleteImageEndpoint.Map(admin);
        GetAdminImageEndpoint.Map(admin);
        ListAdminProductsEndpoint.Map(admin);
        GetAdminProductEndpoint.Map(admin);
        UpdateProductEndpoint.Map(admin);
        ArchiveProductEndpoint.Map(admin);
        RestoreProductEndpoint.Map(admin);
        UpdateVariantEndpoint.Map(admin);
        UpdateOfferEndpoint.Map(admin);
        GetAdminTaxonomyEndpoint.Map(admin);
        CreateCategoryEndpoint.Map(admin);
        UpdateCategoryEndpoint.Map(admin);
        CreateAttributeEndpoint.Map(admin);
        UpdateAttributeEndpoint.Map(admin);
        SetVariantAttributesEndpoint.Map(admin);
        CreateProductEndpoint.Map(admin);
        AddVariantEndpoint.Map(admin);
        AddOfferEndpoint.Map(admin);
        PublishProductEndpoint.Map(admin);
        SetPriceEndpoint.Map(admin);
        LivenessEndpoint.Map(app);
        ReadinessEndpoint.Map(app);
    }
}
