namespace ShipExecAgent.Shared.Models;

/// <summary>
/// The kind of change represented by a <see cref="UserVariance"/>.
/// </summary>
public enum UserVarianceKind
{
    Create,
    Edit,
    Delete,
}

/// <summary>
/// A pending user change queued for deferred apply (analogous to
/// <c>PSI.Sox.BusinessLogic.Variance</c> for shippers).
/// </summary>
public class UserVariance
{
    public Guid             Id          { get; set; } = Guid.NewGuid();
    public UserVarianceKind Kind        { get; set; }

    // ── Identity ────────────────────────────────────────────────────────────
    /// <summary>The target user's Id (empty for Create).</summary>
    public Guid   UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;

    // ── Payload (one of these is populated depending on Kind) ───────────────
    /// <summary>Pre-filled data for a Create operation.</summary>
    public UserAddItem?  CreatePrefill { get; set; }

    /// <summary>Field edits for an Edit operation.</summary>
    public UserEditItem? EditItem      { get; set; }

    // ── Display ─────────────────────────────────────────────────────────────
    public string Description { get; set; } = string.Empty;
}
