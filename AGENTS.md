# AGENTS.md — ERMO

## Visão geral do projeto

**ERMO** é um jogo de terror liminal em primeira pessoa, com estética poligonal retrô inspirada na era PS1, ambientado em cenários cotidianos brasileiros, especialmente do Nordeste.

O projeto prioriza:
- atmosfera
- estranheza espacial
- exploração
- observação de anomalias
- progressão ambiental e narrativa

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
- geração procedural de espaço “infinito”
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

---

## Regras técnicas obrigatórias

### 1. Modularidade
Tudo deve ser pensado de forma modular e reutilizável:
- casas modulares
- props modulares
- terreno segmentado
- sistema de chunks
- anomalias desacopladas

### 2. Procedural com consistência
A fase deve usar **geração procedural baseada em chunks determinísticos**.

Isso significa:
- o mundo é dividido em chunks
- chunks são gerados ao redor do jogador
- chunks fora do raio são descarregados
- se o jogador voltar, o chunk reaparece igual
- a seed do chunk deve depender das coordenadas dele

Objetivo:
- manter a sensação de infinito
- preservar coerência espacial
- permitir repetição com pequenas variações controladas

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

### Procedural
Cada chunk deve poder conter combinações de:
- terreno
- estrada
- casa
- cerca
- árvore
- poste
- pequeno detalhe ambiental

As regras devem priorizar plausibilidade visual, não aleatoriedade total.

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
- paleta levemente dessaturada
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
- cenário rural brasileiro procedural e “infinito”
- câmera com movimento físico sutil e realista
- pelo menos uma anomalia ambiental
- base técnica sólida para futuras fases

---

## Critério de sucesso

O protótipo estará no caminho certo se, em poucos minutos de jogo, o jogador sentir:

> “Isso parece um lugar comum do interior do Brasil...  
> mas alguma coisa está profundamente errada.”
