# Balneário procedural contínuo

## Água, rádios e tobogãs

- Piscinas usam o modelo óptico do antigo **Water Level**: tons escuros, ondas suaves, Fresnel e brilho solar, sem mosaico ou cáusticas pintadas na superfície. Uma captura cúbica HDR local reflete estruturas e céu; atualiza a cada dois segundos, distribuída entre quadros. Não é SSR nem reflexão planar. Água não aparece na própria captura.
- Piscinas dos tobogãs grandes têm **2,60 m de água**, escadas com degraus de até 20 cm e flutuação automática. A lagoa de chegada e as piscinas infantis continuam rasas. O mergulho retorna à altura de nado.
- Nos tobogãs, a película de água é transparente, com reflexos Fresnel, ondulações irregulares e espuma discreta nas bordas/queda, sem listras brancas periódicas. A queda segue uma curva descendente até a piscina.
- Sem placas de texto ou dicas de controles durante a exploração; o menu de opções continua acessível.
- Cada mesa existente tem 18% de chance determinística por seed/posição de receber um rádio pequeno. Toca `Resources/Ermo/Audio/balneario.mp3`, em loop e com posição inicial variável, som 3D, faixa de 340–3100 Hz, compressão e distorção. O MP3 original em `Assets/Ermo/Audio` é preservado.
- No topo de cada tobogã, **E** inicia a descida deitada, acelerando conforme a inclinação, seguida de mergulho e recuperação da postura na piscina. O menu pausa a descida. Não há prompt de texto.
- Uma lâmina animada acompanha o fundo dos tobogãs e derrama na piscina, com ruído espacial próprio. Apoios da própria calha ficam abaixo dela; apoios que cruzariam outra descida são omitidos. No trecho intermediário, colunas de uma estrutura de tubulação superior atravessam as laterais do trajeto, mantendo o centro livre para deslizar.
- A validação batch também verifica início apenas no topo, pausa, término na água, restauração da câmera, ausência de placas e disponibilidade da música.

`Assets/Ermo/Scenes/Balneario.unity` é o segundo cenário. **Não é um parque fechado nem uma cópia repetida do parque inicial**: o mundo é amostrado por coordenadas, com setores gerados durante a exploração e descartados à distância. Não há cerca externa, portão delimitando a fase ou borda final navegável.

A passagem das caixas-d'água continua com o mesmo jogador, câmera e animação. A chegada é sempre no centro desobstruído da lagoa inicial: água a 0 m, fundo a −0,55 m e piso externo a 0,22 m.

## Geração e streaming

- `BalnearioLayout` combina seed e coordenada inteira do setor em um hash FNV-1a; usa um `System.Random` privado. A ordem de visita não altera a geração nem o RNG de Sítios.
- Cada setor mede **160 × 160 m**. As coordenadas são assinadas, incluindo o lado negativo dos eixos. Ao retornar a um setor descartado, ele é reconstruído com o mesmo conteúdo.
- Há quatro composições: lagoas orgânicas com cascata, piscinas familiares com brinquedos/fontes, conjuntos de toboáguas e jardins tranquilos com praça elevada. Posições, quantidade, formatos e tamanhos das piscinas variam; não se replicam cinco piscinas em posições fixas.
- As piscinas pertencem ao interior do setor e respeitam separação e acessos. Caminhos principais são amostrados em **coordenadas globais**, com extremidades compartilhadas, portanto continuam entre setores sem portões ou degraus na junção.
- `WaterWorld` mantém uma janela **5 × 5**, até 25 setores, priorizando os próximos. A chegada é construída primeiro; os demais avançam por etapas entre frames, inclusive durante o preload rural.
- Setores incompletos ficam ocultos. O setor sob o jogador é concluído antes de permitir uma lacuna de colisão, inclusive em teletransportes de depuração. Raízes, malhas e colisores distantes são liberados; materiais/texturas são compartilhados pela janela inteira.
- Uma **origem flutuante** recentraliza jogador e geometria após longos percursos, preservando o endereço lógico e evitando perda progressiva de precisão. A água conserva sua fase óptica e o deslocamento não conta como passo.
- Névoa própria do balneário acompanha a distância de setores prontos, escondendo o limite temporário do streaming. Não muda a atmosfera de Sítios.
- Reiniciar a exploração rural, inclusive com a mesma seed, prepara novamente a piscina inicial. Não reutiliza como chegada a posição distante da exploração anterior.

## Composição visual e referências

As oito fotos de `Assets/referencias/` orientam formas, cores e organização espacial. A pasta não contém modelos 3D ou mapas de textura separados. Geometrias e texturas repetíveis são geradas pelo código; as fotos não são coladas inteiras nas superfícies.

| Referências | Elementos usados |
| --- | --- |
| `1.jpg` | Piscinas sinuosas, passeios acompanhando suas bordas, ilhas verdes, agrupamentos de palmeiras e quiosques de palha. |
| `1.webp`, `2.jpg` | Toboáguas tubular laranja e aberto azul, apoios, juntas aparentes e torre metálica com escadas. |
| `3.jpeg` | Cogumelos com tampo arredondado azul e filetes de água; acabamento de pedra. |
| `4.jpeg`, `5.jpeg` | Vegetação tropical, cascata, mobiliário branco, guarda-corpos de madeira e praças locais elevadas com rampa. |
| `6.jpg`, `7.jpg` | Arcos coloridos, fontes e estrutura infantil com escorregador. |

O gramado é o fundo contínuo do mundo. Pavimentação fica nas faixas orgânicas junto às piscinas, nos caminhos curvos e nas praças de serviço — **não numa laje retangular cobrindo o cenário inteiro**. As faixas das piscinas substituem o gramado com recortes coincidentes, em vez de duas superfícies quase coplanares disputando profundidade. As palmeiras têm ráquis e folíolos separados; cadeiras têm encosto aberto; coberturas de palha têm inclinação e franja. Guichês, lanchonetes e áreas de sombra aparecem como paradas locais, sem uma entrada monumental repetida.

## Regras de distribuição

- A lagoa de chegada não recebe brinquedos ou decoração em seu centro.
- Piscinas têm recorte real no chão, fundo e paredes de mosaico separados, borda clara e escadas proporcionais à profundidade: quatro degraus nas rasas, quinze nas piscinas de tobogã, com espelhos de até **20 cm**.
- Cada saída tem um caminho reservado até o passeio principal. Candidatos de piscina que bloqueiam essas conexões são rejeitados.
- Mesas/sombrinhas e duplas de espreguiçadeiras são propostas nas bordas, voltadas para a água e com afastamentos variáveis, não em uma grade uniforme.
- As reservas são discos de **1,9 m** para grupos de mobiliário, **1,7 m** para palmeiras e **1,05 m** para arbustos. Grupos têm **0,65 m** adicionais entre si e mantêm a borda da piscina desobstruída. As copas podem se sobrepor acima da circulação, como nas fotos.
- Vegetação nasce em pequenos grupos fora dos passeios e das faixas pavimentadas das piscinas. Não há cortina de árvores seguindo um perímetro retangular.
- Torres, desembocaduras, quiosques, praças elevadas e rampas têm áreas reservadas antes da decoração. Guarda-corpos protegem somente desníveis e atrações, nunca os limites dos setores.
- Respingos de passos dependem da piscina do setor atual; caminhar no piso seco não reproduz passos na água.

## Testes

**Ermo → Validate Balneario Layout** verifica 128 seeds de chegada e 128 endereços distantes: determinismo, quatro temas, quantidades variáveis, coordenadas negativas, circulação, afastamentos e continuidade dos passeios nas bordas.

Em uma cópia isolada do projeto:

```
Unity -batchmode -nographics -projectPath <copia> -executeMethod Ermo.Editor.BalnearioValidation.RunBatch -logFile <log>
```

O teste físico verifica fundos sem tampa, chegada, saída caminhando, caminhada entre setores, rampa de praça elevada, viagem distante, descarregamento, origem flutuante e reconstrução da origem. Sem `-nographics`, também captura `Logs/ErmoBalnearioArrival.png` e `Logs/ErmoBalnearioOverview.png`; a vista aérea usa uma distância de névoa maior só para inspeção. O teste completo `ErmoPlayModeValidation.RunBatch` continua verificando a travessia rural → água, efeitos e captura Dreamcore.

Ainda não há natação livre, reflexos planares das construções, fluido físico ou controlador dedicado de descida nos toboáguas. Quiosques são cenográficos. A água dos reservatórios não foi alterada.
