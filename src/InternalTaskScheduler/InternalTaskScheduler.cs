using System.Collections;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SlugEnt;

namespace SlugEnt;

/// <summary>
/// A Task Scheduler that provides running of tasks on a periodic, elapsed time or some other time period.
/// </summary>
public class InternalTaskScheduler
{
    private        Dictionary<string, InternalScheduledTask>                     _tasks;
    private        SortedBucketCollection<DateTime, Guid, InternalScheduledTask> _upcomingTasks;
    private static SemaphoreSlim                                                 _lockUpcomingTasks = new SemaphoreSlim(1);
    private        ILogger<InternalTaskScheduler>                                _logger;


    /// <summary>
    /// Constructor non-Dependency Injection.  If you wish to set Logger, then call the Logger property.
    /// </summary>
    public InternalTaskScheduler()
    {
        _tasks = new();

        // We sort by Date and then by ID (Guid)
        _upcomingTasks = new SortedBucketCollection<DateTime, Guid, InternalScheduledTask>(its => its.NextScheduledRunTime, its => its.Id);
    }


    /// <summary>
    /// Constructor for Dependency Injection
    /// </summary>
    /// <param name="logger"></param>
    public InternalTaskScheduler(ILogger<InternalTaskScheduler> logger) { _logger = logger; }


    /// <summary>
    /// Sets the logger if desired
    /// </summary>
    public ILogger<InternalTaskScheduler> Logger
    {
        set { _logger = value; }
    }


    /// <summary>
    /// If true, tasks will be run in parallel
    /// </summary>
    public bool RunTasksInParallel { get; set; } = true;


    /// <summary>
    /// This is the timeout value for acquiring a lock on the UpcomingTasks Collection.  You should not typically need to change this.  
    /// </summary>
    public int InternalSchedulerWaitTimeout { get; set; } = 3000;


    /// <summary>
    /// Returns the number of tasks that are currently scheduled
    /// </summary>
    public int ScheduledTaskCount
    {
        get { return _upcomingTasks.Count; }
    }


    /// <summary>
    /// Returns the number of tasks that have been run.
    /// </summary>
    public long ExecutedTaskCount { get; protected set; }


    /// <summary>
    /// Returns the number of tasks that have been added to the system.  This is not Scheduled, just tasks we know about.
    /// </summary>
    public int TasksCount
    {
        get { return _tasks.Count; }
    }


    /// <summary>
    /// True, if the Check Tasks process is running
    /// </summary>
    public bool IsCheckTasksProcessRunning { get; protected set; }


    /// <summary>
    /// Adds the given task to the Scheduled Task List and schedules it.
    /// </summary>
    /// <param name="task"></param>
    public void AddTask(InternalScheduledTask task)
    {
        _tasks.Add(task.Name, task);
        task.SetNextRunTime();
        _upcomingTasks.Add(task);
    }


    /// <summary>
    /// Removes the given task from the scheduled task list
    /// </summary>
    /// <param name="taskName"></param>
    public void RemoveTask(string taskName)
    {
        if (_tasks.ContainsKey(taskName))
        {
            _tasks.Remove(taskName);
        }
    }


    /// <summary>
    /// Retrieves the task with the given name or null if not found
    /// </summary>
    /// <param name="taskName"></param>
    /// <returns></returns>
    public InternalScheduledTask GetTask(string taskName)
    {
        if (_tasks.TryGetValue(taskName, out InternalScheduledTask task))
        {
            return task;
        }

        return null;
    }


    /// <summary>
    /// Runs thru all tasks and checks to see if any need to be run.
    /// </summary>
    public async Task CheckTasks()
    {
        if (IsCheckTasksProcessRunning)
            return;


        List<Task>                   tasks       = new List<Task>();
        DateTime                     current     = DateTime.Now;
        List<InternalScheduledTask>  removeTasks = new List<InternalScheduledTask>();
        Queue<InternalScheduledTask> queuedTasks = new();
        bool                         locked      = false;

        try
        {
            IsCheckTasksProcessRunning = true;

            bool success = await _lockUpcomingTasks.WaitAsync(InternalSchedulerWaitTimeout);
            if (!success)
            {
                if (_logger != null)
                    _logger.LogError("Timeout waiting for _lockUpcomingTasks to return.  No tasks have been checked to see if they need to run.  Next run will try again.");
                return;
            }


            // Check all internal tasks, see if any need to run and then add them to a queue to be run
            locked = true;
            {
                foreach (InternalScheduledTask internalScheduledTask in _upcomingTasks)
                {
                    if (internalScheduledTask.NextScheduledRunTime <= current)
                        queuedTasks.Enqueue(internalScheduledTask);
                    else

                        // Exit the foreach as none of the remaining items meet the time criteria
                        break;
                }
            }


            // Clear the upcoming tasks we just processed.
            foreach (InternalScheduledTask internalScheduledTask in queuedTasks)
            {
                _upcomingTasks.Remove(internalScheduledTask);
            }

            _lockUpcomingTasks.Release();
            locked = false;


            // Run normal if in parallel
            if (RunTasksInParallel)
            {
                List<InternalScheduledTask> runningTasks = new();
                while (queuedTasks.TryDequeue(out InternalScheduledTask internalScheduledTask))
                {
                    Task task = Task.Run(() => internalScheduledTask.TaskMethod(internalScheduledTask));
                    tasks.Add(task);
                    runningTasks.Add(internalScheduledTask);
                    ExecutedTaskCount++;
                }

                await Task.WhenAll(tasks);
                foreach (InternalScheduledTask internalScheduledTask in runningTasks)
                    ScheduleNextRunTimeForTask(internalScheduledTask);
            }

            // Run, one at a time.
            else
            {
                while (queuedTasks.TryDequeue(out InternalScheduledTask internalScheduledTask))
                {
                    Task task = Task.Run(() => internalScheduledTask.TaskMethod(internalScheduledTask));
                    await task.ConfigureAwait(false);
                    ExecutedTaskCount++;
                    ScheduleNextRunTimeForTask(internalScheduledTask);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            if (locked)
                _lockUpcomingTasks.Release();
        }
        finally
        {
            IsCheckTasksProcessRunning = false;
        }
    }



    /// <summary>
    /// Schedules the next run time for the taskk
    /// </summary>
    /// <param name="internalScheduledTask"></param>
    protected void ScheduleNextRunTimeForTask(InternalScheduledTask internalScheduledTask)
    {
        bool locked = false;
        try
        {
            // Re-schedule the task for next run.
            internalScheduledTask.SetNextRunTime();

            bool success = _lockUpcomingTasks.Wait(InternalSchedulerWaitTimeout);
            if (!success)
            {
                _logger.LogError($"Timeout waiting for the _lockUpcomingTasks lock to be free so that the Task {internalScheduledTask.Name} can be scheduled for its next run.");
                return;
            }

            locked = true;
            _upcomingTasks.Add(internalScheduledTask);
            internalScheduledTask.IsScheduled = true;
        }
        catch (Exception ex)
        {
            if (_logger != null)
                _logger.LogError(ex.Message, ex);
        }
        finally
        {
            if (locked)
                _lockUpcomingTasks.Release();
            locked = false;
        }
    }


    /// <summary>
    /// Removes all tasks from upcoming and task lists.
    /// </summary>
    public void RemoveAllTasks()
    {
        try
        {
            bool success = _lockUpcomingTasks.Wait(InternalSchedulerWaitTimeout);
            if (!success)
            {
                _logger.LogError("Failed to attain lock to delete tasks.");
                return;
            }

            _upcomingTasks.Clear();
            _tasks.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        finally
        {
            _lockUpcomingTasks.Release();
        }
    }
}