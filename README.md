# ERMO

Protótipo em Unity 6000.6 / URP. A base visual portada de `godot/ermo` foi adaptada para **found footage tropical**, com textura de câmera antiga moderada, cores saturadas, colinas orgânicas e chunks persistentes. Texturas e sons vêm do próprio projeto Godot; não há assets externos para baixar. Veja a comparação técnica em [docs/GODOT_VISUAL_PORT.md](docs/GODOT_VISUAL_PORT.md).

## Jogar

1. Abra `Assets/Ermo/Scenes/Ermo.unity` (ou menu **Ermo → Open Meadow Scene**).
2. Aperte **Play** e clique na Game View, se necessário.
3. **WASD** anda, **Shift** corre, **mouse** olha, **Esc / F1** abre opções, **H** oculta a interface.
4. **F3** alterna a óptica analógica; **F4** alterna Godot 640×360 / resolução nativa. No menu também é possível escolher a névoa Godot ou a antiga névoa só no horizonte.

No menu é possível trocar a seed ou desativar o balanço e a lente usando **Movimento reduzido**. O mundo continua carregado enquanto o menu está aberto, mas o jogador para. Ao voltar a uma seed, os chunks anteriores são recuperados; o jogador começa novamente na estrada (a posição do jogador ainda não é salva).

## Configuração

Selecione `Assets/Ermo/Settings/MeadowWorld.asset` ou **Ermo → Select World Settings**. Contém seed, tamanho/resolução dos chunks, alcance de carregamento, frequências dos objetos, relevo, resolução de renderização, lente e balanço. As alterações no menu são apenas da sessão e não modificam o asset.

- Perfil atual: até **540 linhas**, ampliação pixelada (`Pixelated Upscale`), foco levemente suave, saturação **1,22**, contraste 1,05, highlights suavizados, lente 0,042 e cromática 0,3 px. Grão **0,014** em espaço de exibição: intermediário entre o antigo excesso e o perfil quase limpo. Dithering leve 0,08, RGB bleed discreto, linhas de vídeo sutis, instabilidade/tracking subpixel, variação lenta de exposição e raras falhas de fita de baixa intensidade. `Analog Artifacts` regula esses detalhes; movimento reduzido desliga as distorções animadas, mas preserva a textura. Não há flashes fortes ou glitch de tela inteira.
- **F4** permite experimentar o perfil 640×360 com nearest/escala inteira e voltar ao nativo; **F3** desativa apenas a óptica. O ramo óptico Godot original continua disponível ao desligar `Found Footage` (reponha seus parâmetros se quiser a intensidade original). O VHS legado continua opcional e desligado por padrão.
- Câmera de saída dedicada ao Display 1, além da câmera do jogador que renderiza a paisagem em textura.
- Alcance padrão de 768 m (8 chunks de 96 m por direção). Os 289 chunks da área inicial são carregados progressivamente.
- Névoa Godot: exponencial por distância, densidade 0,0035, cor azul e perspectiva aérea 0,3, sem o antigo ruído animado sobre todo o relevo. **Alternativa preservada:** `Fog Style = Horizon Only` começa a 85% do alcance (653 m) e fecha perto de 99,5% (764 m).
- Céu portado de `sky.gdshader`, com o mesmo gradiente azul, projeção de nuvens anisotrópica e limiares de ruído; não utiliza mais grupos de esferas nem nuvens fixas na vista inicial.
- Colinas gaussianas derivadas dos sete contornos Godot, agora com **altura ×0,65 e largura ×1,65**, e normais suavizadas sobre a mesma malha low-poly. Pátios nivelados em torno das casas, colisão da própria malha e estradas apoiadas na interpolação dos triângulos. A soma periódica considera vizinhos adicionais para evitar emendas nas colinas mais largas. `Godot Legacy Waves` disponibiliza a fórmula senoidal antiga; `Perlin` mantém uma alternativa.
- `Organic Hills` acrescenta deformação espacial contínua por Perlin, ondulações longas assimétricas e pequenas ondulações secundárias, todas dependentes da seed e **estáticas**. Isso quebra a repetição de 300×276 m sem animar o chão nem reintroduzir picos agudos. `Terrain Warp` e `Terrain Wave Height` controlam a intensidade. Malha, pátios, estrada, props e colisão compartilham o mesmo campo global.
- Grama, reboco, madeira e telhado usam as texturas originais, sombreamento Lambert, luz quente e ambiente azulado. No perfil found footage, a grama é filtrada de forma mais suave, sua textura tem menos contraste e o snapping dos props foi reduzido de 0,18 para 0,035; colisões não oscilam.
- Balanço orgânico mais lento: 0,68 ciclos laterais/s caminhando e 1 ciclo/s correndo, variação suave de Perlin e amortecimento de posição/rotação. `Walk Sway Frequency`, `Run Sway Frequency` e `Sway Smoothing` permitem ajustar a sensação sem afetar a resposta do mouse.
- Casas low-poly com colisão; a variante de porta aberta tem entrada física. Caixas-d'água, postes com fios e turbinas brancas raras com pás lentas.
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

`RuralAudio` toca uma brisa leve em loop com pequenas variações de intensidade, e passos por **deslocamento real** do jogador. Andar/correr muda a cadência e a força; variações discretas de pitch/volume evitam repetição mecânica. O piso é identificado por colisão e posição na estrada: grama, terra ou cimento. Parado, contra uma parede, no ar, pausado ou após teleporte não há passos.

Os quatro WAVs de `Assets/Ermo/Audio` foram copiados de `godot/ermo/assets/audio` e ajustados de nível sem clipping; os originais Godot não foram modificados. `Audio Volume`, `Breeze Volume` e `Footstep Volume` ficam no asset; o menu oferece volume geral. A prévia do editor nunca inicia áudio.

## Validação e limites

### Prévia no editor

Abra a cena Ermo e use **Ermo → Refresh 3D Preview**. São criados nove chunks temporários, com malhas, materiais, colisões e props inspecionáveis na Hierarchy/Scene View. Depois de editar `Contours` ou a seed no asset, atualize a prévia. **Clear 3D Preview** remove apenas os objetos marcados como prévia. Não captura o mouse, não toca áudio, não salva chunks e não altera os parâmetros de fog da cena. A prévia não é gravada na cena/build e é removida antes do runtime; céu e efeitos de tela completos são conferidos na Game View com Play.

**Ermo → Validate Generation** verifica determinismo, coordenadas negativas, separação e cobertura espacial dos locais, as quatro combinações de casa/anexos, inclinações suaves, continuidade do terreno, landmarks, persistência e shaders. Os testes de áudio verificam os clips decodificados, ausência de silêncio/clipping e a cadência sem passos em repouso, pausa ou no ar. Também pode executar em batch com `-executeMethod Ermo.Editor.ErmoTools.ValidateBatch`. A checagem automatizada de áudio não substitui uma audição subjetiva nos alto-falantes/fones do jogador.

Para um smoke test com renderização, execute em uma **cópia separada** do projeto com `-batchmode -executeMethod Ermo.Editor.ErmoPlayModeValidation.RunBatch` (sem `-nographics`). Valida a prévia sem duplicação, entra em Play Mode, verifica o carregamento, o framebuffer 640×360 e as alternativas nativa/horizonte. Grava `Logs/ErmoPreview.png` (pós-processado), `Logs/ErmoRawPreview.png` e `Logs/ErmoNativePreview.png`, e fecha o editor com o código de resultado. Uma exceção conhecida do indexador `UnityEditor.Search` é reportada separadamente dos erros de runtime.

O mapa tem expansão sob demanda e não tem uma borda de gameplay, mas ainda usa floats da Unity: viagens muito longas precisarão de floating origin. Escrita e criação de malhas são síncronas por chunk, limitadas por frame; para mundos mais densos, o próximo passo é Jobs e I/O assíncrono. Ainda não há objetivos ou sistema de terror nesta base explorável.

A cena original `Assets/Scenes/SampleScene.unity` foi preservada. `Ermo.unity` é a primeira cena da lista de build.
