# Phase 3 Rendering And PBR Migration Guide

## Scope

Godot files remain read-only. This phase defines how the downloaded ambientCG
PBR materials and the horror lighting setup should be recreated in Unity.

Primary source folders:

- `assets/external/ambientcg/materials/Bark014`
- `assets/external/ambientcg/materials/Grass007`
- `assets/external/ambientcg/materials/Ground037`
- `assets/external/ambientcg/materials/Ground103`
- `assets/external/ambientcg/materials/PavingStones150`
- `assets/external/ambientcg/materials/Plaster001`
- `assets/external/ambientcg/materials/Rock064`
- `assets/external/ambientcg/materials/Wood095`

Unity target folder after migration:

- `Assets/External/ambientcg/materials`
- `Assets/KHorrorGame/Materials/AmbientCG`
- `Assets/KHorrorGame/Materials/AmbientCG/MaskMaps`
- `Assets/KHorrorGame/Rendering`

## Pipeline Decision

Use URP Lit as the default migration target. It is the most practical path for
this prototype because it supports normal maps, occlusion, metallic/smoothness
maps, shadowed realtime lights, SSAO, decals, and a lower-cost post-processing
stack. HDRP remains the later upgrade path if true volumetric fog and heavier
lighting are required.

Fallback shader order used by the editor utility:

1. `Universal Render Pipeline/Lit`
2. `HDRP/Lit`
3. `Standard`

## ambientCG Texture Mapping

ambientCG source file naming:

| Source suffix | Unity usage |
| --- | --- |
| `_Color.jpg` | Base color / albedo texture. Import with sRGB on. |
| `_NormalGL.jpg` | Default Unity normal map candidate. Import as Normal Map with sRGB off. |
| `_NormalDX.jpg` | Backup normal map if the green channel appears inverted in the target pipeline. |
| `_Roughness.jpg` | Invert into smoothness and pack into mask alpha. Import with sRGB off. |
| `_AmbientOcclusion.jpg` | Pack into mask green and also assign to occlusion where supported. Import with sRGB off. |
| `_Displacement.jpg` | Height/parallax input. Keep strength subtle in first pass. Import with sRGB off. |

URP/HDRP channel-packed mask output:

| Channel | Value |
| --- | --- |
| R | Metallic, fixed at `0.0` for these organic/stone/plaster materials. |
| G | Ambient occlusion from `_AmbientOcclusion.jpg`, or `1.0` if missing. |
| B | Detail mask, fixed at `1.0`. |
| A | Smoothness, computed as `1.0 - roughness`. |

Unity documentation reference:

- URP channel-packed texture layout: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/shaders-in-universalrp-channel-packed-texture.html
- Lightmapping overview: https://docs.unity3d.com/Manual/Lightmappers.html

## Material Assignments

| Material | Use in Unity prefabs | Tiling target |
| --- | --- | --- |
| `Ground103` | Forest approach road, damp soil patches, hidden side route floor. | 2.5 to 4.0 meters per tile. |
| `Ground037` | Courtyard packed earth, inner yards, uneven walkable surfaces. | 3.0 to 5.0 meters per tile. |
| `Grass007` | Verge strips, overgrowth edges, yard breakup planes, side passage cover. | 1.5 to 2.5 meters per tile. |
| `Bark014` | Tree trunks, jangseung, old posts, rough gate support wood. | 0.7 to 1.2 meters per tile. |
| `Wood095` | Bongo interior panels, hanok beams, shrine wood, gate leaves. | 1.2 to 2.5 meters per tile. |
| `Rock064` | Stone wall bases, wells, steps, exposed rocks, shrine stones. | 0.8 to 1.6 meters per tile. |
| `PavingStones150` | Gate threshold, broken stepping stones, settlement paving. | 1.5 to 2.5 meters per tile. |
| `Plaster001` | Main house wall plaster, inner partitions, storehouse walls. | 1.5 to 3.0 meters per tile. |

Do not cover the whole map with one material. The Godot prototype looked too
clay-like because large primitive surfaces shared broad flat colors. In Unity,
split floors and walls into material zones first, then apply the PBR textures
with different tiling values per zone.

## Unity Editor Automation

Copy the ambientCG material folders into the Unity project at:

```text
Assets/External/ambientcg/materials
```

Then copy the scripts from this phase into:

```text
Assets/KHorrorGame/Rendering
```

Run:

```text
Tools/K Horror Migration/Build ambientCG Materials
```

The tool will:

1. Find each ambientCG material folder.
2. Configure import settings for color, normal, roughness, AO, and height maps.
3. Generate a Unity mask map in `Assets/KHorrorGame/Materials/AmbientCG/MaskMaps`.
4. Create or update a Lit material in `Assets/KHorrorGame/Materials/AmbientCG`.
5. Assign base color, normal, mask, occlusion, and height textures where the active shader supports them.

## Horror Lighting Baseline

Create a `KHorrorLightingProfile` asset and assign it to a scene object with
`KHorrorLightingRig`.

Baseline values:

| Setting | Value |
| --- | --- |
| Fog mode | `ExponentialSquared` |
| Fog density | `0.035` to `0.055` |
| Fog color | Near black green-gray, around RGB `(0.025, 0.032, 0.028)` |
| Ambient mode | Flat |
| Ambient color | RGB `(0.012, 0.015, 0.014)` |
| Reflection intensity | `0.05` to `0.12` |
| Moon directional intensity | `0.08` to `0.18` |
| Moon color | Desaturated blue-gray |
| Flashlight range | `13` to `16` |
| Flashlight spot angle | `38` to `45` |

Recommended URP scene settings:

- Enable HDR on the URP asset and camera.
- Enable Depth Texture and Opaque Texture if fog or post effects need them.
- Add SSAO through the renderer feature list.
- Use contact shadows on key lights if supported by the active URP renderer.
- Keep most estate geometry static for baked direct/indirect lighting.
- Use realtime point/spot lights only for the flashlight, lanterns, and event scares.

Recommended HDRP upgrade settings:

- Add one Global Volume for Visual Environment, Fog, Exposure, Color Adjustments, Bloom, and Vignette.
- Use Volumetric Fog with a long mean free path and high anisotropy only in the estate area.
- Keep dense fog local around the approach forest, side passage, shrine, and back loops.

## Lightmap And GI Strategy

Use baked GI for static estate geometry and realtime lights for gameplay-driven
lights. Unity lightmaps store precomputed static lighting; realtime lights can
be layered on top at runtime. That split fits this game because the estate walls,
floors, roofs, gates, and courtyard props are mostly static, while the player
flashlight, event lights, and threat cues need frame-by-frame updates.

Initial bake guidance:

| Group | Static flags | Light mode |
| --- | --- | --- |
| Estate floors, walls, roofs | Contribute GI, Occluder Static | Baked |
| Van structural shell | Contribute GI if inside scene, Occluder Static | Baked or Mixed |
| Gate leaves and sliding doors | Occluder Static off if animated | Realtime shadows only |
| Artifacts | Static off | Realtime/direct only |
| Ghost/threat actors | Static off | Realtime/direct only |
| Trees and large brush | Contribute GI for trunks, batching static when not animated | Baked |

Keep lightmap resolution modest in the first Unity pass:

- Approach and forest: `5` to `8` texels per unit.
- Courtyard and main house: `10` to `16` texels per unit.
- Small props: rely on light probes unless they are close to player inspection.

## Visual Polish Pass Order

1. Replace flat primitive materials with the generated PBR materials.
2. Split the long approach into forest road, mud, grass verge, and stone patches.
3. Give the gate, house, shrine, and settlement office different material palettes.
4. Add baked shadowing from roofs, walls, and tall trees.
5. Add realtime flashlight and lantern contrast.
6. Add local fog volumes or HDRP volumetric fog after the scene reads correctly without fog.
7. Tune post-processing last so it does not hide collision or layout problems.

## Acceptance Checklist

- All eight ambientCG folders produce Unity `.mat` files.
- Every generated mask map has sRGB disabled.
- Normal maps use `NormalGL` by default.
- Wood, plaster, stone, earth, grass, and bark are visibly different in the scene.
- The bongo interior text/tablet is not the main lighting source.
- The approach road remains readable for navigation while the forest edges stay dark.
- The courtyard has material variety instead of one broad clay surface.
- Static estate geometry is ready for a baked lighting pass.
