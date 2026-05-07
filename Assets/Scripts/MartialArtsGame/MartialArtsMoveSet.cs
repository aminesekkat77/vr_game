using System.Collections.Generic;
using UnityEngine;

namespace MartialArtsGame
{
    // Available martial arts styles for the sparring game.
    public enum MartialArtStyle { Karate, MuayThai, MMA, BrazilianJiuJitsu }

    // AI difficulty selections from the main menu.
    public enum DifficultyLevel { Easy, Normal, Hard, Adaptive }

    // Strike categories used for animation routing and scoring.
    public enum MoveType { LeftPunch, RightPunch, Kick, Elbow, Knee, RoundKick, FrontKick, Special, Grapple }

    // Single move definition: range, damage, stamina cost, animation id, etc.
    [System.Serializable]
    public class MartialArtsMove
    {
        public string displayName;
        public MoveType moveType;
        public float range = 1.6f;        // Max distance from target center to land hit
        public float damage = 10f;        // HP removed on a clean (unblocked) hit
        public float staminaCost = 8f;    // Stamina drained from attacker on use
        public float startupTime = 0.18f; // Time before active hit window opens
        public float activeTime = 0.16f;  // How long the hit window stays open
        public float recoveryTime = 0.30f;// Locked-out cooldown after the strike
        public float scoreValue = 20f;    // Base score for landing the move
    }

    // Full per-style move table built once and cached.
    public static class MartialArtsMoveSet
    {
        public static List<MartialArtsMove> GetMoves(MartialArtStyle style)
        {
            var moves = new List<MartialArtsMove>();
            switch (style)
            {
                case MartialArtStyle.Karate:
                    moves.Add(new MartialArtsMove { displayName = "Jab",         moveType = MoveType.LeftPunch,  range = 1.7f * 2, damage = 8,  staminaCost = 6,  startupTime = 0.10f, activeTime = 0.14f, recoveryTime = 0.22f, scoreValue = 15 });
                    moves.Add(new MartialArtsMove { displayName = "Cross",       moveType = MoveType.RightPunch, range = 1.8f * 2, damage = 12, staminaCost = 8,  startupTime = 0.14f, activeTime = 0.14f, recoveryTime = 0.30f, scoreValue = 22 });
                    moves.Add(new MartialArtsMove { displayName = "Front Kick",  moveType = MoveType.FrontKick,  range = 2.1f * 2, damage = 15, staminaCost = 14, startupTime = 0.22f, activeTime = 0.18f, recoveryTime = 0.40f, scoreValue = 28 });
                    moves.Add(new MartialArtsMove { displayName = "Counter",     moveType = MoveType.Special,    range = 1.9f * 2, damage = 18, staminaCost = 18, startupTime = 0.10f, activeTime = 0.16f, recoveryTime = 0.45f, scoreValue = 35 });
                    break;
                case MartialArtStyle.MuayThai:
                    moves.Add(new MartialArtsMove { displayName = "Jab",         moveType = MoveType.LeftPunch,  range = 1.7f * 2, damage = 8,  staminaCost = 6,  startupTime = 0.12f, activeTime = 0.14f, recoveryTime = 0.24f, scoreValue = 15 });
                    moves.Add(new MartialArtsMove { displayName = "Elbow",       moveType = MoveType.Elbow,      range = 1.4f * 2, damage = 18, staminaCost = 12, startupTime = 0.16f, activeTime = 0.14f, recoveryTime = 0.32f, scoreValue = 30 });
                    moves.Add(new MartialArtsMove { displayName = "Knee",        moveType = MoveType.Knee,       range = 1.5f * 2, damage = 16, staminaCost = 12, startupTime = 0.20f, activeTime = 0.16f, recoveryTime = 0.36f, scoreValue = 28 });
                    moves.Add(new MartialArtsMove { displayName = "Round Kick",  moveType = MoveType.RoundKick,  range = 2.2f * 2, damage = 20, staminaCost = 18, startupTime = 0.26f, activeTime = 0.20f, recoveryTime = 0.44f, scoreValue = 35 });
                    moves.Add(new MartialArtsMove { displayName = "Low Kick",    moveType = MoveType.RoundKick,  range = 2.0f * 2, damage = 13, staminaCost = 12, startupTime = 0.20f, activeTime = 0.18f, recoveryTime = 0.38f, scoreValue = 26 });
                    moves.Add(new MartialArtsMove { displayName = "Superman",    moveType = MoveType.Special,    range = 2.1f * 2, damage = 22, staminaCost = 22, startupTime = 0.18f, activeTime = 0.16f, recoveryTime = 0.50f, scoreValue = 38 });
                    break;
                case MartialArtStyle.BrazilianJiuJitsu:
                    moves.Add(new MartialArtsMove { displayName = "Push",        moveType = MoveType.LeftPunch,  range = 1.5f * 2, damage = 5,  staminaCost = 5,  startupTime = 0.10f, activeTime = 0.12f, recoveryTime = 0.20f, scoreValue = 10 });
                    moves.Add(new MartialArtsMove { displayName = "Body Lock",   moveType = MoveType.Grapple,    range = 1.4f * 2, damage = 14, staminaCost = 16, startupTime = 0.18f, activeTime = 0.20f, recoveryTime = 0.45f, scoreValue = 35 });
                    moves.Add(new MartialArtsMove { displayName = "Knee Strike", moveType = MoveType.Knee,       range = 1.4f * 2, damage = 12, staminaCost = 10, startupTime = 0.18f, activeTime = 0.14f, recoveryTime = 0.30f, scoreValue = 22 });
                    moves.Add(new MartialArtsMove { displayName = "Takedown",    moveType = MoveType.Special,    range = 1.5f * 2, damage = 18, staminaCost = 22, startupTime = 0.22f, activeTime = 0.22f, recoveryTime = 0.55f, scoreValue = 40 });
                    break;
            }
            return moves;
        }

        // Convenience shortcut for the four player input bindings.
        public static MartialArtsMove GetPlayerMove(MartialArtStyle style, MoveType slot)
        {
            var moves = GetMoves(style);
            // Slot mapping: LeftPunch=0, RightPunch=1, Kick=2, Special=3.
            switch (slot)
            {
                case MoveType.LeftPunch:  return moves[0];
                case MoveType.RightPunch: return moves[1];
                case MoveType.Kick:       return moves[2];
                case MoveType.Special:    return moves[3];
            }
            return moves[0];
        }
    }
}
