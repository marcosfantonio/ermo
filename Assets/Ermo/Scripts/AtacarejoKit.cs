using UnityEngine;

namespace Ermo
{
    [CreateAssetMenu(fileName = "AtacarejoKit", menuName = "Ermo/Atacarejo Kit")]
    public sealed class AtacarejoKit : ScriptableObject
    {
        public GameObject piso, forro, luminaria, pilar, parede, portaCamara, portaSaida, portal, placaSetor, placaCorredor;
        public GameObject gondola, testeira, portaPalete, atendimento, ilha, freezer;
        public GameObject bandeja, palete, caixa, fardo, cartucho, garrafao, pacote;
        public GameObject luminariaMorta, placaSaida, seta, placa13, evaporador;

        public GameObject Prefab(AtacarejoLayout.Kind kind, int variant)
        {
            switch (kind)
            {
                case AtacarejoLayout.Kind.Piso: return piso;
                case AtacarejoLayout.Kind.Forro: return forro;
                case AtacarejoLayout.Kind.Luminaria: return luminaria;
                case AtacarejoLayout.Kind.Pilar: return pilar;
                case AtacarejoLayout.Kind.Gondola: return gondola;
                case AtacarejoLayout.Kind.Bandeja: return bandeja;
                case AtacarejoLayout.Kind.Testeira: return testeira;
                case AtacarejoLayout.Kind.Palete: return palete;
                case AtacarejoLayout.Kind.Caixa: return caixa;
                case AtacarejoLayout.Kind.PortaPalete: return portaPalete;
                case AtacarejoLayout.Kind.Ilha: return ilha;
                case AtacarejoLayout.Kind.Freezer: return freezer;
                case AtacarejoLayout.Kind.Portal: return portal;
                case AtacarejoLayout.Kind.CaixaAtendimento: return atendimento;
                case AtacarejoLayout.Kind.Parede: return parede;
                case AtacarejoLayout.Kind.PortaCamara: return portaCamara;
                case AtacarejoLayout.Kind.PortaFrigorifico: return portaCamara;
                case AtacarejoLayout.Kind.PortaFundo: return portaSaida;
                case AtacarejoLayout.Kind.PlacaSetor: return placaSetor;
                case AtacarejoLayout.Kind.PlacaCorredor: return placaCorredor;
                case AtacarejoLayout.Kind.PortaSaida: return portaSaida;
                case AtacarejoLayout.Kind.LuminariaMorta: return luminariaMorta;
                case AtacarejoLayout.Kind.PlacaSaida: return placaSaida;
                case AtacarejoLayout.Kind.Seta: return seta;
                case AtacarejoLayout.Kind.Placa13: return placa13;
                case AtacarejoLayout.Kind.Evaporador: return evaporador;
                default:
                    switch (variant & 3)
                    {
                        case 1: return cartucho;
                        case 2: return garrafao;
                        case 3: return pacote;
                        default: return fardo;
                    }
            }
        }
    }
}
