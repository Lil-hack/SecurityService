# Stylized office post-processing

- `Assets/Settings/MainMenuPostProcessing.asset`: ACES, HDR grading, warm highlights/cool shadows, bloom 0.48, contrast 13, saturation 10, a soft vignette, very subtle grain/chromatic aberration/lens curvature. Gaussian depth of field softens only the distant background (10–22 m). The foreground character and overlay UI stay clear.
- `Assets/Settings/ExecutiveOfficePostProcessing.asset`: clean cartoon palette: Neutral tonemapping, exposure +0.4, contrast +12, saturation +18, bloom 0.30 and vignette 0.09. Neutral white balance and gamma +0.06 brighten the midtones while preserving the black point. Film grain, chromatic aberration, split toning, lens distortion, depth of field and motion blur are explicitly off. The colorful low-poly geometry stays crisp.

Both scenes use their own profile on the existing global `Atmosphere · evening studio` Volume. The scene lighting, furniture, gameplay and AI evaluation are preserved. Main/game cameras have post-processing, HDR, dithering and high-quality SMAA enabled. Only the menu camera explicitly requests a depth texture. Desktop URP assets use HDR color grading; the previous render scale is preserved.

Profiles are serialized scene dependencies, so URP's build-time scan can retain the bloom, lens, chromatic aberration and depth-of-field shader variants that are actually used. Runtime UI remains an overlay and does not inherit the lens blur or color grade.

Use **Tools / Office / Apply Stylized Post Processing** to reapply both presets, or edit the two assets in the Inspector. **Capture Stylized Post Processing** saves room previews under `.local/PostProcessingQA`. The apply command saves dirty scenes before switching and returns to the original scene. **Tools / Office / Apply Cartoon Executive Office** updates only ExecutiveOffice and leaves the menu profile unchanged.

Play-mode visual checks covered the menu UI, the boss room and kitchen, the MainMenu → ExecutiveOffice transition, and confirmed that gameplay's active volume stack has DOF Off and motion blur intensity 0. No console errors occurred during those checks.
