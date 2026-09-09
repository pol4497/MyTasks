using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using MyTasks.Contexts;
using MyTasks.Filters;

namespace MyTasks.UnitTests;

public class TaskOwnerRequiredFilterTests
{
    private static ActionExecutingContext CreateContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoOwner_ReturnsUnauthorizedAndDoesNotExecuteAction()
    {
        var ownerContext = new TaskOwnerContext();
        var filter = new TaskOwnerRequiredFilter(ownerContext);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ActionDescriptor());
        var context = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.False(nextCalled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_AuthenticatedUser_AllowsAction()
    {
        var ownerContext = new TaskOwnerContext();
        ownerContext.SetUser(42);
        var filter = new TaskOwnerRequiredFilter(ownerContext);
        var context = CreateContext();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                [],
                controller: new object()));
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Guest_AllowsAction()
    {
        var ownerContext = new TaskOwnerContext();
        ownerContext.SetGuest(7);
        var filter = new TaskOwnerRequiredFilter(ownerContext);
        var context = CreateContext();
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                [],
                controller: new object()));
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
