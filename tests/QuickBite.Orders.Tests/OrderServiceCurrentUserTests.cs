using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Kafka;
using QuickBite.Orders.Application;
using QuickBite.Orders.Infrastructure;

namespace QuickBite.Orders.Tests;

public sealed class OrderServiceCurrentUserTests
{
    [Fact]
    public async Task Current_user_summaries_are_owned_newest_first_and_cursor_paginated()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var olderOrder = await service.CreateAsync(CreateRequest(ownerId, "Older bowl", "owner-older"), CancellationToken.None);
        await Task.Delay(5);
        var newerOrder = await service.CreateAsync(CreateRequest(ownerId, "Newer wrap", "owner-newer"), CancellationToken.None);
        await service.CreateAsync(CreateRequest(otherUserId, "Other salad", "other-user"), CancellationToken.None);

        var firstPage = await service.ListSummariesForUserAsync(ownerId, 1, null, CancellationToken.None);
        Assert.Single(firstPage.Items);
        Assert.Equal(newerOrder.Id, firstPage.Items.Single().Id);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await service.ListSummariesForUserAsync(ownerId, 10, firstPage.NextCursor, CancellationToken.None);
        Assert.Single(secondPage.Items);
        Assert.Equal(olderOrder.Id, secondPage.Items.Single().Id);
        Assert.Null(secondPage.NextCursor);

        var ownedDetails = await service.GetDetailsForUserByIdAsync(ownerId, newerOrder.Id, CancellationToken.None);
        Assert.NotNull(ownedDetails);
        Assert.Equal(ownerId, ownedDetails.UserId);

        var forbiddenDetails = await service.GetDetailsForUserByIdAsync(ownerId, (await service.ListForUserAsync(otherUserId, 1, CancellationToken.None)).Single().Id, CancellationToken.None);
        Assert.Null(forbiddenDetails);
    }

    private static OrdersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrdersDbContext(options);
    }

    private static IOrderService CreateService(OrdersDbContext dbContext)
    {
        var serviceType = typeof(OrdersDbContext).Assembly.GetType("QuickBite.Orders.Infrastructure.OrderService", throwOnError: true)!;
        return (IOrderService)Activator.CreateInstance(
            serviceType,
            dbContext,
            Options.Create(new KafkaOptions { Producer = "QuickBite.Orders.Tests" }))!;
    }

    private static CreateOrderRequest CreateRequest(Guid userId, string itemName, string idempotencyKey)
    {
        return new CreateOrderRequest(
            userId,
            [new CreateOrderItemRequest(Guid.NewGuid(), itemName, 2, 7.50m)],
            new DeliveryAddressRequest("123 Market Street", null, "Seattle", "WA", "98101", "USA"),
            idempotencyKey);
    }
}
