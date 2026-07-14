namespace QuickBite.QualityGates.Tests;

public sealed class ApiSurfaceContractTests
{
    [Theory]
    [InlineData("Identity", "AuthController.cs", "[Route(\"api/auth\")]", "[HttpPost(\"register\")]", "[HttpPost(\"login\")]")]
    [InlineData("Catalog", "RestaurantsController.cs", "[Route(\"api/restaurants\")]", "[HttpGet]", "[HttpGet(\"{id:guid}/menu\")]")]
    [InlineData("Orders", "OrdersController.cs", "[Route(\"api/orders\")]", "[HttpPost]", "[HttpGet]", "[HttpGet(\"my\")]", "[HttpGet(\"my/{id:guid}\")]", "[HttpGet(\"{id:guid}\")]")]
    [InlineData("Payments", "PaymentsController.cs", "[Route(\"api/payments\")]", "[HttpGet(\"{orderId:guid}\")]")]
    [InlineData("Delivery", "DeliveriesController.cs", "[Route(\"api/deliveries\")]", "[HttpGet(\"{orderId:guid}\")]")]
    public void Public_api_routes_do_not_drift_without_a_contract_update(
        string serviceName,
        string controllerFile,
        params string[] expectedSnippets)
    {
        var source = File.ReadAllText(ControllerPath(serviceName, controllerFile));

        foreach (var expectedSnippet in expectedSnippets)
        {
            Assert.Contains(expectedSnippet, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Customer_order_creation_is_bound_to_the_authenticated_user()
    {
        var source = File.ReadAllText(ControllerPath("Orders", "OrdersController.cs"));
        var contracts = File.ReadAllText(RepositoryPaths.File(
            "src",
            "Services",
            "Orders",
            "QuickBite.Orders.Application",
            "OrderContracts.cs"));

        Assert.Contains("[Authorize]", source, StringComparison.Ordinal);
        Assert.Contains("[FromBody] CustomerCreateOrderRequest request", source, StringComparison.Ordinal);
        Assert.Contains("new CreateOrderRequest(userId, request.Items, request.DeliveryAddress, idempotencyKey)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[FromBody] CreateOrderRequest request", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record CustomerCreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items", contracts, StringComparison.Ordinal);
    }

    private static string ControllerPath(string serviceName, string controllerFile)
    {
        return RepositoryPaths.File(
            "src",
            "Services",
            serviceName,
            $"QuickBite.{serviceName}.Api",
            "Controllers",
            controllerFile);
    }
}
