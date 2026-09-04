using System.Net;
using System.Net.Http.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;
using TaskStatus = MyTasks.Models.TaskStatus;

namespace MyTasks.IntegrationTests;

public class TaskQueryTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetTasks_ReturnsOnlyCurrentUsersTasks()
    {
        var userA = $"querya_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userA, $"{userA}@example.com");

        var ownResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "User A task", Category = "Personal" });
        Assert.Equal(HttpStatusCode.Created, ownResponse.StatusCode);
        var ownTask = await ownResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(ownTask);

        TestAuthHelper.ClearAuthentication(_client);
        var userB = $"queryb_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userB, $"{userB}@example.com");

        var otherResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "User B task", Category = "Work" });
        Assert.Equal(HttpStatusCode.Created, otherResponse.StatusCode);
        var otherTask = await otherResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(otherTask);

        var response = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(tasks);
        Assert.Contains(tasks, task => task.Id == otherTask.Id);
        Assert.DoesNotContain(tasks, task => task.Id == ownTask.Id);
    }

    [Fact]
    public async Task GetTasks_FiltersByStatus()
    {
        var username = $"status_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "Pending task" });
        var completedCreate = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Completed task" });
        Assert.Equal(HttpStatusCode.Created, completedCreate.StatusCode);
        var completed = await completedCreate.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(completed);

        var update = await _client.PutAsJsonAsync(
            $"/api/tasks/{completed.Id}",
            new TaskUpdateDto { Title = completed.Title, Status = TaskStatus.Completed });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var response = await _client.GetAsync("/api/tasks?status=Completed");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(tasks);
        Assert.NotEmpty(tasks);
        Assert.All(tasks, task => Assert.Equal("Completed", task.Status));
    }

    [Fact]
    public async Task GetTasks_FiltersByCategory_CaseInsensitively()
    {
        var username = $"category_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Work task", Category = "Work" });
        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Home task", Category = "Home" });

        var response = await _client.GetAsync("/api/tasks?category=work");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(tasks);
        Assert.Single(tasks);
        Assert.Equal("Work task", tasks[0].Title);
    }

    [Fact]
    public async Task GetTasks_SearchesTitleAndDescription()
    {
        var username = $"search_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Buy groceries", Description = "Milk and bread" });
        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Project work", Description = "Prepare report" });

        var titleResponse = await _client.GetAsync("/api/tasks?search=groceries");
        var titleTasks = await titleResponse.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(titleTasks);
        Assert.Single(titleTasks);
        Assert.Equal("Buy groceries", titleTasks[0].Title);

        var descriptionResponse = await _client.GetAsync("/api/tasks?search=bread");
        var descriptionTasks = await descriptionResponse.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(descriptionTasks);
        Assert.Single(descriptionTasks);
        Assert.Equal("Buy groceries", descriptionTasks[0].Title);
    }

    [Fact]
    public async Task GetTasks_FiltersByDueDateRange()
    {
        var username = $"duefilter_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var today = DateTime.UtcNow.Date;
        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Soon", DueDate = today.AddDays(1) });
        await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Later", DueDate = today.AddDays(10) });

        var response = await _client.GetAsync(
            $"/api/tasks?dueAfter={today:O}&dueBefore={today.AddDays(5):O}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();

        Assert.NotNull(tasks);
        Assert.Single(tasks);
        Assert.Equal("Soon", tasks[0].Title);
    }

    [Fact]
    public async Task GetTasks_SortsByTitleAscendingAndDescending()
    {
        var username = $"sorttitle_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        foreach (var title in new[] { "Charlie", "Alpha", "Bravo" })
        {
            await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = title });
        }

        var ascResponse = await _client.GetAsync("/api/tasks?sortBy=Title");
        var asc = await ascResponse.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(asc);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], asc.Select(t => t.Title).ToArray());

        var descResponse = await _client.GetAsync("/api/tasks?sortBy=Title&desc=true");
        var desc = await descResponse.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(desc);
        Assert.Equal(["Charlie", "Bravo", "Alpha"], desc.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task GetTasks_SortsByStatus()
    {
        var username = $"sortstatus_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var pendingResponse = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "Pending" });
        var inProgressResponse = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "InProgress" });
        var completedResponse = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "Completed" });

        var pending = await pendingResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        var inProgress = await inProgressResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        var completed = await completedResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(pending);
        Assert.NotNull(inProgress);
        Assert.NotNull(completed);

        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/tasks/{inProgress.Id}",
                new TaskUpdateDto { Title = inProgress.Title, Status = TaskStatus.InProgress })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/tasks/{completed.Id}",
                new TaskUpdateDto { Title = completed.Title, Status = TaskStatus.Completed })).StatusCode);

        var response = await _client.GetAsync("/api/tasks?sortBy=Status");
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(tasks);
        Assert.Equal(["Pending", "InProgress", "Completed"], tasks.Select(t => t.Status).ToArray());
    }

    [Fact]
    public async Task GetTasks_PaginatesWithLimitAndOffset()
    {
        var username = $"pagination_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        foreach (var title in new[] { "Alpha", "Bravo", "Charlie", "Delta" })
        {
            await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = title });
        }

        var response = await _client.GetAsync("/api/tasks?sortBy=Title&limit=2&offset=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskReadDto>>();
        Assert.NotNull(tasks);
        Assert.Equal(2, tasks.Count);
        Assert.Equal(["Bravo", "Charlie"], tasks.Select(t => t.Title).ToArray());
    }
}
