using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FluentRDLC.Renderer;

public class RdlcExpressionEvaluator
{
    private readonly ConcurrentDictionary<string, object?> _parameters = new();
    private readonly ConcurrentDictionary<string, DataTable> _dataSources = new();
    private DataRow? _currentRow;
    private string? _currentDataSetName;

    public void SetParameters(Dictionary<string, object?> parameters)
    {
        _parameters.Clear();
        foreach (var param in parameters)
        {
            _parameters[param.Key] = param.Value;
        }
    }

    public void SetDataSources(Dictionary<string, DataTable> dataSources)
    {
        _dataSources.Clear();
        foreach (var ds in dataSources)
        {
            _dataSources[ds.Key] = ds.Value;
        }
    }

    public void SetCurrentRow(DataRow? row, string? dataSetName = null)
    {
        _currentRow = row;
        _currentDataSetName = dataSetName;
    }

    public string EvaluateExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return string.Empty;

        return EvaluateExpression(expression, _currentRow, _currentDataSetName);
    }

    public string EvaluateExpression(string expression, DataRow? currentRow, string? dataSetName = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return string.Empty;

        // Temporarily set the current row for this evaluation
        var previousRow = _currentRow;
        var previousDataSetName = _currentDataSetName;
        
        _currentRow = currentRow;
        _currentDataSetName = dataSetName;
        
        try
        {
            // Remove leading = if present
            if (expression.StartsWith("="))
                expression = expression[1..];

            var result = EvaluateRdlcExpression(expression);
            
            // Check if the result is still an unprocessed complex expression (likely invalid)
            // But only if it actually looks like an unprocessed expression (contains function calls or field references)
            bool hasUnprocessedExpressions = result.Contains("Fields!") || result.Contains("Parameters!") || 
                                            result.Contains("Format(") || result.Contains("Sum(");
            bool hasInvalidFunctionPattern = System.Text.RegularExpressions.Regex.IsMatch(result, @"[A-Za-z][A-Za-z0-9]*\(");
            
            if (result.Contains("(") && result.Contains(")") && !result.StartsWith("'") && 
                (hasUnprocessedExpressions || hasInvalidFunctionPattern))
            {
                return $"[Error: Unrecognized function or expression] {result}";
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // If evaluation fails, return the original expression for debugging
            return $"[Error: {ex.Message}] {expression}";
        }
        finally
        {
            // Restore previous row state
            _currentRow = previousRow;
            _currentDataSetName = previousDataSetName;
        }
    }

    private string EvaluateRdlcExpression(string expression)
    {
        // Handle Sum function FIRST before processing Fields (since Sum contains Fields expressions)
        expression = Regex.Replace(expression, @"Sum\(([^,]+),\s*'([^']+)'\)", match =>
        {
            var fieldExpr = match.Groups[1].Value;
            var dataset = match.Groups[2].Value;
            
            // Extract field name from Fields!FieldName.Value
            var fieldMatch = Regex.Match(fieldExpr, @"Fields!(\w+)\.Value", RegexOptions.IgnoreCase);
            if (fieldMatch.Success)
            {
                var fieldName = fieldMatch.Groups[1].Value;
                return SumFieldAsString(fieldName, dataset);
            }
            return "0";
        }, RegexOptions.IgnoreCase);

        // Handle Format function - more comprehensive implementation  
        expression = Regex.Replace(expression, @"Format\(([^,]+),\s*'([^']+)'\)", match =>
        {
            var valueExpr = match.Groups[1].Value;
            var format = match.Groups[2].Value;
            
            // Handle Fields!FieldName.Value in Format functions
            if (valueExpr.Contains("Fields!") && valueExpr.Contains(".Value"))
            {
                var fieldMatch = Regex.Match(valueExpr, @"Fields!(\w+)\.Value", RegexOptions.IgnoreCase);
                if (fieldMatch.Success && _currentRow != null)
                {
                    var fieldName = fieldMatch.Groups[1].Value;
                    if (_currentRow.Table.Columns.Contains(fieldName))
                    {
                        var fieldValue = _currentRow[fieldName];
                        return FormatValue(fieldValue, format);
                    }
                }
            }
            
            // Handle Parameters!ParameterName.Value in Format functions
            if (valueExpr.Contains("Parameters!") && valueExpr.Contains(".Value"))
            {
                var paramMatch = Regex.Match(valueExpr, @"Parameters!(\w+)\.Value", RegexOptions.IgnoreCase);
                if (paramMatch.Success)
                {
                    var paramName = paramMatch.Groups[1].Value;
                    if (_parameters.TryGetValue(paramName, out var paramValue))
                    {
                        return FormatValue(paramValue, format);
                    }
                }
            }
            
            // For other expressions, evaluate first
            var evaluatedValue = EvaluateSimpleExpression(valueExpr);
            return FormatValue(evaluatedValue, format);
        }, RegexOptions.IgnoreCase);

        // Handle Parameters!ParameterName.Value
        expression = Regex.Replace(expression, @"Parameters!(\w+)\.Value", match =>
        {
            var paramName = match.Groups[1].Value;
            var paramValue = _parameters.TryGetValue(paramName, out var value) ? value?.ToString() ?? "" : "";
            
            // For numeric values, don't add quotes
            if (decimal.TryParse(paramValue, out _))
            {
                return paramValue;
            }
            return $"'{paramValue}'";
        }, RegexOptions.IgnoreCase);

        // Handle Fields!FieldName.Value (but skip those that are inside Format functions)
        expression = Regex.Replace(expression, @"Fields!(\w+)\.Value", match =>
        {
            // Check if this field is inside a Format function by looking at the context
            var fullMatch = match.Value;
            var matchIndex = match.Index;
            var beforeMatch = matchIndex > 0 ? expression.Substring(Math.Max(0, matchIndex - 20), Math.Min(20, matchIndex)) : "";
            
            // If this field is inside a Format function, don't process it here
            if (beforeMatch.Contains("Format("))
            {
                return fullMatch; // Leave it unchanged for Format processing
            }
            
            var fieldName = match.Groups[1].Value;
            
            var fieldValue = _currentRow?.Table.Columns.Contains(fieldName) == true ? _currentRow[fieldName]?.ToString() ?? "" : "";
            
            // For numeric values, format consistently and don't add quotes
            if (decimal.TryParse(fieldValue, out var decValue))
            {
                return FormatDecimalResult(decValue);
            }
            return $"'{fieldValue}'";
        }, RegexOptions.IgnoreCase);

        // Handle VB.NET line breaks
        expression = expression.Replace("vbCrLf", "'\n'");

        // Handle simple conditional expressions
        expression = EvaluateConditionalExpressions(expression);

        // Handle basic string concatenation
        var result = EvaluateStringConcatenation(expression);
        return result;
    }

    private string EvaluateConditionalExpressions(string expression)
    {
        // Handle basic if() function
        var ifMatch = Regex.Match(expression, @"if\(([^,]+),\s*'([^']*)',\s*'([^']*)'\)", RegexOptions.IgnoreCase);
        if (ifMatch.Success)
        {
            var condition = ifMatch.Groups[1].Value.Trim();
            var trueValue = ifMatch.Groups[2].Value;
            var falseValue = ifMatch.Groups[3].Value;
            
            // Simple condition evaluation (number > number)
            var conditionMatch = Regex.Match(condition, @"(\d+(?:\.\d+)?)\s*>\s*(\d+(?:\.\d+)?)");
            if (conditionMatch.Success)
            {
                if (decimal.TryParse(conditionMatch.Groups[1].Value, out var left) &&
                    decimal.TryParse(conditionMatch.Groups[2].Value, out var right))
                {
                    return left > right ? trueValue : falseValue;
                }
            }
        }
        
        return expression;
    }

    private string EvaluateStringConcatenation(string expression)
    {
        // Check if this is a mathematical expression (contains * / - operators outside quotes)
        if (IsMathematicalExpression(expression))
        {
            return EvaluateMathematicalExpression(expression);
        }
        
        // Simple string concatenation evaluator
        // Split on + and combine strings
        var parts = new List<string>();
        var currentPart = "";
        var inQuotes = false;
        
        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];
            
            if (c == '\'' && (i == 0 || expression[i-1] != '\\'))
            {
                inQuotes = !inQuotes;
                currentPart += c;
            }
            else if (c == '+' && !inQuotes)
            {
                parts.Add(currentPart.Trim());
                currentPart = "";
            }
            else
            {
                currentPart += c;
            }
        }
        
        if (!string.IsNullOrEmpty(currentPart))
        {
            parts.Add(currentPart.Trim());
        }

        // Combine all parts
        var result = "";
        foreach (var part in parts)
        {
            if (part.StartsWith("'") && part.EndsWith("'"))
            {
                // Remove quotes and add to result
                result += part[1..^1];
            }
            else if (decimal.TryParse(part, out var numValue))
            {
                result += numValue.ToString();
            }
            else
            {
                // Evaluate as simple expression
                result += EvaluateSimpleExpression(part);
            }
        }
        
        return result;
    }

    private bool IsMathematicalExpression(string expression)
    {
        // Check if contains mathematical operators outside of quotes
        var inQuotes = false;
        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];
            
            if (c == '\'' && (i == 0 || expression[i-1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == '*' || c == '/' || c == '-'))
            {
                return true;
            }
        }
        return false;
    }

    private string EvaluateMathematicalExpression(string expression)
    {
        try
        {
            // Simple mathematical expression evaluator
            // Remove quotes from numbers
            expression = Regex.Replace(expression, @"'(\d+(?:\.\d+)?)'", "$1");
            
            // Handle basic multiplication
            if (expression.Contains(" * "))
            {
                var parts = expression.Split(" * ");
                if (parts.Length == 2 && 
                    decimal.TryParse(parts[0].Trim(), out var left) && 
                    decimal.TryParse(parts[1].Trim(), out var right))
                {
                    return FormatDecimalResult(left * right);
                }
            }
            
            // Handle basic division
            if (expression.Contains(" / "))
            {
                var parts = expression.Split(" / ");
                if (parts.Length == 2 && 
                    decimal.TryParse(parts[0].Trim(), out var left) && 
                    decimal.TryParse(parts[1].Trim(), out var right) && right != 0)
                {
                    return FormatDecimalResult(left / right);
                }
            }
            
            // Handle basic subtraction
            if (expression.Contains(" - "))
            {
                var parts = expression.Split(" - ");
                if (parts.Length == 2 && 
                    decimal.TryParse(parts[0].Trim(), out var left) && 
                    decimal.TryParse(parts[1].Trim(), out var right))
                {
                    return FormatDecimalResult(left - right);
                }
            }
            
            return expression; // Return as-is if can't evaluate
        }
        catch
        {
            return expression; // Return as-is if error
        }
    }

    private static string FormatDecimalResult(decimal value)
    {
        // Remove trailing zeros for consistent formatting
        // Use G29 to get the most compact representation without scientific notation
        return value.ToString("G29");
    }

    private string EvaluateSimpleExpression(string expression)
    {
        expression = expression.Trim();
        
        // Remove quotes if present
        if (expression.StartsWith("'") && expression.EndsWith("'"))
        {
            return expression[1..^1];
        }
        
        // Return numeric values as-is
        if (decimal.TryParse(expression, out var numValue))
        {
            return numValue.ToString();
        }
        
        return expression;
    }

    private static string FormatValue(object value, string format)
    {
        try
        {
            return format.ToUpper() switch
            {
                "N2" => Convert.ToDecimal(value).ToString("N2", CultureInfo.InvariantCulture),
                "N0" => Convert.ToDecimal(value).ToString("N0", CultureInfo.InvariantCulture),
                "C" => "¤" + Convert.ToDecimal(value).ToString("F2", CultureInfo.InvariantCulture), // Use generic currency symbol
                "C2" => "¤" + Convert.ToDecimal(value).ToString("F2", CultureInfo.InvariantCulture),
                "P" => Convert.ToDecimal(value).ToString("P", CultureInfo.InvariantCulture),
                "P2" => Convert.ToDecimal(value).ToString("P2", CultureInfo.InvariantCulture),
                "D" => Convert.ToInt32(value).ToString("D", CultureInfo.InvariantCulture),
                "MM/DD/YYYY" => Convert.ToDateTime(value).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                "MM/dd/yyyy" => Convert.ToDateTime(value).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                "DD/MM/YYYY" => Convert.ToDateTime(value).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                "YYYY-MM-DD" => Convert.ToDateTime(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => string.Format(CultureInfo.InvariantCulture, "{0:" + format + "}", value)
            };
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private static decimal SumFieldValues(DataTable dataTable, string fieldName)
    {
        if (!dataTable.Columns.Contains(fieldName))
            return 0;

        decimal sum = 0;
        foreach (DataRow row in dataTable.Rows)
        {
            if (row[fieldName] != DBNull.Value && decimal.TryParse(row[fieldName].ToString(), out var value))
            {
                sum += value;
            }
        }
        return sum;
    }

    public decimal SumField(string fieldName, string? datasetName = null)
    {
        // Use current dataset if none specified
        var targetDataset = datasetName ?? _currentDataSetName;
        if (targetDataset == null || !_dataSources.TryGetValue(targetDataset, out var dataTable))
            return 0;

        return SumFieldValues(dataTable, fieldName);
    }

    public string SumFieldAsString(string fieldName, string? datasetName = null)
    {
        var sum = SumField(fieldName, datasetName);
        return FormatDecimalResult(sum);
    }

    public object? GetField(string fieldName)
    {
        return _currentRow?[fieldName];
    }

    public object? GetParameter(string parameterName)
    {
        return _parameters.TryGetValue(parameterName, out var value) ? value : null;
    }
}