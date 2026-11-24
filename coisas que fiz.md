# Coisas que fiz no ERMO

Este texto explica um pouco do que desenvolvi no jogo e também reconhece algumas coisas que meus amigos fizeram. Tentei contar não só quais arquivos existem, mas também a ideia por trás de cada parte.

## Câmera com aparência de câmera digital antiga

Uma das coisas que fiz foi criar um visual inspirado nas câmeras digitais compactas e filmadoras dos anos 2000, como uma Cyber-shot. A ideia era que o jogo parecesse estar sendo gravado por uma câmera daquela época: a imagem fica um pouco mais suave e comprimida, com cores e exposição características, mas sem imitar uma fita VHS. Por isso, não usei os defeitos típicos de fita, como linhas de tracking.

O efeito é dividido entre alguns scripts. Em `CyberShotProfile.cs` ficam os perfis e os valores que controlam a câmera; durante o jogo, uso o perfil Dreamcore, que captura a imagem a 30 quadros por segundo. O `PixelDisplay.cs` faz a câmera renderizar primeiro para uma textura menor, normalmente com 480 linhas e proporção 4:3. Depois, essa imagem processada é mostrada na tela. Essa etapa é importante porque simula a própria captura da câmera, em vez de simplesmente colocar um filtro por cima da resolução normal do jogo.

O processamento visual acontece no shader `Assets/Ermo/Shaders/PixelLens.shader`. Nele, combinei suavidade de lente e compressão com efeitos discretos, como distorção nas bordas, vinheta, pequenas diferenças de cor e ruído de sensor. Também há uma aproximação da perda de detalhes e de cor que pode acontecer em uma câmera digital antiga. O `CyberShotCamera.cs` complementa isso simulando ajustes automáticos de exposição e balanço de branco conforme a iluminação da cena.

Também queria que a câmera parecesse estar sendo segurada por uma pessoa. Para isso, `HandheldCameraMotion.cs` acrescenta movimentos pequenos de respiração e postura, balanço durante os passos e a corrida e uma leve inércia quando o jogador olha para os lados. Mantive esses movimentos sutis para não atrapalhar a visão. Os scripts `ErmoBootstrap.cs` e `AtacarejoBootstrap.cs` configuram esse conjunto quando cada fase começa.

## Como fiz a fase dos sítios infinitos

A fase dos sítios está em `Assets/Ermo/Scenes/Sitios.unity`. Em vez de criar um mapa completo e enorme, fiz o mundo ser montado em pedaços conforme o jogador anda. Esses pedaços são chamados de *chunks*. O jogo carrega os que estão perto, prepara alguns por quadro e descarrega os que já ficaram para trás. Assim, dá para continuar explorando sem manter o mundo inteiro carregado ao mesmo tempo.

O `ProceduralWorld.cs` cuida desse carregamento ao redor do jogador. Quando um chunk precisa ser criado, `WorldData.cs` usa a seed do mundo e as coordenadas dele para decidir como será o terreno e o que vai aparecer ali. Como essa geração é determinística, as mesmas coordenadas e a mesma seed produzem o mesmo lugar. Isso ajuda a manter o mundo coerente quando o jogador se afasta e depois retorna; o sistema também guarda dados dos chunks em arquivos de mundo.

Os locais são distribuídos por `WorldLocations.cs`. Esse script escolhe posições para os sítios e seus objetos com regras de espaçamento, tenta evitar que fiquem sobre as estradas e resolve conflitos entre lugares próximos. Depois, `WorldGeometry.cs` transforma esses dados em cenário: cria a malha do chão e os caminhos e posiciona elementos como casas, torres d’água, turbinas e postes. O `WorldSettings.cs` reúne as configurações que controlam a geração, como seed, relevo, tamanho dos pedaços, distância entre locais e aparência do terreno.

Com essas partes trabalhando juntas, a fase consegue formar uma paisagem rural extensa e continuar gerando áreas enquanto a pessoa explora. A distribuição não é totalmente aleatória a cada visita: ela é baseada na seed, então o mundo mantém uma identidade própria.

## Fase do atacarejo

A fase do atacarejo está em `Assets/Ermo/Scenes/Atacarejo.unity` e foi feita por um amigo meu. Ela tem uma proposta diferente da área aberta dos sítios: o jogador explora um mercado fechado, escuro e com setores que aparecem conforme ele avança.

No projeto, `AtacarejoWorld.cs` coordena os setores carregados ao redor do jogador e a atmosfera escura. Também cuida da porta de saída que faz o jogador voltar ao começo, criando um loop, e da entrada que leva à próxima fase. `AtacarejoLayout.cs` define a organização dos setores, enquanto `AtacarejoBuilder.cs` monta cada um usando os modelos conectados por `AtacarejoKit.cs`. Os prefabs usados nessa construção ficam em `Assets/Ermo/Prefabs/Atacarejo/`: incluem partes da arquitetura, gôndolas, freezers, caixas, placas, paletes e outros objetos de mercado.

## Modelos feitos pelos meus amigos

Na fase da escola, um amigo fez os módulos 3D que formam os ambientes. Os arquivos ficam em `Assets/Ermo/Modelos/modelos_escola/` e incluem salas, corredores e cruzamentos, com variações identificadas pelas letras A, B e C. Também há versões de corredores com portas. Como são módulos separados, eles podem ser combinados para formar diferentes trechos do prédio.

Meus amigos também fizeram o modelo do ponto de ônibus, que está em `Assets/Ermo/Modelos/Ponto de onibus.blend`. Para a Rodoviária Infinita, eles fizeram o modelo da lanchonete, em `Assets/Ermo/Modelos/Modelo de lanchonete.blend`. Esses modelos ajudam a deixar os lugares mais reconhecíveis e dão personalidade aos cenários. A autoria dos modelos é deles; meu trabalho aqui foi reuni-los no projeto junto com as outras partes do jogo.
