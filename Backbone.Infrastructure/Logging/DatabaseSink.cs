// Backbone.Infrastructure/Logging/DatabaseSink.cs
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Entities;
using Backbone.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using System;
using Newtonsoft.Json;

public class DatabaseSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logEvent.Properties.TryGetValue("SourceContext", out var sourceContext);
        logEvent.Properties.TryGetValue("FilePath", out var filePath);
        logEvent.Properties.TryGetValue("MemberName", out var memberName);
        logEvent.Properties.TryGetValue("LineNumber", out var lineNumber);

        var log = new ApplicationLog
        {
            ProjectName = sourceContext?.ToString()?.Split('.')[0], // Gets first part of namespace
            SourceFile = System.IO.Path.GetFileName(filePath?.ToString()?.Trim('"') ?? ""),
            MethodName = memberName?.ToString()?.Trim('"'),
            LineNumber = lineNumber != null ? int.Parse(lineNumber.ToString()) : (int?)null,

            Message = logEvent.RenderMessage(),
            MessageTemplate = logEvent.MessageTemplate.Text,
            Level = logEvent.Level.ToString(),
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Exception = logEvent.Exception?.ToString(),
            Properties = Newtonsoft.Json.JsonConvert.SerializeObject(logEvent.Properties),
            LogEvent = logEvent.ToString()
        };

        dbContext.ApplicationLogs.Add(log);
        dbContext.SaveChanges();
    }
}