using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SemiconCity.Game
{
    public sealed class SemiconInteriorFlowSmokeTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            if (!Environment.GetCommandLineArgs().Contains("--semicon-interior-flow-smoke-test"))
            {
                yield break;
            }

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            yield return null;

            var entrance = FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).FirstOrDefault(portal => portal.name == "Factory Visitor Entrance");
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            if (entrance == null || player == null || cameraController == null)
            {
                Fail(91, "외부 공장 문 또는 플레이어 구성이 없습니다.");
                yield break;
            }

            entrance.Interact(player, cameraController);
            yield return WaitForScene("Semicon_Interior_Factory", 5f);
            if (SceneManager.GetActiveScene().name != "Semicon_Interior_Factory")
            {
                Fail(92, "공장 내부 씬으로 전환되지 않았습니다.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            player = FindFirstObjectByType<SemiconPlayerController>();
            cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var machine = FindFirstObjectByType<SemiconProductionMachine>();
            var panel = FindFirstObjectByType<SemiconProductionPanel>(FindObjectsInactive.Include);
            var exit = FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).FirstOrDefault(portal => portal.name.StartsWith("EXIT DOOR CUBE"));
            if (player == null || cameraController == null || machine == null || panel == null || exit == null)
            {
                Fail(93, "내부 플레이어·기계 큐브·UI·출구 구성이 불완전합니다.");
                yield break;
            }

            var roomCapture = GetCapturePath("semicon-interior-room");
            yield return Capture(roomCapture);
            machine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.45f);
            if (player.InputEnabled || !panel.gameObject.activeInHierarchy)
            {
                Fail(94, "기계 상호작용 후 생산 인터페이스가 열리지 않았습니다.");
                yield break;
            }

            var uiCapture = GetCapturePath("semicon-interior-machine-ui");
            yield return Capture(uiCapture);
            panel.Close();
            yield return new WaitForSecondsRealtime(0.15f);
            exit.Interact(player, cameraController);
            yield return WaitForScene("SemiconCity_Playable", 5f);
            if (SceneManager.GetActiveScene().name != "SemiconCity_Playable")
            {
                Fail(95, "출구 상호작용 후 외부 씬으로 복귀하지 못했습니다.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            player = FindFirstObjectByType<SemiconPlayerController>();
            var exteriorDoor = FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).FirstOrDefault(portal => portal.name == "Factory Visitor Entrance");
            if (player == null || exteriorDoor == null ||
                Vector3.Distance(player.transform.position, exteriorDoor.transform.position) > 5f)
            {
                Fail(96, "외부 복귀 위치가 공장 문 앞에 적용되지 않았습니다.");
                yield break;
            }

            Debug.Log($"[Semicon Interior Flow Smoke] PASS / exterior→factory→machine UI→exterior / " +
                      $"Room={roomCapture} / UI={uiCapture}");
            Application.Quit(0);
        }

        private static IEnumerator WaitForScene(string sceneName, float timeout)
        {
            var started = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup - started < timeout)
            {
                yield return null;
            }
        }

        private static IEnumerator Capture(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            var started = Time.realtimeSinceStartup;
            while (!File.Exists(path) && Time.realtimeSinceStartup - started < 4f) yield return null;
        }

        private static string GetCapturePath(string prefix)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"{prefix}-{Screen.width}x{Screen.height}.png"));
        }

        private static void Fail(int code, string message)
        {
            Debug.LogError($"[Semicon Interior Flow Smoke] FAIL {code} / {message}");
            Application.Quit(code);
        }
    }
}
