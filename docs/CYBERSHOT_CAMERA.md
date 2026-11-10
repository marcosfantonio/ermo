# Cyber-shot digital camera (Unity / URP)

## Configuration and presets

The game always clones **`Assets/Ermo/Resources/Ermo/Camera/ERMO Dreamcore.asset`**, regardless of the legacy World Settings profile selector. Its Inspector controls the image and handheld parameters. Fine and Standard remain archived authoring references, not gameplay choices:

| Profile | Capture reference | Processing |
| --- | --- | --- |
| Cyber-shot Fine | 640×480, 4:3, 30 fps | Moderate compression, restrained CCD noise and optics |
| Cyber-shot Standard (archive) | 640×480, 4:3, 16.6 fps reference | Stronger quantization and temporal detail loss; not selected by the game |
| ERMO Dreamcore (only gameplay profile) | Fine-based, mandatory 30 fps capture | More saturated blues/greens, stronger digital limitations and exposure response; all handheld layers active, idle gain 1.05× |

The options menu selects 320×240 / 640×480 / 800×600 / native, bilinear/nearest upscale and 4:3/widescreen. Capture cadence is always **30 fps**; it does **not** change `Application.targetFrameRate`, physics, player movement, audio or UI updates. There is no preset selector, capture-cadence toggle, digital-processing toggle, reduced-motion option or fog toggle. Native uses the window's height; 4:3 still preserves the corresponding viewport unless widescreen is selected. Distance fog is always selected when starting/restarting a world.

The Dreamcore profile is cloned for the session. Runtime menu changes do not dirty the source asset. In Play Mode, inspect `Walker → View orientation → Handheld motion` and the camera's `CyberShotCamera`; `PixelDisplay` also exposes its runtime world settings. Image, automatic-control and handheld parameters are on the session's profile. Capture size/aspect/upscale can be changed in the menu or runtime world settings. Initial capture settings apply when starting a session; gameplay cadence remains fixed at 30 fps.

**F3/F4 are unbound**, and all comparison/toggle hints have been removed from the HUD. Digital processing and physical handheld stay active. Internal diagnostic/compatibility APIs are not exposed as player options. No previous-engine names appear in the game interface; historical serialized identifiers are kept internally to avoid breaking assets and saved terrain settings.

## Inspection and integration

- Unity **6000.6.8f1**, **URP 17.6**, linear color, PC quality pipeline. No package installation.
- `ErmoBootstrap` already creates the player, offscreen Eye camera and Display 1 output. It now clones/applies the selected profile before starting the existing world.
- `FirstPersonWalker` still owns mouse yaw/pitch, acceleration, `CharacterController.Move`, gravity, running and wake animation. Space now triggers a grounded jump (~0.85 m, adjustable on the controller), with no double jump or menu jump. Horizontal speeds and gravity remain unchanged; the same capsule handles ceiling collisions and landing. Map generation, prop placement and persistence are unaffected.
- Existing walking sway/bob was **moved**, not layered with a second bob. The obsolete `Walk Sway Frequency`, `Run Sway Frequency` and `Sway Smoothing` world fields remain serialized for compatibility; the profile's handheld parameters and shared footstep stride lengths now control this motion.
- Hierarchy: `Walker (mouse yaw) → View orientation (mouse pitch / wake position) → Handheld motion (secondary offsets) → Eye`. Two pivots suffice; there is no redundant body-motion pivot. The mouse orientation has no smoothing or delayed input.
- `Meadow.shader` gains only URP's existing `MotionVectors` pass for moving renderers. It does not change terrain topology, road/house geometry, textures or lighting. URP/Lit/Unlit materials already provide motion-vector passes.

## Processing order

1. The scene renders directly at the **capture size** into a linear HDR half-float RenderTexture, retaining illumination above white **until sensor exposure/clipping**. No cinematic HDR tone mapper is added.
2. URP's `MotionBlurMode.CameraAndObjects` uses camera/object motion vectors before sensor processing. The volume is isolated to the Eye camera on volume layer 31, so it does not inherit unrelated scene post effects or affect the output/UI camera.
3. `PixelLens`, pass 0: radial barrel optics, tiny chromatic offsets, center/peripheral optical response, restrained electronic luma sharpening, highlight spread, metered exposure/white balance, CCD clipping and independent luma/chroma noise. All at capture resolution.
4. Pass 1: 2×2 chroma averaging (4:2:0 approximation), 8×8 block-local DC/residual quantization, separate flat/detail quantization strengths, motion-dependent loss and restrained previous-sensor-frame residual retention. History is reset on resize, profile change or processing bypass.
5. Only the final RawImage is upscaled. The screen-overlay Canvas and existing IMGUI remain native-resolution and unfiltered. The output camera clears unused aspect-ratio regions to black.

No tracking, scanlines, tape dropouts, random exposure flicker or VHS processing is enabled by these profiles. Legacy shader branches remain separate compatibility comparisons, not part of this pipeline.

## Automatic controls

`CyberShotCamera` meters a **16×12 linear image** at a configurable interval (default 0.12 s). A center-weighted geometric luminance mean drives exposure; highlights/sky still influence it. `AsyncGPUReadback` avoids a per-frame GPU/CPU stall. Platforms without async readback use a small synchronous read at the metering rate. There is no random exposure modulation.

The exposure target is `log2(middleGray / measuredLuminance) + compensation`, bounded by EV limits and exponentially adapted. Bright/dark transition times are separate (Dreamcore 0.65 / 1.05 s). Clipping is adjustable independently of adaptation.

Auto White Balance uses bounded gray-world RGB gains, applied weakly and adapted slowly. It is deliberately not a full neutralization of an all-green/all-blue frame. Disable AWB independently if desired.

Autofocus is **off by default**. When enabled, a rate-limited center raycast updates focus distance, with smoothed adaptation and a tightly bounded transient blur based on diopter mismatch. It is an artistic focus-acquisition approximation, not a full depth-aware lens/DoF model. Stable focus does not continuously blur the landscape.

## Physical handheld motion

`HandheldCameraMotion` writes additive, absolute local offsets; it never integrates them into the mouse/base rotation. It contains:

- Breathing at ~0.22 Hz / ±4 mm with slow frequency/amplitude variation. Dreamcore applies an additional 1.05× idle gain to breathing, posture, tremor and occasional corrections; this fades to 1× while walking without increasing walking bob or mouse inertia.
- Independent low-frequency Perlin posture motion (~3 mm / 0.12°).
- Smoothed continuous microtremor (~0.025°), not per-frame random rotations.
- Smooth, bounded idle posture corrections at irregular 12–25 s intervals.
- Distance-driven walking bob (~12 mm / 0.30° base roll), using the same 1.7 m walking / 2.15 m running step distances as `StepCadence`. Dreamcore adds independent walking/running gains of 1.25× / 1.40× over the previous amplitudes; idle gain remains 1.05×. A blocked/airborne/paused player does not generate walking bob; its amplitude damps down after stopping.
- Bounded mouse-rate inertia on the secondary pivot only. The primary look direction remains immediate.

Each layer has its own Inspector intensity. No terrain, house, collision or basic movement code is driven by these offsets.

## Deliberate approximations / limits

- This is **not** an MPEG1 encoder, a calibrated Sony sensor model or a reconstruction of the exact proprietary VX processing. Quantization is block-local DC/residual processing, not a full DCT/entropy codec.
- Temporal compression uses image differences and a previous sensor frame, with small-residual/cut rejection. It does **not** perform codec motion estimation, GOP/I/P-frame reconstruction or motion-compensated prediction. History is not used to fake motion blur or accumulate long trails.
- Motion blur is URP's vector-based filter, not optical flow. The 1/60 s shutter/frame ratio scales intensity; the profile's `Maximum Blur` is forwarded to URP's clamp. **URP 17.6's built-in CameraAndObjects shader does not enforce that clamp on every object-vector sample**, so it is not a strict per-object smear limit; very fast animated props can exceed it. No second blur is stacked to hide this limitation.
- Exposure metering is a sparse image estimate, not a full-resolution histogram. AWB is intentionally conservative. Noise is a CCD-inspired digital approximation, not film grain.
- The isolated volume uses layer 31. If adding another camera-specific volume system, reserve a different layer/mask rather than sharing this one.

## Validation

**Ermo → Validate Cyber-shot Camera** checks all three profiles, deterministic exposure/AWB adaptation, a simulated idle minute, bounded walking/inertia, pause, reduced motion and both shader passes. **Validate Generation** includes these checks and the existing world/audio regressions.

The separate-copy Play Mode smoke test additionally checks fixed Dreamcore/digital/fog settings, exact capture resolutions (640×480 default and 800×600 selectable, even when the window is smaller), aspect/filtering, motion-vector configuration, successful GPU metering, capture-only 30 fps cadence, actual dark/bright rendered test-card transitions, and grounded jump/landing with no menu or double jump. Temporary cards exist only in the batch test, never in the authored scene. Captures are saved in `Logs/` in the validation copy.
