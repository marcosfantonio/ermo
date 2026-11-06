# AGENTS.md — ERMO

## Visão geral do projeto

**ERMO** é um jogo de terror liminal em primeira pessoa, com estética poligonal retrô inspirada na era PS1, ambientado em cenários cotidianos brasileiros, especialmente do Nordeste.

O projeto prioriza:
- atmosfera
- estranheza espacial
- exploração
- observação de anomalias
- progressão ambiental e narrativa

`CONCEPT.md` é a fonte de verdade criativa. Não inventar universos ou fixar as decisões narrativas ainda abertas nesse documento. A direção técnica atual prioriza universos independentes e espaços artesanais finitos; os geradores infinitos existentes são protótipos legados, não a arquitetura-alvo.

O objetivo **não** é criar um “clone genérico de Backrooms”, e sim um jogo com identidade própria, usando arquitetura, paisagem, clima e memória coletiva brasileira como diferencial central.

---

## Foco atual do desenvolvimento

O desenvolvimento inicial deve se concentrar **apenas** na primeira fase:

# Fase 1 — Sítios Infinitos

### Conceito
Um ambiente rural brasileiro de aparência comum:
- morros verdes
- casas simples com alpendre
- cercas
- estradas de terra
- árvores isoladas
- postes
- silêncio e sensação de isolamento

Esse espaço deve parecer familiar, porém progressivamente estranho, repetitivo e espacialmente impossível.

### Objetivo do protótipo
Construir uma vertical slice curta, jogável, que demonstre:

- movimentação em primeira pessoa
- estética retrô de PS1
- espaço planejado que sugere infinitude, com percursos reconectados
- ambientação rural brasileira
- sensação liminal
- pelo menos uma anomalia ambiental
- base para futura expansão narrativa

---

## Princípios criativos obrigatórios

### 1. Familiaridade antes do terror
O cenário deve primeiro parecer reconhecível e cotidiano.  
O desconforto vem do excesso de repetição, silêncio, geografia impossível e pequenas distorções.

### 2. Terror atmosférico antes de sustos
Evitar depender de jumpscares ou perseguições constantes.  
A fase deve ser sustentada por:
- som ambiente
- solidão
- repetição
- mudanças sutis
- sensação de presença

### 3. Estética brasileira autêntica
As referências visuais devem se inspirar em:
- sítios do interior do Brasil
- casas simples com alpendre
- telhados cerâmicos
- vegetação rural
- estradas de terra
- terrenos ondulados
- postes, cercas, caixas d’água, cadeiras de plástico, varandas

Não usar estética rural genérica americana como base principal.

### 4. PS1 como linguagem, não caricatura
A estética retrô deve parecer intencional e atmosférica, não apenas “feia”.
Usar:
- low poly
- texturas de baixa resolução
- iluminação simples
- neblina
- leve instabilidade visual
- baixa resolução interna opcional
- pixelização / dithering quando adequado

A direção atual é **dreamcore tropical**: céu azul intenso, vegetação viva, contraste quente/frio e saturação hierarquizada. Óptica analógica deve complementar geometria e materiais, não substituir a direção de arte por um filtro VHS.

Evitar exageros que prejudiquem a legibilidade.

---

## Stack e ferramentas

### Engine
- **Godot 4.x**

### Linguagem principal
- **GDScript**

### Estrutura de produção
Sempre que possível:
- configurar cenas diretamente no editor
- criar nós, hierarquias, resources e scenes reutilizáveis
- evitar resolver tudo apenas por script

O projeto deve aproveitar o editor da Godot, e não funcionar como um “motor custom feito só em GDScript”.

### Visibilidade 3D no editor — obrigatória em toda alteração
- Em toda alteração, verificar que os elementos 3D afetados continuam visíveis e inspecionáveis no editor da Godot, sem precisar executar o jogo.
- Preferir cenas `.tscn` reutilizáveis, com meshes, materiais, colisões e hierarquias configuráveis pelo Inspector. Não entregar elementos visuais que existam apenas em `_ready()` durante o jogo.
- Quando a geometria precisar ser procedural, fornecer prévia com `@tool`, parâmetros exportados e atualização explícita no Inspector. Manter os nós gerados inspecionáveis na árvore da cena e usar as mesmas regras/seeds da geração em runtime.
- A prévia deve ser limitada e segura: não capturar o mouse, mover o jogador, tocar áudio ou executar lógica de gameplay no editor. Não duplicar geometria nem colisões ao atualizar, salvar/reabrir ou iniciar o jogo.
- Nas fases atuais, selecionar `World` e usar **Atualizar prévia 3D** após alterar a seed, o perfil de mundo ou as regras de geração. Os props procedurais possuem **Atualizar geometria** e atualizam seus parâmetros exportados no editor.
- Ao concluir qualquer alteração, informar como visualizar o resultado no editor e validar a prévia e o funcionamento em runtime. Caso uma validação não possa ser executada, declarar essa limitação.

---

## Regras técnicas obrigatórias

### 1. Modularidade
Tudo deve ser pensado de forma modular e reutilizável:
- casas modulares
- props modulares
- terreno segmentado
- sistema de chunks
- anomalias desacopladas

### 2. Construção artesanal e variação com consistência
Cada universo tem geometria, marcos, percursos, entradas e limites físicos planejados. Proceduralidade é complementar: vegetação e detalhes secundários, nunca preenchimento integral do mapa.

- Selecionar entradas entre `Marker3D` válidos, não coordenadas aleatórias.
- Variar apenas elementos explicitamente autorizados entre visitas.
- Preservar marcos reconhecíveis e pistas suficientes para exploração compreensível.
- Reproduzir configurações com perfil, seed de sessão e índice de visita.
- Ocultar limites por arquitetura, geografia e atmosfera coerentes com o universo; não depender de paredes invisíveis.
- Preservar os geradores legados enquanto houver dependências e testes; não os usar como requisito para novos cenários.

### 3. Performance antes de excesso de detalhe
Priorizar:
- baixo custo de renderização
- poucos triângulos
- instanciamento eficiente
- distância de visão controlada por neblina
- quantidade limitada de objetos por chunk

### 4. Escopo pequeno
Não tentar construir o jogo inteiro agora.

O protótipo atual deve focar apenas em:
- um cenário
- um tipo de exploração
- uma base técnica sólida
- uma atmosfera forte

### 5. Sem assets copiados
Não reutilizar assets protegidos por copyright de outros jogos.
Pode usar placeholders, assets próprios ou referências visuais apenas como inspiração.

---

## Estrutura desejada do protótipo

### Jogador
- movimentação em primeira pessoa
- andar
- correr (opcional)
- colisão estável
- câmera suave
- head bob / camera shake sutil e realista

### Câmera
A câmera deve transmitir presença física.

Implementar:
- sway leve
- bob ao caminhar
- impacto suave ao pousar
- pequenos offsets de movimento
- shake contextual opcional

Evitar câmera exageradamente tremida.  
A sensação desejada é **corpo físico presente**, não câmera caótica.

### Mundo
O cenário deve incluir:
- morros suaves
- estrada(s) de terra
- cercas
- vegetação esparsa
- algumas casas com alpendre
- postes / pequenos marcos de orientação
- sensação de vastidão

### Composição espacial
O trecho artesanal deve combinar:
- terreno
- estrada
- casa
- cerca
- árvore
- poste
- pequeno detalhe ambiental

As regras devem priorizar plausibilidade visual, composição intencional e incerteza espacial controlada, não aleatoriedade total. `scenes/exploration.tscn` é a entrada atual; `scenes/universes/sitios.tscn` contém a construção editável.

### Anomalias
O protótipo deve conter pelo menos uma anomalia, por exemplo:
- mesma casa aparecendo em lugares improváveis
- casa com orientação incoerente
- objeto duplicado
- som vindo de casa vazia
- luz acesa sem origem aparente
- estrada que parece levar adiante mas retorna ao mesmo padrão

Anomalias devem ser discretas e perturbadoras, não chamativas demais.

---

## Direção de arte

### Visual
Inspirado em survival horror e jogos 3D do fim dos anos 90:
- geometria low poly
- textura simples
- paleta tropical com cores vivas e saturação hierarquizada
- céu expressivo
- pôr do sol ou fim de tarde como opção estética forte
- neblina para reduzir horizonte e reforçar o mistério

### Elementos rurais brasileiros importantes
Preferir incluir:
- alpendres
- telhas cerâmicas
- piso de cimento grosso
- cadeira de plástico
- cerca de madeira / arame
- caixas d’água
- pequenas plantações ou capim irregular
- morros verdes ou vegetação semi-rural
- sensação de interior brasileiro

---

## Som e atmosfera

O áudio é parte central da experiência.

Priorizar:
- vento
- insetos
- pássaros distantes
- cachorro ao longe
- ruídos indefinidos
- silêncio prolongado
- som ambiente localizado em casas vazias

Evitar música constante.  
Se houver trilha, deve ser mínima e pontual.

---

## Lore no estágio atual

Neste momento, **não** desenvolver a lore completa.  
O foco atual é apenas estabelecer:
- sensação de lugar estranho
- possibilidade de passagens
- ideia de que “há algo ali”

Pode haver pistas visuais ou sonoras, mas sem sobrecarregar o protótipo com narrativa explícita.

---

## Como o agente deve trabalhar

Ao implementar qualquer sistema:

1. pensar primeiro na solução mais simples que funcione
2. manter organização de cena e scripts
3. evitar scripts gigantes monolíticos
4. documentar decisões importantes
5. explicar claramente o que foi criado
6. sugerir próximos passos objetivos

Sempre que possível, o agente deve:
- criar cenas reutilizáveis
- nomear arquivos de forma clara
- separar lógica por responsabilidade
- manter código legível

---

## O que evitar

- escopo excessivo
- mecânicas demais no primeiro protótipo
- jumpscare barato como base do terror
- procedural totalmente caótico
- exagero de efeitos visuais
- copiar diretamente outros jogos
- resolver tudo em um único script
- ignorar o editor da Godot

---

## Meta do primeiro marco

Entregar um protótipo jogável da fase **Sítios Infinitos**, contendo:

- exploração em primeira pessoa
- visual retrô estilo PS1
- cenário rural brasileiro artesanal, finito, com sensação de infinitude
- câmera com movimento físico sutil e realista
- pelo menos uma anomalia ambiental
- base técnica sólida para futuras fases

---

## Critério de sucesso

O protótipo estará no caminho certo se, em poucos minutos de jogo, o jogador sentir:

> “Isso parece um lugar comum do interior do Brasil...  
> mas alguma coisa está profundamente errada.”
