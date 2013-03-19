namespace Aurora.Framework
{
    public interface IPhysicsStateModule
    {
        /// <summary>
        ///     Saves a state for all active objects in the region so that it can be reloaded later
        /// </summary>
        void SavePhysicsState();

        /// <summary>
        ///     Reset the physics scene to the last saved physics state (with SavePhysicsState())
        /// </summary>
        void ResetToLastSavedState();

        /// <summary>
        ///     Start saving the states that will be reverted with StartPhysicsTimeReversal()
        /// </summary>
        void StartSavingPhysicsTimeReversalStates();

        /// <summary>
        ///     Stop saving the states that will be reverted with StartPhysicsTimeReversal()
        /// </summary>
        void StopSavingPhysicsTimeReversalStates();

        /// <summary>
        ///     Begin reverting prim velocities and positions backwards in time to what they were previously
        ///     Must have StartSavingPhysicsTimeReversalStates() called before it so that it reads the states
        /// </summary>
        void StartPhysicsTimeReversal();

        /// <summary>
        ///     Stop reverting prim velocities and positions backwards in time and let things happen forwards again
        /// </summary>
        void StopPhysicsTimeReversal();
    }
}