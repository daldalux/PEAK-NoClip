using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PeakCheat
{
    [BepInPlugin("com.myname.peaknoclip", "Peak Ultimate Network Noclip", "1.6.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool IsFlyMode = false;
        public static Vector3 FlyDirection = Vector3.zero;
        public static CharacterMovement LocalPlayer = null;
        public static ConfigEntry<float> FlySpeed;

        private void Awake()
        {
            FlySpeed = Config.Bind("General", "FlySpeed", 50f, "Скорость полета персонажа");
            var harmony = new Harmony("com.myname.peaknoclip");
            harmony.PatchAll();
        }

        private void Update()
        {
            if (!Application.isFocused)
            {
                if (IsFlyMode)
                {
                    IsFlyMode = false;
                    FlyDirection = Vector3.zero;
                    ResetPhysics(false);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                IsFlyMode = !IsFlyMode;
                ResetPhysics(IsFlyMode);
            }

            if (IsFlyMode)
            {
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    FlySpeed.Value += 5f;
                }

                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    FlySpeed.Value = Mathf.Max(5f, FlySpeed.Value - 5f);
                }
            }
        }

        private void ResetPhysics(bool flyState)
        {
            if (LocalPlayer == null || LocalPlayer.gameObject == null)
            {
                LocalPlayer = null;
                return;
            }

            var allRbs = LocalPlayer.GetComponentsInChildren<Rigidbody>();
            if (allRbs != null)
            {
                foreach (var rb in allRbs)
                {
                    if (rb != null)
                    {
                        rb.useGravity = !flyState;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            var myColliders = LocalPlayer.GetComponentsInChildren<Collider>();
            if (myColliders != null)
            {
                foreach (var col in myColliders)
                {
                    if (col != null)
                    {
                        col.isTrigger = flyState;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CharacterMovement), "Update")]
    public class UpdatePatch
    {
        [HarmonyPrefix]
        static bool Prefix(CharacterMovement __instance)
        {
            if (__instance == null || !Application.isFocused) return true;

            var characterField = AccessTools.Field(typeof(CharacterMovement), "character");
            if (characterField == null) return true;

            Character player = characterField.GetValue(__instance) as Character;
            if (player == null || Character.localCharacter == null || player != Character.localCharacter) return true;

            Plugin.LocalPlayer = __instance;

            if (Plugin.IsFlyMode)
            {
                var dataField = AccessTools.Field(typeof(Character), "data");
                if (dataField != null)
                {
                    var dataObj = dataField.GetValue(player);
                    if (dataObj != null)
                    {
                        var sinceGroundedField = AccessTools.Field(dataObj.GetType(), "sinceGrounded");
                        if (sinceGroundedField != null)
                        {
                            sinceGroundedField.SetValue(dataObj, 0f);
                        }
                    }
                }

                Camera mainCam = Camera.main;
                if (mainCam == null || mainCam.transform == null)
                {
                    Plugin.FlyDirection = Vector3.zero;
                    return true;
                }

                Vector3 moveDir = Vector3.zero;
                bool hasInput = false;

                Vector3 camForward = Vector3.Scale(mainCam.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 camRight = Vector3.Scale(mainCam.transform.right, new Vector3(1, 0, 1)).normalized;

                if (Input.GetKey(KeyCode.W)) { moveDir += camForward; hasInput = true; }
                if (Input.GetKey(KeyCode.S)) { moveDir -= camForward; hasInput = true; }
                if (Input.GetKey(KeyCode.D)) { moveDir += camRight; hasInput = true; }
                if (Input.GetKey(KeyCode.A)) { moveDir -= camRight; hasInput = true; }

                if (Input.GetKey(KeyCode.Space)) { moveDir += Vector3.up; hasInput = true; }
                if (Input.GetKey(KeyCode.LeftControl)) { moveDir -= Vector3.up; hasInput = true; }

                if (hasInput)
                {
                    Plugin.FlyDirection = moveDir.normalized;
                }
                else
                {
                    Plugin.FlyDirection = Vector3.zero;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterMovement), "FixedUpdate")]
    public class FixedUpdatePatch
    {
        [HarmonyPrefix]
        static bool Prefix(CharacterMovement __instance)
        {
            if (Plugin.IsFlyMode && Application.isFocused && __instance != null && __instance == Plugin.LocalPlayer)
            {
                var rb = __instance.GetComponent<Rigidbody>();
                if (rb == null) rb = __instance.GetComponentInChildren<Rigidbody>();

                if (rb != null)
                {
                    rb.linearVelocity = Plugin.FlyDirection * Plugin.FlySpeed.Value;
                    rb.angularVelocity = Vector3.zero;
                }
                return true;
            }
            return true;
        }
    }
}
