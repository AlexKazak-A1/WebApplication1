using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace WebApplication1.Data;

public static class TaskManager
{
    private static readonly ConcurrentDictionary<Guid, TaskStatusInfo> _tasks = new();
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
    private static readonly Timer _cleanupTimer;

    static TaskManager()
    {
        _cleanupTimer = new Timer(Cleanup, null, Retention, Retention);
    }

    public static Guid StartClusterCreation(Func<Task<object?>> createFunc)
    {
        var taskId = Guid.NewGuid();

        var info = new TaskStatusInfo
        {
            TaskId = taskId
        };

        _tasks[taskId] = info;

        Task.Run(async () =>
        {
            try
            {
                var result = await createFunc();
                info.Result = result;
                info.Status = "Completed";
            }
            catch (Exception ex)
            {
                info.ErrorMessage = ex.Message;
                info.Status = "Failed";
            }
            finally
            {
                info.LastUpdated = DateTime.UtcNow;
            }
        });

        return taskId;
    }

    public static TaskStatusInfo? GetStatus(Guid taskId)
    {
        return _tasks.TryGetValue(taskId, out var result) ? result : null;
    }

    private static void Cleanup(object? _)
    {
        var threshold = DateTime.UtcNow - Retention;
        foreach (var item in _tasks)
        {
            if (item.Value.Status != "InProgress" && item.Value.LastUpdated < threshold)
                _ = _tasks.TryRemove(item);
        }
    }
}
