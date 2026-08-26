# Arquitectura de Animaciones del Personaje — Rules of Survival Classic

> Documento base para la implementación del Animator del personaje en `codex/animator-ros-classic`.
>
> Este documento define la arquitectura acordada para el proyecto. La regla principal es evitar animaciones completas por combinación y componer el resultado mediante layers especializados.

## Objetivo

Organizar el Animator de Unity de manera modular, permitiendo combinar:

- Movimiento de piernas.
- Posturas.
- Armas.
- Apuntado.
- Disparo.
- Recarga.
- Lean.
- Acciones.
- Gestos.
- Paracaídas.
- Natación.
- Vehículos.
- Knocked / muerte.

No se deben crear combinaciones como `RunWithRifle`, `RunWithShotgun`, `RunWithSMG` o `RunWithPistol`.

La composición esperada es:

```text
Lower Body: Run
+
Upper Body: Rifle Pose
```

## Layers principales

```text
0 - Base_Locomotion
1 - UpperBody_Weapon
2 - UpperBody_Actions
3 - Aim_Offset
4 - Lean
5 - FullBody_Actions
```

---

# 0 - Base_Locomotion

Responsabilidad:

- Idle.
- Walk.
- Run.
- Sprint.
- Crouch.
- Prone.
- Jump.
- Fall.
- Landing.

Control principal:

- Pelvis.
- Piernas.
- Pies.

En acciones concretas puede utilizar Full Body.

```text
Base_Locomotion
│
├── Grounded
│   ├── Standing
│   │   ├── Idle
│   │   ├── Walk BlendTree 2D
│   │   ├── Run BlendTree 2D
│   │   └── Sprint
│   ├── Crouch
│   │   ├── Crouch Enter
│   │   ├── Crouch Idle
│   │   ├── Crouch Move BlendTree 2D
│   │   └── Crouch Exit
│   └── Prone
│       ├── Prone Enter
│       ├── Prone Idle
│       ├── Prone Crawl BlendTree 2D
│       └── Prone Exit
│
└── Airborne
    ├── Jump Start
    ├── Jump Rise
    ├── Fall
    └── Land
```

## Blend Trees direccionales

Parámetros:

```text
MoveX
MoveY
```

Distribución:

```text
                     FORWARD
                      (0, 1)

          FORWARD LEFT       FORWARD RIGHT
             (-1, 1)            (1, 1)

LEFT                                             RIGHT
(-1, 0)                IDLE                     (1, 0)
                       (0, 0)

          BACKWARD LEFT      BACKWARD RIGHT
             (-1,-1)            (1,-1)

                    BACKWARD
                     (0,-1)
```

Teclas:

```text
W       => MoveX =  0, MoveY =  1
S       => MoveX =  0, MoveY = -1
A       => MoveX = -1, MoveY =  0
D       => MoveX =  1, MoveY =  0
W + A   => MoveX = -1, MoveY =  1
W + D   => MoveX =  1, MoveY =  1
S + A   => MoveX = -1, MoveY = -1
S + D   => MoveX =  1, MoveY = -1
```

## Standing

```text
Standing
├── Idle
├── Walk 2D
│   ├── Forward
│   ├── Forward Left
│   ├── Left
│   ├── Backward Left
│   ├── Backward
│   ├── Backward Right
│   ├── Right
│   └── Forward Right
├── Run 2D
│   ├── Forward
│   ├── Forward Left
│   ├── Left
│   ├── Backward Left
│   ├── Backward
│   ├── Backward Right
│   ├── Right
│   └── Forward Right
└── Sprint
    ├── Forward
    ├── Forward Left
    └── Forward Right
```

Sprint queda principalmente orientado hacia adelante. No se consideran necesarios Sprint Backward, Sprint Left ni Sprint Right para la base clásica.

## Crouch

```text
Crouch
├── Crouch Enter
├── Crouch Idle
├── Crouch Move 2D
│   ├── Forward
│   ├── Forward Left
│   ├── Left
│   ├── Backward Left
│   ├── Backward
│   ├── Backward Right
│   ├── Right
│   └── Forward Right
└── Crouch Exit
```

Flujo:

```text
Standing
   ↓
Crouch Enter
   ↓
Crouch Idle / Move
   ↓
Crouch Exit
   ↓
Standing
```

## Prone

```text
Prone
├── Prone Enter
├── Prone Idle
├── Prone Crawl 2D
│   ├── Forward
│   ├── Forward Left
│   ├── Left
│   ├── Backward Left
│   ├── Backward
│   ├── Backward Right
│   ├── Right
│   └── Forward Right
└── Prone Exit
```

Transiciones permitidas:

```text
Standing -> Prone
Crouch   -> Prone
Prone    -> Crouch
Prone    -> Standing
```

## Jump / Airborne

```text
Airborne
├── Jump Start
├── Jump Rise
├── Fall Loop
└── Land
```

Flujo base:

```text
Grounded
   ↓
Jump Start
   ↓
Jump Rise
   ↓
Fall
   ↓
Land
   ↓
Grounded
```

Se puede ampliar posteriormente a:

```text
Land Soft
Land Normal
Land Hard
```

---

# 1 - UpperBody_Weapon

Responsabilidad:

- Pose del arma.
- Apuntar.
- Disparar.
- Recoil.
- Diferentes tipos de armas.

Avatar Mask aproximado: Spine hacia arriba.

Incluye:

- Spine.
- Chest.
- UpperChest.
- Shoulders.
- Arms.
- Hands.
- Neck.
- Head.

No debe controlar piernas ni pies.

```text
UpperBody_Weapon
├── Unarmed
├── Rifle
│   ├── Rifle Idle
│   ├── Rifle Aim
│   ├── Rifle Fire
│   └── Rifle Recoil
├── SMG
│   ├── SMG Idle
│   ├── SMG Aim
│   ├── SMG Fire
│   └── SMG Recoil
├── Shotgun
│   ├── Shotgun Idle
│   ├── Shotgun Aim
│   ├── Shotgun Fire
│   └── Shotgun Recoil
├── Sniper
│   ├── Sniper Idle
│   ├── Sniper Aim
│   └── Sniper Fire
├── Pistol
│   ├── Pistol Idle
│   ├── Pistol Aim
│   ├── Pistol Fire
│   └── Pistol Recoil
├── Melee
└── Throwable
```

Regla: no crear `Rifle_Run_Forward`, `Rifle_Run_Left`, `Rifle_Run_Right`, `Rifle_Crouch_Walk`, etc. Esas combinaciones deben surgir de la mezcla de layers.

Ejemplo:

```text
Layer 0: Run Left
+
Layer 1: Rifle Aim
=
Correr a la izquierda manteniendo el rifle apuntando
```

---

# 2 - UpperBody_Actions

Responsabilidad:

- Recargar.
- Sacar arma.
- Guardar arma.
- Cambiar arma.
- Consumibles.
- Lanzables.
- Pickup.
- Revive.
- Otras interacciones.

```text
UpperBody_Actions
├── Empty
├── Reload
│   ├── Rifle Reload
│   ├── SMG Reload
│   ├── Shotgun Reload
│   ├── Sniper Reload
│   └── Pistol Reload
├── Weapon
│   ├── Draw Weapon
│   ├── Holster Weapon
│   └── Switch Weapon
├── Consumables
│   ├── Bandage
│   ├── MedKit
│   ├── Drink
│   └── Booster
├── Throwable
│   ├── Equip
│   ├── Prepare
│   ├── Aim
│   ├── Throw
│   └── Cancel
└── Interaction
    ├── Pickup
    └── Revive
```

Ejemplo esperado:

```text
Layer 0: Run Forward
Layer 1: Rifle Pose
Layer 2: Rifle Reload
```

Resultado: el personaje sigue desplazándose mientras recarga.

---

# 3 - Aim_Offset

Tipo: **Additive**.

Responsabilidad: orientar el torso siguiendo la cámara sin reemplazar locomoción.

```text
Aim_Offset
├── Aim Center
├── Aim Up
├── Aim Down
├── Aim Left
└── Aim Right
```

Parámetros:

```text
AimPitch
AimYaw
```

Blend Tree 2D recomendado:

```text
Aim Center = ( 0,  0)
Aim Up     = ( 0,  1)
Aim Down   = ( 0, -1)
Aim Left   = (-1,  0)
Aim Right  = ( 1,  0)
```

---

# 4 - Lean

Tipo: **Additive**.

Responsabilidad: inclinación del torso izquierda/derecha.

```text
Lean
├── Lean Left
├── Lean Center
└── Lean Right
```

Parámetro:

```text
Lean = -1  -> Left
Lean =  0  -> Center
Lean =  1  -> Right
```

Blend Tree:

```text
Lean Left -------- Center -------- Lean Right
   -1                0                +1
```

Avatar Mask recomendado:

- Spine.
- Chest.
- UpperChest.
- Shoulders.
- Arms.
- Neck.
- Head.

No debe controlar pelvis, piernas ni pies.

Ejemplo simultáneo:

```text
Layer 0: Crouch Move Right
Layer 1: Rifle Aim
Layer 3: Aim Up
Layer 4: Lean Left
```

---

# 5 - FullBody_Actions

Responsabilidad: acciones que necesitan controlar todo el cuerpo.

```text
FullBody_Actions
├── Empty
├── Vault
│   ├── Vault Low
│   ├── Vault Window
│   └── Vault High
├── AirDrop
│   ├── Exit Aircraft
│   ├── FreeFall Enter
│   ├── FreeFall
│   ├── Parachute Deploy
│   ├── Parachute Glide
│   └── Parachute Land
├── Swimming
│   ├── Swim Idle
│   ├── Swim Forward
│   ├── Swim Left
│   ├── Swim Right
│   └── Underwater Swim
├── Vehicle
│   ├── Enter
│   ├── Driver
│   ├── Passenger
│   └── Exit
├── Knocked
│   ├── KnockDown
│   ├── Knocked Idle
│   ├── Knocked Crawl
│   └── Revived
├── Gestures
│   ├── Gesture 01
│   ├── Gesture 02
│   ├── Gesture 03
│   └── Gesture N
└── Death
    ├── Death Forward
    ├── Death Backward
    ├── Death Left
    ├── Death Right
    └── Death Variants
```

## Vault

```text
Detectar obstáculo
        ↓
¿Se puede hacer Vault?
        ↓
Bloquear locomoción normal
        ↓
Vault Animation
        ↓
Root Motion o Match Target
        ↓
Finalizar Vault
        ↓
Grounded
```

Estados:

```text
Vault Low
Vault Window
Vault High
```

## Free Fall / Paracaídas

Arquitectura completa prevista:

```text
AirDrop
├── Exit Aircraft
├── FreeFall Enter
├── FreeFall Loop
├── FreeFall Steering
├── Parachute Deploy
├── Parachute Glide
├── Parachute Turn Left
├── Parachute Turn Right
├── Parachute Forward
├── Parachute Brake
└── Parachute Landing
```

Flujo:

```text
Aircraft
   ↓
Exit Aircraft
   ↓
FreeFall Enter
   ↓
FreeFall Loop
   ↓
Deploy Parachute
   ↓
Parachute Open
   ↓
Parachute Glide
   ↓
Landing
   ↓
Grounded
```

## Swimming

```text
Water
├── Wade
│   ├── Wade Idle
│   ├── Wade Forward
│   ├── Wade Left
│   └── Wade Right
├── Surface Swim
│   ├── Swim Idle
│   ├── Swim Forward
│   ├── Swim Left
│   └── Swim Right
├── Dive
├── Underwater Swim
└── Surface
```

Regla:

- Agua baja: caminar con resistencia.
- Agua profunda: nadar.

## Knocked

```text
Knocked
├── KnockDown
├── Knocked Idle
├── Knocked Crawl
└── Revived
```

Flujo:

```text
Combat
   ↓
Health <= 0
   ↓
KnockDown
   ↓
Knocked Idle / Crawl
```

Revive:

```text
Knocked
   ↓
Revived
   ↓
Standing
```

Muerte:

```text
Knocked
   ↓
Death
```

## Gestures

```text
Gestures
├── Gesture 01
├── Gesture 02
├── Gesture 03
├── Gesture 04
└── Gesture N
```

Flujo esperado:

```text
Guardar arma
    ↓
Gesture
    ↓
Terminar / cancelar
    ↓
Sacar arma
```

Los gestos se consideran Full Body.

## Vehicles

```text
Vehicle
├── Enter Driver
├── Enter Passenger
├── Driver Idle
├── Passenger Idle
├── Passenger Aim
├── Passenger Fire
├── Seat Change
└── Exit
```

---

# Combinación real de layers

## Ejemplo 1 — Run Left + Rifle Aim

```text
Layer 0: Run Left
Layer 1: Rifle Aim
```

## Ejemplo 2 — Run Right + Aim Up + Lean Left

```text
Layer 0: Run Right
Layer 1: Rifle Aim
Layer 3: Aim Up
Layer 4: Lean Left
```

## Ejemplo 3 — Crouch diagonal + Aim

```text
Layer 0: Crouch Forward Right
Layer 1: Rifle Aim
Layer 3: Aim Offset
```

## Ejemplo 4 — Run + Reload

```text
Layer 0: Run Forward
Layer 1: Rifle Pose
Layer 2: Rifle Reload
```

## Ejemplo 5 — Jump + Fire

```text
Layer 0: Jump / Fall
Layer 1: Rifle Fire
Layer 3: Aim Offset
```

Saltar no debe bloquear automáticamente el uso del arma.

## Ejemplo 6 — Crouch + Lean

```text
Layer 0: Crouch Move
Layer 1: Rifle Aim
Layer 3: Aim Offset
Layer 4: Lean Right
```

## Ejemplo 7 — Gesture

```text
Layer 5: Gesture
```

`FullBody_Actions` toma prioridad sobre los demás.

---

# Parámetros del Animator

## Movimiento

```text
MoveX                  Float
MoveY                  Float
Speed                  Float
VerticalVelocity       Float
```

## Estado

```text
IsGrounded             Bool
IsSprinting            Bool
IsAutoRunning          Bool
```

## Postura

```text
Stance                 Int
```

Valores:

```text
0 = Standing
1 = Crouch
2 = Prone
```

## Armas

```text
WeaponType             Int
IsAiming               Bool
IsFiring               Bool
IsReloading            Bool
```

Valores de `WeaponType`:

```text
0 = Unarmed
1 = Rifle
2 = SMG
3 = Shotgun
4 = Sniper
5 = Pistol
6 = Melee
7 = Throwable
```

## Aim

```text
AimPitch               Float
AimYaw                 Float
```

## Lean

```text
Lean                   Float
```

Valores:

```text
-1 = Left
 0 = Center
 1 = Right
```

## Acciones

```text
IsVaulting             Bool
IsSwimming             Bool
IsUnderwater           Bool
IsParachuting          Bool
IsFreeFalling          Bool
IsKnocked              Bool
```

## Full Body

```text
FullBodyAction         Int
```

Valores iniciales:

```text
0 = None
1 = Vault
2 = FreeFall
3 = Parachute
4 = Swimming
5 = Vehicle
6 = Knocked
7 = Gesture
8 = Death
```

---

# Flujo general de locomoción

```text
                         Crouch
                           ↑
                           │
Idle ──→ Walk ──→ Run ─────┼──→ Sprint
 │                         │
 │                         ↓
 │                       Prone
 │
 └──→ Jump
        │
        ↓
     Jump Rise
        │
        ↓
       Fall
        │
        ↓
       Land
        │
        ↓
     Grounded
```

# Transiciones de postura

```text
Standing
   ├── Crouch
   └── Prone

Crouch
   ├── Standing
   └── Prone

Prone
   ├── Crouch
   └── Standing
```

---

# Nombres recomendados de animaciones

## Standing

```text
LOC_Stand_Idle

LOC_Stand_Walk_F
LOC_Stand_Walk_FL
LOC_Stand_Walk_L
LOC_Stand_Walk_BL
LOC_Stand_Walk_B
LOC_Stand_Walk_BR
LOC_Stand_Walk_R
LOC_Stand_Walk_FR

LOC_Stand_Run_F
LOC_Stand_Run_FL
LOC_Stand_Run_L
LOC_Stand_Run_BL
LOC_Stand_Run_B
LOC_Stand_Run_BR
LOC_Stand_Run_R
LOC_Stand_Run_FR

LOC_Sprint_F
LOC_Sprint_FL
LOC_Sprint_FR
```

## Crouch

```text
LOC_Crouch_Enter
LOC_Crouch_Idle
LOC_Crouch_Move_F
LOC_Crouch_Move_FL
LOC_Crouch_Move_L
LOC_Crouch_Move_BL
LOC_Crouch_Move_B
LOC_Crouch_Move_BR
LOC_Crouch_Move_R
LOC_Crouch_Move_FR
LOC_Crouch_Exit
```

## Prone

```text
LOC_Prone_Enter
LOC_Prone_Idle
LOC_Prone_Crawl_F
LOC_Prone_Crawl_FL
LOC_Prone_Crawl_L
LOC_Prone_Crawl_BL
LOC_Prone_Crawl_B
LOC_Prone_Crawl_BR
LOC_Prone_Crawl_R
LOC_Prone_Crawl_FR
LOC_Prone_Exit
```

## Jump

```text
LOC_Jump_Start
LOC_Jump_Rise
LOC_Fall_Loop
LOC_Land
LOC_Land_Soft
LOC_Land_Hard
```

## Rifle

```text
WPN_Rifle_Idle
WPN_Rifle_Aim
WPN_Rifle_Fire
WPN_Rifle_Recoil
WPN_Rifle_Reload
WPN_Rifle_Draw
WPN_Rifle_Holster
```

## SMG

```text
WPN_SMG_Idle
WPN_SMG_Aim
WPN_SMG_Fire
WPN_SMG_Recoil
WPN_SMG_Reload
WPN_SMG_Draw
WPN_SMG_Holster
```

## Shotgun

```text
WPN_Shotgun_Idle
WPN_Shotgun_Aim
WPN_Shotgun_Fire
WPN_Shotgun_Recoil
WPN_Shotgun_Reload
WPN_Shotgun_Draw
WPN_Shotgun_Holster
```

## Sniper

```text
WPN_Sniper_Idle
WPN_Sniper_Aim
WPN_Sniper_Fire
WPN_Sniper_Recoil
WPN_Sniper_Reload
WPN_Sniper_Draw
WPN_Sniper_Holster
```

## Pistol

```text
WPN_Pistol_Idle
WPN_Pistol_Aim
WPN_Pistol_Fire
WPN_Pistol_Recoil
WPN_Pistol_Reload
WPN_Pistol_Draw
WPN_Pistol_Holster
```

## Aim

```text
AIM_Center
AIM_Up
AIM_Down
AIM_Left
AIM_Right
```

## Lean

```text
LEAN_Left
LEAN_Center
LEAN_Right
```

## Consumables

```text
ACT_Bandage
ACT_MedKit
ACT_Drink
ACT_Booster
```

## Throwables

```text
ACT_Throwable_Equip
ACT_Throwable_Prepare
ACT_Throwable_Aim
ACT_Throwable_Throw
ACT_Throwable_Cancel
```

## Interaction

```text
ACT_Pickup
ACT_Revive
ACT_Interact
```

## Vault

```text
ACT_Vault_Low
ACT_Vault_Window
ACT_Vault_High
```

## Parachute

```text
ACT_Aircraft_Exit
ACT_FreeFall_Enter
ACT_FreeFall_Loop
ACT_Parachute_Deploy
ACT_Parachute_Glide
ACT_Parachute_Left
ACT_Parachute_Right
ACT_Parachute_Brake
ACT_Parachute_Land
```

## Swimming

```text
ACT_Swim_Idle
ACT_Swim_Forward
ACT_Swim_Left
ACT_Swim_Right
ACT_Swim_Dive
ACT_Swim_Underwater
ACT_Swim_Surface
```

## Knocked

```text
ACT_Knocked_Enter
ACT_Knocked_Idle
ACT_Knocked_Crawl
ACT_Knocked_Revive
```

## Death

```text
ACT_Death_F
ACT_Death_B
ACT_Death_L
ACT_Death_R
```

## Gestures

```text
GST_Gesture_01
GST_Gesture_02
GST_Gesture_03
GST_Gesture_04
GST_Gesture_N
```

---

# Avatar Masks recomendados

## LowerBodyMask

```text
Pelvis
LeftUpperLeg
LeftLowerLeg
LeftFoot
LeftToes
RightUpperLeg
RightLowerLeg
RightFoot
RightToes
```

## UpperBodyMask

```text
Spine
Chest
UpperChest
LeftShoulder
RightShoulder
LeftArm
RightArm
LeftForeArm
RightForeArm
LeftHand
RightHand
Neck
Head
```

## AimMask

```text
Spine
Chest
UpperChest
Shoulders
Arms
Hands
Neck
Head
```

## LeanMask

```text
Spine
Chest
UpperChest
Shoulders
Arms
Neck
Head
```

## FullBody

Todo el personaje.

---

# Configuración final de los layers

## Layer 0 — Base_Locomotion

```text
Tipo: Override
Mask: Full Body / sistema base
Peso: 1
```

## Layer 1 — UpperBody_Weapon

```text
Tipo: Override
Mask: UpperBodyMask
Peso: 1
```

## Layer 2 — UpperBody_Actions

```text
Tipo: Override
Mask: UpperBodyMask
Peso: 0 normalmente
Peso: 1 durante la acción
```

## Layer 3 — Aim_Offset

```text
Tipo: Additive
Mask: AimMask
Peso: 1 cuando aplica Aim
```

## Layer 4 — Lean

```text
Tipo: Additive
Mask: LeanMask
Peso: 1
```

## Layer 5 — FullBody_Actions

```text
Tipo: Override
Mask: Full Body
Peso: 0 normalmente
Peso: 1 durante una acción Full Body
```

---

# Regla principal de diseño

```text
Base_Locomotion:
¿Qué hacen las piernas?

UpperBody_Weapon:
¿Cómo sostiene/apunta/dispara el arma?

UpperBody_Actions:
¿Qué acción temporal están haciendo brazos/torso?

Aim_Offset:
¿Hacia dónde está mirando/apuntando?

Lean:
¿Cuánto está inclinado el torso?

FullBody_Actions:
¿Existe una acción que necesite controlar todo el cuerpo?
```

---

# Arquitectura final

```text
PLAYER ANIMATOR
│
├── 0 Base_Locomotion
│   ├── Standing
│   │   ├── Idle
│   │   ├── Walk 2D
│   │   ├── Run 2D
│   │   └── Sprint
│   ├── Crouch
│   │   ├── Enter
│   │   ├── Idle
│   │   ├── Move 2D
│   │   └── Exit
│   ├── Prone
│   │   ├── Enter
│   │   ├── Idle
│   │   ├── Crawl 2D
│   │   └── Exit
│   └── Airborne
│       ├── Jump
│       ├── Rise
│       ├── Fall
│       └── Land
│
├── 1 UpperBody_Weapon
│   ├── Unarmed
│   ├── Rifle
│   ├── SMG
│   ├── Shotgun
│   ├── Sniper
│   ├── Pistol
│   ├── Melee
│   └── Throwable
│
├── 2 UpperBody_Actions
│   ├── Reload
│   ├── Draw
│   ├── Holster
│   ├── Switch
│   ├── Bandage
│   ├── MedKit
│   ├── Drink
│   ├── Throwable
│   ├── Pickup
│   └── Revive
│
├── 3 Aim_Offset
│   ├── Center
│   ├── Up
│   ├── Down
│   ├── Left
│   └── Right
│
├── 4 Lean
│   ├── Left
│   ├── Center
│   └── Right
│
└── 5 FullBody_Actions
    ├── Vault
    ├── FreeFall
    ├── Parachute
    ├── Swimming
    ├── Vehicle
    ├── Knocked
    ├── Gestures
    └── Death
```

---

# Orden recomendado de implementación

1. Standing Idle.
2. Walk 8 direcciones.
3. Run 8 direcciones.
4. Sprint.
5. Crouch.
6. Prone.
7. Jump.
8. Fall.
9. Landing.
10. UpperBody Weapon.
11. Aim.
12. Fire.
13. Reload.
14. Aim Offset.
15. Lean.
16. Draw / Holster.
17. Weapon Switch.
18. Consumables.
19. Throwable.
20. Pickup.
21. Revive.
22. Vault.
23. Knocked.
24. FreeFall.
25. Parachute.
26. Swimming.
27. Vehicles.
28. Gestures.
29. Death.

---

# Objetivo final

El personaje debe poder realizar combinaciones como:

```text
Run + Aim + Fire
Run + Reload
Crouch Walk + Aim + Lean
Prone Crawl + Aim
Jump + Aim + Fire
Run Left + Rifle Aim + Aim Up + Lean Right
```

Todo sin crear una animación completa distinta para cada combinación.

Esta arquitectura será la referencia de implementación para aproximar el comportamiento del personaje al Rules of Survival clásico.
