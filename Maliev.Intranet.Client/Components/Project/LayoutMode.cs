namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Controls whether the project editor shows the 3-D configurator or the summary table.</summary>
public enum LayoutMode
{
    /// <summary>Shows the part carousel + detail card for configuring individual parts.</summary>
    Configurator,

    /// <summary>Shows the summary table with all parts listed (implemented in Task 17).</summary>
    SummaryTable,
}
