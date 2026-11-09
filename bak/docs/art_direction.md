# Sítios Infinitos — direção e decisões

## Referências analisadas antes da implementação

As imagens fornecidas estão na raiz, não em `references/`. Foram preservadas nos caminhos originais.

**`liminal`** (imagem vertical, 236 × 421):

- Céu azul limpo ocupa aproximadamente 42% do quadro. Não é uma cena noturna ou de entardecer.
- Morros baixos e arredondados, com intervalos regulares, formam camadas que ocultam parcialmente o próximo vale. Não há montanhas rochosas.
- Casas claras, muito pequenas diante do terreno. A mais próxima ocupa aproximadamente 15% da largura; as seguintes encolhem rapidamente e pontuam diferentes alturas.
- Grandes áreas verdes contínuas, sem matas densas, plantações organizadas ou cercamentos dominantes.
- Caminho claro e estreito, sinuoso. Ele conecta visualmente as camadas, mas não explica o destino.
- Luz lateral, sombras legíveis, verdes amarelados e céu saturado. A familiaridade agradável é parte do desconforto.
- A repetição e o isolamento de cada construção são mais importantes que seus detalhes.

**`nordeste.jpe`**: reboco envelhecido, porta azul, cobertura cerâmica baixa, pilares finos, varanda profunda e piso elevado de cimento. Foi usada para a casa, não para a densidade nem para a composição geral.

## Tradução para o protótipo

- Resolução interna 640 × 360, ampliada em múltiplos inteiros; câmera a 1,62 m, FOV vertical de 62°. O quadro é horizontal, portanto não reproduz literalmente o recorte vertical da referência.
- Sol fixo, céu sem animação e neblina leve. Nenhum ciclo de tempo ou movimento humano.
- Ondulações analíticas sobrepostas, com escalas de aproximadamente 20–90 m; amostragem poligonal de 4 m. Patamares suaves dão apoio às casas.
- Uma sequência de casas espaçadas em 96 m acompanha o caminho. Células laterais ficam majoritariamente vazias; pares são raros. Uma árvore ocorre em apenas 13% dos chunks.
- Casa de 7,4 m, telhado cerâmico original, alpendre aberto e cômodo vazio acessível. Nada de celeiros americanos ou decoração abundante.
- Pixelização sem dithering de tela; snapping subpixel moderado apenas nos materiais de reboco, telha e madeira. `wobble = 0` desativa esse efeito nos materiais.

## Comparação visual realizada

`tests/capture.gd` renderiza o jogo real e grava `captures/01_arrival.png` e `captures/02_alpendre.png`. A comparação levou à revisão do relevo, do ponto inicial, dos patamares das casas, da saturação do céu e à criação da continuação distante do terreno.

O horizonte fica próximo de 44% da altura; a casa mais próxima fica em torno da faixa central inferior. O caminho e as casas atravessam sucessivas encostas, sem ocupar todo o espaço vazio. Os modelos simplificados e a textura de capim são uma interpretação poligonal, não uma reprodução fotográfica. A aprovação subjetiva da atmosfera ainda depende de exploração pelo jogador.

## Áudio e limitações conhecidas

Os oito WAVs foram sintetizados especificamente para o protótipo, com código em `tools/build_assets.py`. São placeholders; pássaros e latidos ainda não têm a naturalidade de gravações de campo. Substituí-los por gravações próprias/licenciadas é a principal melhoria sonora recomendada. Não há música, áudio em loop permanente nem sustos.

A malha distante é apenas visual, mais grosseira e sem casas próprias. Casas e colisões são carregadas na região próxima; a neblina e o terreno distante reduzem a exposição das bordas, mas ainda vale validar o aparecimento de detalhes em diferentes encostas e hardware. A origem flutuante mantém a física perto de zero; coordenadas lógicas extremamente grandes continuam sujeitas aos limites numéricos da engine.
