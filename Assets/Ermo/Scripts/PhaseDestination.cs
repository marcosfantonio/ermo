using System;
using UnityEngine;

namespace Ermo
{
    // Where a phase sends the player when it ends. Serializable, so every
    // phase can expose one in the Inspector and anyone can change the next
    // phase or the spawn point without touching code.
    [Serializable]
    public sealed class PhaseDestination
    {
        public const string SitioScene = "Assets/Ermo/Scenes/Sitios.unity";

        [Tooltip("Cena da próxima fase. Precisa estar em File > Build Profiles (Scene List).")]
        public string scenePath = SitioScene;

        [Tooltip("Ligado: o jogador nasce onde a própria cena decide. Desligado: nasce em Spawn Position / Spawn Yaw.")]
        public bool useSceneSpawn = true;

        [Tooltip("Posição de chegada em coordenadas de mundo da próxima cena (só se Use Scene Spawn estiver desligado).")]
        public Vector3 spawnPosition;

        [Tooltip("Direção do olhar na chegada, em graus (0 = +Z).")]
        public float spawnYaw;

        public bool IsValid => !string.IsNullOrWhiteSpace(scenePath);

        public override string ToString() =>
            useSceneSpawn ? scenePath : $"{scenePath} em {spawnPosition} ({spawnYaw:0}°)";
    }
}
