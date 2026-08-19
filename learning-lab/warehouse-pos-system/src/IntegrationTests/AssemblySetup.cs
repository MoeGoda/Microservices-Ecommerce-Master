using Xunit;

// This suite is a black-box caller against ONE live, shared external stack —
// every test's "before" snapshot and "after" assertion has to see a
// consistent world. [Collection("Gateway")] alone wasn't enough to stop two
// test bodies from actually overlapping in practice (confirmed by wall-clock
// logging: two Facts' bodies both mid-execution within 150ms of each other),
// so parallelization is disabled for the whole assembly as the authoritative
// guarantee, not just a per-collection one.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
