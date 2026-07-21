using System.Runtime.CompilerServices;

// Expose internal lifecycle hooks (Update, Reset, Abort, IsRunning, …) to the
// test assembly so behaviour can be verified white-box without widening the
// public API.
[assembly: InternalsVisibleTo("SatyBT.Tests")]
