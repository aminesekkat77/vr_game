using System.Collections;
using UnityEngine;

namespace MartialArtsGame
{
    public class MartialArtsGameManager : MonoBehaviour
    {
        public enum GameState { Menu, Countdown, Fighting, RoundEnd, Replay, Trailer }

        public static MartialArtsGameManager Instance;

        [Header("Selection")]
        public MartialArtStyle selectedStyle = MartialArtStyle.Karate;
        public DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;

        [Header("Round")]
        public float roundDuration = 60f;
        public int currentRound = 1;
        public float roundTimer = 60f;

        [Header("References")]
        public PlayerCombatController player;
        public AIOpponentController opponent;
        public MartialArtsHUD hud;
        public MainMenuController menu;
        public CoachReplayController replay;
        public TrailerModeController trailer;
        public ScoreAndStatsManager stats;
        public SimpleCameraController fightCamera;
        public FightHandsVisualizer fightHands;

        public GameState State { get; private set; } = GameState.Menu;

        // Names the procedural rig auto-creates on a fighter when those
        // children don't already exist. On the AI's "Male Karate" prefab they
        // don't exist, so the rig spawns four bright-red primitive cubes that
        // hover on the opponent — those are the red squares we want gone.
        static readonly string[] RigLimbNames = { "L_Hand", "R_Hand", "L_Foot", "R_Foot" };
        bool rigCubesHidden;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Hide the red primitive cubes the ProceduralMartialArtsAnimator spawns
        // when it can't find authored children with its expected names. The
        // transforms keep animating underneath (so attacks still resolve);
        // they're just invisible. The player's authored feet/hands meshes are
        // untouched, so the player keeps their grounded foot representation.
        void HideRigCubes()
        {
            if (rigCubesHidden) return;
            var rigs = FindObjectsByType<ProceduralMartialArtsAnimator>(FindObjectsSortMode.None);
            foreach (var rig in rigs)
            {
                if (rig == null) continue;
                foreach (var name in RigLimbNames)
                {
                    var t = rig.transform.Find(name);
                    if (t == null) continue;
                    var mf = t.GetComponent<MeshFilter>();
                    // Only kill renderers on the auto-built primitive cubes —
                    // authored limb meshes (skinned, custom-named meshes) are
                    // safe and stay visible.
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name == "Cube")
                    {
                        var r = t.GetComponent<Renderer>();
                        if (r != null) r.enabled = false;
                    }
                }
            }
            rigCubesHidden = true;
        }

        void Start()
        {
            ShowMenu();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) && State == GameState.Menu) StartTrailer();
            if (Input.GetKeyDown(KeyCode.R) && State == GameState.Fighting) RestartRound();
            if (Input.GetKeyDown(KeyCode.Escape) && State == GameState.Fighting) ShowMenu();

            if (State == GameState.Fighting)
            {
                roundTimer -= Time.deltaTime;
                if (roundTimer <= 0f)
                {
                    roundTimer = 0f;
                    EndRound("Time");
                }
                else if (player != null && player.health <= 0f) EndRound("KO");
                else if (opponent != null && opponent.health <= 0f) EndRound("KO");
            }
        }

        public void ShowMenu()
        {
            State = GameState.Menu;
            Time.timeScale = 1f;
            if (menu != null) menu.Show(true);
            if (hud != null) hud.SetVisible(false);
            if (player != null) player.SetCombatActive(false);
            if (opponent != null) opponent.SetCombatActive(false);
            if (trailer != null) trailer.StopTrailer();
            if (replay != null) replay.HideAll();
            if (fightCamera != null) fightCamera.UseMenuView();
            if (fightHands != null) fightHands.SetActive(false);
        }

        public void StartFight()
        {
            if (menu != null) menu.Show(false);
            if (hud != null) { hud.SetVisible(true); hud.SetStyleAndDifficulty(selectedStyle, selectedDifficulty); }
            if (replay != null) replay.HideAll();
            if (player != null) player.PrepareForFight(selectedStyle);
            if (opponent != null) opponent.PrepareForFight(selectedStyle, selectedDifficulty);
            if (stats != null) stats.ResetRound();
            if (fightCamera != null) fightCamera.UseFightView();
            // Show the visible "controllers" the moment Start is pressed.
            if (fightHands != null) fightHands.SetActive(true);
            // Suppress the procedural rig's auto-built red cubes (player keeps
            // its authored foot / body meshes visible).
            HideRigCubes();

            roundTimer = roundDuration;
            currentRound = 1;
            StartCoroutine(CountdownAndFight());
        }

        IEnumerator CountdownAndFight()
        {
            State = GameState.Countdown;
            if (player != null) player.SetCombatActive(false);
            if (opponent != null) opponent.SetCombatActive(false);
            string[] beats = { "3", "2", "1", "FIGHT!" };
            foreach (var b in beats)
            {
                if (hud != null) hud.ShowBigText(b, 0.85f);
                yield return new WaitForSeconds(0.9f);
            }
            State = GameState.Fighting;
            if (player != null) player.SetCombatActive(true);
            if (opponent != null) opponent.SetCombatActive(true);
        }

        public void EndRound(string reason)
        {
            if (State == GameState.RoundEnd || State == GameState.Replay) return;
            State = GameState.RoundEnd;
            if (player != null) player.SetCombatActive(false);
            if (opponent != null) opponent.SetCombatActive(false);
            if (fightHands != null) fightHands.SetActive(false);

            string outcome;
            if (player != null && opponent != null)
            {
                if (player.health <= 0f && opponent.health <= 0f) outcome = "DRAW";
                else if (player.health <= 0f) outcome = "YOU LOST";
                else if (opponent.health <= 0f) outcome = "YOU WIN!";
                else outcome = player.health >= opponent.health ? "YOU WIN!" : "YOU LOST";
            }
            else outcome = "ROUND END";

            if (hud != null) hud.ShowBigText(outcome, 1.4f);
            if (replay != null) replay.PlayReplay(outcome, stats);
        }

        public void RestartRound()
        {
            if (replay != null) replay.HideAll();
            StartFight();
        }

        public void StartTrailer()
        {
            State = GameState.Trailer;
            if (menu != null) menu.Show(false);
            if (hud != null) hud.SetVisible(false);
            if (replay != null) replay.HideAll();
            if (trailer != null) trailer.StartTrailer();
        }

        public void ReturnToMenuFromTrailer() { ShowMenu(); }

        public void OnSelectStyle(int idx) { selectedStyle = (MartialArtStyle)Mathf.Clamp(idx, 0, 3); }
        public void OnSelectDifficulty(int idx) { selectedDifficulty = (DifficultyLevel)Mathf.Clamp(idx, 0, 3); }
        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
