# Balneário Infinito

## Referências

- `balneario1`: pedra em tons quentes, espiral amarela, guarda-sóis vermelhos/amarelos/verdes, cadeiras de plástico, canteiros, palmeiras e cogumelo vermelho.
- `balneario2`: piscina ampla de azulejos azuis, cascata em rampa com escada lateral, céu de dia e vegetação cercando as áreas de lazer.
- Tradução PS1: piscinas poligonais de aproximadamente 32–40 segmentos, copas facetadas, guarda-sóis de oito lados, materiais simples, padrões pixelados e resolução interna de 640 × 360. Não se pretende reproduzir as fotos literalmente.

## Organização e geração

`balneario_infinito.tscn` herda a interface, jogador e controles do cenário existente. Substitui o gerador e o ambiente e desliga os morros distantes. O gerenciador compartilhado mantém fila por proximidade, orçamento de construção por quadro, descarga e origem flutuante; suas fábricas permitem trocar o cenário sem duplicar o streaming.

Cada chunk contém um pátio e uma piscina central. Os pátios alternam entre **0, 1,6 e 3,2 m**, com seleção determinística por coordenadas absolutas em `balneario_layout.gd`. Um anel de circulação na cota zero acompanha as divisas exatas dos chunks. Rampas largas e escadas paralelas ao norte e ao sul ligam esse anel ao pátio, sem degraus inesperados entre chunks vizinhos.

As piscinas principais alternam entre **oval, oval menor, quadrada de 24 × 24 m e retangular de 25 × 28 m**. O mesmo módulo `pool.tscn` expõe formato, dimensões, dimensões do deck e presença de muretas no Inspector. As amostras incluem os cantos exatos dos contornos retangulares, evitando triângulos ausentes no piso e nas divisas.

Cada chunk tem ainda três piscinas inferiores: uma pequena oval, uma retangular de lazer e uma faixa de banho estreita em uma das antigas passagens laterais. Bancos, mesas com guarda-sol e canteiros compõem as praças. O piso base é recortado antes de inserir os decks e as bacias, incluindo as faixas laterais; não se coloca apenas uma lâmina de água sobre uma laje sólida. As piscinas adicionais reutilizam a profundidade rasa e os acessos do módulo principal, sem multiplicar os emissores de áudio.

`access_footprints()` reserva faixas exclusivas incluindo corrimãos. Os acessos principais terminam antes da divisa, deixando aproximadamente sete metros entre rampas opostas de chunks vizinhos. Rampas e escadas dos terraços laterais têm faixas separadas e espaço livre ao pé. As praças inferiores são verificadas contra reservas do próprio chunk e dos oito vizinhos, com margem de 1,5 m, antes de qualquer corte no chão. Testes com coordenadas positivas e negativas verificam também o espaçamento entre acessos e a correspondência entre as reservas e as malhas construídas.

As áreas de mesas ocupam terraços laterais **1,6 m acima do pátio**, com acessos nas duas extremidades, arrimos revestidos, guarda-corpos brancos e canteiros densos. Muretas curvas com capeamento acompanham as laterais das piscinas sem fechar as entradas na água. Os revestimentos bege/marrom em losangos, as palmeiras em canteiros elevados e os postes seguem os elementos verticais das fotografias. As escadas de circulação têm degraus visuais e uma envoltória de colisão inclinada; o jogador pode subir e descer sem saltar. Isso não adiciona mecânica de deslizar aos toboáguas.

`scenes/balneario/terraces.tscn` expõe altura e dimensões. `balneario_terraces.gd` agrupa paredes, pisos, corrimãos e vegetação em malhas por material, em vez de criar um corpo físico por bloco. As escadas de circulação usam pisos de 28 cm e espelhos de até 18 cm; as rampas continuam longas para evitar inclinações excessivas. Bases em volume fechado preenchem os acessos, incluindo faces laterais e inferiores. Degraus decorativos não duplicam a colisão das rampas. Patamares de transição têm colisão própria, sem ressaltos que prendam o jogador ao descer.

Os prismas dos acessos são indexados antes de entrar no lote de caixas indexadas: sem isso, `SurfaceTool.append_from()` podia acrescentar vértices sem faces referenciadas, deixando as laterais invisíveis. A correção mantém faces externas opacas com orientação correta, sem recorrer a transparência ou material de dupla face para esconder o problema.

Muros altos e fundações usam `balneario_rock.gdshader`: pedra irregular bege/cinza com juntas escuras, recriada proceduralmente a partir do muro à esquerda de `balneario1`. O revestimento em losangos permanece nas muretas baixas e canteiros. Todos os pisos transitáveis compartilham o shader de pedra do pátio inferior. O topo das fundações fica abaixo do piso para impedir sobreposição e exibição do revestimento errado no andar superior.

Variações de raio da piscina, guarda-sóis e vegetação usam apenas seed global e coordenadas absolutas. Os tipos de atração alternam por paridade da célula, evitando distribuição caótica. As passagens nas divisas permanecem livres. Os shaders arquitetônicos usam coordenadas locais para não mudar durante deslocamentos da origem. VHS, saturação e balanço da câmera continuam ativos e configuráveis pelos controles existentes.

Piscina, palmeira e toboágua usam cenas com scripts geométricos pequenos e parâmetros exportados; guarda-sol/mesa e cogumelo são hierarquias editáveis no editor. `balneario_geometry.gd` concentra apenas auxiliares de malha, material e colisão, incluindo prismas fechados para rampas. A cascata tem fundação fechada sob a lâmina de água; a espiral tem seção em U com espessura, fundo, faces externas e tampas nas extremidades. As escadas das atrações também têm base preenchida, corrimãos e envoltória de colisão suave. Não há simulação de passeio de toboágua.

A água usa transparência parcial para revelar o fundo azulejado, normais com ondulações sobrepostas, brilho especular e variação angular tipo Fresnel. Cáusticas animadas aparecem no piso submerso e na superfície; a cascata usa fluxo rápido e faixas de espuma. Sem refração ou reflexos de tela: mantém compatibilidade com OpenGL/Compatibility e resolução PS1, sem exigir renderização adicional da cena.

## Rádio distante

Após **256 m** da origem, cada chunk tem uma chance de **1 em 14** de conter um rádio. A elegibilidade e o sorteio usam seed e coordenadas do chunk; o mapa é estável ao descarregar, revisitar ou reiniciar. O rádio aparece numa das mesas de guarda-sol já geradas, nunca cria uma mesa à parte. Digitar **RADIO** busca a mesa vazia mais próxima (até 100 m) entre os chunks carregados e anexa o mesmo aparelho a ela. O comando não duplica rádios numa mesa ocupada.

`balneario.mp3` toca em um `AudioStreamPlayer3D` com atenuação espacial, até 115 m, para ser ouvido antes de encontrar a mesa; o volume aumenta naturalmente ao se aproximar. Um bus exclusivo aplica filtro passa-altas (320 Hz), passa-baixas (3,1 kHz) e leve overdrive, aproximando o som de um alto-falante de rádio portátil sem alterar música/ambiente do restante do jogo. O stream é repetido durante a reprodução e interrompido quando o chunk sai do raio carregado.

## Exploração e atmosfera

O jogador pode atravessar os pátios, entrar nas piscinas rasas e sair pelas rampas azuis em suas duas extremidades longitudinais. Rampas evitam depender de subida automática de degraus do CharacterBody3D. As atrações são cenográficas: não há simulação de toboágua ou natação.

A repetição dos setores e da placa PISCINA 01 é a anomalia ambiental inicial. O sol não avança; não há visitantes, perseguições ou jumpscares. Cada piscina emite um loop original de ruído filtrado de água, compartilhado em memória e atenuado por distância. O áudio rural existente fornece vento e chamados espaçados.

## Verificação

`tests/balneario_test.gd` verifica regeneração de módulos/cores, presença dos três níveis, colisão no spawn e na piscina, saída pela rampa, subida e descida de rampas/escadas do pátio e do terraço, caminhada sobre divisas, limite de chunks, descarga, retorno, origem flutuante, movimento de câmera e controles VHS. Também verifica escadas compactas, shader compartilhado pelos pisos, pedra nos muros altos e fechamento das malhas da fundação da cascata e da espiral (cada aresta compartilhada por duas faces). `tests/world_test.gd` mantém a regressão da fase rural.

`tests/balneario_capture.gd` gera capturas reais em `captures/balneario_arrival.png` e `captures/balneario_overview.png`. Precisa de sessão gráfica.

Também captura `captures/balneario_lower_plaza.png` e `captures/balneario_square_pool.png` para avaliar as novas praças e a variação de formas.

Próximo refinamento: avaliar densidade da vegetação e proporções das atrações durante uma caminhada mais longa, antes de acrescentar novas mecânicas. A construção ainda ocorre na thread principal e o orçamento só pode ser respeitado entre chunks; por isso, custos de geração devem continuar pequenos.
