# ERMO

Protótipo em Unity 6000.6 / URP. A base visual portada de `godot/ermo` foi adaptada para **câmera digital de consumo dos anos 2000**, inspirada em Cyber-shot/Handycam: uma memória de infância ensolarada, acolhedora e levemente estranha, não VHS ou analog horror. Há cores saturadas, colinas orgânicas e chunks persistentes. Texturas e sons vêm do próprio projeto Godot; não há assets externos para baixar. Veja a comparação técnica em [docs/GODOT_VISUAL_PORT.md](docs/GODOT_VISUAL_PORT.md).

## Jogar

1. Abra `Assets/Ermo/Scenes/Ermo.unity` (ou menu **Ermo → Open Meadow Scene**).
2. Aperte **Play** e clique na Game View, se necessário.
3. **WASD** anda, **Shift** corre, **Espaço** pula, **mouse** olha, **Esc / F1** abre opções, **H** oculta as dicas. O título e o subtítulo não são exibidos no canto da tela durante a exploração.
4. O perfil **ERMO Dreamcore**, a captura a **30 fps**, a câmera digital e a névoa de distância estão sempre ativos. O menu permite ajustar resolução, enquadramento, upscale e volume; não há atalhos F3/F4 de comparação.

No menu é possível trocar a seed. O mundo continua carregado enquanto o menu está aberto, mas o jogador para. Ao voltar a uma seed, os chunks anteriores são recuperados; o jogador começa novamente na estrada (a posição do jogador ainda não é salva). O movimento orgânico em repouso mantém ganho de **1,05×**; o shake de caminhada e corrida tem ganhos independentes, agora **1,25×** e **1,40×** sobre as intensidades anteriores.

## Configuração

Selecione `Assets/Ermo/Settings/MeadowWorld.asset` ou **Ermo → Select World Settings**. Contém seed, tamanho/resolução dos chunks, alcance de carregamento, frequências dos objetos, relevo, resolução de renderização, lente e balanço. As alterações no menu são apenas da sessão e não modificam o asset.

- O jogo utiliza exclusivamente `Assets/Ermo/Resources/Ermo/Camera/ERMO Dreamcore.asset`. O Inspector reúne sensor, compressão, óptica, exposição/AWB, foco, motion blur e intensidades de movimento. Os perfis Fine/Standard foram preservados apenas como referências autorais; não são selecionáveis no jogo. Detalhes e limites em [docs/CYBERSHOT_CAMERA.md](docs/CYBERSHOT_CAMERA.md).
- Captura **640×480 / 4:3**, bilinear, antes do upscale; interface nativa. O menu seleciona 320×240, 640×480, 800×600 ou nativa, widescreen opcional e nearest. A cadência de captura permanece em **30 fps**, sem limitar a lógica/física/UI e sem opção de desligamento.
- Dois passes digitais: óptica/CCD e compressão. Chroma 4:2:0 aproximado, quantização local 8×8, perdas distintas para áreas uniformes/detalhadas e histórico temporal discreto dependente do movimento. Ruídos de luminância/cor independentes, sharpening eletrônico, suavidade periférica, barril discreto e cromática radial sutil. Não há tracking, scanlines, falhas de fita ou glitches.
- Exposição medida na imagem linear, com adaptação gradual, EV limitado e clipping ajustável; balanço de branco conservador. Motion blur por vetores de câmera/objetos da URP, com referência de obturador 1/60 s. Autofocus opcional e desligado por padrão. Os campos ópticos antigos do World Settings continuam serializados por compatibilidade; no modo digital, os parâmetros de imagem vêm do **Camera Profile**.
- Não há opções ou atalhos para desativar a câmera digital, trocar o preset, reduzir o movimento, desligar a névoa ou ativar a antiga grade 640×360. Ramos legados foram preservados internamente para compatibilidade, não como opções de gameplay.
- Câmera de saída dedicada ao Display 1, além da câmera do jogador que renderiza a paisagem em textura.
- Alcance padrão de 768 m (8 chunks de 96 m por direção). Os 289 chunks da área inicial são carregados progressivamente.
- Névoa de distância sempre ativa: exponencial, densidade 0,0035, cor azul e perspectiva aérea 0,3, sem ruído animado sobre todo o relevo. O jogo não oferece a alternativa de névoa só no horizonte.
- Céu portado de `sky.gdshader`, com o mesmo gradiente azul, projeção de nuvens anisotrópica e limiares de ruído; não utiliza mais grupos de esferas nem nuvens fixas na vista inicial.
- Colinas gaussianas derivadas dos sete contornos Godot, agora com **altura ×0,65 e largura ×1,65**, e normais suavizadas sobre a mesma malha low-poly. Pátios nivelados em torno das casas, colisão da própria malha e estradas apoiadas na interpolação dos triângulos. A soma periódica considera vizinhos adicionais para evitar emendas nas colinas mais largas. `Godot Legacy Waves` disponibiliza a fórmula senoidal antiga; `Perlin` mantém uma alternativa.
- `Organic Hills` acrescenta deformação espacial contínua por Perlin, ondulações longas assimétricas e pequenas ondulações secundárias, todas dependentes da seed e **estáticas**. Isso quebra a repetição de 300×276 m sem animar o chão nem reintroduzir picos agudos. `Terrain Warp` e `Terrain Wave Height` controlam a intensidade. Malha, pátios, estrada, props e colisão compartilham o mesmo campo global.
- Grama, reboco, madeira e telhado usam as texturas originais, sombreamento Lambert, luz quente e ambiente azulado. No perfil found footage, a grama é filtrada de forma mais suave, sua textura tem menos contraste e o snapping dos props foi reduzido de 0,18 para 0,035; colisões não oscilam.
- `HandheldCameraMotion` substitui o bob/sway secundário anterior, sem duplicá-lo: respiração e postura mesmo parado, microtremores, ajustes ocasionais, bob por distância real/cadência dos passos e inércia amortecida apenas no pivô secundário. O mouse continua imediato. `Idle Motion Gain`, `Walk Motion Gain` e `Run Motion Gain` controlam as intensidades separadamente no perfil Dreamcore. O salto usa o mesmo CharacterController, tem altura inicial de 0,85 m e não permite salto duplo nem salto com o menu aberto.
- Casas low-poly com colisão; a variante de porta aberta tem entrada física. Caixas-d'água, postes com fios e turbinas brancas raras com pás lentas.
- A casa importada possui um volume de iluminação interna, delimitado pelos painéis das paredes e pela inclinação do telhado, sem incluir a varanda. `HouseInteriorLighting` reduz a luz ambiente apenas nas faces voltadas para o cômodo; fachadas, chão externo e topo do telhado mantêm a iluminação anterior. Uma porta e cinco janelas funcionam como aberturas: fechadas deixam o interior escuro; abertas acrescentam luz ambiente com queda por distância e permitem sol direto apenas nos raios que atravessam a abertura. A intensidade acompanha o ângulo real da animação. A névoa de distância não é aplicada às superfícies internas, evitando clareá-las com a cor do céu. É uma aproximação para o cômodo único deste modelo, não iluminação global física nem detecção genérica de cômodos arbitrários. Não altera exposição, névoa externa, luz global ou materiais compartilhados.
- Caixas-d'água com reservatório aberto, água e borda caminhável: a superfície superior parece água normal e há outra superfície d'água invertida no fundo, que mostra o céu do outro lado. Escada: **E** usa/solta, **W/S** sobem/descem e **Espaço** salta. Ao mergulhar, a queda é normal até a metade; depois corpo e câmera viram com a gravidade e atravessam a superfície inferior para emergir em `WaterWorld`, mantendo a mesma câmera. Detalhes em [docs/WATER_PASSAGE.md](docs/WATER_PASSAGE.md).
- Terreno contínuo amostrado em coordenadas globais; chunks próximos carregam primeiro e são descarregados com uma margem de segurança.
- Estradas sinuosas seguem o eixo Z em ambas as direções, em faixas espaçadas a cada 256 m em X. Assim, também há estradas e postes ao explorar lateralmente; cruzamentos, ramificações e outros biomas ficam para uma próxima etapa.

## Persistência e futuras personalizações

Dados atuais em `Application.persistentDataPath/Ermo/worlds/<SHA256-da-seed>/layout-6-organic-sites/`. Saves anteriores permanecem intactos, mas não são misturados ao novo relevo/distribuição. Não é necessário trocar a seed para ver as mudanças. O espaçamento mínimo dos locais continua em 150 m.

- `world.json`: versão, seed e parâmetros do terreno e da distribuição de locais. Esses parâmetros são restaurados ao reabrir a seed, evitando alterar o relevo e as distâncias mínimas nas bordas de chunks já visitados.
- `<chunkX>_<chunkZ>.json`: posições, rotações, escalas, tipos e IDs dos objetos já gerados. A lista é lida do disco, **não sorteada novamente**. A malha é reconstruída a partir do relevo determinístico; não são armazenados GameObjects ou malhas inteiras.
- Escrita temporária e substituição do arquivo; erros são exibidos no menu e no Console. JSONs inválidos são preservados com sufixo `.corrupt-…` para diagnóstico.

**Landmarks** no asset permite duas modalidades:

1. `Exact Chunk`: coordenada específica e posição local. Ex.: chunk `(2, -1)`, posição `(48, 48)`, tipo `Open House`.
2. Probabilística: um candidato por célula de `Spacing Meters`, com `Chance` e distância mínima da origem (`Minimum Distance`). `550` e `0.22` significam 22% por célula de 550 × 550 m, não uma garantia de uma casa a cada 550 m percorridos. A seleção não depende da ordem de exploração.

### Locais e combinações

- `Location Spacing = 180`: tamanho das células de distribuição, em metros.
- `Minimum Location Distance = 150`: separação mínima entre os **centros dos locais**, não entre objetos do mesmo local. A seleção consulta candidatos vizinhos em coordenadas globais, independentemente de quais chunks estão carregados. Não significa que cada casa fique exatamente a 150 m da próxima; células vazias e locais especiais produzem intervalos maiores.
- `Location Chance = 1`: todas as células são consideradas, em vez de deixar 28% vazias por sorteio. Candidatos perto da estrada ganham até 12 novas tentativas dentro do limite de jitter, em vez de descartar o local inteiro. A margem viária passa a 14 m para o centro e os anexos ficam para o lado afastado da estrada. Landmarks conflitantes ainda têm prioridade, e a distância mínima continua global. Não se coloca uma casa em todo chunk de 96 m: isso contrariaria os 150 m entre locais e eliminaria a sensação de espaço aberto.
- `House Chance`: chance de um local comum ter uma casa. `Water Tower Chance` e `Turbine Chance`: chances independentes de anexos ao local com casa; também podem existir torres e turbinas isoladas.
- Uma casa, casa + torre, casa + turbina ou casa + torre + turbina contam como **um único local**. Anexos ficam a aproximadamente 20–32 m do centro, compartilham `groupId`/`groupAnchor` e são persistidos com o chunk do centro, mesmo que atravessem sua borda.
- Landmarks também respeitam a distância mínima. Locais exatos têm prioridade sobre probabilísticos e comuns; conflitos entre candidatos da mesma categoria usam uma prioridade estável. Candidatos conflitantes podem ser descartados, então a distância é um mínimo, não uma promessa de ocupação de todas as células.
- `Include Water Tower` e `Include Turbine` permitem compor landmarks com casa e anexos. Postes não contam como locais e continuam ao longo das estradas.

Mantenha o `Id` de cada regra único e estável. A configuração de distribuição, inclusive landmarks, é registrada na criação do mundo para preservar o espaçamento entre chunks antigos e futuros. Para testar novas configurações, use outra seed; mudanças autorais em mundos existentes precisarão de uma migração explícita no futuro. `PropRecord.interacted` é um ponto de extensão: um sistema futuro deverá modificar o registro e chamar `WorldStore.Save` ao persistir uma interação. Portas interativas, inventário e eventos ainda não estão implementados.

O asset inclui uma casa a aproximadamente 180 m do início e, mais distante, uma casa com torre e turbina. Elas exemplificam locais autorais e combinações dentro do mundo procedural.

## Brisa e passos

`RuralAudio` toca uma brisa leve em loop com pequenas variações de intensidade, e passos por **deslocamento real** do jogador. A brisa está 30% mais alta que o volume configurado e para sob o teto de uma casa; retoma ao sair. Andar/correr muda a cadência e a força; variações discretas de pitch/volume evitam repetição mecânica. O piso é identificado por colisão e posição na estrada: grama, terra ou cimento. Parado, contra uma parede, no ar, pausado ou após teleporte não há passos.

Os quatro WAVs de `Assets/Ermo/Audio` foram copiados de `godot/ermo/assets/audio` e ajustados de nível sem clipping; os originais Godot não foram modificados. `Audio Volume`, `Breeze Volume` e `Footstep Volume` ficam no asset; o menu oferece volume geral. A prévia do editor nunca inicia áudio.

## Validação e limites

### Prévia no editor

Abra a cena Ermo e use **Ermo → Refresh 3D Preview**. São criados nove chunks temporários, com malhas, materiais, colisões e props inspecionáveis na Hierarchy/Scene View. Depois de editar `Contours` ou a seed no asset, atualize a prévia. **Clear 3D Preview** remove apenas os objetos marcados como prévia. Não captura o mouse, não toca áudio, não salva chunks e não altera os parâmetros de fog da cena. A prévia não é gravada na cena/build e é removida antes do runtime; céu e efeitos de tela completos são conferidos na Game View com Play.

**Ermo → Validate Generation** verifica determinismo, coordenadas negativas, separação e cobertura espacial dos locais, as quatro combinações de casa/anexos, inclinações suaves, continuidade do terreno, landmarks, persistência e shaders. Os testes de áudio verificam os clips decodificados, ausência de silêncio/clipping e a cadência sem passos em repouso, pausa ou no ar. Também pode executar em batch com `-executeMethod Ermo.Editor.ErmoTools.ValidateBatch`. A checagem automatizada de áudio não substitui uma audição subjetiva nos alto-falantes/fones do jogador.

Para um smoke test com renderização, execute em uma **cópia separada** do projeto com `-batchmode -executeMethod Ermo.Editor.ErmoPlayModeValidation.RunBatch` (sem `-nographics`). Valida a prévia sem duplicação, entra em Play Mode, verifica o carregamento, o framebuffer 640×360 e as alternativas nativa/horizonte. Grava `Logs/ErmoPreview.png` (pós-processado), `Logs/ErmoRawPreview.png` e `Logs/ErmoNativePreview.png`, e fecha o editor com o código de resultado. Uma exceção conhecida do indexador `UnityEditor.Search` é reportada separadamente dos erros de runtime.

`-batchmode -executeMethod Ermo.Editor.HouseInteriorValidation.RunBatch`, também em cópia separada e sem `-nographics`, verifica o volume interno, as seis aberturas, o ângulo animado e imagens renderizadas com o interior fechado/aberto, incluindo comparação dos pixels externos e proteção do topo do telhado.

O mapa tem expansão sob demanda e não tem uma borda de gameplay, mas ainda usa floats da Unity: viagens muito longas precisarão de floating origin. Escrita e criação de malhas são síncronas por chunk, limitadas por frame; para mundos mais densos, o próximo passo é Jobs e I/O assíncrono. Ainda não há objetivos ou sistema de terror nesta base explorável.

A cena original `Assets/Scenes/SampleScene.unity` foi preservada. `Ermo.unity` é a primeira cena da lista de build.
