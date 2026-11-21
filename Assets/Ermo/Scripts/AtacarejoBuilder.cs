using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Turns one sector plan (AtacarejoLayout) into objects: floor and ceiling
    // first so the sector is walkable at once, then one piece per step.
    // Fixtures get their lights here; the cold room interior, the meat locker
    // and door lintels come from AtacarejoDressing.
    public sealed class AtacarejoBuilder
    {
        // Light levels are tuned against the Dreamcore auto exposure, which
        // can only lift a frame by about 2.5 EV. Below this the market reads black.
        static readonly Color PathColor = new Color(.80f, .93f, .68f);
        public static readonly Color ColdColor = new Color(.52f, .74f, 1f);
        static readonly Color ExitColor = new Color(1f, .93f, .80f);
        static readonly Color FaintColor = new Color(.95f, .62f, .38f);
        static readonly Color TubeGlow = new Color(.55f, .72f, .48f);
        static readonly Color ColdGlow = new Color(.30f, .52f, .95f);
        static readonly Color ExitGlow = new Color(.95f, .90f, .75f);
        static MaterialPropertyBlock block;

        readonly Transform root;
        readonly AtacarejoLayout layout;
        readonly AtacarejoKit kit;
        readonly Vector3 origin;

        public AtacarejoBuilder(Transform root, AtacarejoLayout layout, AtacarejoKit kit)
        {
            this.root = root;
            this.layout = layout;
            this.kit = kit;
            origin = AtacarejoLayout.SectorOrigin(layout.Sector);
        }

        public IEnumerable<int> BuildSteps()
        {
            foreach (var item in layout.Items)
            {
                if (item.Kind != AtacarejoLayout.Kind.Piso && item.Kind != AtacarejoLayout.Kind.Forro) continue;
                Spawn(item);
            }
            root.gameObject.SetActive(true);
            yield return 1;
            foreach (var item in layout.Items)
            {
                if (item.Kind == AtacarejoLayout.Kind.Piso || item.Kind == AtacarejoLayout.Kind.Forro) continue;
                Spawn(item);
                yield return 0;
            }
            if (layout.Sector == AtacarejoLayout.ColdSector)
            {
                var dressing = new AtacarejoDressing(root, origin, kit);
                dressing.BuildColdRoom();
                yield return 0;
                dressing.BuildLocker();
                yield return 0;
            }
        }

        void Spawn(AtacarejoLayout.Item item)
        {
            var prefab = kit != null ? kit.Prefab(item.Kind, item.Variant) : null;
            if (prefab == null) return;
            var go = Object.Instantiate(prefab, root);
            go.transform.localPosition = item.World - origin;
            go.transform.localRotation = Quaternion.Euler(0f, item.Yaw, 0f);
            go.transform.localScale = Vector3.one;
            switch (item.Kind)
            {
                case AtacarejoLayout.Kind.Luminaria:
                    Lamp(go, (AtacarejoLayout.Lamp)item.Variant);
                    break;
                case AtacarejoLayout.Kind.LuminariaMorta:
                    if (item.Variant == 1) Faint(go);
                    break;
                case AtacarejoLayout.Kind.PortaSaida:
                    go.AddComponent<AtacarejoExitDoor>().Destination = AtacarejoExitDoor.Leads.BackToVestibule;
                    ExitFocus(go);
                    new AtacarejoDressing(root, origin, kit).Lintel(item.World, true);
                    break;
                case AtacarejoLayout.Kind.PortaCamara:
                    go.AddComponent<ColdDoor>();
                    break;
                case AtacarejoLayout.Kind.PortaFrigorifico:
                    go.AddComponent<ColdDoor>().Noun = "o frigorífico";
                    break;
                case AtacarejoLayout.Kind.PortaFundo:
                    // The exit leaf is 1.4 m; this gap is 1.6 m.
                    go.transform.localScale = new Vector3(1f, 1f, 1.6f / 1.4f);
                    go.AddComponent<AtacarejoExitDoor>().Destination = AtacarejoExitDoor.Leads.NextPhase;
                    break;
            }
        }

        static Light AddLight(GameObject fixture, string name, Color color, float intensity, float range)
        {
            var lamp = new GameObject(name);
            lamp.transform.SetParent(fixture.transform, false);
            // Hung below the fixture so the ceiling does not burn white above it.
            lamp.transform.localPosition = new Vector3(0f, -.95f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        static void Glow(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", color);
            renderer.SetPropertyBlock(block);
        }

        static void Lamp(GameObject fixture, AtacarejoLayout.Lamp kind)
        {
            var tube = fixture.GetComponentInChildren<Renderer>();
            switch (kind)
            {
                case AtacarejoLayout.Lamp.Cold:
                {
                    var light = AddLight(fixture, "Fluorescente fria", ColdColor, 8.5f, 11f);
                    Glow(tube, ColdGlow);
                    AtacarejoLights.Register(light, light.intensity, tube, ColdGlow, AtacarejoLights.Flicker.Gentle);
                    break;
                }
                case AtacarejoLayout.Lamp.Exit:
                {
                    AddLight(fixture, "Fluorescente da saída", ExitColor, 5.5f, 10f);
                    Glow(tube, ExitGlow);
                    break;
                }
                case AtacarejoLayout.Lamp.Sick:
                {
                    var light = AddLight(fixture, "Fluorescente doente", PathColor, 9f, 11.5f);
                    var buzz = AtacarejoAudio.Loop3D(light.gameObject, AtacarejoSounds.Buzz, .3f, 1f, 13f);
                    AtacarejoLights.Register(light, light.intensity, tube, TubeGlow, AtacarejoLights.Flicker.Sick, buzz);
                    break;
                }
                default:
                {
                    var light = AddLight(fixture, "Fluorescente", PathColor, 9f, 11.5f);
                    AtacarejoLights.Register(light, light.intensity, tube, TubeGlow, AtacarejoLights.Flicker.Gentle);
                    break;
                }
            }
        }

        static void Faint(GameObject fixture)
        {
            var light = AddLight(fixture, "Fluorescente quase morta", FaintColor, 1.6f, 7f);
            AtacarejoLights.Register(light, light.intensity, null, Color.black, AtacarejoLights.Flicker.Faint);
        }

        static void ExitFocus(GameObject door)
        {
            // A ceiling spot in front of the door, aimed at the leaf. In door
            // space +X faces the corridor the player arrives from.
            var lamp = new GameObject("Foco da saída");
            lamp.transform.SetParent(door.transform, false);
            lamp.transform.localPosition = new Vector3(2.2f, 4.9f, 0f);
            lamp.transform.LookAt(door.transform.TransformPoint(new Vector3(0f, 1.1f, 0f)));
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = ExitColor;
            light.intensity = 5f;
            light.range = 8f;
            light.spotAngle = 62f;
            light.innerSpotAngle = 30f;
            light.shadows = LightShadows.None;
        }
    }
}
