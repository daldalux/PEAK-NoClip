using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PeakCheat
{
    [BepInPlugin("com.myname.peaknoclip", "Peak Ultimate Network Noclip", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool IsFlyMode = false;
        public static Vector3 FlyDirection = Vector3.zero;
        public static CharacterMovement LocalPlayer = null;

        // Конфиг для сохранения скорости
        public static ConfigEntry<float> FlySpeed;

        private void Awake()
        {

            FlySpeed = Config.Bind("General", "FlySpeed", 50f, "Скорость полета персонажа");

            var harmony = new Harmony("com.myname.peaknoclip");
            harmony.PatchAll();
            Logger.LogInfo($"Чит на сетевой полет загружен! F3 - Вкл/Выкл. +/- - Изменение скорости. Текущая: {FlySpeed.Value}");
        }

        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.F3))
            {
                IsFlyMode = !IsFlyMode;
                Debug.Log("[PEAK ЧИТ] Сетевой Ноуклип: " + IsFlyMode);

                if (LocalPlayer != null)
                {
                    var allRbs = LocalPlayer.GetComponentsInChildren<Rigidbody>();
                    var myColliders = LocalPlayer.GetComponentsInChildren<Collider>();

                    foreach (var rb in allRbs)
                    {
                        if (rb != null)
                        {
                            rb.useGravity = !IsFlyMode;
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }

                    foreach (var col in myColliders)
                    {
                        if (col != null)
                        {
                            col.isTrigger = IsFlyMode;
                        }
                    }
                }
            }

            // Динамическое изменение скорости во время игры
            if (IsFlyMode)
            {
                // Проверяем как обычные клавиши Плюс/Равно, так и Плюс на Numpad
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    FlySpeed.Value += 5f;
                    Debug.Log($"[PEAK ЧИТ] Скорость полета увеличена: {FlySpeed.Value}");
                }

                // Проверяем обычный Минус и Минус на Numpad
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    // Не даем скорости опуститься ниже 5
                    FlySpeed.Value = Mathf.Max(5f, FlySpeed.Value - 5f);
                    Debug.Log($"[PEAK ЧИТ] Скорость полета уменьшена: {FlySpeed.Value}");
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
            var characterField = AccessTools.Field(typeof(CharacterMovement), "character");
            if (characterField == null) return true;

            Character player = characterField.GetValue(__instance) as Character;

            if (player != null && player == Character.localCharacter)
            {
                Plugin.LocalPlayer = __instance;

                if (Plugin.IsFlyMode)
                {
                    var dataField = AccessTools.Field(typeof(Character), "data");
                    if (dataField != null)
                    {
                        var dataObj = dataField.GetValue(player);
                        if (dataObj != null)
                        {
                            AccessTools.Field(dataObj.GetType(), "sinceGrounded").SetValue(dataObj, 0f);
                        }
                    }

                    Camera mainCam = Camera.main;
                    if (mainCam == null) return true;

                    Vector3 moveDir = Vector3.zero;


                    Vector3 camForward = Vector3.Scale(mainCam.transform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 camRight = Vector3.Scale(mainCam.transform.right, new Vector3(1, 0, 1)).normalized;

                    if (Input.GetKey(KeyCode.W)) moveDir += camForward;
                    if (Input.GetKey(KeyCode.S)) moveDir -= camForward;
                    if (Input.GetKey(KeyCode.D)) moveDir += camRight;
                    if (Input.GetKey(KeyCode.A)) moveDir -= camRight;

                    if (Input.GetKey(KeyCode.Space))
                    {
                        moveDir += Vector3.up;
                    }
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        moveDir -= Vector3.up;
                    }

                    Plugin.FlyDirection = moveDir.normalized;
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
            if (Plugin.IsFlyMode && __instance == Plugin.LocalPlayer)
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
