using UnityEngine;

namespace Ermo
{
    // Every market sound that is not a recording. All synthesized here,
    // deterministic, no external audio. Clips are cached and rebuilt only if
    // Unity destroyed them (domain reload is off in this project).
    public static class AtacarejoSounds
    {
        const int Rate = 22050;
        const int TableSize = 4096;
        static float[] sine;
        static AudioClip hum, buzz, evaporator, muzak, chime, babble, drips;
        const int ShoeVariants = 4;
        static readonly AudioClip[] shoes = new AudioClip[ShoeVariants];

        public static AudioClip Hum => hum != null ? hum : hum = MakeHum();
        public static AudioClip Buzz => buzz != null ? buzz : buzz = MakeBuzz();
        public static AudioClip Evaporator => evaporator != null ? evaporator : evaporator = MakeEvaporator();
        public static AudioClip Muzak => muzak != null ? muzak : muzak = MakeMuzak();
        public static AudioClip Chime => chime != null ? chime : chime = MakeChime();
        public static AudioClip Babble => babble != null ? babble : babble = MakeBabble();
        // Any integer works: it picks one of the shoe-step variants.
        public static AudioClip ShoeStep(int pick)
        {
            int i = (pick % ShoeVariants + ShoeVariants) % ShoeVariants;
            return shoes[i] != null ? shoes[i] : shoes[i] = MakeShoeStep(i);
        }
        public static AudioClip Drips => drips != null ? drips : drips = MakeDrips();

        static float Sin(double phase)
        {
            if (sine == null)
            {
                sine = new float[TableSize];
                for (int i = 0; i < TableSize; i++) sine[i] = Mathf.Sin(i * Mathf.PI * 2f / TableSize);
            }
            double f = phase - System.Math.Floor(phase);
            return sine[(int)(f * TableSize) & (TableSize - 1)];
        }

        static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            foreach (var v in data) max = Mathf.Max(max, Mathf.Abs(v));
            if (max <= 0f) return;
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= gain;
        }

        // One-pole low-pass run twice around a loop, so the seam has no click.
        static void LoopLowPass(float[] data, float cutoff)
        {
            float a = Mathf.Exp(-2f * Mathf.PI * cutoff / Rate), y = 0f;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < data.Length; i++)
                {
                    y = (1f - a) * data[i] + a * y;
                    if (pass == 1) data[i] = y;
                }
        }

        // 60 Hz mains: the tubes hum at 120 Hz with a ragged edge.
        static AudioClip MakeHum()
        {
            int length = Rate * 2;
            var data = new float[length];
            var random = new System.Random(120);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)Rate;
                float swell = .8f + .2f * Sin(t * .5);
                float edge = Mathf.Sign(Sin(t * 120)) * .05f;
                data[i] = swell * (Sin(t * 120) * .5f + Sin(t * 240) * .22f + Sin(t * 360) * .1f + edge)
                    + (float)(random.NextDouble() - .5) * .03f;
            }
            LoopLowPass(data, 2600f);
            Normalize(data, .8f);
            return Clip("Atacarejo — hum fluorescente", data);
        }

        static AudioClip MakeBuzz()
        {
            int length = Rate;
            var data = new float[length];
            var random = new System.Random(13);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)Rate;
                float saw = (float)(2.0 * (t * 120 - System.Math.Floor(t * 120 + .5)));
                float crackle = random.NextDouble() < .004 ? (float)(random.NextDouble() - .5) * 2.2f : 0f;
                data[i] = saw * .5f + Sin(t * 240) * .2f + crackle;
            }
            LoopLowPass(data, 3800f);
            Normalize(data, .8f);
            return Clip("Atacarejo — reator queimando", data);
        }

        static AudioClip MakeEvaporator()
        {
            int length = Rate * 3;
            var data = new float[length];
            var random = new System.Random(7);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)Rate;
                data[i] = (float)(random.NextDouble() - .5) * .9f + Sin(t * 60) * .35f + Sin(t * 180) * .08f
                    + Sin(t * 23.333) * .05f;
            }
            LoopLowPass(data, 420f);
            Normalize(data, .8f);
            return Clip("Atacarejo — evaporador da câmara", data);
        }

        // Slow bossa from a speaker two aisles away: bass, electric piano,
        // a vibraphone line, tape wow. One chord is a little wrong.
        static AudioClip MakeMuzak()
        {
            const float bpm = 80f;
            float beat = 60f / bpm, bar = beat * 4f;
            const int bars = 8;
            int length = Mathf.RoundToInt(bar * bars * Rate);
            var data = new float[length];
            int[][] chords =
            {
                new[] { 41, 53, 57, 60, 64 },
                new[] { 46, 57, 62, 65, 69 },
                new[] { 45, 55, 60, 64, 67 },
                new[] { 38, 53, 57, 60, 63 }
            };
            for (int b = 0; b < bars; b++)
            {
                var chord = chords[(b / 2) % chords.Length];
                float start = b * bar;
                Note(data, start, beat * 1.4f, chord[0], .34f, 0);
                Note(data, start + beat * 2f, beat * 1.3f, chord[0] + 7, .26f, 0);
                float[] hits = b % 2 == 0 ? new[] { 0f, 1.5f, 3f } : new[] { .5f, 2f, 3.5f };
                foreach (var hit in hits)
                    for (int v = 1; v < chord.Length; v++)
                    {
                        // The wrong chord: its top note is a few cents flat.
                        float detune = b >= 6 && v == chord.Length - 1 ? -.32f : 0f;
                        Note(data, start + hit * beat, beat * .45f, chord[v] + detune, v == chord.Length - 1 ? .045f : .07f, 1);
                    }
            }
            float[,] melody =
            {
                { 0, 0, 72, 2 }, { 0, 2.5f, 69, 1 }, { 1, 1, 67, 2.5f }, { 2, 0, 70, 2 }, { 2, 2.5f, 69, 1 },
                { 3, 1, 65, 2.5f }, { 4, 0, 67, 2 }, { 4, 2.5f, 64, 1 }, { 5, 1, 60, 2.5f }, { 6, 0, 69, 1.5f },
                { 6, 2, 66, 2 }, { 7, 1, 62, 3 }
            };
            for (int i = 0; i < melody.GetLength(0); i++)
                Note(data, melody[i, 0] * bar + melody[i, 1] * beat, melody[i, 3] * beat, melody[i, 2], .13f, 2);
            LoopLowPass(data, 1700f);
            // Hall echo, around the loop twice.
            int delay = Mathf.RoundToInt(.31f * Rate);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < length; i++)
                    data[i] += data[(i - delay + length) % length] * (pass == 0 ? .28f : .12f);
            var random = new System.Random(84);
            for (int i = 0; i < length; i++) data[i] = (float)System.Math.Tanh(data[i] * 1.4f) + (float)(random.NextDouble() - .5) * .006f;
            Normalize(data, .85f);
            return Clip("Atacarejo — música ambiente", data);
        }

        // kind 0 bass, 1 electric piano, 2 vibraphone. Writes wrap around the
        // loop, so tails that cross the end continue at the start.
        static void Note(float[] data, float start, float duration, float midi, float amp, int kind)
        {
            float freq = Midi(midi);
            float tau = kind == 0 ? .75f : kind == 1 ? .38f : .95f;
            float total = Mathf.Min(duration + tau * 3f, 4f);
            int first = Mathf.RoundToInt(start * Rate), count = Mathf.RoundToInt(total * Rate);
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                double absolute = (first + i) / (double)Rate;
                float wow = 1f + .0035f * Sin(absolute * .33) + .0012f * Sin(absolute * 1.7);
                phase += freq * wow / Rate;
                float attack = Mathf.Clamp01(t / (kind == 2 ? .02f : .006f));
                float release = t > duration ? Mathf.Exp(-(t - duration) / (tau * .5f)) : 1f;
                float env = attack * Mathf.Exp(-t / tau) * release;
                float v;
                if (kind == 0) v = Sin(phase) + Sin(phase * 2) * .3f;
                else if (kind == 1) v = Sin(phase) + Sin(phase * 2) * .22f + Sin(phase * 3.5) * .06f * Mathf.Exp(-t * 9f);
                else v = (Sin(phase) + Sin(phase * 4) * .1f) * (.85f + .15f * Sin(t * 5.2));
                data[(first + i) % data.Length] += v * env * amp;
            }
        }

        // The supermarket call: three soft rising bells.
        static AudioClip MakeChime()
        {
            int length = Mathf.RoundToInt(2.4f * Rate);
            var data = new float[length];
            float[] notes = { 67, 72, 76 };
            for (int n = 0; n < notes.Length; n++)
            {
                float freq = Midi(notes[n]);
                int first = Mathf.RoundToInt(n * .3f * Rate);
                for (int i = first; i < length; i++)
                {
                    float t = (i - first) / (float)Rate;
                    float env = Mathf.Clamp01(t / .004f) * Mathf.Exp(-t / .7f);
                    data[i] += (Sin(t * freq) + Sin(t * freq * 2.76) * .18f * Mathf.Exp(-t * 6f)) * env * .3f;
                }
            }
            Normalize(data, .8f);
            return Clip("Atacarejo — chamada", data);
        }

        // Words you almost understand: a pulse voice through two moving
        // formants and a cheap horn speaker. No real speech.
        static AudioClip MakeBabble()
        {
            int length = Rate * 7;
            var data = new float[length];
            var random = new System.Random(31);
            float[,] vowels = { { 730, 1090 }, { 270, 2290 }, { 300, 870 }, { 530, 1840 }, { 570, 840 }, { 440, 1020 } };
            float y1a = 0, y2a = 0, y1b = 0, y2b = 0;
            double glottal = 0;
            int index = 0;
            while (index < length)
            {
                bool gap = random.NextDouble() < .22;
                int syllable = Mathf.RoundToInt((gap ? .1f + (float)random.NextDouble() * .12f : .13f + (float)random.NextDouble() * .12f) * Rate);
                int vowel = random.Next(vowels.GetLength(0));
                float f1 = vowels[vowel, 0], f2 = vowels[vowel, 1];
                float pitch = 118f + (float)random.NextDouble() * 22f;
                for (int i = 0; i < syllable && index < length; i++, index++)
                {
                    float u = i / (float)syllable;
                    float env = gap ? 0f : Mathf.Pow(Mathf.Sin(u * Mathf.PI), .6f);
                    glottal += pitch * (1f - .06f * u) / Rate;
                    float source = glottal - System.Math.Floor(glottal) < .08 ? 1f : 0f;
                    if (u < .12f) source += (float)(random.NextDouble() - .5) * .8f;
                    float a = Resonate(source, f1, 90f, ref y1a, ref y2a);
                    float b = Resonate(source, f2, 140f, ref y1b, ref y2b);
                    data[index] = (a + b * .6f) * env;
                }
            }
            float hp = 0f, prev = 0f, lp = 0f;
            float aHigh = Mathf.Exp(-2f * Mathf.PI * 380f / Rate), aLow = Mathf.Exp(-2f * Mathf.PI * 3000f / Rate);
            for (int i = 0; i < length; i++)
            {
                hp = aHigh * (hp + data[i] - prev); prev = data[i];
                lp = (1f - aLow) * hp + aLow * lp;
                data[i] = lp;
            }
            Normalize(data, 1f);
            for (int i = 0; i < length; i++) data[i] = (float)System.Math.Tanh(data[i] * 2.2f) * .8f;
            return Clip("Atacarejo — alto-falante", data);
        }

        static float Resonate(float x, float freq, float bandwidth, ref float y1, ref float y2)
        {
            float r = Mathf.Exp(-Mathf.PI * bandwidth / Rate);
            float a1 = 2f * r * Mathf.Cos(2f * Mathf.PI * freq / Rate), a2 = -r * r;
            float y = (1f - r) * x + a1 * y1 + a2 * y2;
            y2 = y1; y1 = y;
            return y;
        }

        // Meat locker: thick drops on a wet floor at uneven intervals, over a
        // low compressor rumble and a chain that creaks now and then. Loops
        // every 9 s, so the ear does not catch the repeat.
        static AudioClip MakeDrips()
        {
            int length = Rate * 9;
            var data = new float[length];
            var random = new System.Random(13);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)Rate;
                data[i] = (float)(random.NextDouble() - .5) * .05f + Sin(t * 50) * .07f + Sin(t * 100.4) * .03f;
            }
            LoopLowPass(data, 300f);
            float[] drops = { .35f, 1.6f, 2.05f, 3.4f, 4.9f, 5.25f, 6.8f, 8.1f };
            foreach (float at in drops)
            {
                int start = Mathf.RoundToInt(at * Rate);
                float pitch = 700f + (float)random.NextDouble() * 500f;
                double phase = 0;
                for (int i = 0; i < Rate / 6 && start + i < length; i++)
                {
                    float t = i / (float)Rate;
                    // A drop is a short sine that bends up as the bubble closes.
                    phase += (pitch * (1f + t * 6f)) / Rate;
                    data[start + i] += Sin(phase) * .55f * Mathf.Exp(-t * 38f);
                }
            }
            for (int creak = 0; creak < 2; creak++)
            {
                int start = Mathf.RoundToInt((2.6f + creak * 4.1f) * Rate);
                for (int i = 0; i < Rate * 7 / 10 && start + i < length; i++)
                {
                    float t = i / (float)Rate;
                    float envelope = Mathf.Sin(Mathf.PI * t / .7f);
                    float grain = (i % Mathf.RoundToInt(Rate / (38f + 14f * Mathf.Sin(t * 5f)))) == 0 ? 1f : 0f;
                    data[start + i] += grain * .5f * envelope;
                }
            }
            Normalize(data, .8f);
            return Clip("Atacarejo — frigorífico", data);
        }

        // One step of a hard-soled shoe on polished concrete: a sharp heel
        // click with a short hollow knock, then the sole landing ~60 ms later
        // (softer tick plus a little scuff). Each variant changes the seed,
        // the knock pitch and the heel-to-toe gap, so steps never repeat.
        static AudioClip MakeShoeStep(int variant)
        {
            const float HeelClickDecay = 900f, HeelKnockDecay = 55f;
            const float SoleClickDecay = 1100f, SoleScuffDecay = 70f;
            var random = new System.Random(100 + variant);
            int length = Mathf.RoundToInt(.3f * Rate);
            var data = new float[length];
            float knock = 185f + variant * 19f;
            int sole = Mathf.RoundToInt((.052f + variant * .007f) * Rate);
            float highPass = 0f, lastNoise = 0f, lowPass = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(random.NextDouble() - .5);
                highPass = .82f * (highPass + noise - lastNoise);
                lastNoise = noise;
                lowPass += .35f * (noise - lowPass);
                float heel = highPass * 1.5f * Mathf.Exp(-t * HeelClickDecay) + Sin(t * knock) * .5f * Mathf.Exp(-t * HeelKnockDecay);
                float toe = 0f;
                if (i >= sole)
                {
                    float s = (i - sole) / (float)Rate;
                    toe = highPass * .7f * Mathf.Exp(-s * SoleClickDecay) + lowPass * .45f * Mathf.Exp(-s * SoleScuffDecay)
                        + Sin(s * knock * 1.3f) * .18f * Mathf.Exp(-s * 80f);
                }
                data[i] = heel + toe;
            }
            Normalize(data, .9f);
            return Clip("Atacarejo — sapato " + variant, data);
        }
    }
}
