# Elementos 3D no editor

## Fatia artesanal atual

Abra **`scenes/universes/sitios.tscn`** para editar o universo, ou **`scenes/exploration.tscn`** para vê-lo com a iluminação completa. Na cena instanciada, habilite **Editable Children** se precisar inspecionar seus filhos; prefira editar a cena original.

Selecione **World → Atualizar prévia 3D** depois de alterar os contornos/patamares do terreno, pontos das rotas, seed ou visita. Casas, marcos, zonas de vegetação, entradas e variantes são nós configuráveis na cena. Apenas a triangulação do terreno, estradas, muros e vegetação secundária é reconstruída; não é um gerador integral do mapa.

**Session Seed / Visit Index** controlam a prévia determinística. **Reproduce Visit** aplica esses valores ao executar `scenes/exploration.tscn`; desativado, usa a progressão persistida. Não execute o universo isolado com F6: ele não contém jogador/interface. A câmera livre do editor não simula os efeitos ópticos do jogo.

Os meshes gerados permanecem inspecionáveis, sem duplicação ao atualizar/salvar/reabrir. Não edite esses meshes diretamente: edite seus contornos, pontos e parâmetros. A prévia não toca áudio, move o jogador ou altera o progresso. Detalhes em [reformulacao.md](reformulacao.md).

## Protótipos legados por chunks

Abra `scenes/balneario_infinito.tscn` ou `scenes/main.tscn` na aba **3D**: o nó `World` gera uma prévia determinística de 3 × 3 chunks, sem iniciar o jogo.

Selecione `World` no Inspector:
- **Editor Preview Enabled**: ativa/desativa a prévia.
- **Editor Preview Radius**: limita a região mostrada (0 a 2, independente do raio do jogo).
- **Editor Preview Center**: escolhe a coordenada de chunk a inspecionar.
- **Atualizar prévia 3D**: refaz a região após alterar o resource `settings`, a seed ou as regras do gerador.

As cenas `scenes/balneario/pool.tscn`, `palm.tscn`, `slide.tscn` e `terraces.tscn` também mostram sua geometria isoladamente. Parâmetros exportados atualizam a geometria; o botão **Atualizar geometria** permite refazê-la manualmente.

Os nós gerados ficam disponíveis na árvore para inspeção. **São uma prévia procedural, não um bake autoral**: alterações manuais nesses nós são descartadas ao regenerar. Para alterações persistentes, edite os parâmetros do prop, os materiais, as cenas reutilizáveis ou as regras dos geradores. Props já configurados em `.tscn`, como casas e mesas, continuam sendo editados nas cenas de origem.

A prévia não carrega o mundo infinito inteiro, não movimenta o jogador nem executa scripts de áudio/gameplay. Ao jogar, os chunks de prévia eventualmente salvos são removidos antes do streaming normal; os props reconstruídos substituem sua geometria anterior, sem acumular colisões.
