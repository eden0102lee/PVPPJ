# Plugin Setup Guide

Unity **6000.0.39f1** / **URP 17.0.3** — third-party asset dependency map and conflict resolution.

Run **GamePJ > Plugin Setup > Run Conflict Audit** in the Unity Editor after any package or Asset Store import change.

---

## Package Manager (no version conflicts)

| Package | Version | Role |
|---------|---------|------|
| `com.unity.render-pipelines.universal` | 17.0.3 | Project render pipeline |
| `jp.lilxyzw.liltoon` | 2.3.4 (git UPM) | P09 character materials |
| `com.unity.toonshader` | 0.10.2-preview | CombatGirls fallback / UTS3 |
| `com.projectdawn.cozy-builder` | 0.5.6 (embedded) | Procedural building tool |
| MagicaCloth2 | 2.18.2 (Assets) | Cloth simulation |

Burst, Collections, and Mathematics resolve to a single version via `packages-lock.json`.

---

## Toon shader stack (by asset pack)

Do **not** merge these into one global toon solution unless you intentionally want a unified art direction.

| Asset pack | Required shader | How to install |
|------------|-----------------|----------------|
| **P09_Modular_Humanoid** | lilToon | Installed via `Packages/manifest.json` → `jp.lilxyzw.liltoon` |
| **CombatGirlsCharacterPack** | Unity Chan Toon Shader SDF | Import `Unity Chan Toon Shader - SDF - Unity6_URP_SwordShield.unitypackage` from Asset Store, or use `com.unity.toonshader` and convert materials |
| **ToonScapes** | `ToonScapes/URP/*` | Already included in the pack |
| **Vefects** | URP / Shader Graph variants | Import `Stylized VFX Shader Graph Assets.unitypackage` |

**Keep `com.unity.toonshader`?** Yes, for now — it provides a UPM-based fallback for CombatGirls when the bundled UTS SDF `.unitypackage` is missing, and does not conflict with lilToon or ToonScapes at compile time.

---

## Missing `.unitypackage` files

These paths have `.meta` files but the binary is missing from the repo. Re-download from the Asset Store and place the file at the exact path, then use **GamePJ > Plugin Setup > Import Available .unitypackage Files**.

| File | Path |
|------|------|
| P09 MagicaCloth2 setup | `Assets/P09_Modular_Humanoid/Setup_MagicaCloth2.unitypackage` |
| CombatGirls UTS (Unity 6 URP) | `Assets/CombatGirlsCharacterPack/Unity Chan Toon Shader - SDF - Unity6_URP_SwordShield.unitypackage` |
| Vefects Shader Graph | `Assets/Vefects/Stylized VFX/Shader Graph/Stylized VFX Shader Graph Assets.unitypackage` |

lilToon no longer needs the legacy P09 installer — it is pulled from GitHub via Package Manager.

---

## Vefects × URP

The project ships many **BIRP** Amplify shaders under `Assets/Vefects/`. They compile but are not correct for URP production use.

1. Import the **Shader Graph Assets** `.unitypackage` from the Vefects Asset Store download.
2. Use **GamePJ > Plugin Setup > Fix URP Depth and Opaque Textures** (or confirm manually):
   - `Assets/Settings/PC_RPAsset.asset` — Depth + Opaque enabled
   - `Assets/Settings/Mobile_RPAsset.asset` — Depth + Opaque enabled
3. Distortion VFX requires both textures (see `Assets/Vefects/Stylized VFX/Documentation/DISTORTION INFO.txt`).

---

## Verification checklist

After opening the project in Unity 6:

- [ ] Console: no shader / asmdef errors
- [ ] **GamePJ > Plugin Setup > Run Conflict Audit** — lilToon GUID resolves
- [ ] P09 demo / prefab materials are not pink
- [ ] CombatGirls scene: import UTS package or accept Standard fallback until imported
- [ ] Vefects demo: import Shader Graph package before using in production
- [ ] MagicaCloth2 example scene (URP) runs
- [ ] Cozy Builder window opens without exceptions

Latest automated report: `Assets/Editor/PluginSetup/last-audit-report.txt`
