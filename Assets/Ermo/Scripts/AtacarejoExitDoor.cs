using UnityEngine;

namespace Ermo
{
    // A door that looks like a way out of the market. Added by AtacarejoBuilder.
    //   BackToVestibule: the "SAÍDA" at the end of the lit row. A trap: the
    //                    screen goes dark and the player is back at the arrival.
    //   NextPhase:       the staff entrance at the back of the meat locker,
    //                    behind cold room 13. The only real way out.
    public sealed class AtacarejoExitDoor : MonoBehaviour
    {
        public enum Leads { BackToVestibule, NextPhase }

        public Leads Destination { get; set; } = Leads.BackToVestibule;

        public string Prompt => Destination == Leads.NextPhase ? "Aperte E para entrar" : "Aperte E para sair";

        public bool Use()
        {
            var market = AtacarejoWorld.Instance;
            if (market == null) return false;
            return Destination == Leads.NextPhase ? market.LeaveToNextPhase() : market.LoopToVestibule();
        }
    }
}
