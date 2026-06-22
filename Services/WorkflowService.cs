using System.Text.Json;

public class WorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly HttpClient _client;

    public WorkflowService(ILogger<WorkflowService> logger)
    {
        _logger = logger;
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<List<string>> ExecuteAsync(WorkflowInput input)
    {
        var results = new List<string>();

        foreach(var rule in input.Rules)
        {
            _logger.LogInformation($"Evaluating rule: {rule.Field} {rule.Operator} {rule.Value}");

            var propertyInfo = typeof(WorkflowInput).GetProperty(rule.Field);

            if(propertyInfo == null)
            {
                _logger.LogWarning($"Invalid field: {rule.Field}");
                continue;
            }

            int fieldValue = Convert.ToInt32(propertyInfo.GetValue(input));

            bool conditionMet = EvaluateCondition(fieldValue, rule);

            if (!conditionMet)
            {
                _logger.LogInformation($"Condition not met for rule: {rule.Field}");
                continue;
            }

            _logger.LogInformation($"Conditions met for rule: {rule.Field}");

            var result = await ExecuteAction(rule, input);
            if (result != null)
                results.Add(result);
        }

        return results;
    }

    private bool EvaluateCondition(int fieldValue, Rule rule)
    {
        return rule.Operator switch
        {
            ">" => fieldValue > rule.Value,
            "<" => fieldValue < rule.Value,
            "=" => fieldValue == rule.Value,
            _ => false
        };
    }

    private async Task<string?> ExecuteAction(Rule rule, WorkflowInput input)
    {
        switch(rule.Action)
        {
            case "CallAPI":

                if(string.IsNullOrEmpty(rule.Endpoint))
                {
                    _logger.LogWarning("Endpoint is missing.");
                    return null;
                } 
                
                try
                {
                    var content = BuildPayload(rule, input);

                    _logger.LogInformation($"Calling API: {rule.Endpoint}");

                    var response = await _client.PostAsync(rule.Endpoint, content);

                    _logger.LogInformation($"API response: {response.StatusCode}");

                    return $"Called API: {rule.Endpoint} - Status: {response.StatusCode}";
                } catch (Exception ex)
                {
                    _logger.LogError(ex, "API call failed.");
                    return $"API call failed: {ex.Message}";
                }
            case "Log":
                return "Log action executed.";
            case "Notify":
                return "Notify action executed.";
            default:
                _logger.LogWarning($"Unknown action: {rule.Action}");
                return null;
        }
    }

    private HttpContent? BuildPayload(Rule rule, WorkflowInput input)
    {
        if (rule.Payload == null)
            return null;
        
        var json = JsonSerializer.Serialize(rule.Payload);

        json = ReplacePlaceholders(json, input);

        _logger.LogInformation($"Payload: {json}");

        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private string ReplacePlaceholders(string json, WorkflowInput input)
    {
        foreach (var prop in typeof(WorkflowInput).GetProperties())
        {
            var value = prop.GetValue(input)?.ToString() ?? "";

            json = json.Replace($"{{{{{prop.Name}}}}}", value);
        }

        return json;
    }
}