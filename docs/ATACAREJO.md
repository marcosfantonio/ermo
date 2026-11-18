# Atacarejo

Supermercado atacadista infinito, escuro, com uma única fileira de luzes acesas. Fase independente:
a cena `Assets/Ermo/Scenes/Atacarejo.unity` monta o próprio jogador (mesma câmera Dreamcore do Sítio)
e não depende de nenhuma outra fase.

## Jogar

- Menu **Ermo > Jogar o Atacarejo** (abre a cena e dá Play), ou abrir `Atacarejo.unity` e dar Play.
- **WASD** andar, **Shift** correr, **Espaço** pular, **mouse** olhar, **E** abrir portas, **Esc/F1** pausa.

1. O jogador acorda no vestíbulo, diante dos caixas. Dica: "As luzes acesas levam à saída."
2. A fileira acesa (x = 4) leva à porta **SAÍDA** (z = 62). É uma armadilha: "Aperte E para sair" →
   tela escura → de volta ao vestíbulo. O alto-falante muda a cada volta e aponta o setor 13.
3. Placas azuis "13 →" e luzes frias levam à **câmara 13** (x ≈ 103, z ≈ 39). No fundo dela, a porta do
   **frigorífico**: rastro de sangue, carcaças em trilhos, poças e respingos, luz vermelha, gotejamento.
4. No fundo do frigorífico, a **ENTRADA DE FUNCIONÁRIOS**: "Aperte E para entrar" → próxima fase.
   É a única saída do mercado.

## Configurar (Inspector, sem código)

Cena `Atacarejo.unity` → objeto **Atacarejo** → componente **Atacarejo Bootstrap**:

| Campo | Padrão | Efeito |
|---|---|---|
| Arrival / Arrival Yaw | `(0, 0.1, -8)`, `0` | Onde o jogador acorda (coordenadas da planta). A SAÍDA devolve para cá. |
| Next Phase → Scene Path | `Assets/Ermo/Scenes/Sitios.unity` | Cena carregada pela entrada de funcionários. Precisa estar na Scene List. |
| Next Phase → Use Scene Spawn | ligado | Ligado: a cena decide onde nascer. Desligado: usa Spawn Position / Spawn Yaw. |

## Ligar fases entre si

As fases podem vir em qualquer ordem e cada uma tem o seu gatilho. Três peças genéricas servem a todas:

- `PhaseDestination` — cena + spawn opcional, serializável (exponha um no Inspector da sua fase).
- `PhaseTransition.Go(destino, corDoFade)` — fade, carrega a cena no lugar da atual, posiciona o jogador
  se pedido, reabre a tela. `PhaseTransition.Busy` e o evento `PhaseTransition.Arrived`.
- `ScreenFade.Instance` — cor por cima de tudo, sobrevive à troca de cena.

Para entrar no Atacarejo a partir de qualquer gatilho:

```csharp
PhaseTransition.Go(new PhaseDestination { scenePath = AtacarejoWorld.ScenePath }, Color.black);
```

Se a cena de destino já tiver um `FirstPersonWalker`, o Atacarejo usa esse jogador em vez de criar outro.

## Código

| Arquivo | Função |
|---|---|
| `AtacarejoBootstrap` | Entrada da fase: monta o jogador, guarda chegada e próxima fase, pausa |
| `AtacarejoWorld` | Setores de 32 m ao redor do jogador, origem flutuante, névoa, as duas saídas |
| `AtacarejoLayout` | Planta determinística por seed: corredores, rotas, câmara 13, frigorífico, saída |
| `AtacarejoBuilder` | Instancia os prefabs de um setor e acende as luminárias |
| `AtacarejoDressing` | Interior da câmara 13, frigorífico e vergas, feitos de primitivas |
| `AtacarejoLights` | Luzes que piscam: suave, doente, quase morta |
| `AtacarejoAudio` | Zumbido, música, passos de sapato no concreto, portas |
| `AtacarejoAnnouncer` | Alto-falante: falas, legenda, dica inicial |
| `AtacarejoUse` / `AtacarejoExitDoor` / `ColdDoor` | Mira + E; saídas; porta com dobradiça |
| `AtacarejoSounds` | Todos os sons, sintetizados em código |
| `PixelLetters` | Letreiros em pixel 5x7 feitos de malha (não use `TextMesh`: atravessa paredes) |
| `AtacarejoKit` + `Settings/AtacarejoKit.asset` | Prefabs do kit |

Modelos: `Assets/Ermo/Modelos/Atacarejo/gerar_atacarejo.py` (Blender) gera `.blend`, `.fbx` e `kit.json`;
**Ermo > Rebuild Atacarejo Prefabs** cria materiais URP e prefabs a partir deles.

## Testar

Com a Unity fechada: `testar_atacarejo.bat` (ou `testar_atacarejo.bat semblender` para pular o Blender).
Caminhos diferentes: `set UNITY=...` / `set BLENDER=...` antes. Com a Unity aberta: **Ermo > Test Atacarejo Play Mode**.

Etapas: modelos (Blender) → prefabs → validação da planta (rotas sem bloqueio, portas fechando os vãos,
luzes só nas rotas) → Play Mode (acorda no vestíbulo, passos, SAÍDA devolve ao vestíbulo, câmara 13,
frigorífico, entrada de funcionários carrega a próxima fase). Resultado em `Logs/atacarejo-status.txt`;
relatório e capturas em `Logs/Atacarejo/`.
