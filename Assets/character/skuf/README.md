# Skuf player

Original model: skuf.fbx. The four JPEG textures were embedded in this FBX.
Tools/extract_skuf_textures.py extracts their original bytes into Textures/.

Skuf_URP.mat uses BaseColor (sRGB), Normal_Bake (normal map), and
Skuf_MetallicSmoothness.png (linear: metallic R, smoothness A = 1 - roughness).
The original Metallic and Roughness JPEGs are retained.

Skuf_Textured.prefab contains a Humanoid Animator with the existing player
locomotion controller: Idle, Walk, Run, Jump, Fall. Root motion is disabled.
The office CharacterController and third-person camera provide movement.

Setup menu: Tools > Office > Use Textured Skuf As Player.
The scene instance is normalized to a height of 1.72 m and placed on the floor.
WASD: move; Shift: run; Space: jump; mouse: orbit camera.
