using UnityEngine;

namespace SemiconCity.Game
{
    /// <summary>
    /// Carries only the arrival transform across a single-scene load. Gameplay
    /// state remains in SemiconGameState's save data, so interiors can be replaced
    /// by modeled scenes later without changing portal or machine code.
    /// </summary>
    public static class SemiconSceneTravel
    {
        private static bool pending;
        private static string targetScene;
        private static Vector3 position;
        private static Quaternion rotation;
        private static string message;
        private static bool interiorCamera;

        public static void Request(string sceneName, Vector3 arrivalPosition, Quaternion arrivalRotation,
            string arrivalMessage, bool useInteriorCamera)
        {
            pending = true;
            targetScene = sceneName;
            position = arrivalPosition;
            rotation = arrivalRotation;
            message = arrivalMessage;
            interiorCamera = useInteriorCamera;
        }

        public static bool TryConsume(string sceneName, out Vector3 arrivalPosition,
            out Quaternion arrivalRotation, out string arrivalMessage, out bool useInteriorCamera)
        {
            arrivalPosition = position;
            arrivalRotation = rotation;
            arrivalMessage = message;
            useInteriorCamera = interiorCamera;
            if (!pending || targetScene != sceneName)
            {
                return false;
            }

            pending = false;
            targetScene = string.Empty;
            message = string.Empty;
            return true;
        }
    }

}
