public class RuleEvaluator
{
    private readonly ILogger<RuleEvaluator> _logger;

    public RuleEvaluator(ILogger<RuleEvaluator> logger)
    {
        _logger = logger;
    }

    public bool Evaluate(int fieldValue, Rule rule)
    {
        var result = rule.Operator switch
        {
            ">" => fieldValue > rule.Value,
            "<" => fieldValue < rule.Value,
            "=" => fieldValue == rule.Value,
            _ => false
        };

        _logger.LogInformation($"Evaluated condition: {rule.Field} {rule.Operator} {rule.Value} -> {result}");

        return result;
    }
}