using Microsoft.IdentityModel.Abstractions;
using UnityEngine;

public class Logger : IIdentityLogger
{
    public bool IsEnabled(EventLogLevel eventLogLevel) => true;

    public void Log(LogEntry entry)
    {
        Debug.Log($"{entry.Message}");
    }
}
