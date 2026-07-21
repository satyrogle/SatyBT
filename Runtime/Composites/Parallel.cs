using System;

namespace SatyBT
{
    /// <summary>
    /// Composite that ticks all of its children every tick until a policy
    /// resolves. A child that completes on an earlier tick is remembered and
    /// not ticked again, but its result still counts toward the policies, so
    /// children may finish on different ticks. When a policy resolves, any
    /// children still Running are aborted.
    ///
    /// If both the success and failure policies resolve on the same tick,
    /// success takes precedence.
    /// </summary>
    public sealed class Parallel : CompositeNode
    {
        /// <summary>When a parallel is considered to have succeeded.</summary>
        public enum SuccessPolicy
        {
            /// <summary>Succeed as soon as one child succeeds.</summary>
            RequireOne,

            /// <summary>Succeed only once every child has succeeded.</summary>
            RequireAll
        }

        /// <summary>When a parallel is considered to have failed.</summary>
        public enum FailurePolicy
        {
            /// <summary>Fail as soon as one child fails.</summary>
            RequireOne,

            /// <summary>Fail only once every child has failed.</summary>
            RequireAll
        }

        private readonly SuccessPolicy _successPolicy;
        private readonly FailurePolicy _failurePolicy;

        // Per-child completion flags, reused across runs. Grows only if the
        // child count grows; steady-state ticking allocates nothing.
        private bool[] _finished;
        private int _successCount;
        private int _failureCount;

        /// <summary>Create an empty parallel; add children with AddChild.</summary>
        public Parallel(SuccessPolicy successPolicy, FailurePolicy failurePolicy) : base(8)
        {
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
        }

        /// <summary>Create a parallel with the given children.</summary>
        public Parallel(SuccessPolicy successPolicy, FailurePolicy failurePolicy, params NodeBase[] children)
            : base(children)
        {
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
        }

        public override BTStatus Tick(float deltaTime)
        {
            int count = ChildCount;
            EnsureCapacity(count);

            for (int i = 0; i < count; i++)
            {
                if (_finished[i])
                    continue;

                BTStatus status = GetChild(i).Update(deltaTime);
                if (status == BTStatus.Running)
                    continue;

                _finished[i] = true;
                if (status == BTStatus.Success)
                    _successCount++;
                else
                    _failureCount++;
            }

            bool succeeded = _successPolicy == SuccessPolicy.RequireOne
                ? _successCount >= 1
                : _successCount >= count;

            if (succeeded)
            {
                AbortUnfinished();
                return BTStatus.Success;
            }

            bool failed = _failurePolicy == FailurePolicy.RequireOne
                ? _failureCount >= 1
                : _failureCount >= count;

            if (failed)
            {
                AbortUnfinished();
                return BTStatus.Failure;
            }

            return BTStatus.Running;
        }

        protected override void OnEnter()
        {
            EnsureCapacity(ChildCount);
            Array.Clear(_finished, 0, _finished.Length);
            _successCount = 0;
            _failureCount = 0;
        }

        protected override void OnReset()
        {
            if (_finished != null)
                Array.Clear(_finished, 0, _finished.Length);
            _successCount = 0;
            _failureCount = 0;
        }

        // Ensure the completion buffer holds at least 'required' slots,
        // preserving existing flags. Only allocates when the buffer must grow.
        private void EnsureCapacity(int required)
        {
            if (required < 1)
                required = 1;

            if (_finished == null)
            {
                _finished = new bool[required];
                return;
            }

            if (_finished.Length < required)
            {
                var grown = new bool[required];
                Array.Copy(_finished, grown, _finished.Length);
                _finished = grown;
            }
        }

        // Abort children that were still Running when a policy resolved.
        private void AbortUnfinished()
        {
            int count = ChildCount;
            for (int i = 0; i < count; i++)
            {
                if (i < _finished.Length && !_finished[i])
                {
                    GetChild(i).Abort();
                    _finished[i] = true;
                }
            }
        }
    }
}
