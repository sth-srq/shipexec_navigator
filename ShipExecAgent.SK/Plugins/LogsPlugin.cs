using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace ShipExecAgent.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that surfaces loaded log data (application and security) so
/// the AI can filter and return matching entries as a structured <c>log-find</c> response.
/// </summary>
public sealed class LogsPlugin
{
    private readonly string _logsJson;

    public LogsPlugin(string logsJson)
    {
        _logsJson = logsJson;
    }

    [KernelFunction("find_logs")]
    [Description(
        "Returns the list of loaded log entries (both application and security) as a JSON array. " +
        "Use this when the user asks to FIND, SEARCH, FILTER, or SUMMARIZE log entries. " +
        "You MUST then filter the returned list based on the user's criteria and respond with " +
        "action type \"log-find\" and payload as a JSON array of matching entries " +
        "(each object must have `id` (int) and `source` (\"App\" or \"Security\") fields). " +
        "Do NOT use action type \"javascript\".")]
    public string FindLogs(
        [Description("The user's request describing which log entries to find or filter")] string userRequest)
    {
        try
        {
            using var doc = JsonDocument.Parse(_logsJson);
            var entries = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id            = el.TryGetProperty("id",            out var idProp)  ? idProp.GetInt32()   : 0,
                    source        = el.TryGetProperty("source",        out var srcProp) ? srcProp.GetString() ?? "" : "",
                    logLevel      = el.TryGetProperty("logLevel",      out var lvlProp) ? lvlProp.GetString() ?? "" : "",
                    logger        = el.TryGetProperty("logger",        out var logProp) ? logProp.GetString() ?? "" : "",
                    logDate       = el.TryGetProperty("logDate",       out var dtProp)  ? dtProp.GetString()  ?? "" : "",
                    message       = el.TryGetProperty("message",       out var msgProp) ? msgProp.GetString() ?? "" : "",
                    transactionId = el.TryGetProperty("transactionId", out var txProp)  ? txProp.GetString()  ?? "" : "",
                    serverAddress = el.TryGetProperty("serverAddress", out var saProp)  ? saProp.GetString()  ?? "" : "",
                    userName      = el.TryGetProperty("userName",      out var unProp)  ? unProp.GetString()  ?? "" : "",
                    actionType    = el.TryGetProperty("actionType",    out var atProp)  ? atProp.GetString()  ?? "" : "",
                    entity        = el.TryGetProperty("entity",        out var entProp) ? entProp.GetString() ?? "" : "",
                    entityName    = el.TryGetProperty("entityName",    out var enProp)  ? enProp.GetString()  ?? "" : "",
                })
                .Where(e => e.id > 0)
                .ToList();

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entries.Count} log entry/entries:\n{json}\n\n" +
                   "Filter this list based on the user's condition and respond with " +
                   "action type \"log-find\" and payload as a JSON array of matching entries. " +
                   "Each object must have `id` (int) and `source` (\"App\" or \"Security\") fields. " +
                   "Do NOT include any JavaScript — the Navigator will highlight those log entries directly.";
        }
        catch (Exception ex)
        {
            return $"Failed to parse logs: {ex.Message}";
        }
    }
}
