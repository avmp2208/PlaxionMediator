namespace PlaxionMediator.Sample.WebApi.Tests;

public sealed class HandlerTests
{
    [Fact]
    public async Task GetItemsHandler_Returns_All_Items_From_Store()
    {
        var store = new ItemStore();
        ItemDto first = store.Add("Alpha");
        ItemDto second = store.Add("Beta");

        var handler = new GetItemsHandler(store);

        IReadOnlyList<ItemDto> result = await handler.Handle(new GetItemsRequest(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id == first.Id && i.Name == "Alpha");
        Assert.Contains(result, i => i.Id == second.Id && i.Name == "Beta");
    }

    [Fact]
    public async Task GetItemsHandler_Returns_Empty_List_When_Store_Is_Empty()
    {
        var store = new ItemStore();
        var handler = new GetItemsHandler(store);

        IReadOnlyList<ItemDto> result = await handler.Handle(new GetItemsRequest(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemHandler_Returns_Matching_Item()
    {
        var store = new ItemStore();
        var counter = new GetItemInvocationCounter();
        ItemDto created = store.Add("Gamma");

        var handler = new GetItemHandler(store, counter);

        ItemDto result = await handler.Handle(new GetItemRequest(created.Id), CancellationToken.None);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Gamma", result.Name);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task GetItemHandler_Throws_KeyNotFoundException_When_Missing()
    {
        var store = new ItemStore();
        var counter = new GetItemInvocationCounter();
        var handler = new GetItemHandler(store, counter);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new GetItemRequest(Guid.NewGuid()), CancellationToken.None).AsTask());

        Assert.Equal(1, counter.Count);
    }
}
