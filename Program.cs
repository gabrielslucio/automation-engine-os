using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();



app.MapPost("/workflow/execute", async (WorkflowInput input) =>
{
    logger.LogInformation("Workflow execution has started.");

    var results = new List<string>();

    using var client = new HttpClient(); 

    foreach (var rule in input.Rules)
    {
        var propertyInfo = typeof(WorkflowInput).GetProperty(rule.Field);

        logger.LogInformation($"Evaluating rule: {rule.Field} {rule.Operator} {rule.Value}");

        if (propertyInfo == null)
        {
            logger.LogWarning($"Invalid field: {rule.Field}");
            continue;   
        }
        
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
        {
            logger.LogInformation($"Condition not met for rule: {rule.Field}");
            continue;
        }

        logger.LogInformation($"Condition met for rule: {rule.Field}");

        // Executar ação
        switch (rule.Action)
        {
            case "CallAPI":
            {
                if (string.IsNullOrEmpty(rule.Endpoint))
                {
                    logger.LogWarning("Endpoint is missing, cannot call API.");
                    break;
                }

                logger.LogInformation($"Calling API: {rule.Endpoint}");

                try
                {
                    HttpContent? content = null;

                    if (rule.Payload != null)
                        {
                            var json = JsonSerializer.Serialize(rule.Payload);
                            logger.LogInformation($"Payload: {json}");

                            // aplicar template
                            json = ReplacePlaceholders(json, input);

                            content = new StringContent(
                                json,
                                System.Text.Encoding.UTF8,
                                "application/json"
                            );
                        }

                    var response = await client.PostAsync(rule.Endpoint, content);

                    logger.LogInformation($"API response from {rule.Endpoint}: {response.StatusCode}");

                    results.Add($"Called API: {rule.Endpoint} - Status: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,"Failed calling API.");
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

string ReplacePlaceholders(string json, WorkflowInput input)
{
    foreach(var prop in typeof(WorkflowInput).GetProperties())
    {
        var value = prop.GetValue(input)?.ToString() ?? "";

        json = json.Replace($"{{{{{prop.Name}}}}}", value);
    }

    return json;
}


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