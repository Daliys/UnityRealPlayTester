using System;
using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Represents a building placement attempt for diagnostics.
    /// </summary>
    public class PlacementAttempt
    {
        public Vector2Int Position { get; set; }
        public string DefinitionId { get; set; }
        public string Result { get; set; }

        public PlacementAttempt(Vector2Int position, string definitionId, string result)
        {
            Position = position;
            DefinitionId = definitionId;
            Result = result;
        }
    }
}
