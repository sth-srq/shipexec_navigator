using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipExecNavigator.BusinessLogic.EntityComparison
{
    public class Variance
    {
        public string EntityName { get; set; }

        public object OriginalObject { get; set; }

        public object NewObject { get; set; }

        public bool IsAdd { get; set; } = false;

        public bool IsRemove { get; set; } = false;

        public bool IsUpdated { get; set; } = false;

        /// <summary>
        /// For aggregate entities (e.g. Site), holds the individual child-entity
        /// variances used for display.  The parent variance still drives the API
        /// request (e.g. UpdateSite); these are display-only.
        /// </summary>
        public List<Variance> ChildVariances { get; set; } = new List<Variance>();

        /// <summary>
        /// Human-readable label for the parent entity, used in the variance display
        /// (e.g. "Site: Chicago").
        /// </summary>
        public string ParentContext { get; set; } = string.Empty;

        /// <summary>
        /// When this variance belongs to a Site sub-collection (e.g. a Machine inside
        /// a Site), holds the Site's Id so generators can populate SiteId on their
        /// Add/Update/Remove requests.
        /// </summary>
        public Guid? ParentSiteId { get; set; }
        public Guid  CompanyId    { get; set; }

        // ── UI / tracking properties (migrated from VarianceEntry) ────────────

        public Guid     Id                { get; set; } = Guid.NewGuid();
        public Guid     NodeId            { get; set; }
        public string   PathDescription   { get; set; } = string.Empty;
        public string   Description       { get; set; } = string.Empty;
        public string   ChangeType        { get; set; } = string.Empty;
        public DateTime Timestamp         { get; set; } = DateTime.Now;
        public string   UndoAttributeName { get; set; }
        /// <summary>XmlNodeViewModel at WinForm call sites; typed as object to avoid cross-project dependency.</summary>
        public object   SnapshotNode      { get; set; }
        public Guid?    SnapshotParentId  { get; set; }
        public int      SnapshotIndex     { get; set; } = -1;
        public string   OriginalXML       { get; set; } = string.Empty;
        public string   NewXML            { get; set; } = string.Empty;
        public bool     IsHistorical      { get; set; }
        public string   Comments          { get; set; }
        public string   VarianceJson      { get; set; }
        public bool     IsUndone          { get; set; }
        public bool     IsRevert          { get; set; }
        /// <summary>True once this variance has been successfully applied to the live server.</summary>
        public bool     IsApplied         { get; set; }
    }
}
