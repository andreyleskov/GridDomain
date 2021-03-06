﻿namespace GridDomain.Configuration.SnapshotPolicies {
    public interface IOperationTracker<in T>
    {
        int  InProgress { get; }
        void Start(T criteria);
        void Complete(T instance);
        void Fail(T instance);
                               
    }
}