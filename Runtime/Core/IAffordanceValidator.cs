namespace RealPlayTester.Core
{
    /// <summary>
    /// Interface for components that can validate interaction requests.
    /// Implement this on your MonoBehaviours to tell the AI why it can't click something.
    /// </summary>
    public interface IAffordanceValidator
    {
        /// <summary>
        /// Checks if the specified intent (e.g., "Click", "Drag") is currently allowed.
        /// </summary>
        bool CanInteract(string intent);

        /// <summary>
        /// Returns a human-readable reason if CanInteract returns false.
        /// </summary>
        string GetBlockReason();
    }
}