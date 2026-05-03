namespace ShipExecAgent.BusinessLogic.ResponseModel
{
    /// <summary>
    /// Returned by <c>AppManager.ApplyVarianceEntry</c> for each variance sent
    /// to the ShipExec admin API.
    /// </summary>
    public sealed class ApplyChangeResult
    {
        /// <summary>Whether the server accepted and applied the change.</summary>
        public bool   Success     { get; set; }

        /// <summary>Human-readable status message from the server or from a local exception.</summary>
        public string Message     { get; set; } = string.Empty;

        /// <summary>The <c>PathDescription</c> of the originating <see cref="Variance"/>.</summary>
        public string EntityPath  { get; set; } = string.Empty;

        /// <summary>The <c>ChangeType</c> of the originating entry ("Modified", "Added", "Removed").</summary>
        public string ChangeType  { get; set; } = string.Empty;

        /// <summary>Raw JSON body returned by the server, preserved for diagnostics.</summary>
        public string? RawResponse { get; set; }

        /// <summary>True when the entry was skipped (already undone or a revert marker).</summary>
        public bool   WasSkipped  { get; set; }

        /// <summary>The entity name from the originating <see cref="CompanyBuilder.RequestBaseWithURL"/> (e.g. "Site", "Client").</summary>
        public string EntityName  { get; set; } = string.Empty;

        /// <summary>The CRUD operation: "Add", "Remove", or "Update".</summary>
        public string Operation   { get; set; } = string.Empty;

        /// <summary>The API endpoint URL that was (or would have been) called.</summary>
        public string Endpoint    { get; set; } = string.Empty;

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a result representing a locally-skipped entry that required
        /// no server round-trip.
        /// </summary>
        public static ApplyChangeResult Skipped(string entityPath) => new()
        {
            Success    = true,
            WasSkipped = true,
            Message    = "Skipped — entry is undone or a revert marker.",
            EntityPath = entityPath,
        };
    }
}
