using System.Collections.Generic;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    public static class WeaponFamilyLootBuilder
    {
        private const string DefinitionFolder =
            "Assets/_Game/Data/WeaponDefinitions";

        private const string ItemFolder =
            "Assets/_Game/Data/Weapons";

        private const string PrefabFolder =
            "Assets/_Game/Prefabs/Weapons";

        private const string BulletImpactPrefabPath =
            "Assets/_Game/Prefabs/Effects/PF_BulletImpact.prefab";

        private const string BulletHolePrefabPath =
            "Assets/_Game/Prefabs/Effects/PF_BulletHole.prefab";

        private const string TracerMaterialPath =
            "Assets/_Game/Resources/EditorFirst/WeaponTracer.mat";

        private const string LootTablePath =
            "Assets/_Game/Data/LootTables/LootTable_TestArea.asset";

        private const string LootSpawnerPath =
            "Assets/_Game/Resources/LootSpawner_TestArea.prefab";

        private const string LegacyRifleItemPath =
            "Assets/_Game/Data/Weapons/Item_PrototypeRifle.asset";

        [MenuItem("ROS Battle Royale/Build Weapon Family Loot")]
        public static void BuildWeaponFamilyLoot()
        {
            EnsureFolder(DefinitionFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(PrefabFolder);

            List<InventoryItemDefinition> weaponItems =
                new List<InventoryItemDefinition>();

            foreach (WeaponProfile profile in CreateProfiles())
            {
                GameObject model =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        profile.modelPath
                    );

                if (model == null)
                {
                    Debug.LogError(
                        $"No se encontró el modelo de {profile.displayName}: {profile.modelPath}"
                    );
                    continue;
                }

                WeaponDefinition definition =
                    GetOrCreateAsset<WeaponDefinition>(
                        $"{DefinitionFolder}/Weapon_{profile.assetName}.asset"
                    );

                ApplyProfile(definition, profile);

                GameObject weaponPrefab =
                    CreateWeaponPrefab(
                        profile,
                        definition,
                        model
                    );

                InventoryItemDefinition item =
                    GetOrCreateAsset<InventoryItemDefinition>(
                        $"{ItemFolder}/Item_{profile.assetName}.asset"
                    );

                ConfigureItem(
                    item,
                    profile,
                    definition,
                    weaponPrefab,
                    model
                );

                weaponItems.Add(item);
            }

            ReplaceLegacyRifleAndAddWeapons(weaponItems);
            AddWeaponsToGuaranteedLoot(weaponItems);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Armas por familia generadas: {weaponItems.Count}."
            );

            ValidateWeaponFamilyLoot();
        }

        [MenuItem("ROS Battle Royale/Validate Weapon Family Loot")]
        public static void ValidateWeaponFamilyLoot()
        {
            int errors = 0;

            foreach (WeaponProfile profile in CreateProfiles())
            {
                WeaponDefinition definition =
                    AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                        $"{DefinitionFolder}/Weapon_{profile.assetName}.asset"
                    );

                InventoryItemDefinition item =
                    AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(
                        $"{ItemFolder}/Item_{profile.assetName}.asset"
                    );

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{PrefabFolder}/PF_Weapon_{profile.assetName}.prefab"
                    );

                Check(
                    definition != null,
                    $"Falta la definición de {profile.displayName}.",
                    ref errors
                );

                Check(
                    item != null,
                    $"Falta el item de {profile.displayName}.",
                    ref errors
                );

                Check(
                    prefab != null,
                    $"Falta el prefab de {profile.displayName}.",
                    ref errors
                );

                if (definition != null)
                {
                    Check(
                        definition.family == profile.family &&
                        definition.ammoType == profile.ammoType &&
                        definition.GetProjectileCount() == profile.projectiles,
                        $"El perfil balístico de {profile.displayName} no coincide.",
                        ref errors
                    );
                }

                if (item != null)
                {
                    Check(
                        item.weaponDefinition == definition &&
                        item.weaponPrefab == prefab &&
                        item.worldModel != null,
                        $"Las referencias de loot de {profile.displayName} no coinciden.",
                        ref errors
                    );
                }

                if (prefab != null)
                {
                    Check(
                        prefab.GetComponent<WeaponController>() != null &&
                        prefab.GetComponent<WeaponEffects>() != null &&
                        prefab.GetComponent<WeaponRecoil>() != null,
                        $"El prefab jugable de {profile.displayName} está incompleto.",
                        ref errors
                    );
                }
            }

            if (errors == 0)
            {
                Debug.Log(
                    "Validación de armas por familia correcta: 9 armas, loot y balística listos."
                );
            }
            else
            {
                Debug.LogError(
                    $"La validación de armas por familia encontró {errors} error(es)."
                );
            }
        }

        private static void Check(
            bool condition,
            string message,
            ref int errors)
        {
            if (condition)
            {
                return;
            }

            errors++;
            Debug.LogError(message);
        }

        private static WeaponProfile[] CreateProfiles()
        {
            return new[]
            {
                new WeaponProfile
                {
                    assetName = "M4A1",
                    weaponId = "weapon_m4a1",
                    itemId = "weapon_item_m4a1",
                    displayName = "M4A1",
                    family = WeaponFamily.AssaultRifle,
                    ammoType = AmmoType.Rifle,
                    modelPath = "Assets/_Game/Art/Weapons/Models/AssaultRifles/M4A1.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/assault_rifles__M4A1.png",
                    rarity = LootRarity.Uncommon,
                    lootWeight = 6f,
                    inventoryWeight = 6f,
                    preferredSlot = 1,
                    damage = 31f,
                    shotsPerSecond = 10f,
                    range = 320f,
                    magazineSize = 30,
                    reserveAmmo = 90,
                    reloadTime = 2.15f,
                    emptyReloadTime = 2.65f,
                    supportsSingle = true,
                    supportsAuto = true,
                    initialMode = WeaponFireMode.Auto,
                    projectiles = 1,
                    hipSpread = 1.25f,
                    adsSpread = 0.25f,
                    bloomPerShot = 0.09f,
                    maxBloom = 1f,
                    verticalRecoil = 1.1f,
                    horizontalRecoil = 0.32f,
                    impactScale = 1f,
                    bulletHoleScale = 1f,
                    tracerWidth = 0.012f,
                    muzzleX = 0.0095f
                },
                new WeaponProfile
                {
                    assetName = "AKM",
                    weaponId = "weapon_akm",
                    itemId = "weapon_item_akm",
                    displayName = "AKM",
                    family = WeaponFamily.AssaultRifle,
                    ammoType = AmmoType.Rifle,
                    modelPath = "Assets/_Game/Art/Weapons/Models/AssaultRifles/AKM.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/assault_rifles__M4A1.png",
                    rarity = LootRarity.Uncommon,
                    lootWeight = 5.5f,
                    inventoryWeight = 6.5f,
                    preferredSlot = 1,
                    damage = 38f,
                    shotsPerSecond = 9f,
                    range = 300f,
                    magazineSize = 30,
                    reserveAmmo = 90,
                    reloadTime = 2.3f,
                    emptyReloadTime = 2.8f,
                    supportsSingle = true,
                    supportsAuto = true,
                    initialMode = WeaponFireMode.Auto,
                    projectiles = 1,
                    hipSpread = 1.45f,
                    adsSpread = 0.3f,
                    bloomPerShot = 0.12f,
                    maxBloom = 1.2f,
                    verticalRecoil = 1.45f,
                    horizontalRecoil = 0.42f,
                    impactScale = 1.05f,
                    bulletHoleScale = 1f,
                    tracerWidth = 0.013f,
                    muzzleX = 0.0105f
                },
                new WeaponProfile
                {
                    assetName = "M14EBR",
                    weaponId = "weapon_m14ebr",
                    itemId = "weapon_item_m14ebr",
                    displayName = "M14 EBR",
                    family = WeaponFamily.SniperRifle,
                    ammoType = AmmoType.Rifle,
                    modelPath = "Assets/_Game/Art/Weapons/Models/AssaultRifles/M14EBR.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/M14EBR.png",
                    rarity = LootRarity.Rare,
                    lootWeight = 3f,
                    inventoryWeight = 7.5f,
                    preferredSlot = 1,
                    damage = 58f,
                    shotsPerSecond = 4.2f,
                    range = 520f,
                    magazineSize = 20,
                    reserveAmmo = 60,
                    reloadTime = 2.55f,
                    emptyReloadTime = 3.05f,
                    supportsSingle = true,
                    initialMode = WeaponFireMode.Single,
                    projectiles = 1,
                    hipSpread = 1.8f,
                    adsSpread = 0.12f,
                    bloomPerShot = 0.28f,
                    maxBloom = 1.25f,
                    verticalRecoil = 1.9f,
                    horizontalRecoil = 0.48f,
                    impactScale = 1.25f,
                    bulletHoleScale = 1.15f,
                    tracerWidth = 0.016f,
                    muzzleX = 0.011f
                },
                new WeaponProfile
                {
                    assetName = "MP7",
                    weaponId = "weapon_mp7",
                    itemId = "weapon_item_mp7",
                    displayName = "MP7",
                    family = WeaponFamily.SubmachineGun,
                    ammoType = AmmoType.SMG,
                    modelPath = "Assets/_Game/Art/Weapons/Models/SMGs/MP7.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/MP7.png",
                    rarity = LootRarity.Common,
                    lootWeight = 7f,
                    inventoryWeight = 4f,
                    preferredSlot = 2,
                    damage = 22f,
                    shotsPerSecond = 14f,
                    range = 180f,
                    magazineSize = 30,
                    reserveAmmo = 120,
                    reloadTime = 1.85f,
                    emptyReloadTime = 2.25f,
                    supportsSingle = true,
                    supportsAuto = true,
                    initialMode = WeaponFireMode.Auto,
                    projectiles = 1,
                    hipSpread = 1.55f,
                    adsSpread = 0.38f,
                    bloomPerShot = 0.08f,
                    maxBloom = 0.9f,
                    verticalRecoil = 0.75f,
                    horizontalRecoil = 0.22f,
                    impactScale = 0.8f,
                    bulletHoleScale = 0.75f,
                    tracerWidth = 0.009f,
                    muzzleX = 0.0075f
                },
                new WeaponProfile
                {
                    assetName = "Thompson",
                    weaponId = "weapon_thompson",
                    itemId = "weapon_item_thompson",
                    displayName = "Thompson",
                    family = WeaponFamily.SubmachineGun,
                    ammoType = AmmoType.SMG,
                    modelPath = "Assets/_Game/Art/Weapons/Models/SMGs/Thompson.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/Thompson.png",
                    rarity = LootRarity.Common,
                    lootWeight = 6.5f,
                    inventoryWeight = 4.8f,
                    preferredSlot = 2,
                    damage = 27f,
                    shotsPerSecond = 11f,
                    range = 165f,
                    magazineSize = 30,
                    reserveAmmo = 120,
                    reloadTime = 2.15f,
                    emptyReloadTime = 2.55f,
                    supportsSingle = true,
                    supportsAuto = true,
                    initialMode = WeaponFireMode.Auto,
                    projectiles = 1,
                    hipSpread = 1.65f,
                    adsSpread = 0.42f,
                    bloomPerShot = 0.1f,
                    maxBloom = 1f,
                    verticalRecoil = 0.95f,
                    horizontalRecoil = 0.3f,
                    impactScale = 0.85f,
                    bulletHoleScale = 0.8f,
                    tracerWidth = 0.01f,
                    muzzleX = 0.009f
                },
                new WeaponProfile
                {
                    assetName = "AWM",
                    weaponId = "weapon_awm",
                    itemId = "weapon_item_awm",
                    displayName = "AWM",
                    family = WeaponFamily.SniperRifle,
                    ammoType = AmmoType.Sniper,
                    modelPath = "Assets/_Game/Art/Weapons/Models/SniperRifles/AWM.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/AWM.png",
                    rarity = LootRarity.Rare,
                    lootWeight = 2f,
                    inventoryWeight = 8f,
                    preferredSlot = 1,
                    damage = 105f,
                    shotsPerSecond = 0.75f,
                    range = 700f,
                    magazineSize = 5,
                    reserveAmmo = 20,
                    reloadTime = 3.2f,
                    emptyReloadTime = 3.8f,
                    supportsSingle = true,
                    initialMode = WeaponFireMode.Single,
                    projectiles = 1,
                    hipSpread = 2.2f,
                    adsSpread = 0.05f,
                    bloomPerShot = 0.8f,
                    maxBloom = 1.5f,
                    verticalRecoil = 2.8f,
                    horizontalRecoil = 0.55f,
                    impactScale = 1.5f,
                    bulletHoleScale = 1.35f,
                    tracerWidth = 0.02f,
                    muzzleX = 0.012f
                },
                new WeaponProfile
                {
                    assetName = "M1887",
                    weaponId = "weapon_m1887",
                    itemId = "weapon_item_m1887",
                    displayName = "M1887",
                    family = WeaponFamily.Shotgun,
                    ammoType = AmmoType.Shotgun,
                    modelPath = "Assets/_Game/Art/Weapons/Models/Shotguns/M1887.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/m1887__M1887.png",
                    rarity = LootRarity.Common,
                    lootWeight = 5f,
                    inventoryWeight = 5f,
                    preferredSlot = 2,
                    damage = 16f,
                    shotsPerSecond = 1.1f,
                    range = 65f,
                    magazineSize = 2,
                    reserveAmmo = 24,
                    reloadTime = 2.4f,
                    emptyReloadTime = 2.8f,
                    supportsSingle = true,
                    initialMode = WeaponFireMode.Single,
                    projectiles = 8,
                    hipSpread = 4.2f,
                    adsSpread = 3f,
                    bloomPerShot = 0.75f,
                    maxBloom = 2.5f,
                    verticalRecoil = 2.2f,
                    horizontalRecoil = 0.65f,
                    impactScale = 0.65f,
                    bulletHoleScale = 0.65f,
                    tracerWidth = 0.006f,
                    muzzleX = 0.011f
                },
                new WeaponProfile
                {
                    assetName = "M870",
                    weaponId = "weapon_m870",
                    itemId = "weapon_item_m870",
                    displayName = "M870",
                    family = WeaponFamily.Shotgun,
                    ammoType = AmmoType.Shotgun,
                    modelPath = "Assets/_Game/Art/Weapons/Models/Shotguns/M870.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/M870.png",
                    rarity = LootRarity.Common,
                    lootWeight = 5.5f,
                    inventoryWeight = 5.5f,
                    preferredSlot = 2,
                    damage = 14f,
                    shotsPerSecond = 0.85f,
                    range = 72f,
                    magazineSize = 5,
                    reserveAmmo = 25,
                    reloadTime = 2.8f,
                    emptyReloadTime = 3.35f,
                    supportsSingle = true,
                    initialMode = WeaponFireMode.Single,
                    projectiles = 9,
                    hipSpread = 4.5f,
                    adsSpread = 3.1f,
                    bloomPerShot = 0.82f,
                    maxBloom = 2.7f,
                    verticalRecoil = 2.45f,
                    horizontalRecoil = 0.72f,
                    impactScale = 0.68f,
                    bulletHoleScale = 0.68f,
                    tracerWidth = 0.006f,
                    muzzleX = 0.011f
                },
                new WeaponProfile
                {
                    assetName = "DesertEagle",
                    weaponId = "weapon_desert_eagle",
                    itemId = "weapon_item_desert_eagle",
                    displayName = "Desert Eagle",
                    family = WeaponFamily.Pistol,
                    ammoType = AmmoType.Pistol,
                    modelPath = "Assets/_Game/Art/Weapons/Models/Pistols/DesertEagle.fbx",
                    iconPath = "Assets/_Game/UI/Icons/Weapons/Desert_Eagle.png",
                    rarity = LootRarity.Uncommon,
                    lootWeight = 6f,
                    inventoryWeight = 2f,
                    preferredSlot = 3,
                    damage = 52f,
                    shotsPerSecond = 2.5f,
                    range = 120f,
                    magazineSize = 7,
                    reserveAmmo = 35,
                    reloadTime = 1.9f,
                    emptyReloadTime = 2.25f,
                    supportsSingle = true,
                    initialMode = WeaponFireMode.Single,
                    projectiles = 1,
                    hipSpread = 1.8f,
                    adsSpread = 0.45f,
                    bloomPerShot = 0.25f,
                    maxBloom = 1.2f,
                    verticalRecoil = 1.8f,
                    horizontalRecoil = 0.5f,
                    impactScale = 1.1f,
                    bulletHoleScale = 1f,
                    tracerWidth = 0.01f,
                    muzzleX = 0.006f
                }
            };
        }

        private static void ApplyProfile(
            WeaponDefinition definition,
            WeaponProfile profile)
        {
            definition.weaponId = profile.weaponId;
            definition.displayName = profile.displayName;
            definition.family = profile.family;
            definition.ammoType = profile.ammoType;
            definition.animationStyle = profile.family == WeaponFamily.Pistol
                ? WeaponAnimationStyle.Pistol
                : WeaponAnimationStyle.Rifle;
            definition.dataConfidence = DataConfidence.Estimated;
            definition.fireMode = profile.initialMode;
            definition.supportsSingle = profile.supportsSingle;
            definition.supportsBurst = profile.supportsBurst;
            definition.supportsAuto = profile.supportsAuto;
            definition.damage = profile.damage;
            definition.shotsPerSecond = profile.shotsPerSecond;
            definition.range = profile.range;
            definition.projectilesPerShot = profile.projectiles;
            definition.impactScale = profile.impactScale;
            definition.bulletHoleScale = profile.bulletHoleScale;
            definition.tracerWidth = profile.tracerWidth;
            definition.magazineSize = profile.magazineSize;
            definition.startingReserveAmmo = profile.reserveAmmo;
            definition.reloadTime = profile.reloadTime;
            definition.emptyReloadTime = profile.emptyReloadTime;
            definition.burstCount = 3;
            definition.verticalRecoil = profile.verticalRecoil;
            definition.horizontalRecoil = profile.horizontalRecoil;
            definition.recoilReturnSpeed = 8f;
            definition.recoilSnappiness = 14f;
            definition.hipSpreadDegrees = profile.hipSpread;
            definition.adsSpreadDegrees = profile.adsSpread;
            definition.walkSpreadMultiplier = 1.2f;
            definition.runSpreadMultiplier = 1.6f;
            definition.sprintSpreadMultiplier = 2.35f;
            definition.crouchSpreadMultiplier = 0.72f;
            definition.airborneSpreadMultiplier = 2.75f;
            definition.spreadBloomPerShot = profile.bloomPerShot;
            definition.maxSpreadBloom = profile.maxBloom;
            definition.spreadRecoveryPerSecond = 3.5f;
            definition.spreadDegrees = profile.hipSpread;
            EditorUtility.SetDirty(definition);
        }

        private static GameObject CreateWeaponPrefab(
            WeaponProfile profile,
            WeaponDefinition definition,
            GameObject model)
        {
            string prefabPath =
                $"{PrefabFolder}/PF_Weapon_{profile.assetName}.prefab";

            GameObject existingPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject root =
                new GameObject($"Weapon_{profile.assetName}");

            WeaponController controller =
                root.AddComponent<WeaponController>();

            WeaponEffects effects = root.AddComponent<WeaponEffects>();
            WeaponRecoil recoil = root.AddComponent<WeaponRecoil>();
            WeaponMount mount = root.AddComponent<WeaponMount>();

            SerializedObject controllerObject =
                new SerializedObject(controller);

            controllerObject.FindProperty("definition").objectReferenceValue =
                definition;
            controllerObject.FindProperty("reserveAmmo").intValue =
                profile.reserveAmmo;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject modelInstance =
                PrefabUtility.InstantiatePrefab(
                    model,
                    root.transform
                ) as GameObject;

            if (modelInstance == null)
            {
                modelInstance = Object.Instantiate(model, root.transform);
            }

            modelInstance.name = $"Visual_{profile.assetName}";
            modelInstance.transform.localPosition =
                new Vector3(-0.044f, 0.228f, 0.021f);
            modelInstance.transform.localEulerAngles =
                new Vector3(1.246f, -88.748f, 89.526f);
            modelInstance.transform.localScale =
                Vector3.one * 40f;

            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(modelInstance.transform, false);
            muzzle.transform.localPosition =
                new Vector3(profile.muzzleX, 0f, 0.0015f);
            muzzle.transform.localEulerAngles =
                new Vector3(0f, 90f, 0f);

            Transform leftHandTarget = null;
            if (profile.family != WeaponFamily.Pistol)
            {
                GameObject leftHand = new GameObject("LeftHandIK");
                leftHand.transform.SetParent(modelInstance.transform, false);
                leftHand.transform.localPosition =
                    new Vector3(0.0023f, 0f, 0.001f);
                leftHandTarget = leftHand.transform;
            }

            GameObject tracerObject = new GameObject("Tracer");
            tracerObject.transform.SetParent(root.transform, false);
            LineRenderer tracer = tracerObject.AddComponent<LineRenderer>();
            Material tracerMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TracerMaterialPath
            );
            tracer.sharedMaterial = tracerMaterial;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.enabled = false;

            GameObject impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BulletImpactPrefabPath
            );
            GameObject bulletHolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BulletHolePrefabPath
            );

            controllerObject.FindProperty("weaponMount").objectReferenceValue = mount;
            controllerObject.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            controllerObject.FindProperty("weaponEffects").objectReferenceValue = effects;
            controllerObject.FindProperty("weaponRecoil").objectReferenceValue = recoil;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject effectsObject = new SerializedObject(effects);
            effectsObject.FindProperty("weapon").objectReferenceValue = controller;
            effectsObject.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            effectsObject.FindProperty("tracer").objectReferenceValue = tracer;
            effectsObject.FindProperty("tracerMaterial").objectReferenceValue = tracerMaterial;
            effectsObject.FindProperty("impactPrefab").objectReferenceValue = impactPrefab;
            effectsObject.FindProperty("bulletHolePrefab").objectReferenceValue = bulletHolePrefab;
            effectsObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject recoilObject = new SerializedObject(recoil);
            recoilObject.FindProperty("visualRecoilTransform").objectReferenceValue =
                modelInstance.transform;
            recoilObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject mountObject = new SerializedObject(mount);
            mountObject.FindProperty("muzzlePoint").objectReferenceValue = muzzle.transform;
            mountObject.FindProperty("aimPoint").objectReferenceValue = muzzle.transform;
            mountObject.FindProperty("rightHandGrip").objectReferenceValue = root.transform;
            mountObject.FindProperty("leftHandIKTarget").objectReferenceValue = leftHandTarget;
            mountObject.FindProperty("shellEjectionPoint").objectReferenceValue = muzzle.transform;
            mountObject.FindProperty("visualRoot").objectReferenceValue = modelInstance.transform;
            mountObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab =
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            Object.DestroyImmediate(root);
            return savedPrefab;
        }

        private static void ConfigureItem(
            InventoryItemDefinition item,
            WeaponProfile profile,
            WeaponDefinition definition,
            GameObject weaponPrefab,
            GameObject model)
        {
            item.itemId = profile.itemId;
            item.displayName = profile.displayName;
            item.itemType = ItemType.Weapon;
            item.dataConfidence = DataConfidence.Estimated;
            item.maxStack = 1;
            item.weight = profile.inventoryWeight;
            item.rarity = profile.rarity;
            item.pickupMode = LootPickupMode.EquipOnPickup;
            item.weaponDefinition = definition;
            item.weaponPrefab = weaponPrefab;
            item.preferredWeaponSlot = profile.preferredSlot;
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(profile.iconPath);
            item.nearbySecondaryText = profile.family switch
            {
                WeaponFamily.AssaultRifle => "Assault Rifle",
                WeaponFamily.SubmachineGun => "Submachine Gun",
                WeaponFamily.SniperRifle => "Marksman / Sniper Rifle",
                WeaponFamily.Shotgun => "Shotgun",
                WeaponFamily.Pistol => "Pistol",
                _ => "Weapon"
            };
            item.worldModel = model;
            item.worldOffset = new Vector3(0f, 0.28f, 0f);
            item.worldEulerAngles =
                new Vector3(1.246f, -88.748f, 89.526f);
            item.worldScale = Vector3.one * 40f;
            EditorUtility.SetDirty(item);
        }

        private static void ReplaceLegacyRifleAndAddWeapons(
            IReadOnlyList<InventoryItemDefinition> weaponItems)
        {
            if (weaponItems.Count == 0)
            {
                return;
            }

            LootTableDefinition lootTable =
                AssetDatabase.LoadAssetAtPath<LootTableDefinition>(
                    LootTablePath
                );

            if (lootTable == null)
            {
                Debug.LogError($"No se encontró {LootTablePath}.");
                return;
            }

            InventoryItemDefinition legacyRifle =
                AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(
                    LegacyRifleItemPath
                );

            SerializedObject tableObject =
                new SerializedObject(lootTable);
            SerializedProperty entries =
                tableObject.FindProperty("entries");

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(i);
                SerializedProperty itemProperty =
                    entry.FindPropertyRelative("item");

                if (itemProperty.objectReferenceValue == legacyRifle)
                {
                    itemProperty.objectReferenceValue = weaponItems[0];
                }
            }

            foreach (InventoryItemDefinition item in weaponItems)
            {
                if (ContainsItem(entries, item))
                {
                    continue;
                }

                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("item").objectReferenceValue = item;
                entry.FindPropertyRelative("weight").floatValue =
                    GetProfileWeight(item.itemId);
                entry.FindPropertyRelative("minAmount").intValue = 1;
                entry.FindPropertyRelative("maxAmount").intValue = 1;
            }

            tableObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lootTable);
        }

        private static void AddWeaponsToGuaranteedLoot(
            IReadOnlyList<InventoryItemDefinition> weaponItems)
        {
            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(LootSpawnerPath);

            if (prefabRoot == null)
            {
                Debug.LogError($"No se encontró {LootSpawnerPath}.");
                return;
            }

            try
            {
                LootSpawner spawner =
                    prefabRoot.GetComponent<LootSpawner>();

                if (spawner == null)
                {
                    Debug.LogError("El prefab de loot no contiene LootSpawner.");
                    return;
                }

                InventoryItemDefinition legacyRifle =
                    AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(
                        LegacyRifleItemPath
                    );

                SerializedObject spawnerObject =
                    new SerializedObject(spawner);
                SerializedProperty guaranteed =
                    spawnerObject.FindProperty("guaranteedItems");

                for (int i = 0; i < guaranteed.arraySize; i++)
                {
                    SerializedProperty element =
                        guaranteed.GetArrayElementAtIndex(i);

                    if (element.objectReferenceValue == legacyRifle)
                    {
                        element.objectReferenceValue = weaponItems[0];
                    }
                }

                foreach (InventoryItemDefinition item in weaponItems)
                {
                    if (ContainsReference(guaranteed, item))
                    {
                        continue;
                    }

                    int index = guaranteed.arraySize;
                    guaranteed.InsertArrayElementAtIndex(index);
                    guaranteed.GetArrayElementAtIndex(index)
                        .objectReferenceValue = item;
                }

                spawnerObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, LootSpawnerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool ContainsItem(
            SerializedProperty entries,
            InventoryItemDefinition item)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                Object current =
                    entries.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("item")
                        .objectReferenceValue;

                if (current == item)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsReference(
            SerializedProperty array,
            Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetProfileWeight(string itemId)
        {
            foreach (WeaponProfile profile in CreateProfiles())
            {
                if (profile.itemId == itemId)
                {
                    return profile.lootWeight;
                }
            }

            return 1f;
        }

        private static T GetOrCreateAsset<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private sealed class WeaponProfile
        {
            public string assetName;
            public string weaponId;
            public string itemId;
            public string displayName;
            public WeaponFamily family;
            public AmmoType ammoType;
            public string modelPath;
            public string iconPath;
            public LootRarity rarity;
            public float lootWeight;
            public float inventoryWeight;
            public int preferredSlot;
            public float damage;
            public float shotsPerSecond;
            public float range;
            public int magazineSize;
            public int reserveAmmo;
            public float reloadTime;
            public float emptyReloadTime;
            public bool supportsSingle;
            public bool supportsBurst;
            public bool supportsAuto;
            public WeaponFireMode initialMode;
            public int projectiles;
            public float hipSpread;
            public float adsSpread;
            public float bloomPerShot;
            public float maxBloom;
            public float verticalRecoil;
            public float horizontalRecoil;
            public float impactScale;
            public float bulletHoleScale;
            public float tracerWidth;
            public float muzzleX;
        }
    }
}
