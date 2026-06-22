var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<WorkflowService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/workflow/execute", async (WorkflowInput input, WorkflowService service) =>
{
    var logger = app.Logger;

    logger.LogInformation("Workflow execution started.");

    if (input == null || input.Rules == null || input.Rules.Count == 0)
        return Results.BadRequest("Invalid input");

    var results = await service.ExecuteAsync(input);

    if (results.Count == 0)
        return Results.Ok("No actions executed.");

    return Results.Ok(results);
});

app.Run();


record WorkflowInput(int OverdueTasks, int TotalTasks, List<Rule> Rules);
record Rule (
    string Field,
    string Operator,
    int Value,
    string Action,
    string? Endpoint,
    object? Payload
);