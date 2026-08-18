RulesOfSurvival_Go - Combat Visual Integration v5

Base: user-provided RulesOfSurvival_Go.zip (2026-08-18)

Adds/repairs runtime integration for:
- Crosshair and prototype combat HUD without requiring a Canvas in the scene.
- PlayerAimController auto-created on the player if missing.
- WeaponEffects auto-created/configured for every weapon slot.
- Runtime muzzle flash and tracer when references are absent.
- Impact and bullet-hole prefabs loaded from Resources.
- WeaponRecoil auto-created when missing.
- WeaponController lazily reconnects Aim/Muzzle/Effects/Recoil references.

Existing Animator Controller is preserved. The project already contains RifleIdle and BT_AimLocomotion.
No fire/reload animation clips are added because those clips are not present in the current project source.
