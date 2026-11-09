using UnityEngine;

namespace Ermo
{
    public sealed class SlowRotor : MonoBehaviour
    {
        private void Update() => transform.Rotate(0, 0, 9 * Time.deltaTime, Space.Self);
    }
}
