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

    foreach(var rule in input.Rules)
    {
        var propertyInfo = typeof(WorkflowInput).GetProperty(rule.Field);

        if (propertyInfo == null)
            continue;

        int fieldValue = (int)propertyInfo.GetValue(input);

        bool conditionMet = false;

        switch(rule.Operator)
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

        if(conditionMet)
        {
            // Executa a ação
            switch(rule.Action)
            {
                case "CallAPI": // Chama a API
                    {
                        if(string.IsNullOrEmpty(rule.Endpoint))
                        break;

                        var client = new HttpClient();

                        HttpContent content = null;

                        if(!string.IsNullOrEmpty(rule.Payload))
                        {
                            content = new StringContent(rule.Payload, System.Text.Encoding.UTF8, "application/json");
                        }

                        var response = await client.PostAsync(rule.Endpoint, content);

                        results.Add($"Called API: {rule.Endpoint} - Status: {response.StatusCode}");

                        break;
                    }
                case "Log":
                    // guardar ou print
                    break;
                case "Notify":
                    // simular alerta
                    break;
            }
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
    string? Payload
);