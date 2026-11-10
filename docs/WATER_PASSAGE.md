# Caixas-d'água → mundo de água

## Jogar

1. Aproxime-se da escada externa de uma caixa-d'água e pressione **E**.
2. **W/S** sobem/descem; **E** solta a escada e **Espaço** solta com impulso de salto.
3. No último degrau, continue com W: o personagem passa para a borda superior, que tem colisão e permite caminhar.
4. Entre ou pule na abertura central. A passagem começa quando a cabeça atravessa a água em queda, não ao simplesmente se aproximar da torre.
5. O mergulho continua, a câmera se inverte sob a água e emerge de pé do outro lado. WASD, mouse e salto voltam a funcionar normalmente.

O destino inicial é **`Assets/Ermo/Scenes/Balneario.unity`**: somente água e céu, sem costa ou construções. Há um piso de colisão submerso e invisível para o jogador poder emergir e explorar em pé. Voltar a uma seed pelo menu recria **Sitios**; ainda não há passagem de retorno pela água.

## Integração

- A distribuição procedural e os registros salvos de torres não mudaram. `WorldGeometry` apenas constrói a nova versão visual/física das mesmas torres: reservatório oco, borda anular, água sem tampa de colisão, escada completa e `WaterTowerAccess`.
- Os pontos de acesso são locais à torre; sua rotação e escala autorais/procedurais continuam válidas.
- `FirstPersonWalker` mantém o mesmo CharacterController. Uma trava temporária separa a movimentação de escada/mergulho do movimento normal, sem criar outro player controller.
- A escada fica além da projeção da borda superior, evitando bater a cabeça no piso antes de chegar ao topo. A passagem final para a borda é feita com colisão, não atravessando a parede do tanque.
- `WaterPassage` pré-carrega o destino aditivamente. Ao cruzar, move o mesmo rig de jogador/câmera/captura para a cena de água e desativa a paisagem rural e seu streaming. A câmera de saída permanece disponível na cena principal, que não é descarregada.
- A cena de água fica distante da paisagem original e com seu ambiente desativado até a chegada. Não adiciona sol ou cenário sobre a fase rural durante o preload.
- Não há segundo jogador, câmera de gameplay duplicada, fade preto ou tela de carregamento. Se o preload demorar, o mergulho espera na região submersa antes de atravessar, sem expor uma cena incompleta.
- O menu pausa a coreografia. Ao trocar a seed, o rig anterior é descartado com segurança, o ambiente de água é desativado e a cena rural volta a ser a cena ativa.

## Aparência e transição

`Resources/Ermo/Water/ReservoirWater.shader` é exclusivo do reservatório. O mar usa `Water.shader` e `WaterSky.hlsl`, restaurados à versão anterior aos ajustes de transparência/poço; mudanças ópticas do reservatório não alteram mais o destino. A referência visual do reservatório é `agua.png`: um poço de água escura e reflexiva que termina numa abertura para o céu, não dois discos azuis ou água praticamente invisível.

No reservatório, `_WellDepth` ativa um volume óptico cilíndrico de 7,5 unidades locais: o shader refrata o raio da câmera na entrada, calcula interseções e reflexões nas paredes aquáticas e mostra o céu invertido na extremidade inferior. Absorção por distância escurece o interior e pequenas ondulações quebram os reflexos. A superfície compõe transmissão e reflexão num pixel opaco: água cristalina não é simulada simplesmente reduzindo alpha. A profundidade óptica é virtual, contida na abertura para não expor o piso/plataforma da torre ou atravessar o terreno. A segunda superfície física invertida continua sendo usada na travessia; sua distância física é menor que a profundidade óptica impossível vista de cima.

Durante o mergulho, `PixelLens` acrescenta distorção, absorção de vermelho, dispersão e bolhas animadas em espaço de tela, preservando o cenário visível (não uma tela azul uniforme). O jogador cai normalmente na primeira metade do tanque; na segunda metade, câmera e corpo viram gradualmente, acompanhando a inversão da gravidade. A passagem de cena acontece junto à superfície inferior, seguida da emersão para cima no outro mar. O perfil Dreamcore, a captura 640×480/30 fps, a interface e o processamento da câmera são preservados.

`WaterImmersionEffects` reproduz `water_jump.wav` ao entrar, ruído grave/borbulhamento contínuo e splash de saída ao cruzar a superfície do destino. Ao caminhar no mundo da água, passos geram respingos procedurais cadenciados pelo deslocamento real. As fontes de áudio presentes na entrada recebem filtro passa-baixas até 650 Hz enquanto submerso; filtros preexistentes são preservados/restaurados. Os efeitos respeitam o volume do jogo e pausam no menu. O ruído subaquático, o splash de saída e os passos na água são sintetizados localmente. Ao emergir, gotas refratam a imagem e escorrem pela lente durante sete segundos. Bolhas são um efeito óptico procedural, não partículas volumétricas simuladas.

A inversão é uma coreografia local da câmera e da trajetória vertical entre as duas superfícies, não uma alteração de `Physics.gravity` que afetaria o mapa inteiro. Visualmente, a gravidade é normal até a metade e vira junto com o jogador no restante do percurso, que termina com o personagem em pé.

### Limites do protótipo

- Reflexão procedural de céu, não SSR ou reflexão planar de objetos. No destino atual só existem céu e água, portanto não há construções a refletir.
- O volume óptico calcula refração/reflexões de um cilindro procedural, não de objetos arbitrários submersos; não é simulação de fluido nem natação livre.
- A continuidade usa a região submersa opaca para esconder o remapeamento entre cenas; não é um portal geométrico que permite ver o outro mundo através de uma abertura transparente.
- Não há retorno ao reservatório pelo mundo de água nesta primeira versão.

## Validação

`Ermo → Validate Generation` verifica shaders e inclusão da cena no build. O smoke test batch verifica subida/colisão, preload, passagem, isolamento do shader do mar, áudio de entrada/saída, filtro subaquático, gotas, pausa e secagem. Captura `Logs/ErmoUnderwaterPreview.png` e `Logs/ErmoWaterWorldPreview.png` na cópia de validação. O teste batch não avalia a qualidade auditiva/perceptual dos efeitos.
