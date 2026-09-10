using System.Collections;
using UnityEngine;

namespace SurakshaAR.AR
{
    /// <summary>
    /// PHASE 2 — Realistic AR Fire Effect.
    /// 
    /// Features:
    ///   1. Multi-layered Particle Systems (Core flame, turbulent tongues, billowing smoke, flying embers).
    ///   2. Dynamic flickering warm point light for realistic environmental illumination.
    ///   3. Procedural particle textures and gradient materials (no missing pink shaders).
    ///   4. Real-time worker proximity and spatial tracking ("Investigate the fire").
    /// </summary>
    public class ARRealisticFireEffect : MonoBehaviour
    {
        private GameObject coreFlamesGO;
        private GameObject turbulentFlamesGO;
        private GameObject smokeGO;
        private GameObject embersGO;
        private GameObject fireLightGO;
        private Light fireLight;

        private ParticleSystem corePS;
        private ParticleSystem turbulentPS;
        private ParticleSystem smokePS;
        private ParticleSystem embersPS;

        // Light flicker parameters
        private float baseLightIntensity = 3.5f;
        private float baseLightRange = 4.0f;
        private float flickerSpeed = 18f;
        private float noiseOffset;

        private void Awake()
        {
            noiseOffset = Random.Range(0f, 100f);
            BuildRealisticFireSystem();
        }

        private void Update()
        {
            // Procedural realistic fire flicker
            if (fireLight != null)
            {
                float n1 = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
                float n2 = Mathf.PerlinNoise(0f, Time.time * (flickerSpeed * 1.5f) + noiseOffset);
                float combinedNoise = (n1 * 0.7f) + (n2 * 0.3f);

                fireLight.intensity = baseLightIntensity * Mathf.Lerp(0.75f, 1.35f, combinedNoise);
                fireLight.range = baseLightRange * Mathf.Lerp(0.85f, 1.15f, n1);
            }
        }

        private void BuildRealisticFireSystem()
        {
            // ── 1. Materials ──────────────────────────────────────────────────
            Material additiveMat = CreateAdditiveParticleMaterial();
            Material blendedMat = CreateAlphaBlendedParticleMaterial();

            // ── 2. Core Inner Flames (Bright Yellow / White Incandescent) ─────
            coreFlamesGO = new GameObject("CoreFlames");
            coreFlamesGO.transform.SetParent(transform, false);
            coreFlamesGO.transform.localPosition = Vector3.zero;

            corePS = coreFlamesGO.AddComponent<ParticleSystem>();
            var coreRenderer = coreFlamesGO.GetComponent<ParticleSystemRenderer>();
            coreRenderer.material = additiveMat;
            coreRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var coreMain = corePS.main;
            coreMain.loop = true;
            coreMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            coreMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            coreMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            coreMain.startColor = new Color(1f, 0.95f, 0.7f, 0.9f); // Bright golden white
            coreMain.simulationSpace = ParticleSystemSimulationSpace.World;
            coreMain.maxParticles = 150;

            var coreEmission = corePS.emission;
            coreEmission.rateOverTime = 45f;

            var coreShape = corePS.shape;
            coreShape.shapeType = ParticleSystemShapeType.Cone;
            coreShape.angle = 8f;
            coreShape.radius = 0.08f;
            coreShape.position = Vector3.zero;
            coreShape.rotation = new Vector3(-90f, 0f, 0f); // Upward

            var coreColor = corePS.colorOverLifetime;
            coreColor.enabled = true;
            Gradient coreGrad = new Gradient();
            coreGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.65f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            coreColor.color = coreGrad;

            var coreSize = corePS.sizeOverLifetime;
            coreSize.enabled = true;
            AnimationCurve coreCurve = new AnimationCurve();
            coreCurve.AddKey(0f, 0.4f);
            coreCurve.AddKey(0.35f, 1.0f);
            coreCurve.AddKey(1f, 0.1f);
            coreSize.size = new ParticleSystem.MinMaxCurve(1f, coreCurve);

            // ── 3. Turbulent Outer Flames (Vivid Orange / Deep Crimson) ───────
            turbulentFlamesGO = new GameObject("TurbulentFlames");
            turbulentFlamesGO.transform.SetParent(transform, false);
            turbulentFlamesGO.transform.localPosition = Vector3.zero;

            turbulentPS = turbulentFlamesGO.AddComponent<ParticleSystem>();
            var turbRenderer = turbulentFlamesGO.GetComponent<ParticleSystemRenderer>();
            turbRenderer.material = additiveMat;
            turbRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var turbMain = turbulentPS.main;
            turbMain.loop = true;
            turbMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            turbMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            turbMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            turbMain.startColor = new Color(1f, 0.5f, 0.05f, 0.75f);
            turbMain.simulationSpace = ParticleSystemSimulationSpace.World;
            turbMain.maxParticles = 200;

            var turbEmission = turbulentPS.emission;
            turbEmission.rateOverTime = 55f;

            var turbShape = turbulentPS.shape;
            turbShape.shapeType = ParticleSystemShapeType.Cone;
            turbShape.angle = 14f;
            turbShape.radius = 0.18f;
            turbShape.rotation = new Vector3(-90f, 0f, 0f);

            var turbNoise = turbulentPS.noise;
            turbNoise.enabled = true;
            turbNoise.strength = 0.45f;
            turbNoise.frequency = 1.2f;
            turbNoise.scrollSpeed = 1.8f;
            turbNoise.damping = true;

            var turbColor = turbulentPS.colorOverLifetime;
            turbColor.enabled = true;
            Gradient turbGrad = new Gradient();
            turbGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.7f, 0.1f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.02f), 0.45f),
                    new GradientColorKey(new Color(0.65f, 0.08f, 0.02f), 0.85f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            turbColor.color = turbGrad;

            var turbSize = turbulentPS.sizeOverLifetime;
            turbSize.enabled = true;
            AnimationCurve turbCurve = new AnimationCurve();
            turbCurve.AddKey(0f, 0.5f);
            turbCurve.AddKey(0.4f, 1.1f);
            turbCurve.AddKey(1f, 0.15f);
            turbSize.size = new ParticleSystem.MinMaxCurve(1f, turbCurve);

            // ── 4. Billowing Volumetric Smoke ─────────────────────────────────
            smokeGO = new GameObject("BillowingSmoke");
            smokeGO.transform.SetParent(transform, false);
            smokeGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            smokePS = smokeGO.AddComponent<ParticleSystem>();
            var smokeRenderer = smokeGO.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = blendedMat;
            smokeRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var smokeMain = smokePS.main;
            smokeMain.loop = true;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            smokeMain.startColor = new Color(0.18f, 0.17f, 0.16f, 0.45f); // Charcoal dark smoke
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
            smokeMain.maxParticles = 80;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 18f;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 18f;
            smokeShape.radius = 0.2f;
            smokeShape.rotation = new Vector3(-90f, 0f, 0f);

            var smokeNoise = smokePS.noise;
            smokeNoise.enabled = true;
            smokeNoise.strength = 0.35f;
            smokeNoise.frequency = 0.8f;
            smokeNoise.scrollSpeed = 0.6f;

            var smokeColor = smokePS.colorOverLifetime;
            smokeColor.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.25f, 0.22f, 0.2f), 0.0f),
                    new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 0.5f),
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.4f, 0.2f),
                    new GradientAlphaKey(0.25f, 0.6f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            smokeColor.color = smokeGrad;

            var smokeSize = smokePS.sizeOverLifetime;
            smokeSize.enabled = true;
            AnimationCurve smokeCurve = new AnimationCurve();
            smokeCurve.AddKey(0f, 0.4f);
            smokeCurve.AddKey(0.5f, 1.8f);
            smokeCurve.AddKey(1f, 3.2f);
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, smokeCurve);

            // ── 5. Flying Sparks & Embers ──────────────────────────────────────
            embersGO = new GameObject("FlyingEmbers");
            embersGO.transform.SetParent(transform, false);
            embersGO.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            embersPS = embersGO.AddComponent<ParticleSystem>();
            var embersRenderer = embersGO.GetComponent<ParticleSystemRenderer>();
            embersRenderer.material = additiveMat;
            embersRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var embersMain = embersPS.main;
            embersMain.loop = true;
            embersMain.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            embersMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            embersMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            embersMain.startColor = new Color(1f, 0.85f, 0.2f, 1f); // Bright yellow spark
            embersMain.simulationSpace = ParticleSystemSimulationSpace.World;
            embersMain.maxParticles = 60;

            var embersEmission = embersPS.emission;
            embersEmission.rateOverTime = 22f;

            var embersShape = embersPS.shape;
            embersShape.shapeType = ParticleSystemShapeType.Cone;
            embersShape.angle = 22f;
            embersShape.radius = 0.15f;
            embersShape.rotation = new Vector3(-90f, 0f, 0f);

            var embersNoise = embersPS.noise;
            embersNoise.enabled = true;
            embersNoise.strength = 0.6f;
            embersNoise.frequency = 1.5f;
            embersNoise.scrollSpeed = 2.2f;

            var embersColor = embersPS.colorOverLifetime;
            embersColor.enabled = true;
            Gradient embersGrad = new Gradient();
            embersGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.05f), 0.6f),
                    new GradientColorKey(new Color(0.8f, 0.15f, 0.0f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.7f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            embersColor.color = embersGrad;

            // ── 6. Dynamic Flickering Point Light ──────────────────────────────
            fireLightGO = new GameObject("FireLight");
            fireLightGO.transform.SetParent(transform, false);
            fireLightGO.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            fireLight = fireLightGO.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.12f); // Warm flame orange
            fireLight.intensity = baseLightIntensity;
            fireLight.range = baseLightRange;
            fireLight.shadows = LightShadows.None;

            Debug.Log("[ARRealisticFireEffect] Multi-layered realistic AR fire system built successfully.");
        }

        // ── Procedural Particle Texture & Material Generators ──────────────────

        private static Material CreateAdditiveParticleMaterial()
        {
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.mainTexture = GenerateSoftParticleTexture();
            return mat;
        }

        private static Material CreateAlphaBlendedParticleMaterial()
        {
            Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.mainTexture = GenerateSoftParticleTexture();
            return mat;
        }

        private static Texture2D GenerateSoftParticleTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.5f;

            Color[] cols = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = Mathf.Clamp01(dist / maxRadius);

                    // Smooth gaussian-like falloff
                    float alpha = Mathf.Exp(-3.5f * normDist * normDist);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }
    }
}
