namespace SlugEnt;

/// <summary>
/// The return values from a Task that has been requested to be run.
/// </summary>
public enum EnumInternalTaskReturn
{
    /// <summary>
    /// Task was run and ran successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Task was run, but failed.
    /// </summary>
    Failed = 20,

    /// <summary>
    /// Task was not run for some reason, maybe required resources (DB, MQ Stream, etc) were not available.
    /// </summary>
    NotRunMissingResources = 30,

    /// <summary>
    /// Task started, but did no processing as there was no data for it to process.
    /// </summary>
    NotRunNoData = 31,
}