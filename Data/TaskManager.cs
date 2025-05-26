using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace WebApplication1.Data;

/// <summary>
/// Описывает управление состоянием заданий на созданние нового кластера
/// </summary>
public static class TaskManager
{
    private static readonly ConcurrentDictionary<Guid, TaskStatusInfo> _tasks = new();
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
    private static readonly Timer _cleanupTimer;

    static TaskManager()
    {
        _cleanupTimer = new Timer(Cleanup, null, Retention, Retention);
    }

    /// <summary>
    /// запускает создание нового кластера
    /// </summary>
    /// <param name="createFunc">описывает спецификацию создания кластера</param>
    /// <returns>ID задачи для контроля состояния её выполнения</returns>
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

    /// <summary>
    /// Запрашивает текущий статус задачи
    /// </summary>
    /// <param name="taskId">ID задачи, полученный при создании кластера</param>
    /// <returns></returns>
    public static TaskStatusInfo? GetStatus(Guid taskId)
    {
        return _tasks.TryGetValue(taskId, out var result) ? result : null;
    }

    /// <summary>
    /// Очистка данных о состоянии задачи после истечения периода
    /// </summary>
    /// <param name="_"></param>
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
