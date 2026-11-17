using System.Collections.Generic;

namespace Desconecta
{
    /// <summary>Tracks only registered coins; each can reward a run once.</summary>
    public sealed class CoinCollectionProgress
    {
        public const int PointsPerCoin = 10;
        private readonly HashSet<string> available = new HashSet<string>();
        private readonly HashSet<string> collected = new HashSet<string>();
        public int Total => available.Count;
        public int Collected => collected.Count;
        public int BonusScore => Collected * PointsPerCoin;
        public bool Complete => Total > 0 && Collected == Total;

        public CoinCollectionProgress(IEnumerable<string> ids)
        {
            if (ids == null) return;
            foreach (string id in ids)
                if (!string.IsNullOrWhiteSpace(id)) available.Add(id);
        }

        public bool TryCollect(string id)
        {
            return id != null && available.Contains(id) && collected.Add(id);
        }

        public void Reset() { collected.Clear(); }
    }
}
