# ERMO — Sítios Infinitos

**Fonte de verdade: [CONCEPT.md](CONCEPT.md).** A entrada atual é `scenes/exploration.tscn`: uma fatia artesanal e finita dos Sítios Infinitos, em Godot **4.7.2**, Compatibility/OpenGL. Os universos futuros não foram construídos nesta reformulação.

## Reformulação atual

- **F5**: trecho planejado, com três casas, alpendres acessíveis, morros, estradas reconectadas e quatro entradas alternativas.
- **E junto à porteira de entrada**: atravessar e visitar novamente os Sítios; a visita avança, muda a entrada e pode alterar uma porteira secundária, uma cerca lateral e a cadeira da casa repetida. Os marcos permanecem fixos.
- **F2**: movimento da câmera; **F3**: óptica analógica; **Esc**: pausa/volume; WASD, Shift e Espaço mantidos.
- Dreamcore tropical: céu azul, verdes vivos, luz quente, sombra fria, materiais PS1 originais, resolução 640 × 360 e óptica ajustável por universo.
- Limites reais: muros rurais visíveis com colisão, posicionados junto a cristas irregulares e vegetação. Névoa complementa, não substitui a geometria.
- `scenes/universes/sitios.tscn`: abra diretamente para editar relevo, rotas, marcos, entradas e variantes. Selecione **World → Atualizar prévia 3D**. Para reproduzir no jogo, ative **Reproduce Visit**, informe **Session Seed / Visit Index** e execute `scenes/exploration.tscn`.
- `resources/sitios_universe.tres`: ambiente, FOV, áudio e intensidades ópticas. Cada universo pode apontar para uma cena e um perfil próprios.
- Visitas/flags persistem em `user://ermo_visits_v1.json`; não se salva a posição exata do jogador. Seed de sessão + índice de visita reproduzem a configuração.

Auditoria, arquitetura, validação e limitações: **[docs/reformulacao.md](docs/reformulacao.md)**. Capturas reais: `captures/reform_0.png` a `reform_3.png` (geráveis pelo teste abaixo).

```sh
godot --headless --editor --path . --import
godot --headless --path . --script tests/exploration_test.gd
godot --headless --editor --path . --script tests/authored_preview_test.gd
godot --path . --script tests/exploration_capture.gd
```

Os testes de editor devem ser executados **um por vez**, sem outra instância modificando/importando o mesmo projeto.

## Protótipos legados preservados

O restante desta página documenta os protótipos anteriores. `scenes/main.tscn` continua sendo o rural por chunks; `scenes/balneario_infinito.tscn` continua sendo o Balneário procedural. Eles não definem a nova arquitetura nem a cena inicial. Os atalhos **1/2** desses protótipos são ferramentas de desenvolvimento, não passagens narrativas. Na nova fatia, **2** ainda abre o Balneário legado; volte à fatia com **F5** no editor.

## Balneário Infinito

Inspirado em `balneario1` e `balneario2`: piscinas poligonais com bordas de azulejo azul, água turquesa animada, piso de pedra irregular, cascatas largas, toboáguas amarelos em espiral, cogumelos vermelhos, guarda-sóis coloridos, mesas e cadeiras brancas, palmeiras e canteiros. O parque permanece vazio, com água localizada, vento e sons distantes, sem música constante.

- Para executar este protótipo legado, abra `scenes/balneario_infinito.tscn` e use **F6**. O rural legado permanece em `scenes/main.tscn` (**F6**).
- `resources/balneario.tres`: seed e streaming; chunks padrão de **64 m**, até **49 ativos**, com descarga, retorno determinístico e origem flutuante.
- `scenes/balneario/`: módulos reutilizáveis de piscina, guarda-sol/mesa, palmeira, cogumelo e toboágua. Dimensões da piscina e tipo de toboágua são propriedades exportadas.
- Piscinas de **0,48 m**, com piso sólido e rampas de acesso nas duas extremidades. Não há natação nem mecânica de deslizar; os toboáguas são cenográficos.
- Pátios em **0, 1,6 e 3,2 m**, terraços de mesas elevados, rampas e escadas transitáveis. Muretas curvas com losangos junto à água, muros de arrimo, guarda-corpos brancos, postes e canteiros mais densos. As bordas dos chunks compartilham um anel de circulação na cota zero.
- Muros altos de pedra irregular com juntas escuras, como na lateral esquerda de `balneario1`; losangos reservados às muretas baixas. Pisos superiores e inferiores com o mesmo revestimento. Escadas mais compactas e bases fechadas nos acessos, na cascata e no toboágua espiral.
- Água parcialmente transparente com fundo azulejado visível, ondulações, brilho especular, Fresnel, cáusticas animadas e espuma na cascata; sem reflexos/refração de tela pesados.
- Piscinas ovais grandes/pequenas, quadradas e retangulares; **três piscinas adicionais no nível inferior de cada chunk**, com bancos e áreas de estar em vez de apenas corredores vazios. Acessos em faixas reservadas, com folga entre rampas e validação das praças contra os chunks vizinhos.
- Easter egg raro após **256 m**: rádio determinístico em uma das mesas já existentes, tocando `balneario.mp3` com banda estreita, leve distorção e volume espacial que cresce ao se aproximar. Digite **RADIO** para fazê-lo aparecer numa mesa próxima. O evento procedural pode ser reencontrado ao revisitar o mesmo chunk.
- Anomalia discreta: todas as placas identificam **PISCINA 01**, mesmo atravessando setores diferentes.
- As fotos são referências, não texturas extraídas: geometria, shaders e loop de água são originais.
- Filtro **VHS** com granulação, linhas de varredura, leve desvio de cor e tracking; **F3** alterna os artefatos, mantendo a saturação aumentada. O filtro afeta o cenário, não os menus.
- Câmera com balanço lateral, oscilação vertical e pequenos movimentos de rotação sincronizados aos passos, mais fortes ao correr e amortecidos ao parar. **F2** desativa o movimento.

Detalhes e limitações em `docs/balneario.md`.

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
| Ir para Fazendas / Sítios Infinitos | 1 |
| Ir para Balneário Infinito | 2 |

Os atalhos de fase também aceitam o teclado numérico e funcionam durante a pausa. A troca inicia no ponto de entrada da fase escolhida, sem guardar a posição anterior. Pressionar o número da fase atual não a reinicia.

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
godot --headless --path . --script tests/balneario_test.gd
godot --path . --script tests/capture.gd
godot --path . --script tests/balneario_capture.gd
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
