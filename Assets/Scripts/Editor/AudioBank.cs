using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlightSim.Build
{
    /// <summary>
    /// Every sound in the game, worked out from maths and written into Assets/Audio as plain
    /// 16-bit WAV files. Nothing is downloaded and nothing is licensed: a tone is a sine wave,
    /// a rush of air is filtered random numbers, and a .wav file is a short header followed by
    /// those numbers. Delete the folder and this menu item builds it again, identically.
    ///
    /// THE CLICK PROBLEM, which is the reason this file is so careful:
    /// a looping sound plays its last sample and then instantly plays its first one again. If
    /// those two samples are far apart, the speaker is yanked from one position to the other in
    /// 1/44100 of a second. That jump is heard as a sharp CLICK, once per loop - on a three
    /// second engine loop that is twenty clicks a minute, and it is the single thing that makes
    /// home-made game audio sound broken.
    ///
    /// There are two fixes here, one for each kind of layer:
    ///  - TONES are snapped to a frequency that fits a WHOLE number of cycles into the clip. A
    ///    whole number of cycles finishes exactly where it started (at zero, heading upwards),
    ///    so the join is mathematically invisible. See LoopHz.
    ///  - NOISE has no cycles to line up, so the end of the clip is CROSSFADED into the material
    ///    that runs up to the start. The tail fades out while that material fades in, so by the
    ///    last sample the clip is playing exactly what comes immediately before sample 0. See
    ///    AddNoise.
    /// After every looping clip is written the join is measured and logged, so the fix is
    /// checked rather than hoped for. See CheckSeam.
    ///
    /// One-shots have the same problem at their start: if sample 0 is far from zero, or the
    /// whole clip sits off-centre (a DC offset), then merely starting it clicks. So one-shots
    /// are centred on zero and ramped in and out over a few milliseconds. Looping clips are
    /// deliberately NOT ramped - a fade at both ends of a loop is heard as a throb once per
    /// cycle, and the crossfade has already made the join safe.
    /// </summary>
    public static class AudioBank
    {
        public const string AudioDir = "Assets/Audio";

        // -------------------------------------------------------------------- clip names
        // The builders ask for sounds by these constants, never by a typed-out string, so a
        // spelling mistake is a compile error instead of a silent missing sound.

        public const string JetIdle = "jet_idle";
        public const string JetPower = "jet_power";
        public const string Wind = "wind";
        public const string TyreRoll = "tyre_roll";
        public const string Touchdown = "touchdown";
        public const string Gear = "gear";
        public const string Chime = "chime";
        public const string PaChime = "pa_chime";
        public const string HallHum = "hall_hum";
        public const string Aircon = "aircon";
        public const string Footstep = "footstep";
        public const string Reverse = "reverse";
        public const string Warning = "warning";

        /// <summary>Every clip this file makes, in the order it makes them.</summary>
        public static readonly string[] All =
        {
            JetIdle, JetPower, Wind, TyreRoll, Touchdown, Gear, Chime,
            PaChime, HallHum, Aircon, Footstep, Reverse, Warning
        };

        // ---------------------------------------------------------------- format settings

        /// <summary>CD quality: 44100 samples a second, one channel, 16 bits a sample.</summary>
        public const int SampleRate = 44100;

        /// <summary>How much of a looping clip is used for the noise crossfade: the last 15%.</summary>
        const float CrossfadeFraction = 0.15f;

        /// <summary>The filters below start from silence, so this much is generated and thrown
        /// away first, leaving only the part where they have settled.</summary>
        const int WarmUpSamples = SampleRate / 10;

        /// <summary>Loudest sample a finished clip is allowed to reach. Below 1 so that several
        /// sounds playing at once still have somewhere to go.</summary>
        const float TargetPeak = 0.8f;

        /// <summary>Fade in and out of a one-shot, in milliseconds. Short enough not to soften
        /// the sound, long enough that the speaker is never asked to jump.</summary>
        const float RampMs = 6f;

        /// <summary>Same seed every run, so the same "random" noise comes out every time. The
        /// cabin passengers are placed the same way: a rebuild must never change the game.</summary>
        const int NoiseSeed = 1471;

        /// <summary>A loop join counts as bad when the step across it is this many times bigger
        /// than the clip's own average step from one sample to the next.</summary>
        const float SeamStepRatio = 8f;

        // Readable names for the last argument of AddSine and AddNoise.
        const bool Seamless = true;
        const bool OneShot = false;

        // -------------------------------------------------------------------- jet_idle
        const float IdleSeconds = 3f;
        const float IdleHz = 72f;              // the engine's fundamental note, felt more than heard
        const float IdleNoiseCutHz = 260f;     // everything above this is filtered off: a dull roar
        const float IdlePeak = 0.55f;          // quieter than power, so opening the throttle is obvious

        // ------------------------------------------------------------------- jet_power
        const float PowerSeconds = 3f;
        const float PowerHz = 90f;             // higher and brighter than idle
        const float PowerNoiseLowHz = 200f;    // the mid-band rush of air through the engine
        const float PowerNoiseHighHz = 3000f;

        // ------------------------------------------------------------------------ wind
        const float WindSeconds = 2.5f;
        const float WindLowHz = 400f;          // no bass: air over a fuselage is mid and high
        const float WindHighHz = 6000f;
        const float WindWobbleHz = 0.7f;       // a slow swell, so it doesn't sound like static
        const float WindWobbleDepth = 0.25f;

        // ------------------------------------------------------------------- tyre_roll
        const float TyreSeconds = 2f;
        const float TyreRumbleHz = 55f;
        const float TyreLowHz = 40f;
        const float TyreHighHz = 500f;         // tarmac rumble is almost all low frequency
        const float TyreWobbleHz = 7f;         // the roughness of the surface going past
        const float TyreWobbleDepth = 0.35f;

        // ------------------------------------------------------------------- touchdown
        const float TouchdownSeconds = 1.2f;
        const float TouchdownAttackMs = 1.5f;  // near instant: wheels hit tarmac, they don't swell
        const float TouchdownTau = 0.18f;
        const float SquealFromHz = 900f;       // the tyre spinning up to speed, dropping in pitch
        const float SquealToHz = 400f;
        const float SquealTau = 0.35f;
        const float SquealDelaySeconds = 0.03f;
        const float ThudHz = 60f;

        // ------------------------------------------------------------------------ gear
        const float GearSeconds = 1.8f;
        const float GearMotorHz = 55f;         // a buzzy motor: a low note plus a stack of harmonics
        const int GearMotorHarmonics = 8;
        const float GearMotorSeconds = 1.5f;   // the whirr, before the thump
        const float GearMotorFlutterHz = 30f;
        const float GearThumpHz = 70f;
        const float GearThumpTau = 0.16f;

        // ----------------------------------------------------------------------- chime
        const float ChimeSeconds = 1.8f;
        const float ChimeFirstHz = 880f;       // the classic two-note seatbelt ding, falling
        const float ChimeSecondHz = 660f;
        const float ChimeGapSeconds = 0.55f;
        const float ChimeAttackMs = 25f;       // soft, so it sounds struck rather than switched on
        const float ChimeTau = 0.55f;

        // --------------------------------------------------------------------- pa_chime
        const float PaSeconds = 1.9f;
        const float PaFirstHz = 660f;          // rising, not falling: "an announcement is coming"
        const float PaSecondHz = 880f;
        const float PaGapSeconds = 0.5f;
        const float PaEchoOneSeconds = 0.11f;  // two quiet delayed copies stand in for a big hall
        const float PaEchoTwoSeconds = 0.23f;
        const float PaEchoOneLevel = 0.4f;
        const float PaEchoTwoLevel = 0.18f;

        // --------------------------------------------------------------------- hall_hum
        const float HumSeconds = 4f;
        const float HumHz = 48f;               // air handling plant: two tones slightly apart
        const float HumSecondHz = 62f;
        const float HumNoiseLowHz = 100f;
        const float HumNoiseHighHz = 900f;
        const float HumPeak = 0.35f;           // ambience: present, never noticed

        // ----------------------------------------------------------------------- aircon
        const float AirconSeconds = 3f;
        const float AirconLowHz = 2000f;       // hiss only, no rumble underneath
        const float AirconHighHz = 9000f;      // shaved off at the top, or it fizzes
        const float AirconPeak = 0.4f;

        // --------------------------------------------------------------------- footstep
        const float FootstepSeconds = 0.18f;
        const float FootstepLowHz = 300f;
        const float FootstepHighHz = 4000f;
        const float FootstepTau = 0.035f;      // a click, not a thud
        const float FootstepThudHz = 110f;

        // ---------------------------------------------------------------------- reverse
        const float ReverseSeconds = 2.5f;
        const float ReverseLowHz = 80f;
        const float ReverseHighHz = 8000f;     // much wider than jet_power: that is the harshness
        const float ReverseRumbleHz = 55f;
        const float ReverseWobbleHz = 3f;
        const float ReverseWobbleDepth = 0.2f;
        const float ReversePeak = 0.85f;

        // ---------------------------------------------------------------------- warning
        const float WarningSeconds = 0.25f;
        const float WarningHz = 700f;          // the pitch cockpit warnings live at: hard to ignore
        const int WarningHarmonics = 9;        // odd harmonics only, which makes it square-ish

        // ====================================================================== menu items

        /// <summary>
        /// Makes every sound in the game and puts it in Assets/Audio. Safe to run at any time:
        /// it overwrites the files it made last time with identical ones.
        /// </summary>
        [MenuItem("Tools/Flight Sim/Generate Audio", priority = 41)]
        public static void Generate()
        {
            var started = DateTime.Now;
            Directory.CreateDirectory(AudioDir);

            SaveLoop(JetIdle, BuildJetIdle(), IdlePeak);
            SaveLoop(JetPower, BuildJetPower(), TargetPeak);
            SaveLoop(Wind, BuildWind(), TargetPeak);
            SaveLoop(TyreRoll, BuildTyreRoll(), TargetPeak);
            SaveOneShot(Touchdown, BuildTouchdown(), TargetPeak, true);
            SaveOneShot(Gear, BuildGear(), TargetPeak, false);
            SaveOneShot(Chime, BuildChime(), TargetPeak, false);
            SaveOneShot(PaChime, BuildPaChime(), TargetPeak, false);
            SaveLoop(HallHum, BuildHallHum(), HumPeak);
            SaveLoop(Aircon, BuildAircon(), AirconPeak);
            SaveOneShot(Footstep, BuildFootstep(), TargetPeak, true);
            SaveLoop(Reverse, BuildReverse(), ReversePeak);
            SaveOneShot(Warning, BuildWarning(), TargetPeak, false);

            // Unity has to be told the files exist before their import settings can be read.
            AssetDatabase.Refresh();

            for (int i = 0; i < All.Length; i++) ApplyImportSettings(All[i]);
            AssetDatabase.SaveAssets();

            Debug.Log(string.Format("[AUDIO] Generated {0} clips into {1} in {2:0.0}s.",
                                    All.Length, AudioDir, (DateTime.Now - started).TotalSeconds));
        }

        /// <summary>Command-line entry point: make the sounds and quit, exit code 1 on failure.</summary>
        public static void BatchGenerate()
        {
            int code = 0;
            try
            {
                Generate();
            }
            catch (Exception e)
            {
                Debug.LogError("[AUDIO] Generate failed: " + e);
                code = 1;
            }

            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        /// <summary>
        /// Fetches a finished clip so a scene builder can drop it into an AudioSource. Returns
        /// null and says so loudly if the file isn't there, because a game that is silent for no
        /// stated reason is a miserable thing to debug.
        /// </summary>
        public static AudioClip Load(string clipName)
        {
            string path = AudioDir + "/" + clipName + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip == null)
                Debug.LogWarning("[AUDIO] " + path + " is missing. Run Tools > Flight Sim > Generate Audio.");

            return clip;
        }

        // ======================================================================= the clips

        /// <summary>Engine ticking over: a low note, its first two harmonics, and a dull roar.</summary>
        static float[] BuildJetIdle()
        {
            var buf = NewBuffer(IdleSeconds);

            AddSine(buf, IdleHz, 0.50f, Seamless);
            AddSine(buf, IdleHz * 2f, 0.22f, Seamless);
            AddSine(buf, IdleHz * 3f, 0.10f, Seamless);
            AddNoise(buf, Rng(1), 0.55f, 0f, IdleNoiseCutHz, Seamless);

            return buf;
        }

        /// <summary>
        /// The same engine with the throttle open: the note is higher, there are harmonics up
        /// into the hundreds of hertz, and far more mid-band air rushing through it.
        /// </summary>
        static float[] BuildJetPower()
        {
            var buf = NewBuffer(PowerSeconds);

            AddSine(buf, PowerHz, 0.45f, Seamless);
            AddSine(buf, PowerHz * 2f, 0.28f, Seamless);
            AddSine(buf, PowerHz * 3f, 0.20f, Seamless);
            AddSine(buf, PowerHz * 4f, 0.14f, Seamless);
            AddNoise(buf, Rng(2), 0.45f, 0f, IdleNoiseCutHz, Seamless);
            AddNoise(buf, Rng(3), 0.80f, PowerNoiseLowHz, PowerNoiseHighHz, Seamless);

            return buf;
        }

        /// <summary>Airflow over the outside of the plane: noise with the bass taken out.</summary>
        static float[] BuildWind()
        {
            var buf = NewBuffer(WindSeconds);

            AddNoise(buf, Rng(4), 1f, WindLowHz, WindHighHz, Seamless);
            Wobble(buf, WindWobbleHz, WindWobbleDepth);

            return buf;
        }

        /// <summary>
        /// Wheels running on tarmac. Low, rough noise whose loudness wobbles a few times a
        /// second, which is what stops it sounding like a hiss and starts it sounding like a
        /// surface going past underneath you.
        /// </summary>
        static float[] BuildTyreRoll()
        {
            var buf = NewBuffer(TyreSeconds);

            AddSine(buf, TyreRumbleHz, 0.35f, Seamless);
            AddNoise(buf, Rng(5), 1f, TyreLowHz, TyreHighHz, Seamless);
            Wobble(buf, TyreWobbleHz, TyreWobbleDepth);

            return buf;
        }

        /// <summary>
        /// Wheels meeting the runway: a burst of noise at full volume from the first sample (a
        /// fade in would sound like a landing played backwards), a squeal falling in pitch as
        /// the tyres spin up to the speed of the plane, and a low thud through the airframe.
        /// </summary>
        static float[] BuildTouchdown()
        {
            var buf = NewBuffer(TouchdownSeconds);

            var burst = NewBuffer(TouchdownSeconds);
            AddNoise(burst, Rng(6), 1f, 150f, 7000f, OneShot);
            Decay(burst, TouchdownAttackMs, TouchdownTau);
            Mix(buf, burst, 0, 1f);

            var squeal = NewBuffer(TouchdownSeconds);
            AddSweep(squeal, SquealFromHz, SquealToHz, 0.5f, SquealTau);
            Mix(buf, squeal, Samples(SquealDelaySeconds), 1f);

            var thud = NewBuffer(TouchdownSeconds);
            AddSine(thud, ThudHz, 0.9f, OneShot);
            Decay(thud, TouchdownAttackMs, TouchdownTau * 1.5f);
            Mix(buf, thud, 0, 1f);

            return buf;
        }

        /// <summary>
        /// Landing gear: an electric motor whirring for a second and a half, then the thump of
        /// the leg locking into place. The whirr is a buzzy low tone - a note plus a stack of
        /// harmonics - rather than a clean sine, because motors are anything but clean.
        /// </summary>
        static float[] BuildGear()
        {
            var buf = NewBuffer(GearSeconds);

            var motor = NewBuffer(GearMotorSeconds);
            for (int h = 1; h <= GearMotorHarmonics; h++)
                AddSine(motor, GearMotorHz * h, 0.6f / h, OneShot);
            AddNoise(motor, Rng(7), 0.15f, 200f, 2500f, OneShot);
            Wobble(motor, GearMotorFlutterHz, 0.15f);      // the flutter of a motor under load
            Ramp(motor, 40f, 60f);                         // it starts and stops, it doesn't cut
            Mix(buf, motor, 0, 1f);

            var thump = NewBuffer(GearSeconds - GearMotorSeconds);
            AddSine(thump, GearThumpHz, 1f, OneShot);
            AddNoise(thump, Rng(8), 0.35f, 100f, 1800f, OneShot);
            Decay(thump, 2f, GearThumpTau);
            Mix(buf, thump, Samples(GearMotorSeconds), 1f);

            return buf;
        }

        /// <summary>
        /// The cabin seatbelt ding: two clean sine waves, the second lower than the first, each
        /// eased in over 25 ms so it sounds struck, and left to die away slowly.
        /// </summary>
        static float[] BuildChime()
        {
            var buf = NewBuffer(ChimeSeconds);

            var first = NewBuffer(ChimeSeconds);
            AddSine(first, ChimeFirstHz, 1f, OneShot);
            Decay(first, ChimeAttackMs, ChimeTau);
            Mix(buf, first, 0, 1f);

            var second = NewBuffer(ChimeSeconds);
            AddSine(second, ChimeSecondHz, 1f, OneShot);
            Decay(second, ChimeAttackMs, ChimeTau * 1.3f);
            Mix(buf, second, Samples(ChimeGapSeconds), 0.85f);

            return buf;
        }

        /// <summary>
        /// The airport PA two-tone. The same idea as the cabin chime but rising, and with two
        /// quiet delayed copies mixed underneath: in a big hall the sound reaches you again a
        /// fraction of a second later after bouncing off the far wall, and copying the sound is
        /// the cheapest honest way to imitate that without a reverb effect.
        /// </summary>
        static float[] BuildPaChime()
        {
            var dry = NewBuffer(PaSeconds);

            var first = NewBuffer(PaSeconds);
            AddSine(first, PaFirstHz, 1f, OneShot);
            Decay(first, ChimeAttackMs, ChimeTau);
            Mix(dry, first, 0, 1f);

            var second = NewBuffer(PaSeconds);
            AddSine(second, PaSecondHz, 1f, OneShot);
            Decay(second, ChimeAttackMs, ChimeTau * 1.2f);
            Mix(dry, second, Samples(PaGapSeconds), 0.9f);

            var buf = NewBuffer(PaSeconds);
            Mix(buf, dry, 0, 1f);
            Mix(buf, dry, Samples(PaEchoOneSeconds), PaEchoOneLevel);
            Mix(buf, dry, Samples(PaEchoTwoSeconds), PaEchoTwoLevel);

            return buf;
        }

        /// <summary>Terminal ambience: two very low tones and a little filtered noise. It should
        /// never be noticed, only missed when it is switched off.</summary>
        static float[] BuildHallHum()
        {
            var buf = NewBuffer(HumSeconds);

            AddSine(buf, HumHz, 0.45f, Seamless);
            AddSine(buf, HumSecondHz, 0.25f, Seamless);
            AddNoise(buf, Rng(9), 0.30f, HumNoiseLowHz, HumNoiseHighHz, Seamless);

            return buf;
        }

        /// <summary>Cabin air conditioning: steady hiss, with all the bass filtered away.</summary>
        static float[] BuildAircon()
        {
            var buf = NewBuffer(AirconSeconds);

            AddNoise(buf, Rng(10), 1f, AirconLowHz, AirconHighHz, Seamless);

            return buf;
        }

        /// <summary>A footstep: a very short filtered click with a small thud underneath it.</summary>
        static float[] BuildFootstep()
        {
            var buf = NewBuffer(FootstepSeconds);

            var click = NewBuffer(FootstepSeconds);
            AddNoise(click, Rng(11), 1f, FootstepLowHz, FootstepHighHz, OneShot);
            Decay(click, 1f, FootstepTau);
            Mix(buf, click, 0, 1f);

            var thud = NewBuffer(FootstepSeconds);
            AddSine(thud, FootstepThudHz, 0.6f, OneShot);
            Decay(thud, 1f, FootstepTau * 1.5f);
            Mix(buf, thud, 0, 1f);

            return buf;
        }

        /// <summary>
        /// Reverse thrust after landing. Deliberately harsher than jet_power: the noise runs
        /// from 80 Hz all the way up to 8 kHz instead of sitting in a polite mid band, and that
        /// width is what makes it a roar rather than a hum.
        /// </summary>
        static float[] BuildReverse()
        {
            var buf = NewBuffer(ReverseSeconds);

            AddSine(buf, ReverseRumbleHz, 0.40f, Seamless);
            AddSine(buf, ReverseRumbleHz * 2f, 0.22f, Seamless);
            AddNoise(buf, Rng(12), 1f, ReverseLowHz, ReverseHighHz, Seamless);
            Wobble(buf, ReverseWobbleHz, ReverseWobbleDepth);

            return buf;
        }

        /// <summary>
        /// A cockpit warning beep. A true square wave is a sine plus every odd harmonic, so
        /// adding a few odd harmonics gives the hard edge without the stepped shape, which at
        /// 44100 samples a second would produce false extra tones (aliasing).
        /// </summary>
        static float[] BuildWarning()
        {
            var buf = NewBuffer(WarningSeconds);

            for (int h = 1; h <= WarningHarmonics; h += 2)
                AddSine(buf, WarningHz * h, 1f / h, OneShot);

            return buf;
        }

        // ================================================================ making the sound

        /// <summary>An empty clip of the given length, every sample silent (zero).</summary>
        static float[] NewBuffer(float seconds)
        {
            return new float[Samples(seconds)];
        }

        /// <summary>Seconds turned into a whole number of samples.</summary>
        static int Samples(float seconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        }

        /// <summary>
        /// A random number source with a fixed starting point, so the "random" noise is the same
        /// noise every run. The salt gives each clip its own stream, which means adding a clip
        /// later cannot change the ones already made.
        /// </summary>
        static System.Random Rng(int salt)
        {
            return new System.Random(NoiseSeed + salt * 7919);
        }

        /// <summary>
        /// Nudges a frequency so that a WHOLE number of cycles fits in the clip. 72.4 Hz in a
        /// 3 s clip is 217.2 cycles, and that leftover fifth of a cycle is exactly the step that
        /// clicks at the loop point. Rounding to 217 cycles gives 72.33 Hz, which nobody can
        /// hear the difference of and which joins up perfectly.
        /// </summary>
        static float LoopHz(float hz, float seconds)
        {
            return Mathf.Max(1f, Mathf.Round(hz * seconds)) / seconds;
        }

        /// <summary>
        /// Adds a pure tone. When seamless is asked for, the frequency is snapped by LoopHz
        /// first. The wave starts at zero and, after a whole number of cycles, the sample that
        /// would come after the last one is zero again, so the loop joins without a step.
        /// The levels passed in only set the balance between layers; the finished clip is scaled
        /// to a fixed peak afterwards, so they do not have to add up to anything in particular.
        /// </summary>
        static void AddSine(float[] buf, float hz, float level, bool seamless)
        {
            float seconds = buf.Length / (float)SampleRate;
            float f = seamless ? LoopHz(hz, seconds) : hz;
            float step = 2f * Mathf.PI * f / SampleRate;

            for (int i = 0; i < buf.Length; i++)
                buf[i] += Mathf.Sin(step * i) * level;
        }

        /// <summary>
        /// A tone that slides from one pitch to another while dying away - the tyre squeal.
        /// The angle has to be added up step by step. Working it out instead as frequency times
        /// time would make the wave jump every time the frequency changed, and every one of
        /// those jumps is a click.
        /// </summary>
        static void AddSweep(float[] buf, float fromHz, float toHz, float level, float tau)
        {
            float phase = 0f;

            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)buf.Length;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                float env = Mathf.Exp(-(i / (float)SampleRate) / tau);

                buf[i] += Mathf.Sin(phase) * env * level;
                phase += 2f * Mathf.PI * hz / SampleRate;
            }
        }

        /// <summary>
        /// Adds a layer of noise, filtered down to a band, and made loop-safe if asked.
        ///
        /// Noise is only random numbers, so there is nothing to line up at the loop point the
        /// way there is with a tone. The fix is a crossfade. Extra material is generated BEFORE
        /// the clip (the pre-roll), and over the last 15% of the clip the tail is faded out
        /// while that pre-roll is faded in. The very last sample then IS the sample that comes
        /// immediately before sample 0, so wrapping round to the start continues the sound
        /// instead of jumping.
        ///
        /// The fade uses square roots rather than straight lines because the two halves are
        /// unrelated noise: fading one down and the other up in a straight line leaves the
        /// middle of the crossfade about 3 dB quiet, which is heard as a dip once per loop.
        ///
        /// Pass 0 for highPassHz or lowPassHz to skip that filter.
        /// </summary>
        static void AddNoise(float[] buf, System.Random rng, float level,
                             float highPassHz, float lowPassHz, bool seamless)
        {
            int fade = seamless ? Mathf.Max(1, Mathf.RoundToInt(buf.Length * CrossfadeFraction)) : 0;

            // The filters start from silence, so their first fraction of a second is a fade-in
            // that is not part of the sound. Generate a throwaway warm-up and drop it, or that
            // fade-in would be crossfaded into the tail and heard as a dip at the loop point.
            var raw = new float[WarmUpSamples + fade + buf.Length];
            for (int i = 0; i < raw.Length; i++)
                raw[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            if (lowPassHz > 0f) LowPass(raw, lowPassHz);
            if (highPassHz > 0f) HighPass(raw, highPassHz);

            int preRoll = WarmUpSamples;          // the material running up to the clip's start
            int start = preRoll + fade;           // where the clip itself begins
            int fadeFrom = buf.Length - fade;     // where the crossfade begins

            for (int i = 0; i < buf.Length; i++)
            {
                float v = raw[start + i];

                if (seamless && i >= fadeFrom)
                {
                    int into = i - fadeFrom;
                    float t = (into + 1f) / fade;               // 0 -> 1 across the crossfade
                    v = v * Mathf.Sqrt(1f - t) + raw[preRoll + into] * Mathf.Sqrt(t);
                }

                buf[i] += v * level;
            }
        }

        /// <summary>
        /// Keeps the low frequencies and removes the high ones, which is what turns hissing
        /// static into a distant rumble. Each sample is dragged part of the way towards the new
        /// one instead of jumping straight to it, and the cutoff sets how far it is dragged.
        /// </summary>
        static void LowPass(float[] buf, float cutoffHz)
        {
            float k = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate);
            float y = 0f;

            for (int i = 0; i < buf.Length; i++)
            {
                y += (buf[i] - y) * k;
                buf[i] = y;
            }
        }

        /// <summary>
        /// The opposite: work out the low frequencies and subtract them, leaving the high ones
        /// behind. That is how the air conditioning ends up as hiss with no rumble underneath.
        /// </summary>
        static void HighPass(float[] buf, float cutoffHz)
        {
            float k = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate);
            float y = 0f;

            for (int i = 0; i < buf.Length; i++)
            {
                y += (buf[i] - y) * k;
                buf[i] -= y;
            }
        }

        /// <summary>
        /// Makes the loudness rise and fall. The wobble frequency is snapped by LoopHz as well,
        /// because otherwise the volume would jump at the loop point even though the sound
        /// underneath it joined up perfectly.
        /// </summary>
        static void Wobble(float[] buf, float hz, float depth)
        {
            float seconds = buf.Length / (float)SampleRate;
            float step = 2f * Mathf.PI * LoopHz(hz, seconds) / SampleRate;

            for (int i = 0; i < buf.Length; i++)
                buf[i] *= 1f + Mathf.Sin(step * i) * depth;
        }

        /// <summary>
        /// The shape of a one-shot: a short rise out of silence, then a steady dying away. The
        /// rise is what stops the first sample being a step out of nowhere, and even 1.5 ms of
        /// it still sounds instant to a listener.
        /// </summary>
        static void Decay(float[] buf, float attackMs, float tau)
        {
            int attack = Mathf.Max(1, Mathf.RoundToInt(attackMs * 0.001f * SampleRate));

            for (int i = 0; i < buf.Length; i++)
            {
                float env = Mathf.Exp(-(i / (float)SampleRate) / tau);
                if (i < attack) env *= i / (float)attack;
                buf[i] *= env;
            }
        }

        /// <summary>Fades the start and the end of a clip over the given number of milliseconds.</summary>
        static void Ramp(float[] buf, float inMs, float outMs)
        {
            int rampIn = Mathf.Clamp(Mathf.RoundToInt(inMs * 0.001f * SampleRate), 0, buf.Length / 2);
            int rampOut = Mathf.Clamp(Mathf.RoundToInt(outMs * 0.001f * SampleRate), 0, buf.Length / 2);

            for (int i = 0; i < rampIn; i++)
                buf[i] *= i / (float)rampIn;

            for (int i = 0; i < rampOut; i++)
                buf[buf.Length - 1 - i] *= i / (float)rampOut;
        }

        /// <summary>Adds one buffer into another, starting at a given sample, at a given level.</summary>
        static void Mix(float[] dst, float[] src, int start, float level)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int at = start + i;
                if (at < 0 || at >= dst.Length) continue;
                dst[at] += src[i] * level;
            }
        }

        /// <summary>
        /// Moves the whole clip back on to zero. If the numbers average out above or below zero
        /// (a DC offset) then the speaker is already pushed out of place before the sound even
        /// starts, so starting and stopping the clip both click - and the offset itself is
        /// inaudible, so there is nothing to hear that would explain where the clicks came from.
        /// </summary>
        static void Centre(float[] buf)
        {
            double sum = 0.0;
            for (int i = 0; i < buf.Length; i++) sum += buf[i];

            float mean = (float)(sum / buf.Length);
            for (int i = 0; i < buf.Length; i++) buf[i] -= mean;
        }

        /// <summary>
        /// Scales the clip so its loudest sample is exactly the peak asked for. Adding layers
        /// together can easily push past 1, and anything past 1 has to be chopped off when the
        /// file is written, which sounds like a loud crack rather than a loud sound. Scaling
        /// first means nothing is ever chopped. The quiet background clips are given a lower
        /// peak here so they already sit at the right level next to the loud ones.
        /// </summary>
        static void Normalise(float[] buf, float peak)
        {
            float loudest = 0f;
            for (int i = 0; i < buf.Length; i++) loudest = Mathf.Max(loudest, Mathf.Abs(buf[i]));

            if (loudest < 1e-6f) return;

            float gain = peak / loudest;
            for (int i = 0; i < buf.Length; i++) buf[i] *= gain;
        }

        // ================================================================ saving the clip

        /// <summary>
        /// Finishes a looping clip and writes it. It is centred and scaled but NOT faded in and
        /// out: a fade at both ends of a loop is heard as a throb once per cycle. The join is
        /// handled by the whole-cycle tones and the noise crossfade instead, and then checked.
        /// </summary>
        static void SaveLoop(string clipName, float[] buf, float peak)
        {
            Centre(buf);
            Normalise(buf, peak);
            CheckSeam(clipName, buf);
            Write(clipName, buf);
        }

        /// <summary>
        /// Finishes a one-shot and writes it. sharpAttack means the sound is meant to hit
        /// immediately (a wheel on tarmac, a foot on carpet), so only the end is faded: its own
        /// envelope has already brought it up from zero, which is all that is needed.
        /// </summary>
        static void SaveOneShot(string clipName, float[] buf, float peak, bool sharpAttack)
        {
            Centre(buf);
            Ramp(buf, sharpAttack ? 0f : RampMs, RampMs);
            Normalise(buf, peak);
            Write(clipName, buf);
        }

        /// <summary>
        /// Measures the join of a looping clip and says so in the console, so that the fix above
        /// is proved rather than assumed.
        ///
        /// Two numbers are reported. The first is the plain step from the last sample back to
        /// the first. On its own that is not enough to judge by, because noisy clips move a long
        /// way between any two samples, so a hissing clip with a perfect join still shows a
        /// fair-sized step. The second number is the honest test: the size of the join compared
        /// with the clip's own average step from one sample to the next. A click is a jump much
        /// bigger than the sound's normal movement, so anything near 1 is inaudible, and the
        /// warning only fires well above that.
        /// </summary>
        static void CheckSeam(string clipName, float[] buf)
        {
            float gap = Mathf.Abs(buf[0] - buf[buf.Length - 1]);

            double total = 0.0;
            for (int i = 1; i < buf.Length; i++) total += Mathf.Abs(buf[i] - buf[i - 1]);
            float average = (float)(total / Mathf.Max(1, buf.Length - 1));

            float ratio = average < 1e-6f ? 0f : gap / average;
            string message = string.Format("[AUDIO] {0} loop join: step {1:0.00000}, which is {2:0.0}x "
                                           + "the clip's average step.", clipName, gap, ratio);

            if (ratio > SeamStepRatio)
                Debug.LogWarning(message + " That is big enough to click once per loop.");
            else
                Debug.Log(message + " Clean.");
        }

        /// <summary>
        /// Writes the samples out as a .wav file, header and all. A WAV is a "RIFF" container:
        /// twelve bytes saying what the file is, a "fmt " chunk describing the format, and a
        /// "data" chunk holding the samples themselves. Writing it by hand keeps the project
        /// free of any extra package.
        ///
        /// Each sample is turned from a number between -1 and 1 into a whole number between
        /// -32768 and 32767. It is clamped first, because a value past 1 would overflow the
        /// 16-bit number and wrap round to a large NEGATIVE one - the loudest possible sound
        /// flipping to its opposite, which is heard as a violent crack.
        /// </summary>
        static void Write(string clipName, float[] buf)
        {
            Directory.CreateDirectory(AudioDir);
            string path = AudioDir + "/" + clipName + ".wav";

            const int channels = 1;
            const int bits = 16;
            int dataBytes = buf.Length * (bits / 8);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(stream))
            {
                Tag(w, "RIFF");
                w.Write(36 + dataBytes);                       // size of everything after these 8 bytes
                Tag(w, "WAVE");

                Tag(w, "fmt ");
                w.Write(16);                                   // length of this chunk
                w.Write((short)1);                             // 1 means uncompressed PCM
                w.Write((short)channels);
                w.Write(SampleRate);
                w.Write(SampleRate * channels * bits / 8);     // bytes per second
                w.Write((short)(channels * bits / 8));         // bytes per sample frame
                w.Write((short)bits);

                Tag(w, "data");
                w.Write(dataBytes);

                for (int i = 0; i < buf.Length; i++)
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(buf[i], -1f, 1f) * 32767f));
            }

            Debug.Log(string.Format("[AUDIO] Wrote {0} ({1:0.00}s, {2} samples).",
                                    path, buf.Length / (float)SampleRate, buf.Length));
        }

        /// <summary>The four-letter labels inside a WAV file are plain ASCII bytes.</summary>
        static void Tag(BinaryWriter w, string tag)
        {
            for (int i = 0; i < tag.Length; i++) w.Write((byte)tag[i]);
        }

        /// <summary>
        /// Tells Unity how to treat each file: decompress it into memory when the scene loads,
        /// and keep it as raw PCM. These clips are tiny and several of them loop underneath the
        /// player constantly, so decoding them over and over while the game runs would cost more
        /// than simply storing them.
        /// </summary>
        static void ApplyImportSettings(string clipName)
        {
            string path = AudioDir + "/" + clipName + ".wav";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
            {
                Debug.LogWarning("[AUDIO] No importer for " + path + ", so its settings were left alone.");
                return;
            }

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;

            importer.SaveAndReimport();
        }
    }
}
