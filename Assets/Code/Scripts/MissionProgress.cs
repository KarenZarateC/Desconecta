using System.Collections.Generic;

namespace Desconecta
{
    /// <summary>Owns scoring and time independently from rendering and input.</summary>
    public sealed class MissionProgress
    {
        private readonly HashSet<string> completed = new HashSet<string>();
        public int Total { get; }
        public int Completed => completed.Count;
        public int Score { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool Unlimited { get; set; }
        public bool Won => Completed == Total;
        public bool Expired => !Unlimited && RemainingSeconds <= 0f;
        public bool Finished => Won || Expired;

        public MissionProgress(int total, float duration = 600f)
        {
            Total = System.Math.Max(1, total);
            RemainingSeconds = System.Math.Max(0f, duration);
        }

        public bool Resolve(string id)
        {
            // A completed object can never award points twice.
            if (Finished || string.IsNullOrWhiteSpace(id) || !completed.Add(id)) return false;
            Score += 100;
            return true;
        }

        public void Penalize()
        {
            if (!Finished) Score = System.Math.Max(0, Score - 25);
        }

        public void Tick(float deltaTime, bool running)
        {
            // Instructions, decisions, and pause screens never consume play time.
            if (!running || Unlimited || Finished || deltaTime <= 0) return;
            RemainingSeconds = System.Math.Max(0f, RemainingSeconds - deltaTime);
        }
    }
}
