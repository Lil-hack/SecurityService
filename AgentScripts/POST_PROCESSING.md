# Stylized office post-processing

- `Assets/Settings/MainMenuPostProcessing.asset`: ACES, HDR grading, warm highlights/cool shadows, bloom 0.48, contrast 13, saturation 10, a soft vignette, very subtle grain/chromatic aberration/lens curvature. Gaussian depth of field softens only the distant background (10–22 m). The foreground character and overlay UI stay clear.
- `Assets/Settings/ExecutiveOfficePostProcessing.asset`: clean cartoon palette: Neutral tonemapping, exposure +0.4, contrast +12, saturation +18, bloom 0.30 and vignette 0.09. Neutral white balance and gamma +0.06 brighten the midtones while preserving the black point. Film grain, chromatic aberration, split toning, lens distortion, depth of field and motion blur are explicitly off. The colorful low-poly geometry stays crisp.

Both scenes use their own profile on the existing global `Atmosphere · evening studio` Volume. The scene lighting, furniture, gameplay and AI evaluation are preserved. Main/game cameras have post-processing, HDR, dithering and high-quality SMAA enabled. Only the menu camera explicitly requests a depth texture. Desktop URP assets use HDR color grading; the previous render scale is preserved.

Profiles are serialized scene dependencies, so URP's build-time scan can retain the bloom, lens, chromatic aberration and depth-of-field shader variants that are actually used. Runtime UI remains an overlay and does not inherit the lens blur or color grade.

Use **Tools / Office / Apply Stylized Post Processing** to reapply both presets, or edit the two assets in the Inspector. **Capture Stylized Post Processing** saves room previews under `.local/PostProcessingQA`. The apply command saves dirty scenes before switching and returns to the original scene. **Tools / Office / Apply Cartoon Executive Office** updates only ExecutiveOffice and leaves the menu profile unchanged.

Play-mode visual checks covered the menu UI, the boss room and kitchen, the MainMenu → ExecutiveOffice transition, and confirmed that gameplay's active volume stack has DOF Off and motion blur intensity 0. No console errors occurred during those checks.

## Golden window lighting in ExecutiveOffice

`Window light · golden hour` contains four warm, shadow-casting spot sources with a procedural window-frame cookie, plus a soft director rim light. Cool ambient/room fill gives the shadows a blue tint. Glass and skyline backdrops no longer block sunlight; opaque furniture and characters still cast shadows. This uses existing URP light cookies/additional-light shadows and preserves the cartoon post-processing profile.

Reapply with **Tools / Office / Apply Golden Window Light**. **Capture Golden Window Light** saves director/office/kitchen comparisons under `.local/WindowSunlightQA`. The command affects ExecutiveOffice and leaves MainMenu unchanged.

## Cartoon silhouettes

`Assets/Settings/OfficeOutlineSettings.asset` controls dark navy ink, character width (1.7 pixels), prop width (1.05 pixels), face/hand multiplier (0.55), and distance fade (20 m). ExecutiveOffice and MainMenu reference this shared asset. All four player prefabs, the director, staff and dog have skinned outlines; selected interactive props have merged silhouettes. Only geometry silhouettes are drawn, with ordinary depth occlusion and no face-facet edge detection. Tiny/distant objects receive thinner lines.

**Tools / Office / Apply Cartoon Silhouettes** bakes reusable smoothed meshes in `Assets/ExecutiveOffice/Outlines/Meshes` and updates the character prefabs and both scenes. Original render meshes/materials remain intact. Generated renderers share bones and copy blendshape weights after speech motion; static multipart props use one merged renderer each. These renderers are recreated on enable, omitted from scene serialization, and have no colliders or shadows. Play-mode checks covered all four avatar swaps, director mouth blendshape synchronization, NPC knockdown/recovery and pickup highlight material restoration, with no console errors.

## Living office details and toy finishes

**Tools / Office / Apply Living Details and Toy Materials** updates ExecutiveOffice and MainMenu. Each scene contains 20 steaming cups, 41 plant meshes with a light breeze, 12 changing monitors, one printer with green ready/amber activity indicators, and two retro desk fans (director's console and shared coffee counter). Empty shelf mugs stay empty. Imported filled mugs get a coffee surface below the rim.

- `Office/Plant Breeze` retains the installed URP Lit shading and atlas textures, with the same leaf deformation in forward, GBuffer, shadow, depth and depth-normal passes. The lower pot stays fixed. Plant materials expose `_BreezeParams` (base, height, phase, strength); static batching is disabled for these meshes.
- `Office/Animated Monitor` draws a dashboard, calendar and illustrated landscape; pictures change every 9 seconds with a 0.8-second crossfade and different phases by workstation position. It affects only the monitor display meshes; laptop interaction/UI stays intact.
- `OfficeCoffeeSteam` uses small soft transparent particles, capped at 24 per cup. Emission stops when the cup tilts or is more than 16 metres from the camera. Steam follows carried cups; effects are excluded from pickup bounds.
- `OfficeFanMotion` exposes rotor speed (285 degrees/second), head oscillation (14 degrees) and its period (11 seconds). `OfficePrinterActivity` exposes the 9-second activity cycle. Neither effect adds lights or modifies furniture physics.
- Wood uses tiled procedural grain and a restrained normal map; rounded furniture meshes receive missing tangents. Timber/paint/ceramics have soft satin highlights; walls and fabrics stay matte. Existing parquet textures, character materials and post-processing profiles are preserved.

Assets live in `Assets/ExecutiveOffice/LivingDetails`. Play checks confirmed monitor picture changes, fan rotation, active printer emission, coffee pickup/drop and stopping steam on inversion. Isolated plant captures changed 878 leaf-silhouette pixels and zero pot-silhouette pixels. Visual captures are under `.local/LivingDetailsQA`.

## Airborne dust

**Tools / Office / Apply Airborne Dust** replaces the three older window-dust systems in MainMenu and ExecutiveOffice with five prewarmed zones per scene: director, two studio windows, kitchen, and a much sparser studio centre. The group is `Airborne dust · sunlight`. Each ParticleSystem exposes density, tint, size and drift in the Inspector. The scene-wide cap is 333 particles; the Play check counted 310.

`Office/Airborne Dust` renders small soft round specks with ordinary depth occlusion and alpha blending, fading within 0.65–1.3 m of the camera and at 16–20 m distance. Screen size is capped, motion uses slow world-space drift and low-frequency noise, and lifetime opacity fades avoid popping. Window motes are warm; the central fill is quieter and cooler. There are no new lights, depth-texture requirements or per-frame C# scripts. Footstep dust, steam, lighting and UI remain independent. Matched On/Off room captures confirmed visible dust (233 changed pixels, 82 with stronger contrast) with no console errors.
