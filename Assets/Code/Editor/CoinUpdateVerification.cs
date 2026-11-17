using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Desconecta;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in local checks. Commands are consumed only from the ignored Temp folder.</summary>
[InitializeOnLoad]
public static class CoinUpdateVerification
{
    private const string Command = "Temp/CoinUpdateCommand.txt";
    private const string Result = "Temp/CoinUpdateResult.json";
    private static double nextPoll;
    private static TestRunnerApi runner;
    private static readonly List<Check> checks = new List<Check>();
    [Serializable] private sealed class Check { public string name; public bool passed; }
    [Serializable] private sealed class Report { public string unityVersion; public string status; public Check[] checks; public int coins; }

    static CoinUpdateVerification() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        if (SessionState.GetBool("Desconecta.VerifyCoins", false) && EditorApplication.isPlaying && GameSession.Instance != null)
        {
            SessionState.SetBool("Desconecta.VerifyCoins", false);
            GameSession.Instance.StartCoroutine(VerifyRoutine());
        }
        if (!File.Exists(Command)) return;
        string command = File.ReadAllText(Command).Trim(); File.Delete(Command);
        try
        {
            switch (command)
            {
                case "build": CoinUpdateBuilder.Build(); WriteResult("built"); break;
                case "play": EditorApplication.isPlaying = true; break;
                case "stop": EditorApplication.isPlaying = false; break;
                case "verify":
                    if (EditorApplication.isPlaying) GameSession.Instance.StartCoroutine(VerifyRoutine());
                    else { SessionState.SetBool("Desconecta.VerifyCoins",true); EditorApplication.isPlaying = true; }
                    break;
                case "tests": RunTests(); break;
                case "demo":
                    GameSession.Instance.Begin(); GameSession.Instance.explorer.enabled = false;
                    GameSession.Instance.explorer.viewCamera.transform.localRotation = Quaternion.Euler(12, 0, 0);
                    break;
                case "results":
                    var session = GameSession.Instance;
                    session.Begin();
                    foreach (var coin in session.coins.Take(18)) session.CollectCoin(coin);
                    foreach (var target in session.situations) { session.Inspect(target); session.Answer(target.correctOption); }
                    break;
                case "home": GameSession.Instance.explorer.enabled = true; GameSession.Instance.Home(); break;
                case "capture":
                    Directory.CreateDirectory("Documentation/Screenshots");
                    ScreenCapture.CaptureScreenshot("Documentation/Screenshots/Coin-" + GameSession.Instance.Screen + ".png");
                    break;
                default: throw new ArgumentException("Unknown local verification command.");
            }
        }
        catch (Exception error) { Debug.LogException(error); File.WriteAllText(Result, JsonUtility.ToJson(new Report { status = error.ToString() }, true)); }
    }

    private static void WriteResult(string status)
    {
        File.WriteAllText(Result, JsonUtility.ToJson(new Report { status = status, unityVersion = Application.unityVersion }, true));
    }

    private static void CheckResult(string name, bool passed)
    {
        checks.Add(new Check { name = name, passed = passed });
        if (!passed) Debug.LogError("COIN_VERIFY_FAILED: " + name);
    }

    [MenuItem("Desconecta/Coins/Verify in Play Mode")]
    public static void Verify()
    {
        if (GameSession.Instance == null) throw new InvalidOperationException("Enter Play Mode first.");
        GameSession.Instance.StartCoroutine(VerifyRoutine());
    }

    private static IEnumerator VerifyRoutine()
    {
        checks.Clear();
        var session = GameSession.Instance;
        bool reduced = session.ReducedMotion, muted = session.Muted, large = session.LargeText;
        float music = session.MusicVolume, effects = session.EffectsVolume;
        bool unlimited = session.UnlimitedTime;
        session.SetReducedMotion(false); session.SetUnlimited(true);
        session.Begin(); session.explorer.enabled = false;
        var controller = session.explorer.GetComponent<CharacterController>();
        yield return null;
        CheckResult("24 unique registered coins", session.coins.Length == 24 && session.Coins.Total == 24 && session.coins.Select(c => c.coinId).Distinct().Count() == 24);
        var reachable = CoinUpdateBuilder.ReachablePositions(session.explorer);
        CheckResult("All 24 coins reachable from player spawn with capsule clearance", session.coins.All(c => reachable.Any(p => Vector2.Distance(new Vector2(p.x,p.z), new Vector2(c.transform.position.x,c.transform.position.z)) < .1f)));
        CheckResult("Each coin has a model, spin and collect animation, trigger and kinematic body", session.coins.All(c => c.visual.GetComponentInChildren<MeshFilter>().sharedMesh != null && c.coinAnimation["CoinSpin"] != null && c.coinAnimation["CoinCollect"] != null && c.GetComponent<SphereCollider>().isTrigger && c.GetComponent<Rigidbody>().isKinematic));
        CheckResult("Camilo music loops and coin sound is assigned", session.musicSource.clip != null && session.musicSource.clip.name == "Musica de fondo" && session.musicSource.loop && session.musicSource.isPlaying && session.coinClip != null && session.coinClip.length > 0);
        var coin = session.coins[0];
        controller.enabled = false;
        session.explorer.transform.position = new Vector3(coin.transform.position.x, .1f, coin.transform.position.z - .8f);
        controller.enabled = true; Physics.SyncTransforms();
        CheckResult("Pickup works at trigger edge without requiring player to cross coin center", coin.TryPickup(controller));
        session.Begin(); session.explorer.enabled = false;
        yield return null;
        Quaternion rotation = coin.visual.localRotation;
        yield return new WaitForSeconds(.24f);
        CheckResult("Spin changes orientation over time", Quaternion.Angle(rotation, coin.visual.localRotation) > 5);
        session.Pause(); yield return null;
        rotation = coin.visual.localRotation; float time = session.Progress.RemainingSeconds;
        yield return new WaitForSeconds(.16f);
        CheckResult("Pause freezes coin motion and timer", Quaternion.Angle(rotation,coin.visual.localRotation) < .1f && Mathf.Approximately(time,session.Progress.RemainingSeconds));
        CheckResult("Pause blocks collection", !coin.TryPickup(controller) && session.Coins.Collected == 0);
        session.Resume(); session.Inspect(session.situations[0]);
        CheckResult("Decision blocks collection", !session.CollectCoin(coin) && session.Coins.Collected == 0);
        session.Resume();
        // Place the actual controller on the coin; physics must call OnTriggerEnter/Stay.
        controller.enabled = false;
        session.explorer.transform.position = new Vector3(coin.transform.position.x, .1f, coin.transform.position.z);
        controller.enabled = true; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
        // CharacterController.Move drives trigger reporting for stationary controllers too.
        controller.Move(Vector3.forward * .002f);
        yield return new WaitForFixedUpdate(); yield return null;
        CheckResult("Physical player trigger collects exactly one coin", coin.IsCollected && session.Coins.Collected == 1 && session.TotalScore == 10);
        CheckResult("Duplicate pickup cannot add points", !coin.TryPickup(controller) && !session.CollectCoin(coin) && session.TotalScore == 10);
        CheckResult("Pickup has matching written and audio feedback", session.Caption.Contains("moneda") && session.Caption.Contains("1 / 24") && session.feedbackSource.isPlaying);
        yield return new WaitForSeconds(.12f);
        CheckResult("Pickup animates upward and shrinks", coin.visual.localPosition.y > .05f && coin.visual.localScale.x < 1.3f);
        yield return new WaitForSeconds(.5f);
        CheckResult("Collected visual disappears and trigger stays disabled", !coin.visual.gameObject.activeSelf && !coin.GetComponent<SphereCollider>().enabled);
        yield return null;
        var counter = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).FirstOrDefault(t => t.gameObject.name == "CoinCounter");
        CheckResult("HUD updates to 1 / 24 coins", counter != null && counter.text.Contains("1 / 24"));
        session.Begin(); session.explorer.enabled = false;
        CheckResult("Restart restores all 24 coins, visuals and triggers", session.Coins.Collected == 0 && session.TotalScore == 0 && session.coins.All(c => !c.IsCollected && c.visual.gameObject.activeSelf && c.GetComponent<SphereCollider>().enabled));
        session.SetReducedMotion(true); yield return null;
        rotation = coin.visual.localRotation; yield return new WaitForSeconds(.15f);
        CheckResult("Reduced motion stops spinning", Quaternion.Angle(rotation,coin.visual.localRotation) < .1f);
        controller.enabled = false; session.explorer.transform.position = new Vector3(coin.transform.position.x,.1f,coin.transform.position.z); controller.enabled = true; Physics.SyncTransforms();
        session.SetMuted(true);
        bool picked = coin.TryPickup(controller);
        CheckResult("Reduced motion pickup uses immediate visual feedback", picked && !coin.visual.gameObject.activeSelf && !coin.pickupParticles.isPlaying);
        CheckResult("Mute suppresses music and feedback", session.musicSource.volume == 0 && session.feedbackSource.mute);
        session.SetMusic(.12f); session.SetEffects(.34f); session.SetMuted(false);
        CheckResult("Independent music and effects volume controls remain active", Mathf.Approximately(session.musicSource.volume,.12f) && Mathf.Approximately(session.EffectsVolume,.34f));
        session.SetReducedMotion(false); session.Begin(); session.explorer.enabled = false;
        controller.enabled = false;
        session.explorer.transform.position = new Vector3(coin.transform.position.x, .1f, coin.transform.position.z - .82f);
        controller.enabled = true; Physics.SyncTransforms();
        CheckResult("Pickup works at the edge of the trigger without the player blocking its own ray", coin.TryPickup(controller));
        session.Begin(); session.explorer.enabled = false;
        // Deliberate nearby obstruction verifies the anti-through-wall guard.
        controller.enabled = false; session.explorer.transform.position = coin.transform.position + Vector3.back * .9f - Vector3.up * .95f; controller.enabled = true;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = coin.transform.position + Vector3.back * .45f; wall.transform.localScale = new Vector3(1,2,.08f); Physics.SyncTransforms();
        CheckResult("A solid wall blocks coin collection", !coin.TryPickup(controller) && session.Coins.Collected == 0);
        UnityEngine.Object.Destroy(wall); yield return null;
        session.Begin(); session.explorer.enabled = false;
        foreach (var item in session.coins.Take(18)) session.CollectCoin(item);
        foreach (var target in session.situations) { session.Inspect(target); session.Answer(target.correctOption); }
        yield return null;
        string ui = string.Join("\n", UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled).Select(t => t.text));
        CheckResult("Victory includes coin points and preserves seven household objectives", session.Screen == SessionScreen.Results && session.Progress.Score == 700 && session.TotalScore == 880);
        CheckResult("Final results show 18 / 24, 75 percent and 180 bonus points", ui.Contains("18 / 24") && ui.Contains("75%") && ui.Contains("+180"));
        CheckResult("Results block collection", !session.CollectCoin(session.coins[23]));
        session.Begin(); session.explorer.enabled = false;
        foreach (var item in session.coins) session.CollectCoin(item);
        CheckResult("Collecting all coins does not end the educational objectives", session.Coins.Complete && session.TotalScore == 240 && session.Screen == SessionScreen.Playing && !session.Progress.Won);
        session.Begin(); session.explorer.enabled = false; session.SetUnlimited(false);
        foreach (var item in session.coins.Take(6)) session.CollectCoin(item);
        session.Progress.Tick(601,true); yield return null; yield return null;
        ui = string.Join("\n", UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled).Select(t => t.text));
        CheckResult("Timeout also shows partial collection statistics", session.Screen == SessionScreen.Results && ui.Contains("6 / 24") && ui.Contains("25%") && ui.Contains("+60"));
        session.SetLargeText(true); yield return null;
        var stats = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).FirstOrDefault(t => t.text.Contains("25%"));
        CheckResult("Final statistics fit their UI rect", stats != null && stats.preferredHeight <= stats.rectTransform.rect.height + 1);
        session.SetReducedMotion(reduced); session.SetMuted(muted); session.SetMusic(music); session.SetEffects(effects); session.SetLargeText(large); session.SetUnlimited(unlimited);
        session.Begin(); session.explorer.enabled = true; session.Home();
        var report = new Report { unityVersion = Application.unityVersion, status = checks.All(c => c.passed) ? "passed" : "failed", coins = session.coins.Length, checks = checks.ToArray() };
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/CoinValidation.json", JsonUtility.ToJson(report,true));
        File.WriteAllText(Result, JsonUtility.ToJson(report,true));
        Debug.Log("COIN_VERIFY_FINISHED: " + report.status + " (" + checks.Count + " checks)");
    }

    [MenuItem("Desconecta/Coins/Run EditMode Tests")]
    public static void RunTests()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before EditMode tests.");
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new TestCallbacks());
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Desconecta.Tests" } }));
    }

    private sealed class TestCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Documentation");
            TestRunnerApi.SaveResultToFile(result, "Documentation/CoinTests.xml");
            WriteResult("tests: " + result.PassCount + " passed, " + result.FailCount + " failed");
            Debug.Log("COIN_TESTS_FINISHED: " + result.PassCount + " passed, " + result.FailCount + " failed");
        }
    }
}
