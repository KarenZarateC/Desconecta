using System.Collections.Generic;

namespace Desconecta
{
    /// <summary>Owns scoring and time independently from rendering and input.</summary>
    public sealed class MissionProgress
    {
        // Unique identifiers prevent a resolved situation from awarding points twice.
        private readonly HashSet<string> completed = new HashSet<string>();
        /// <summary>Total number of situations required by the current mission.</summary>
        public int Total { get; }
        /// <summary>Number of unique situations resolved so far.</summary>
        public int Completed => completed.Count;
        /// <summary>Current mission score after rewards and penalties.</summary>
        public int Score { get; private set; }
        /// <summary>Remaining active-play time in seconds.</summary>
        public float RemainingSeconds { get; private set; }
        /// <summary>When enabled, prevents active play from consuming the mission timer.</summary>
        public bool Unlimited { get; set; }
        /// <summary>Indicates that every required situation has been resolved.</summary>
        public bool Won => Completed == Total;
        /// <summary>Indicates that limited time has reached zero.</summary>
        public bool Expired => !Unlimited && RemainingSeconds <= 0f;
        /// <summary>Indicates that the mission can no longer accept progress because it was won or expired.</summary>
        public bool Finished => Won || Expired;

        /// <summary>Creates a mission tracker with a valid objective count and initial duration.</summary>
        public MissionProgress(int total, float duration = 600f)
        {
            Total = System.Math.Max(1, total);
            RemainingSeconds = System.Math.Max(0f, duration);
        }

        /// <summary>Resolves a unique situation, awards points once, and reports whether progress was accepted.</summary>
        public bool Resolve(string id)
        {
            // A completed object can never award points twice.
            if (Finished || string.IsNullOrWhiteSpace(id) || !completed.Add(id)) return false;
            Score += 100;
            return true;
        }

        /// <summary>Removes the mistake penalty without allowing the score to become negative.</summary>
        public void Penalize()
        {
            if (!Finished) Score = System.Math.Max(0, Score - 25);
        }

        /// <summary>Consumes active-play time only while the mission is running and not configured as unlimited.</summary>
        public void Tick(float deltaTime, bool running)
        {
            // Instructions, decisions, and pause screens never consume play time.
            if (!running || Unlimited || Finished || deltaTime <= 0) return;
            RemainingSeconds = System.Math.Max(0f, RemainingSeconds - deltaTime);
        }
    }
}
