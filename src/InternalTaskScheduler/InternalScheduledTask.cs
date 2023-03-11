namespace SlugEnt;

public class InternalScheduledTask
{
    private DateTime _nextScheduledRunTime;


    /// <summary>
    /// Name for this task.  It must be unique.  So if the same task runs every 6 hours and every 5 minutes, those should be 2 tasks.
    /// </summary>
    public string Name { get; set; }


    /// <summary>
    /// How the task is scheduled
    /// </summary>
    public EnumScheduledTaskType ScheduledTaskType { get; protected set; }


    /// <summary>
    /// The last time it ran
    /// </summary>
    public DateTime LastRan { get; protected set; }


    /// <summary>
    /// When the task is next scheduled to run
    /// </summary>
    public DateTime NextScheduledRunTime
    {
        get { return _nextScheduledRunTime; }
        protected set
        {
            _nextScheduledRunTime = value;
            IsScheduled           = false;
        }
    }


    /// <summary>
    /// An object that can store data for a task between runs
    /// </summary>
    public Object TaskData { get; set; }


    /// <summary>
    /// The method to be run.
    /// </summary>
    public Func<InternalScheduledTask, Task<bool>> TaskMethod { get; set; }


    /// <summary>
    /// How often to run the task
    /// </summary>
    public TimeSpan RunInterval { get; set; }


    /// <summary>
    /// Unique ID that identifies this task
    /// </summary>
    public Guid Id { get; private set; }


    /// <summary>
    /// Data that can be stored for the internal task.  
    /// </summary>
    public Object Data { get; set; }


    /// <summary>
    /// If true, the task has been scheduled to run.  Note:  NextScheduleRunTime only indicates the next time IT should run.  It does not indicate IF it has been scheduled.  This flag determines that.
    /// </summary>
    public bool IsScheduled { get; set; }


    /// <summary>
    /// Construct a scheduledtask that is based upon an elapsed time period
    /// </summary>
    /// <param name="name">Name of this task</param>
    /// <param name="methodToRun">The method to run.  It must accept an object and return a bool indicating success or failure</param>
    /// <param name="runInterval">How often it should run, so, every x minutes for example</param>
    public InternalScheduledTask(string name, Func<InternalScheduledTask, Task<bool>> methodToRun, TimeSpan runInterval) : this(name, methodToRun,
     EnumScheduledTaskType.Normal_ElapsedTime)
    {
        RunInterval = runInterval;
        SetNextRunTime();
    }



    /// <summary>
    /// Construct a scheduledtask that is based upon a custom scheduling algorithm (non elapsed time based)
    /// </summary>
    /// <param name="name">Name of this task</param>
    /// <param name="methodToRun">The method to run.  It must accept an object and return a bool indicating success or failure</param>
    /// <param name="scheduledTaskType">What type of scheduling algorithm to use.</param>
    public InternalScheduledTask(string name, Func<InternalScheduledTask, Task<bool>> methodToRun, EnumScheduledTaskType scheduledTaskType)
    {
        Name              = name;
        TaskMethod        = methodToRun;
        ScheduledTaskType = scheduledTaskType;
        Id                = Guid.NewGuid();
    }



    /// <summary>
    /// Schedules the next run time for this task.
    /// </summary>
    public void SetNextRunTime() { NextScheduledRunTime = DateTime.Now + RunInterval; }
}