namespace QuickBite.BuildingBlocks.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    protected void UseSeedIdentity(Guid id, DateTimeOffset? createdAtUtc = null)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc ?? CreatedAtUtc;
    }
}

public sealed record Result(bool IsSuccess, string? Error = null)
{
    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
}

public sealed record Result<T>(bool IsSuccess, T? Value = default, string? Error = null)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public static class ServiceDefaults
{
    public const string HealthEndpoint = "/health";
}

public static class DemoSeedData
{
    public const string DemoPassword = "Pass123!";

    public static readonly Guid CustomerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerTwoUserId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    public static readonly Guid RestaurantAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CourierUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PlatformAdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid UrbanBowlRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    public static readonly Guid PizzaPortRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    public static readonly Guid TacoLaneRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
    public static readonly Guid SushiCentralRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
    public static readonly Guid BreakfastClubRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5");
    public static readonly Guid CurryHouseRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6");
    public static readonly Guid BurgerForgeRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7");
    public static readonly Guid NoodleHouseRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8");
    public static readonly Guid GreenGardenRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9");
    public static readonly Guid SmokehouseRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
    public static readonly Guid SweetCornerRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa11");
    public static readonly Guid MediterraneanTableRestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa12");

    public static readonly Guid UrbanBowlChickenPowerBowlId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb001");
    public static readonly Guid UrbanBowlFalafelWrapId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb002");
    public static readonly Guid PizzaPortMargheritaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb101");
    public static readonly Guid TacoLaneBirriaTacosId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb201");
    public static readonly Guid SushiCentralSalmonSetId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb301");

    public static readonly Guid DemoConfirmedOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
    public static readonly Guid DemoProcessingOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
    public static readonly Guid DemoFailedOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3");
}
