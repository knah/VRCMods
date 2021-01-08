using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MelonLoader;
using ParticleAndBoneLimiterSettings;
using UIExpansionKit;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using Object = UnityEngine.Object;

[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonInfo(typeof(ParticleAndBoneLimiterSettingsMod), "Particle and DynBone limiter settings UI", "1.1.3", "knah", "https://github.com/knah/VRCMods")]

namespace ParticleAndBoneLimiterSettings
{
    public class ParticleAndBoneLimiterSettingsMod : MelonMod
    {
        private const string SettingsCategory = "VrcParticleLimiter";
        private static bool ourIsExpanded;
        
        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomParticleSettingsUiHandler>();

            MelonPrefs.RegisterCategory(SettingsCategory, "Particle and DynBone limits");
            MelonPrefs.RegisterBool(SettingsCategory, "dummy", false, "ignore this", true);

            ExpansionKitApi.RegisterWaitConditionBeforeDecorating(WaitForUixPrefabsAndRegister());
        }

        private IEnumerator WaitForUixPrefabsAndRegister()
        {
            var field = typeof(UiExpansionKitMod).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(it => it.FieldType == typeof(PreloadedBundleContents));
            var mod = MelonHandler.Mods.Single(it => it is UiExpansionKitMod);

            while (field.GetValue(mod) == null)
                yield return null;

            ourIsExpanded = !ExpansionKitSettings.IsCategoriesStartCollapsed();

            var prefabs = CustomParticleSettingsUiHandler.UixBundle = (PreloadedBundleContents) field.GetValue(mod);

            var rootPrefab = Object.Instantiate(prefabs.SettingsCategory, prefabs.StoredThingsParent.transform, false);
            rootPrefab.GetComponentInChildren<Text>().text = MelonPrefs.GetCategoryDisplayName(SettingsCategory);
            rootPrefab.SetActive(true);
            rootPrefab.AddComponent<CustomParticleSettingsUiHandler>();

            ExpansionKitApi.RegisterCustomSettingsCategory(SettingsCategory, rootPrefab);
        }

        private static readonly (string id, string displayName, int defaultValue)[] ourSettings =
        {
            ("dynamic_bone_max_affected_transform_count", "Max dynamic bone transforms", 32),
            ("dynamic_bone_max_collider_check_count", "Max dynamic bone collision checks", 8),

            ("ps_max_particles", "Max particles", 50_000),
            ("ps_max_systems", "Max particle systems", 200),
            ("ps_max_emission", "Max particle emission", 5_000),

            ("ps_max_total_emission", "Max total emission", 40_000),
            ("ps_mesh_particle_divider", "Mesh particle cost divider", 50),
            ("ps_mesh_particle_poly_limit", "Mesh particle polygon limit", 50_000),

            ("ps_collision_penalty_high", "High collision penalty", 120),
            ("ps_collision_penalty_med", "Med collision penalty", 60),
            ("ps_collision_penalty_low", "Low collision penalty", 10),

            ("ps_trails_penalty", "Particle trails penalty", 10),

            //LocalConfig.GetList("betas").Contains("particle_system_limiter") || AvatarValidation.ps_limiter_enabled;
        };

        internal static void InitializeSettingsCategory(GameObject categoryUi)
        {
            var categoryUiContent = categoryUi.transform.Find("CategoryEntries");
            var expandButtonTransform = categoryUi.transform.Find("ExpandButton");
            var expandButton = expandButtonTransform.GetComponent<Button>();
            var expandButtonText = expandButtonTransform.GetComponentInChildren<Text>();

            void SetExpanded(bool expanded)
            {
                expandButtonText.text = expanded ? "^" : "V";
                categoryUiContent.gameObject.SetActive(expanded);
            }
                
            expandButton.onClick.AddListener(new Action(() =>
            {
                SetExpanded(ourIsExpanded = !ourIsExpanded);
            }));
            
            SetExpanded(ourIsExpanded);

            var textPrefab = CustomParticleSettingsUiHandler.UixBundle.SettingsText;
            
            var boolSetting = Object.Instantiate(CustomParticleSettingsUiHandler.UixBundle.SettingsBool, categoryUiContent, false);
            boolSetting.GetComponentInChildren<Text>().text = "Enable particle limiter (restart required)";
            var mainToggle = boolSetting.transform.Find("Toggle").GetComponent<Toggle>();
            var localConfig = ConfigManager.LocalConfig.Cast<LocalConfig>();
            mainToggle.isOn = localConfig.GetList("betas").Contains("particle_system_limiter");
            mainToggle.onValueChanged.AddListener(new Action<bool>(
                isSet =>
                {
                    var list = localConfig.GetList("betas");
                    if (isSet)
                    {
                        if (!list.Contains("particle_system_limiter")) list.Add("particle_system_limiter");
                    }
                    else
                        list.Remove("particle_system_limiter");
                    
                    var newList = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>();
                    foreach (var s in list) newList.Add((Il2CppSystem.String) s);
                    localConfig.SetValue("betas", newList);
                }));
            var pinToggle = boolSetting.transform.Find("PinToggle");
            pinToggle.gameObject.SetActive(false);

            foreach (var valuePair in ourSettings)
            {
                var prefId = valuePair.id;
                var prefDesc = valuePair.displayName;
                var defaultValue = valuePair.defaultValue;

                var textSetting = Object.Instantiate(textPrefab, categoryUiContent, false);
                textSetting.GetComponentInChildren<Text>().text = prefDesc;
                var textField = textSetting.GetComponentInChildren<InputField>();
                textField.text = localConfig.GetInt(prefId, defaultValue).ToString();
                textField.contentType = InputField.ContentType.IntegerNumber;
                textField.onValueChanged.AddListener(new Action<string>(value =>
                {
                    int parsedValue;
                    if(int.TryParse(textField.text, out parsedValue))
                        localConfig.SetValue(prefId, new Il2CppSystem.Double { m_value = parsedValue }.BoxIl2CppObject());
                }));
                textSetting.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                {
                    BuiltinUiUtils.ShowInputPopup(prefDesc, textField.text,
                        InputField.InputType.Standard, true, "Done",
                        (result, _, __) => textField.text = result);
                }));
            }
            
            var reloadButton = Object.Instantiate(CustomParticleSettingsUiHandler.UixBundle.QuickMenuButton, categoryUiContent, false);
            var reloadButtonText = reloadButton.GetComponentInChildren<Text>();
            reloadButtonText.text = "Click to apply limits and reload all avatars (particle limits need world rejoin)";
            reloadButtonText.resizeTextMaxSize = reloadButtonText.resizeTextMaxSize * 15 / 10;
            reloadButtonText.fontSize = reloadButtonText.fontSize * 15 / 10;
            reloadButton.GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                Object.FindObjectOfType<VRCAvatarManager>().Start(); // this triggers particle system limit reload
                ReloadAllAvatars();
            }));
        }
        
        private static void ReloadAllAvatars() {
            // reloadAllAvatars
            var vrcPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            vrcPlayer.StartCoroutine(vrcPlayer.Method_Private_IEnumerator_Boolean_PDM_0(false));
        }
    }
}