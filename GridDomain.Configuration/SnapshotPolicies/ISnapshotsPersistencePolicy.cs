﻿namespace GridDomain.Configuration.SnapshotPolicies {
    public interface ISnapshotsPersistencePolicy :ISnapshotsSavePolicy, ISnapshotsDeletePolicy
    {
        bool ShouldDelete(out SnapshotSelectionCriteria selection);
        void MarkSnapshotApplied(long seqNum);
    }
}