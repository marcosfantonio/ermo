namespace Ermo
{
    // Distance-driven: never emits steps while idle, blocked, paused or airborne.
    public sealed class StepCadence
    {
        private float accumulated;
        public bool Advance(float distance, bool grounded, bool paused, bool running)
        {
            if (paused || !grounded || distance <= .001f || distance > 2) { accumulated = 0; return false; }
            accumulated += distance;
            float stride = running ? 2.15f : 1.7f;
            if (accumulated < stride) return false;
            accumulated -= stride;
            return true;
        }
    }
}
