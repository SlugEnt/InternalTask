using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using SlugEnt;

namespace Test_InternalTasks;

[TestFixture]
public class Test_ParallelTasks
{
    private DateTime task1_Start;
    private DateTime task2_Start;
    private DateTime task3_Start;

    private bool _task1Running;
    private bool _task2Running;
    private bool _task3Running;

    private const int taskDelay = 200;


    [SetUp]
    public void Setup()
    {
        _task1Running = false;
        _task2Running = false;
        _task3Running = false;
    }



    [Test]
    [Order(10)]
    public async Task RunTasksInParellel_IsTrue()
    {
        int                   runInterval = 500;
        InternalTaskScheduler scheduler   = new InternalTaskScheduler();
        scheduler.RunTasksInParallel = true;

        TimeSpan timeInterval = TimeSpan.FromMilliseconds(runInterval);
        scheduler.AddTask(new InternalScheduledTask("task1", TestParallelTaskMethod, timeInterval));
        scheduler.AddTask(new InternalScheduledTask("task2", TestParallelTaskMethod, timeInterval));
        scheduler.AddTask(new InternalScheduledTask("task3", TestParallelTaskMethod, timeInterval));

        Assert.AreEqual(3, scheduler.TasksCount, "A10");
        Assert.AreEqual(3, scheduler.ScheduledTaskCount, "A20");

        // Wait for tasks to be scheduled
        Thread.Sleep(runInterval + 2);

        scheduler.CheckTasks();
        Thread.Sleep(10);
        Assert.IsTrue(_task1Running, "A100");
        Assert.IsTrue(_task2Running, "A110");
        Assert.IsTrue(_task3Running, "A120");

        Assert.AreEqual(3, scheduler.ExecutedTaskCount, "A30");
    }



    [Test]
    [Order(20)]
    public async Task RunTasksInParellel_IsFalse()
    {
        int                   runInterval = 500;
        InternalTaskScheduler scheduler   = new InternalTaskScheduler();
        scheduler.RunTasksInParallel = false;

        TimeSpan timeInterval = TimeSpan.FromMilliseconds(runInterval);
        scheduler.AddTask(new InternalScheduledTask("task1", TestParallelTaskMethod, timeInterval));
        scheduler.AddTask(new InternalScheduledTask("task2", TestParallelTaskMethod, timeInterval));
        scheduler.AddTask(new InternalScheduledTask("task3", TestParallelTaskMethod, timeInterval));

        Assert.AreEqual(3, scheduler.TasksCount, "A10");
        Assert.AreEqual(3, scheduler.ScheduledTaskCount, "A20");

        // Wait for tasks to be scheduled
        Thread.Sleep(runInterval + 2);

        await scheduler.CheckTasks();

        Assert.IsTrue(_task1Running, "A100");
        Assert.IsTrue(_task2Running, "A110");
        Assert.IsTrue(_task3Running, "A120");

        DateTime testValue = DateTime.Now;
        testValue = task1_Start.AddMilliseconds(taskDelay);
        Assert.Greater(task2_Start, testValue, "A310");

        testValue = task2_Start.AddMilliseconds(taskDelay);
        Assert.Greater(task3_Start, testValue, "A300");

        Assert.AreEqual(3, scheduler.ExecutedTaskCount, "A30");
    }



    /// <summary>
    /// Method called by the Parallel Task Tests
    /// </summary>
    /// <param name="internalScheduledTask"></param>
    /// <returns></returns>
    private async Task<EnumInternalTaskReturn> TestParallelTaskMethod(InternalScheduledTask internalScheduledTask)
    {
        DateTime current = DateTime.Now;

        if (internalScheduledTask.Name == "task1")
        {
            task1_Start   = current;
            _task1Running = true;
        }
        else if (internalScheduledTask.Name == "task2")
        {
            task2_Start   = current;
            _task2Running = true;
        }
        else if (internalScheduledTask.Name == "task3")
        {
            task3_Start   = current;
            _task3Running = true;
        }

        await DelayTime(taskDelay);


        Thread.Sleep(taskDelay);


        //RunSynchronously();

        /*
        if (internalScheduledTask.Name == "task1")
            _task1Running = false;
        else if (internalScheduledTask.Name == "task2")
            _task2Running = false;
        else if (internalScheduledTask.Name == "task3")
            _task3Running = false;
        */
        return EnumInternalTaskReturn.Success;
    }


    private async Task DelayTime(int ms) { await Task.Delay(ms); }
}