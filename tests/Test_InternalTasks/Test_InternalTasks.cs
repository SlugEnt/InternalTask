using NUnit.Framework;
using SlugEnt;

namespace Test_InternalTasks;

[TestFixture]
public class Test_InternalTasks
{
    /// <summary>
    /// Test that scheduled task does not run tasks that are not past their next scheduled run time
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ScheduledTaskNotYetReadyDoesNotRun()
    {
        InternalTaskScheduler scheduler = new InternalTaskScheduler();

        InternalScheduledTask internalScheduledTask = new("taska", TestTaskMethod, TimeSpan.FromMilliseconds(200));
        scheduler.AddTask(internalScheduledTask);
        await scheduler.CheckTasks();

        Assert.AreEqual(1, scheduler.TasksCount, "A10");
        Assert.AreEqual(1, scheduler.ScheduledTaskCount, "A20");
        Assert.AreEqual(0, scheduler.ExecutedTaskCount, "A30");
    }


    /// <summary>
    /// Test that scheduled task runs if it is past its scheduled time
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ScheduledTaskPastScheduledTimeRuns()
    {
        InternalTaskScheduler scheduler = new InternalTaskScheduler();

        InternalScheduledTask internalScheduledTask = new("taska", TestTaskMethod, TimeSpan.FromMilliseconds(0));
        scheduler.AddTask(internalScheduledTask);
        Thread.Sleep(1);
        await scheduler.CheckTasks();

        Assert.AreEqual(1, scheduler.TasksCount, "A10");
        Assert.AreEqual(1, scheduler.ScheduledTaskCount, "A20");
        Assert.AreEqual(1, scheduler.ExecutedTaskCount, "A30");
        Assert.IsTrue(internalScheduledTask.IsScheduled, "A40:");
    }



    /// <summary>
    /// Test that scheduled task that runs then sets the next scheduled time to future
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ScheduledTaskInFutureAfteRun()
    {
        InternalTaskScheduler scheduler = new InternalTaskScheduler();

        InternalScheduledTask internalScheduledTask = new("taska", TestTaskMethod, TimeSpan.FromMilliseconds(100));
        scheduler.AddTask(internalScheduledTask);
        Thread.Sleep(200);
        await scheduler.CheckTasks();

        Assert.AreEqual(1, scheduler.TasksCount, "A10");
        Assert.AreEqual(1, scheduler.ScheduledTaskCount, "A20");
        Assert.AreEqual(1, scheduler.ExecutedTaskCount, "A30");
        Assert.IsTrue(internalScheduledTask.IsScheduled, "A40:");
        Assert.GreaterOrEqual(internalScheduledTask.NextScheduledRunTime, DateTime.Now, "A50:");
    }



    /// <summary>
    /// When First constructred the IsScheduled should be false.
    /// </summary>
    [Test]
    public void IsScheduled_IsFalseAfterConstruction()
    {
        InternalScheduledTask task = new InternalScheduledTask("abc", TestTaskMethod, TimeSpan.FromSeconds(2));
        Assert.Less(DateTime.Now, task.NextScheduledRunTime, "A10:");
        Assert.IsFalse(task.IsScheduled, "A20:");
    }


    /// <summary>
    /// Method called by the scheduled tasks
    /// </summary>
    /// <param name="internalScheduledTask"></param>
    /// <returns></returns>
    private Task<bool> TestTaskMethod(InternalScheduledTask internalScheduledTask)
    {
        Task.Delay(1000);
        return Task.FromResult(true);
    }
}