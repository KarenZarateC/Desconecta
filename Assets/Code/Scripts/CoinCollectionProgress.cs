using System.Collections.Generic;

namespace Desconecta
{
    /// <summary>Tracks the registered coin identifiers for the current run, calculates the collection bonus, and ensures each coin can reward the player only once.</summary>
    public sealed class CoinCollectionProgress
    {
        /// <summary>Defines the score bonus awarded for each successfully collected coin.</summary>
        public const int PointsPerCoin = 10;
        /// <summary>Contains the unique identifiers of all coins registered as collectible in the current run.</summary>
        private readonly HashSet<string> available = new HashSet<string>();
        /// <summary>Stores the identifiers of coins already collected during the current run to prevent duplicate rewards.</summary>
        private readonly HashSet<string> collected = new HashSet<string>();
        /// <summary>Returns the total number of unique coins registered as collectible for the current run.</summary>
        public int Total => available.Count;
        /// <summary>Returns the number of unique coins collected by the player during the current run.</summary>
        public int Collected => collected.Count;
        /// <summary>Calculates the total collection bonus by multiplying the collected coin count by the points awarded per coin.</summary>
        public int BonusScore => Collected * PointsPerCoin;
        /// <summary>Indicates whether the player has collected every registered coin and at least one collectible exists.</summary>
        public bool Complete => Total > 0 && Collected == Total;

        /// <summary>Initializes the collection progress with the valid, unique coin identifiers available in the current run.</summary>
        public CoinCollectionProgress(IEnumerable<string> ids)
        {
            if (ids == null) return;
            foreach (string id in ids)
                if (!string.IsNullOrWhiteSpace(id)) available.Add(id);
        }

        /// <summary>Attempts to collect a registered coin and returns true only when it grants its reward for the first time.</summary>
        public bool TryCollect(string id)
        {
            return id != null && available.Contains(id) && collected.Add(id);
        }

        /// <summary>Clears the collected coin state while preserving the registered coins for a new collection attempt.</summary>
        public void Reset() { collected.Clear(); }
    }
}
