#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ROS.Game.Audio;
using ROS.Game.UI;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    // Explicit authoring tool. It never changes assets on editor startup.
    public static class AudioSetupWizard
    {
        private const string AudioRoot   = "Assets/_Game/Audio";

        [MenuItem("ROS/Audio/Setup Audio Clips")]
        public static void RunSetup()
        {
            SetupCharacterAudio();
            SetupWeaponAudio();
            SetupAirplaneAudio();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AudioSetupWizard] Audio setup complete.");
        }

        // ─────────────────────────────────────────────── character ──

        private static void SetupCharacterAudio()
        {
            string path = FindPrefabPath("Player_Prototype");
            if (path == null) return;

            ModifyPrefab(path, root =>
            {
                CharacterAudioController ctrl =
                    root.GetComponent<CharacterAudioController>() ??
                    root.AddComponent<CharacterAudioController>();

                SerializedObject so = new SerializedObject(ctrl);

                // ── AudioSources ──────────────────────────────────
                so.FindProperty("footstepSource").objectReferenceValue =
                    GetOrAddAudioSource(root, "AudioSource_Footstep", spatial: true, vol: 0.65f);
                so.FindProperty("actionSource").objectReferenceValue =
                    GetOrAddAudioSource(root, "AudioSource_Action", spatial: true, vol: 0.75f);
                so.FindProperty("loopSource").objectReferenceValue =
                    GetOrAddAudioSource(root, "AudioSource_Loop", spatial: false, vol: 0.55f);

                // ── Footstep clips ────────────────────────────────
                SetClipArray(so, "walkClips",       FindClips("walk_outdoor_0", "Character"));
                SetClipArray(so, "runClips",        FindClips("run_outdoor_0",  "Character", excludes: new[]{"3p"}));
                SetClipArray(so, "sprintClips",     FindClips("run_outdoor_0",  "Character", excludes: new[]{"3p"}));
                SetClipArray(so, "crouchWalkClips", FindClips("crouch_outdoor_0", "Character", excludes: new[]{"3p"}));
                SetClipArray(so, "crawlClips",      FindClips("crawl_outdoor_0", "Character"));
                SetClipArray(so, "metalMoveClips",  FindClips("move_metal_0",  "Character"));

                // ── Jump / land ───────────────────────────────────
                SetClipArray(so, "jumpClips",       FindExact(new[]{"jump","jump_02","jump_03"}, "Character"));
                SetClipArray(so, "jump3Clips",      FindExact(new[]{"jump_3","jump_3_02","jump_3_03"}, "Character"));
                SetClipArray(so, "hardLandClips",   FindExact(new[]{"jump_down_hard"}, "Character"));
                SetClipArray(so, "waterEntryClips", FindClips("jump_into_water_0", "Character"));

                // ── Prone ─────────────────────────────────────────
                SetClipArray(so, "proneDownClips",      FindExact(new[]{"body_dwn","body_dwn_02","body_dwn_03"}, "Character"));
                SetClipArray(so, "proneDownFoleyClips", FindClips("body_dwn_foley", "Character"));
                SetClipArray(so, "proneUpClips",        FindExact(new[]{"body_up","body_up_02","body_up_03"}, "Character"));
                SetClipArray(so, "proneUpFoleyClips",   FindClips("body_up_foley", "Character"));

                // ── Crouch / die / actions ────────────────────────
                SetClipArray(so, "crouchClips",      FindExact(new[]{"squat","squat_02","squat_03"}, "Character"));
                SetClipArray(so, "dieClips",         FindExact(new[]{"die","die_02","die_03","dead_v1"}, "Character"));
                SetClipArray(so, "pickupClips",      FindExact(new[]{"pick","pick_02","pick_03"}, "Character"));
                SetClipArray(so, "pickupHandClips",  FindExact(new[]{"pick_hand","pick_hand_02","pick_hand_03"}, "Character"));
                SetClipArray(so, "vaultClips",       FindClips("act_cross_wall_0", "Character"));
                SetClipArray(so, "throwGrenadeClips",FindExact(new[]{"throw_grenade_1","throw_grenade_2","throw_grenade_3"}, "Character"));

                // ── Parachute / skydive ───────────────────────────
                SetClipArray(so, "parachuteOpenClips", FindClips("skydrive_open", "Character"));
                SetSingleClip(so, "fallWindIntroClip",   FindClipExact("fly_partb_intro_v2", "Character"));
                SetSingleClip(so, "skydiveWindClip",     FindClipExact("fly_partb_loop",     "Character"));
                SetSingleClip(so, "skydiveWindFastClip", FindClipExact("skydive_wind_fast",  "Character"));
                SetSingleClip(so, "skydiveLandingClip",  FindClipExact("skydive_landing",    "Character"));

                so.ApplyModifiedProperties();

                // ── CombatFeedbackPresenter ───────────────────────
                SetupCombatFeedback(root);
            });
        }

        private static void SetupCombatFeedback(GameObject root)
        {
            var presenter = root.GetComponentInChildren<CombatFeedbackPresenter>(true);
            if (presenter == null) return;

            SerializedObject so = new SerializedObject(presenter);

            AudioSource src = so.FindProperty("audioSource").objectReferenceValue as AudioSource
                ?? GetOrAddAudioSource(root, "AudioSource_CombatFeedback", spatial: false, vol: 1f);

            so.FindProperty("audioSource").objectReferenceValue = src;

            SetClipArray(so, "hitConfirmedClips",  FindExact(
                new[]{"sfx_body_hit","sfx_body_hit_01","sfx_body_hit_02"}, "UI"));
            SetClipArray(so, "receivedDamageClips", FindExact(
                new[]{"sfx_body_exp","sfx_body_hit_01","sfx_body_hit_02"}, "UI"));

            so.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────── airplane ──

        private static void SetupAirplaneAudio()
        {
            string path = FindPrefabPath("PF_AirplaneStart");
            if (path == null)
            {
                Debug.LogWarning("[AudioSetupWizard] PF_AirplaneStart prefab not found.");
                return;
            }

            ModifyPrefab(path, root =>
            {
                AirplaneAudioController ctrl =
                    root.GetComponent<AirplaneAudioController>() ??
                    root.AddComponent<AirplaneAudioController>();

                SerializedObject so = new SerializedObject(ctrl);

                AudioSource src = GetOrAddAudioSource(root, "AudioSource_Airplane", spatial: false, vol: 0.85f);
                if (src != null) so.FindProperty("audioSource").objectReferenceValue = src;

                SetSingleClip(so, "flyLoopClip",      FindClipExact("fly2_part_a_loop",               "Character"));
                SetSingleClip(so, "flyDaylightClip",  FindClipExact("Fly2_partc_daylight_oneshot_v2", "Character"));

                so.ApplyModifiedProperties();
            });
        }

        // ─────────────────────────────────────────────── weapons ──

        private static void SetupWeaponAudio()
        {
            // (prefab name, fire prefix, reload names, shell prefix, shell subfolder)
            var weapons = new (string name, string firePrefix, string[] reloadNames, string shellPrefix, string shellFolder)[]
            {
                ("PF_Weapon_M4A1",       "wp_m4a1_shot", new[]{"reload_general_type_a"}, "sfx_bulshell_drop_sgl_0", "UI"),
                ("PF_Weapon_AWM",        "wp_awm",       new[]{"reload_awm"},            "sfx_bulshell_drop_sgl_0", "UI"),
                ("PF_Weapon_M1887",      "wp_m1887",     new[]{"reload_m1887_start","reload_m1887_load","reload_m1887_end"}, "shotgun_shell", "Weapons"),
                ("PF_Weapon_MP7",        "wp_mp7_shot",  new[]{"reload_general_type_b"}, "sfx_bulshell_drop_sgl_0", "UI"),
                ("PF_Weapon_DesertEagle","wp_colt",      new[]{"reload_desert_eagle"},   "sfx_bulshell_drop_sgl_0", "UI"),
            };

            foreach (var w in weapons)
            {
                string path = FindPrefabPath(w.name);
                if (path == null) continue;

                ModifyPrefab(path, root =>
                {
                    SetupWeaponController(root, w.firePrefix, w.reloadNames, w.shellPrefix, w.shellFolder);
                    SetupWeaponEffects(root);
                });
            }
        }

        private static void SetupWeaponController(
            GameObject root,
            string firePrefix,
            string[] reloadNames,
            string shellPrefix,
            string shellFolder)
        {
            WeaponAudioController ctrl =
                root.GetComponent<WeaponAudioController>() ??
                root.AddComponent<WeaponAudioController>();

            SerializedObject so = new SerializedObject(ctrl);

            AudioSource fireSrc   = GetOrAddAudioSource(root, "AudioSource_Fire",         spatial: true, vol: 1f);
            AudioSource actionSrc = GetOrAddAudioSource(root, "AudioSource_WeaponAction", spatial: true, vol: 0.8f);
            if (fireSrc != null)   so.FindProperty("fireSource").objectReferenceValue   = fireSrc;
            if (actionSrc != null) so.FindProperty("actionSource").objectReferenceValue = actionSrc;

            // Fire clips: exclude silencer/suppressor variants until attachments are implemented.
            string[] noSilencer = new[]{ "silencer", "suppressed", "suppressor", "silenc" };
            var fireClips = FindClips(firePrefix, "Weapons", excludes: noSilencer);
            if (fireClips.Length == 0)
                fireClips = FindClips(firePrefix.Replace("_shot",""), "Weapons", excludes: noSilencer, includes: new[]{"shot"});
            SetClipArray(so, "fireClips", fireClips);

            // Reload clips
            var reloadList = new List<AudioClip>();
            foreach (string rName in reloadNames)
            {
                AudioClip c = FindClipExact(rName, "Weapons");
                if (c != null) reloadList.Add(c);
            }

            // reloadStart = first reload clip, reloadEnd = last
            if (reloadList.Count >= 1)
                SetClipArray(so, "reloadStartClips", new[]{ reloadList[0] });
            if (reloadList.Count >= 2)
                SetClipArray(so, "reloadEndClips",   new[]{ reloadList[reloadList.Count - 1] });
            else if (reloadList.Count == 1)
                SetClipArray(so, "reloadEndClips",   new[]{ reloadList[0] });

            // Shell drops
            SetClipArray(so, "shellDropClips", FindClips(shellPrefix, shellFolder));

            so.ApplyModifiedProperties();
        }

        private static void SetupWeaponEffects(GameObject root)
        {
            WeaponEffects effects = root.GetComponent<WeaponEffects>();
            if (effects == null) return;

            SerializedObject so = new SerializedObject(effects);

            // Character impact: flesh hit sounds
            SetClipArray(so, "characterImpactClips", FindExact(
                new[]{"Bullet_Flesh_Meaty","Bullet_Flesh_Meaty_1","Bullet_Flesh_Meaty_3",
                      "bullet_imp_flesh02","bullet_imp_flesh05","bullet_imp_flesh07"},
                "Weapons"));

            // Surface impact: concrete as generic default
            SetClipArray(so, "surfaceImpactClips", FindClips("blt_hit_conc_0", "Weapons"));

            so.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────── helpers ──

        private static void ModifyPrefab(string path, Action<GameObject> modify)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;

            try
            {
                modify(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioSetupWizard] Error modifying {path}: {e.Message}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AudioSource GetOrAddAudioSource(
            GameObject parent,
            string childName,
            bool spatial,
            float vol)
        {
            Transform existing = parent.transform.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent.transform, false);
            }

            AudioSource src = go.GetComponent<AudioSource>();
            if (src == null)
                src = go.AddComponent<AudioSource>();

            if (src == null)
            {
                Debug.LogError($"[AudioSetupWizard] Failed to add AudioSource to '{childName}'");
                return null;
            }

            src.spatialBlend  = spatial ? 1f : 0f;
            src.volume        = vol;
            src.playOnAwake   = false;
            src.loop          = false;
            return src;
        }

        private static void SetClipArray(SerializedObject so, string fieldName, AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;

            prop.ClearArray();
            prop.arraySize = clips.Length;

            for (int i = 0; i < clips.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        private static void SetSingleClip(SerializedObject so, string fieldName, AudioClip clip)
        {
            if (clip == null) return;
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = clip;
        }

        // Find clips whose filename STARTS WITH `prefix` inside the given subfolder.
        private static AudioClip[] FindClips(
            string prefix,
            string subFolder,
            string[] excludes  = null,
            string[] includes  = null)
        {
            string folder = $"{AudioRoot}/{subFolder}";
            string[] guids = AssetDatabase.FindAssets($"{prefix} t:AudioClip", new[]{ folder });

            var clips = new List<AudioClip>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename  = Path.GetFileNameWithoutExtension(assetPath);

                if (!filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (excludes != null)
                {
                    bool skip = false;
                    foreach (string ex in excludes)
                        if (filename.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0)
                        { skip = true; break; }
                    if (skip) continue;
                }

                if (includes != null)
                {
                    bool found = false;
                    foreach (string inc in includes)
                        if (filename.IndexOf(inc, StringComparison.OrdinalIgnoreCase) >= 0)
                        { found = true; break; }
                    if (!found) continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null) clips.Add(clip);
            }

            clips.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return clips.ToArray();
        }

        // Find clips by exact filename (without extension).
        private static AudioClip[] FindExact(string[] names, string subFolder)
        {
            string folder = $"{AudioRoot}/{subFolder}";
            var clips = new List<AudioClip>();

            foreach (string name in names)
            {
                AudioClip c = FindClipExact(name, subFolder);
                if (c != null) clips.Add(c);
            }

            return clips.ToArray();
        }

        private static AudioClip FindClipExact(string name, string subFolder)
        {
            string folder = $"{AudioRoot}/{subFolder}";
            string[] guids = AssetDatabase.FindAssets($"{name} t:AudioClip", new[]{ folder });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename  = Path.GetFileNameWithoutExtension(assetPath);

                if (filename.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            }

            return null;
        }

        private static string FindPrefabPath(string prefabName)
        {
            string[] searchFolders = new[]
            {
                "Assets/_Game/Prefabs",
                "Assets/_Game/Resources"
            };

            string[] guids = AssetDatabase.FindAssets(
                $"{prefabName} t:Prefab", searchFolders);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename  = Path.GetFileNameWithoutExtension(assetPath);
                if (filename.Equals(prefabName, StringComparison.OrdinalIgnoreCase))
                    return assetPath;
            }

            return null;
        }
    }
}
#endif
