namespace SlugEnt;

public enum EnumScheduledTaskType
{
    /// <summary>
    /// Elapsed time, so every X seconds or minutes or hours or days.
    /// </summary>
    Normal_ElapsedTime = 0,

    /// <summary>
    /// Only on Specific Days.  Note caller must call the SpecificTimePeriods method to set when it should be scheduled.
    /// </summary>
    SpecificDays = 1
}