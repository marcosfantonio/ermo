# ERMO — Sítios Infinitos

Primeira fase jogável em **Godot 4**, com paisagem rural liminal diurna, exploração em primeira pessoa e mundo procedural determinístico. Testado na Godot **4.7.2**, renderizador Compatibility/OpenGL.

## Iniciar

Abra `project.godot` na Godot, aguarde a importação e pressione **F5**. Ou:

```sh
godot --path . --editor
godot --path .
```

A preparação inicial carrega o entorno antes de liberar o jogador. Não são necessárias bibliotecas externas nem downloads de assets.

## Controles

| Ação | Controle |
|---|---|
| Caminhar / olhar | WASD ou setas / mouse |
| Correr / pequeno salto | Shift / Espaço |
| Pausar, ajustar volume e movimento da câmera | Esc |
| Ligar/desligar bob, sway e impacto | F2 |
| Tela cheia | F11 |

## O que está implementado

- Morros poligonais contínuos, caminho estreito sinuoso e grandes espaços vazios.
- Casas de reboco com alpendre, portas azuis, telhas, cadeira e interior vazio acessível; árvores, cercas, postes e caixas d’água esparsos.
- Chunks de 64 m com seed por coordenada, fila priorizada por distância, descarregamento e regeneração idêntica ao retornar. Configuração padrão: até 121 chunks ativos.
- Terreno distante leve, neblina e origem flutuante para percursos longos.
- Movimento acelerado/amortecido, aderência às encostas, bob por distância percorrida, inclinação lateral e aterrissagem discretos; efeitos desativáveis.
- Passos em capim, terra e cimento; vento e chamados distantes separados por silêncio. **Áudio sintetizado original, ainda placeholder.**
- Uma anomalia persistente de repetição, sem entidade ou jumpscare.
- Renderização interna 640 × 360 com ampliação inteira, texturas nearest e instabilidade geométrica leve nos materiais arquitetônicos.

## Organização e ajustes

- `scenes/main.tscn`: fase, sol, ambiente e pausa simples.
- `scenes/player/player.tscn`: corpo, colisão, cabeça e câmera.
- `scenes/props/`: objetos reutilizáveis, editáveis no editor.
- `resources/sitios.tres`: seed, tamanho/raio de chunks, movimento, sensibilidade e áudio.
- `scripts/world/landscape.gd`: campo de altura, estrada, distribuição e patamares.
- `scripts/world/chunk_generator.gd`: malha procedural e instâncias das cenas.
- `scripts/world/chunk_manager.gd`: streaming e origem flutuante.
- `environment/sitios_environment.tres`, `materials/`, `shaders/`: direção visual.
- `docs/art_direction.md`: análise das imagens fornecidas, comparação visual e limitações.
- `tools/build_assets.py`: fonte dos assets originais; não precisa ser executado para jogar.

Mudanças de seed alteram a disposição. Com a mesma seed, o mundo retorna igual, inclusive após fechar e iniciar novamente. Não há salvamento da posição do jogador nem persistência dos ajustes de pausa. A memória de sons já ouvidos dura uma sessão.

## Validação

```sh
godot --headless --editor --path . --import
godot --headless --path . --script tests/world_test.gd
godot --path . --script tests/capture.gd
```

O teste verifica determinismo, bordas compartilhadas, colisão inicial, streaming, retorno, origem flutuante, caminhada e pausa. A captura precisa de uma sessão gráfica e salva imagens reais em `captures/`.

### Percurso de avaliação

1. Observe a paisagem inicial: céu amplo, casa isolada à direita e construções menores nas encostas seguintes.
2. Aproxime-se do alpendre e entre no cômodo vazio. Observe a cadeira, a porta e o número.
3. Siga a estrada por vários morros; saia dela e retorne. Verifique se os pontos reconhecíveis permanecem iguais.
4. Alterne F2 e teste subidas, descidas e paradas. Esc deve pausar e retomar pelo botão ou pelo teclado.
5. Explore por alguns minutos sem procurar um objetivo. A atmosfera depende do vazio e da repetição, não de eventos frequentes.

<details>
<summary>Anomalia — detalhes para teste (spoiler)</summary>

A casa **13**, com a mesma orientação, cadeira e porta aberta, reaparece a cada quatro intervalos da sequência principal (384 m). A primeira fica perto do ponto inicial; as outras continuam para norte e sul. Não se teletransporta nem muda ao recarregar um chunk. Permanecer perto dela por sete segundos produz um ruído tonal muito baixo vindo do cômodo vazio, uma única vez por casa na sessão.

</details>

Próximo refinamento recomendado: avaliar as capturas e o percurso em conjunto com a referência, ajustar as proporções no Inspector e substituir os placeholders sonoros por gravações rurais próprias, sem aumentar a densidade de objetos.
