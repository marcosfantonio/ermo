# Reformulação — universos artesanais e memória espacial

## Fonte e escopo

`CONCEPT.md` foi lido integralmente antes das alterações. Seus dez ambientes e suas decisões narrativas abertas permanecem intactos. Nenhum universo adicional, monstro, explicação de lore ou abertura definitiva foi criado. Esta entrega implementa apenas um trecho dos **Sítios Infinitos** e a infraestrutura para cenas independentes.

A screenshot citada na solicitação não estava anexada. As referências locais `liminal` e `nordeste.jpe` e os assets originais existentes foram usados para orientar composição e arquitetura. Não se afirma correspondência exata com a imagem ausente de Your Happy Place; não foram extraídos assets desse jogo.

## 1. Auditoria e decisões

| Área existente | Diagnóstico | Decisão |
|---|---|---|
| `CONCEPT.md` | Espaços brasileiros reconhecíveis; geração integral não é requisito; primeiro protótipo rural | Preservar como fonte de verdade |
| `scenes/main.tscn`, `Landscape`, `ChunkGenerator`, `chunk_manager.gd` | Casas/estrada/relevo definidos por células, até 121 chunks, fila e origem flutuante | Manter como legado testável, retirar da entrada do projeto |
| Balneário local ainda não versionado | Herda o mesmo manager; geradores de piscinas, terraços, rampas e rádio dependem dele | Preservar integralmente; não migrar nem expandir nesta tarefa |
| Props `.tscn` | Casa de 7,4 m com alpendre, telhas, piso e cômodo navegável; cadeira, cerca, árvore, poste, caixa-d'água | Reaproveitar módulos originais; adicionar porteira reutilizável |
| Jogador | Aceleração, colisão de cápsula, snap de piso, corrida/salto, bob, sway, aterrissagem, pausa | Preservar; permitir classificação de piso por rotas artesanais; evitar duplicação de InputMap entre cenas |
| Materiais | Texturas originais de baixa resolução, nearest, reboco/telha/madeira com snapping subpixel, terreno por cor de vértice | Reaproveitar sem alterar materiais compartilhados dos legados |
| Céu/ambiente | Céu azul já existente, iluminação simples e névoa, Compatibility | Perfil rural novo: luz quente, ambiente frio e névoa azul; não usar sépia |
| VHS | Efeito separado do Balneário, com F3 | Preservar; nova óptica não depende desse filtro |
| Áudio | Passos por superfície, vento e chamados sintetizados, eco de casa | Reusar cena de áudio própria do universo; seed da visita controla sua sequência; placeholders continuam identificados |
| Interface/transições | Menu compartilhado, troca direta por 1/2, sem persistência de visita | Acrescentar passagem cotidiana, fade, gerenciamento/persistência; preservar atalhos legados de desenvolvimento |
| Editor | Prévia `@tool` limitada com reconstrução e ownership | Reutilizar mecanismo de limpeza/ownership; geometria nova também visível sem executar |

### Plano executado

1. Isolar o novo fluxo em `scenes/exploration.tscn`, sem sobrescrever o trabalho local dos geradores.
2. Criar perfil, estado de visita, entradas planejadas e variantes autorizadas.
3. Construir um trecho rural editável com relevo moldado por contornos, marcos e rotas explícitas.
4. Aplicar pipeline tropical/PS1 e óptica independente da interface.
5. Conectar porteira → nova visita ao mesmo universo, demonstrando retorno a uma região familiar.
6. Testar runtime, reprodução, persistência, prévia, regressões e capturas reais.

## 2. Arquitetura

```text
UniverseManager (autoload)
  ├─ progresso por ID: seed de sessão, contador de visitas, flags
  ├─ configuração determinística / reprodução explícita
  └─ troca de PackedScene com liberação da cena anterior

scenes/exploration.tscn (sessão jogável)
  ├─ Player + menu compartilhado
  ├─ Environment + StillSun
  ├─ Soundscape (específico do universo)
  ├─ Optics (reutilizável, anterior à UI)
  └─ World → scenes/universes/sitios.tscn
       ├─ Terrain / Contours / Yards
       ├─ Routes
       ├─ Landmarks (fixos)
       ├─ Spawns (Marker3D)
       ├─ Variations (únicos elementos autorizados a mudar)
       ├─ Boundary / Vegetation
       └─ Passage
```

`UniverseProfile` é um Resource com ID, nome, caminho da cena, seed-base, quantidade de configurações, regra de seleção de entrada (rodízio ou sorteio determinísticos), Environment, FOV, volume e parâmetros ópticos. Cenas futuras podem manter áudio, iluminação, geometria e regras próprias; não há geração rural compartilhada obrigatória. Não foram criados perfis vazios para os outros ambientes.

`authored_world.gd` coordena o trecho, não o gera integralmente. `authored_terrain.gd` triangula os contornos **posicionados à mão na cena**; `authored_route.gd` apenas projeta os caminhos definidos no Inspector sobre esse relevo. Não existe expansão, streaming ou regeneração aleatória da estrutura principal.

A implementação reutiliza a hierarquia de interface do protótipo e estende `scripts/main.gd`, evitando refatorar prematuramente o Balneário. O contrato atual das cenas jogáveis inclui `Player`, `World.world_ready`, `Soundscape`, `Environment`, `Interface` e `Optics`. Para novos universos, manter esse contrato ou fornecer um controlador de sessão específico, sem herdar regras rurais.

### Visitas e seeds

- Primeira execução: seed de sessão é sorteada uma vez e registrada. Não existe sorteio de coordenadas.
- Configuração: hash de **ID + seed-base + seed de sessão + índice de visita**; RNG local determina a variante e a vegetação secundária.
- Entrada: rotação determinística entre candidatos habilitados, com deslocamento derivado da seed. Evita repetir imediatamente a entrada quando o conjunto de candidatos é o mesmo.
- `spawn_point.gd`: ID, habilitação e máscara de configurações permitidas. Posição/orientação são planejadas; altura acompanha exatamente a triangulação do solo.
- `visit_variant.gd`: máscara de variantes; desliga visibilidade e colisão em conjunto.
- `prop_variation.gd`: duas posições/orientações explicitamente configuradas para um objeto, sem deslocamentos aleatórios.
- `UniverseManager.replay(id, visit, session_seed)`: reproduz sem avançar o contador persistido.
- `UniverseManager.remember/recall`: flags por universo; a porteira registra `gate_crossed`.

O arquivo `user://ermo_visits_v1.json` guarda versão, visitas e flags, usando escrita temporária seguida de rename. Entradas malformadas são ignoradas. A seed cabe exatamente em JSON; não se salva o estado interno do RNG. **Não há salvamento da posição exata, inventário ou opções do menu nesta entrega.** Alterar perfil-base, ordem de entradas, malha ou algoritmo muda a configuração: reprodução pressupõe a mesma revisão de conteúdo.

## 3. Level design implementado

- Três casas em posições artesanais: alpendre inicial, casa repetida do outro lado da encosta e casa alta junto à caixa-d'água.
- A casa repetida mantém número e arquitetura; sua cadeira pode aparecer no outro lado da varanda. Os ecos sonoros anteriores foram preservados.
- Caixa-d'água, árvore de bifurcação e postes permanecem fixos como referências, em vez de mudanças arbitrárias em todos os marcos.
- Estrada do alpendre + retorno pela encosta formam um percurso fechado; um caminho transversal oferece outra sequência de exploração.
- A variante de desvio coloca uma porteira real em um trecho do caminho transversal, sem bloquear o percurso principal. O pasto continua explorável: a porteira é uma pista/composição de desvio, **não** um puzzle que força um único caminho.
- Quatro entradas: estrada, frente do alpendre, encosta e retorno próximo à casa alta. Todas foram testadas no solo com cápsula e movimentação normais.
- A porteira principal é visível desde a estrada de chegada e próxima de um poste. **E** permite uma nova visita; não há portal luminoso nem teleporte automático durante caminhada comum.
- Um anel irregular de muros rurais sólidos, junto a cristas e vegetação, impede abandonar a área. Nenhum clamp de coordenadas, collider invisível periférico ou queda infinita resolve o limite. A névoa reduz a leitura distante; não substitui colisões.

O espaço é uma demonstração de **entrada variável, repetição reconhecível e mudanças entre visitas**, não uma implementação de interiores maiores por dentro, distâncias dinâmicas ou rearranjos em tempo real. Não se afirma que essa primeira composição já sustente os 10–15 minutos de experiência final indicados no conceito.

## 4. Pipeline visual

- Geometria low-poly e materiais originais existentes, com telha cerâmica, porta azul, reboco, cimento e textura de capim.
- Relevo em malha de 4 m e estradas amostradas sobre a mesma superfície da colisão.
- Vegetação apenas em zonas autorais, com escalas, proporções e rotações variáveis. Casas, estrada e marcos nunca são espalhados por ruído.
- FOV rural de 73°, resolução interna 640 × 360, nearest e snapping arquitetônico moderado.
- Sol lateral quente e ambiente frio; céu azul vivo. Construções claras e terra quente contrastam com verdes vivos e copas mais escuras.
- `memory_optics.gdshader`: distorção de barril moderada com overscan, amostragem linear leve, aberração subpixel nas bordas, vinheta, granulação temporal, dithering e espalhamento discreto de highlights. Um único passe, compatível com OpenGL; **não é bloom físico nem simulação completa de CRT/VHS**.
- F3 compara com a imagem sem óptica. UI é renderizada depois e permanece legível. F2 continua permitindo reduzir desconforto da câmera.

As capturas `captures/reform_0.png` a `reform_3.png` vêm do jogo real em OpenGL, não de montagem. Foram inspecionadas durante a implementação: céu saturado, arquitetura rural legível, estrada e marcos distinguíveis, sem dominante sépia ou bordas pretas de distorção.

## 5. Como trabalhar no editor

1. Abra **`scenes/universes/sitios.tscn`** para editar o universo. Na cena jogável instanciada, use **Editable Children** se precisar inspecionar seus filhos; prefira editar a cena original.
2. Mova `Terrain/Contours` para moldar as colinas: X/Z = centro, Y = amplitude, escala X/Z = extensão. `Terrain/Yards` define os patamares das casas. Mover uma casa exige ajustar seu patamar correspondente.
3. Edite os pontos X/Z de `Routes/*`, posições dos marcos e `Spawns/*`. Entradas recebem a altura exata do terreno; sua orientação continua autoral.
4. Apenas `Variations/*` muda por visita. Não coloque marcos essenciais nesse ramo. Máscaras **Familiar/Desvio** vinculam objetos e entradas às variantes.
5. Selecione **World → Atualizar prévia 3D** depois de mudar terreno, seed, visita ou regras. A mesma configuração é aplicada na prévia e no runtime. Os geradores individuais também oferecem **Atualizar geometria**.
6. Para prévia com iluminação completa, abra `scenes/exploration.tscn` (o universo isolado usa a iluminação de preview do editor). O pós-processamento e o FOV do perfil são aplicados no jogo; não são reproduzidos pela câmera livre do editor.
7. Para testar uma configuração, ative **Reproduce Visit**, informe **Session Seed** e **Visit Index** no World e execute a cena jogável. Desative a reprodução para voltar à progressão de visitas.

A prévia nunca move o jogador, captura mouse, toca áudio ou avança progresso. Elementos gerados têm ownership e metadata de reconstrução; atualizar/salvar/reabrir não acumula terreno, vegetação ou colisões. O teste de prévia cobre esse ciclo. Execute testes com `--editor` um por vez, sem outra instância importando os mesmos arquivos.

## 6. Validação executada

Godot **4.7.2**, Linux. Runtime em headless e capturas com Compatibility/OpenGL, NVIDIA RTX 3060.

| Teste | Resultado |
|---|---|
| `tests/exploration_test.gd` | **189 verificações, zero falhas**: determinismo, regras de seleção, quatro entradas, colisão, caminhada pelo circuito, entrada da casa, variantes, limites, corrida/salto nos muros, persistência isolada, pausa, interação E, fade e descarregamento/reentrada |
| `tests/authored_preview_test.gd` | **14 verificações, zero falhas**: geometria visível, ausência de gameplay, ownership, atualizações, variantes e salvar/reabrir |
| `tests/world_test.gd` | **449 verificações, zero falhas**: rural legado |
| `tests/balneario_test.gd` | **204 verificações, zero falhas**: Balneário legado |
| `tests/level_switch_test.gd` | **10 verificações, zero falhas**: atalhos, pausa e liberação de cenas legadas |
| `tests/exploration_capture.gd` | Quatro capturas reais geradas e inspecionadas; 185–316 draw calls e aproximadamente 78–82 mil primitivas reportadas nos quadros capturados (incluindo passes de renderização) |

O teste de caminhada leva cerca de dois minutos; aguarde sua conclusão. O contorno físico inteiro usa uma única malha/material e uma única colisão trimesh, em vez de centenas de draw calls de muros separados. O mapa não cresce com distância ou visitas.

**Ressalva do harness de editor:** as asserções de prévia passam, mas esta versão da Godot imprime avisos/erros de RIDs e objetos remanescentes ao encerrar uma `SceneTree` personalizada sob `--editor`. O teste de prévia legado também apresentou mensagens de diálogos ao carregar sua cena salva. Os testes de runtime e captura não apresentaram esses avisos. Não confundir o resultado das asserções com um encerramento de editor sem diagnósticos; a inspeção manual no editor continua recomendada.

## 7. Limitações e próximos passos objetivos

- A percepção de incerteza sem frustração exige playtest humano; testes de colisão e capturas não demonstram essa sensação subjetiva.
- O contorno é inteiramente físico, mas a capacidade de disfarçar o mapa finito precisa de revisão caminhando nas cristas. Não se afirma que todo ângulo esconda o muro.
- O atalho secundário admite contorno pelo pasto. Conexões topológicas impossíveis/interiores maiores não foram implementados.
- A passagem usa um breve fade e troca síncrona de cena; carregamento assíncrono de universos pesados fica para quando houver esses conteúdos. Não existem outros universos no fluxo novo para testar transição cruzada.
- Perfis de áudio/renderização são independentes por cena; não há mixer com crossfade de múltiplos universos residentes.
- Os sons continuam sintetizados e a memória de ecos das casas é de sessão, enquanto o progresso da porteira é persistente. Gravações próprias continuam sendo o próximo refinamento sonoro recomendado.
- A quantidade de draw calls foi medida, mas não há certificação de FPS em hardware de entrada. Testar em máquina-alvo antes de aumentar detalhe.
- Próximo marco: playtest cego do circuito/porteira, revisão das vistas junto aos limites e refinamento de pequenas diferenças da casa repetida. Só então migrar outro ambiente já definido no conceito.
