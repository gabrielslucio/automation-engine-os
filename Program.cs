using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/workflow/execute", async (WorkflowInput input) =>
{
    var results = new List<string>();

    using var client = new HttpClient(); 

    foreach (var rule in input.Rules)
    {
        var propertyInfo = typeof(WorkflowInput).GetProperty(rule.Field);

        if (propertyInfo == null)
            continue;

        int fieldValue = (int)propertyInfo.GetValue(input);

        bool conditionMet = false;

        switch (rule.Operator)
        {
            case ">":
                conditionMet = fieldValue > rule.Value;
                break;

            case "<":
                conditionMet = fieldValue < rule.Value;
                break;

            case "=":
                conditionMet = fieldValue == rule.Value;
                break;
        }

        if (!conditionMet)
            continue;

        // Executar ação
        switch (rule.Action)
        {
            case "CallAPI":
            {
                if (string.IsNullOrEmpty(rule.Endpoint))
                    break;

                try
                {
                    HttpContent? content = null;

                    if (rule.Payload != null)
                        {
                            var json = JsonSerializer.Serialize(rule.Payload);

                            content = new StringContent(
                                json,
                                System.Text.Encoding.UTF8,
                                "application/json"
                            );
                        }

                    var response = await client.PostAsync(rule.Endpoint, content);

                    results.Add($"Called API: {rule.Endpoint} - Status: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    results.Add($"API call failed: {ex.Message}");
                }

                break;
            }

            case "Log":
                results.Add("Log action executed");
                break;

            case "Notify":
                results.Add("Notify action executed");
                break;
        }
    }

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