# Unity & C# Optimization Rules

## C#: Allocations and the Garbage Collector

The main goal is zero heap allocations per frame in hot code: every GC spike shows up as a hitch.

1. Don't create `new` objects, arrays or lists in `Update`, `FixedUpdate`, `LateUpdate`. Allocate up front and reuse (`list.Clear()` instead of a new list).
2. Don't concatenate strings in loops or every frame. Use `StringBuilder`, cache finished strings, and set UI numbers (`Label.text`) only when the value changes.
3. Avoid LINQ in hot code: it allocates iterators and closures. It's fine during initialization.
4. Don't capture outer variables in lambdas in frequently called code: a closure is an allocation. Cache delegates in fields.
5. Avoid boxing: don't pass a `struct` as `object` or an interface, and don't use an `enum` as a `Dictionary` key without your own `IEqualityComparer`.
6. Use pools: `UnityEngine.Pool.ObjectPool<T>`, `ListPool<T>`, `ArrayPool<T>.Shared` for temporary buffers.
7. Use `struct` for small immutable data and `class` for large or shared data. Pass large structs via `in` / `ref`.
8. Use `Span<T>` and `stackalloc` for short-lived temporary buffers without heap allocation.
9. Pre-size collections: set `capacity` in the `List`/`Dictionary` constructor when the size is known.
10. `foreach` over `List<T>` and arrays is safe; over an interface (`IEnumerable<T>`) it may allocate.
11. Enable Incremental GC (Player Settings). It spreads collection across frames, but it doesn't replace avoiding allocations.

## Unity Scripts: Update, GetComponent, Caching

Fewer calls per frame and no scene searches during gameplay.

1. Cache references in `Awake`/`Start`: `GetComponent`, `transform`, `Camera.main` (in older versions it searches by tag every time).
2. Never call `Find`, `FindObjectOfType`, `FindObjectsByType`, `GameObject.FindWithTag` in `Update`. Pass references via the Inspector or by registering with a manager.
3. Use `TryGetComponent` instead of `GetComponent` plus a `null` check — no allocations in the Editor.
4. Remove empty `Update`, `FixedUpdate`, `OnGUI` methods: Unity calls them even when the body is empty.
5. With hundreds of objects, replace individual `Update` methods with one manager that updates a list in a loop — fewer native → managed transitions.
6. Don't run everything every frame: spread AI logic, target searches and distance checks across frames or on a timer.
7. Compare distances with `sqrMagnitude` instead of `Vector3.Distance` (no square root).
8. Cache hashes: `Animator.StringToHash`, `Shader.PropertyToID` — instead of strings on every call.
9. Use `CompareTag("Enemy")` instead of `tag == "Enemy"` — no string allocation.
10. Set position and rotation in one call with `SetPositionAndRotation` / `SetLocalPositionAndRotation`; don't touch `transform` unnecessarily.
11. Keep the hierarchy flat: deeply nested objects with frequent `transform` changes are more expensive.
12. Don't overuse `Instantiate`/`Destroy` in the game loop — use object pools.
13. For async code use `Awaitable` (Unity 6) or UniTask; with coroutines, cache `WaitForSeconds` in a field.
14. Move heavy array computations to the Job System + Burst; for very large numbers of entities, consider DOTS/ECS.
15. Strip `Debug.Log` from release builds (`[Conditional]` attribute or your own logger wrapper) — it's slow and allocates.

## Physics

Physics cost grows with the number of pairs to check — reduce them.

1. Configure the layer collision matrix (Project Settings → Physics): disable layer pairs that should never collide.
2. Use primitive colliders (Box, Sphere, Capsule) instead of `MeshCollider`. If a `MeshCollider` is unavoidable, use a simplified mesh and `Convex`.
3. Don't move static colliders via `transform` — a moving object should have a `Rigidbody` (`isKinematic` if needed).
4. Move a `Rigidbody` with `MovePosition`/`MoveRotation` or forces, not via `transform`.
5. Tune `Fixed Timestep` (default 0.02 s = 50 Hz): don't set it higher than the game needs; limit `Maximum Allowed Timestep`.
6. Use non-alloc queries: `Physics.RaycastNonAlloc`, `OverlapSphereNonAlloc` with a reusable buffer.
7. Always pass `layerMask` and `maxDistance` to raycasts.
8. For bulk raycasts, use `RaycastCommand` with the Job System.
9. Disable `Auto Sync Transforms` if you don't read physics right after changing `transform`.
10. Enable `Reuse Collision Callbacks` so `OnCollision*` doesn't allocate a new `Collision` each time.
11. Keep non-physics logic in `Update`, not `FixedUpdate`.

## Rendering and Graphics

First find the bottleneck — CPU (draw calls, SetPass) or GPU (fill rate, shaders, shadows) — and only then optimize.

1. Use URP for mobile and mid-range platforms; HDRP only when targeting high-end hardware.
2. Enable the SRP Batcher and keep shader variants to a minimum — it groups objects that share a shader.
3. Use GPU Instancing for many identical objects; in Unity 6, use the GPU Resident Drawer for large scenes.
4. Mark non-moving objects as Static for static batching and baking.
5. Set up LOD Groups for detailed models and configure draw distance (Far Clip Plane, per-layer culling distances).
6. Use Occlusion Culling in enclosed scenes (interiors, cities).
7. Bake lighting (Lightmaps, Light Probes) instead of using many real-time lights. Keep real-time shadows to a minimum.
8. Limit shadow distance and cascades (Shadow Distance, Shadow Cascades) — shadows are often the most expensive part of the frame.
9. Avoid overdraw: fewer large transparent objects and full-screen particles.
10. Combine textures into atlases so objects can share a material.
11. Don't create material copies via `renderer.material` — use `sharedMaterial` or `MaterialPropertyBlock`.
12. Keep post-processing minimal on mobile: Bloom, SSAO and Depth of Field are expensive.
13. On mobile, lower Render Scale and use simple shaders (Simple Lit / Unlit).
14. Remove unused shader variants (Shader Stripping) — it reduces build size and load times.

## UI (UI Toolkit)

UI Toolkit re-resolves styles and recomputes layout only for elements that changed (and what depends on them), so the cost is in how many elements you touch and whether a change triggers layout.

1. Keep the tree small: create elements when they are first shown and remove them from the hierarchy when hidden; pool elements that spawn often (`ElementPool<T>`). Don't keep hidden windows or overlays in the tree.
2. Set `picking-mode="Ignore"` on everything that isn't clickable (labels, decor, containers) — picking walks the tree.
3. Animate `translate`, `scale`, `rotate` and `opacity`: they don't trigger layout. Tween layout properties (`left`/`top`/`bottom`/`width`) only on a few absolutely positioned elements, never on many elements at once.
4. Put shared styling into USS classes and toggle classes; avoid writing many inline styles from code each frame.
5. Don't update `Label.text` every frame if the value hasn't changed.
6. For long lists, use `ListView` (virtualized) or pooled rows instead of hundreds of elements in a `ScrollView`.
7. Query elements once (`Q<T>(name)`) and cache them; never query per frame.
8. Use one `PanelSettings` for the whole game; a second panel means a second layout and render pass.
9. Don't animate UI with an `Animator`; use DOTween on element styles (`VisualElementTweens`) and kill tweens when the view is unbound or returned to a pool.
10. **Sprite atlases:** UI sprites drawn at the same time on one screen go into one Sprite Atlas (V2) in `Content/Visual/Atlases/` (one atlas for the shared kit and icons, one per screen for its own sprites). UI Toolkit batches elements whose images share an atlas texture. Don't put sprites of different screens or rarely shown sprites (the album's item sprites) into a screen's atlas: the whole atlas texture is loaded with it.

## Assets, Memory and Loading

Textures and audio usually take most of the memory — configure import settings before the asset count grows.

1. Set texture Max Size to match actual on-screen size; don't keep 4K where 512 is enough.
2. Use platform-appropriate compression: ASTC for mobile, BC7/DXT for PC and consoles.
3. Disable `Read/Write Enabled` on textures and meshes you don't read from code — otherwise the in-memory copy is doubled.
4. Generate Mip Maps for 3D textures and disable them for UI.
5. Audio: long music — `Streaming`; short frequent sounds — `Decompress On Load`; medium clips — `Compressed In Memory`. Force mono where stereo isn't needed.
6. Models: disable what you don't need on import (Read/Write, Blend Shapes, extra UVs, Normals) and enable Mesh Compression.
7. Animations: enable compression (`Optimal`) and `Optimize Game Objects` on rigged models.
8. Don't use the `Resources` folder — everything in it goes into the build and is indexed at startup. Use Addressables.
9. Load scenes and assets asynchronously (`LoadSceneAsync`, Addressables) and unload what you don't need (`Addressables.Release`).
10. Check for leaks: the Memory Profiler shows which objects remain after a scene is unloaded.
11. Don't hold references to large assets in static fields and singletons unless necessary.

## Build and Project Settings

The right Player settings give a boost without changing code.

1. Use the IL2CPP backend for release — faster at runtime than Mono.
2. Enable Managed Stripping Level (Medium/High) and verify reflection still works (`link.xml`, `[Preserve]`).
3. Set the target frame rate explicitly: `Application.targetFrameRate` (60 or 30 on mobile) — saves battery and reduces heat.
4. Disable VSync where you control frame rate via `targetFrameRate` (mobile), and the other way around on PC.
5. Configure Quality Settings tiers (Low/Medium/High) and pick a tier per device.
6. Split code into Assembly Definitions — speeds up compilation and Editor iteration.
7. Enable Enter Play Mode Options (no domain reload) for faster Play mode entry — but reset static fields manually.
8. Remove unused packages and assets — smaller builds and faster imports.
9. Check the Build Report (or the Build Report Inspector package) to see what takes up space.

## Profiling and Process

The main rule: measure first, then optimize. Guesses about what's slow are usually wrong.

1. Profile on the target device in a Development Build, not in the Editor — the Editor skews the numbers.
2. Set a frame budget: 16.6 ms for 60 FPS, 33.3 ms for 30 FPS — and split it between systems.
3. Use the tools: Unity Profiler (CPU, GPU, memory), Frame Debugger (draw calls), Memory Profiler (leaks), Profile Analyzer (before/after comparison).
4. Enable Deep Profile only selectively — it slows the game down significantly and distorts the picture.
5. Mark up your own code with `ProfilerMarker` / `Profiler.BeginSample` so your systems show up in the profiler.
6. Watch the GC Alloc column in the CPU Profiler — the target in the game loop is 0 B per frame.
7. Use native platform profilers for the GPU: Xcode Instruments, Android GPU Inspector, RenderDoc, PIX.
8. Test on the weakest target device, not just a flagship.
9. Change one thing at a time and compare measurements before and after.
10. Build performance checks into your process: regular measurements on a reference scene to catch regressions immediately.
