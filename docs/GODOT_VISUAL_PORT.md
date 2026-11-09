# Port visual Godot → Unity

> **Ajustes posteriores — perfil atual:** a base portada continua disponível, mas o padrão usa found footage de intensidade intermediária: até 540 linhas com ampliação pixelada, grão 0,014 em espaço de exibição, dither 0,08, saturação 1,22 e pequenas imperfeições analógicas. Os contornos têm altura ×0,65 e largura ×1,65, normais suaves e deformação orgânica por seed. Para comparar a fórmula original do terreno, use ambas as escalas em 1, desligue `Organic Hills` e `Smooth Terrain Normals`. O histórico abaixo explica a referência Godot, não os novos valores padrão.

## O que está realmente ativo no Godot

`project.godot` inicia `scenes/exploration.tscn`, não o gerador infinito legado. Essa cena usa `scenes/universes/sitios.tscn`, `sitios_dreamcore.tres` e `memory_optics.tscn`. Separar isso dos protótipos antigos evita combinar filtros que a referência não usa ao mesmo tempo.

| Referência em `godot/ermo` | Funcionamento | Port Unity |
| --- | --- | --- |
| `scripts/universes/authored_terrain.gd` e `scenes/universes/sitios.tscn` | Soma de sete colinas `altura × exp(-2d²)`, distâncias elípticas; pátios nivelados; grade 4 m; triângulos com normais facetadas | `GodotTerrain.cs`, `WorldSampler.Height/MeshHeight`, `WorldLocations.Yards`, array `Contours` no asset |
| `scripts/world/landscape.gd` (legado) | Quatro ondas senoidais com fase de seed; curva da estrada com duas ondas; nivelamento a 10–28 m de casas | Opção `Godot Legacy Waves`; curva da estrada usada na continuação procedural; nivelamento preserva grupos de 150 m |
| `shaders/terrain.gdshader` | Cor por vértice × textura de grama nearest, mistura 40% com ganho 1,65 e patches senoidais 7%; Lambert sem specular | `Meadow.shader`, UV global × 0,24, cores `(0,32; 0,59; 0,16)` e variação da cena artesanal |
| `shaders/retro.gdshader` | Texturas nearest/repeat, tint, escala de UV e snap em NDC numa grade 320×180 com força 0,18 | Mesmo cálculo nos vértices de props em `Meadow.shader`; grama/estrada não oscilam e a colisão permanece intacta |
| `shaders/sky.gdshader` | Gradiente azul por altura; plano projetado da direção do olho; ruído `(1,4; 5)` + ruído ×10; máscaras 0,79–0,88 | `DreamSky.shader`: mesmas fórmulas e cores, sem os antigos puffs esféricos |
| `environment/sitios_dreamcore.tres` | Fog exponencial 0,0035; cor `(0,35; 0,64; 0,82)`, energia 0,65, sky affect 0,04, perspectiva aérea 0,3; ambiente frio e sol quente | `GodotAtmosphere.cs` e `Atmosphere.hlsl`; alternativa `Horizon Only` preserva o pedido anterior de fog apenas no fim |
| `project.godot` | Viewport 640×360, janela 1280×720, nearest e integer stretch | Dois RenderTextures 640×360 (cena → óptica), imagem final nearest com escala inteira e letterbox; F4 muda para nativo |
| `shaders/memory_optics.gdshader` | Barrel 0,045 com overscan, cromática 0,35 px, grão 0,045 a 18 Hz, dither 2×2/31 a 0,28, vinheta 0,16, highlight spread 0,09 | `PixelLens.shader`, executado no framebuffer interno antes de ampliar. F3 desliga o efeito sem mudar resolução |
| `shaders/vhs.gdshader` (legado) | Jitter horizontal/tracking a 24 Hz, RGB bleed, saturação 1,45, contraste 1,06, scanlines e grão | Ramo opcional `Legacy Vhs` no mesmo shader, **desligado por padrão**, pois não é o filtro da cena atual |

As texturas `grass.png`, `plaster.png`, `wood.png` e `roof.png` são cópias dos assets presentes nessa versão Godot, não aproximações com ruído. Seus imports Unity usam sRGB, nearest e repeat. Os arquivos Godot foram mantidos sem alterações.

## Adaptações deliberadas

- Não é uma conversão integral da cena artesanal: não migra casas, cercas, áudio, anomalias, universos ou narrativa. Os modelos e o streaming Unity existentes continuam em uso.
- Os sete contornos são estendidos em cópias vizinhas de 300×276 m para manter a exploração infinita solicitada para Unity; a versão Godot atual é finita. O centro mantém os mesmos parâmetros de relevo, mas a continuação e a posição das casas não são o mesmo mapa.
- Pátios seguem as casas dos chunks Unity, não os quatro markers fixos do mapa finito. Terreno, props e colisão usam o mesmo campo; estradas usam a altura real dos triângulos para não atravessar o chão.
- O modo de contornos é autoral: trocar a seed muda as localizações, mas não randomiza os sete contornos. Os modos senoidal e Perlin também variam o relevo com a seed.
- O fog Godot por distância começa antes do horizonte, por definição. Ele é o perfil de referência ativo; a opção de menu permite retornar ao fog que só começa nos últimos 15% da visão, sem recriar o mundo.
- Lambert, sombras, espaço de cor e a implementação de perspectiva aérea diferem entre GL Compatibility e URP. O port mantém parâmetros e equações dos shaders próprios; a equivalência de iluminação/fog do engine é uma aproximação, não uma promessa de pixels idênticos.
- Efeitos de tela rodam após a câmera de mundo e antes da câmera de saída; a interface não recebe o filtro. A câmera Display 1 continua presente também fora do Play.

## Inspeção e validação

- **Ermo → Refresh 3D Preview**: nove chunks temporários e inspecionáveis; repetir atualiza sem duplicar. Não muda os parâmetros de ambiente da cena, grava dados do mundo ou inicia jogador/áudio.
- **Play**: conferir céu, fog e óptica completos. F3 compara efeito ligado/desligado; F4 compara 640×360/nativo; o menu compara fog Godot/horizonte.
- **Ermo → Validate Generation**: contornos gaussianos, nivelamento, interpolação da colisão, bordas entre chunks, determinismo, separação e combinações, persistência e shaders.
- Batch Play Mode: também verifica a prévia, o framebuffer, a saída Display 1 e as alternativas nativa/horizonte, salvando capturas em `Logs/`.

O relevo orgânico e a distribuição mais contínua usam `layout-6-organic-sites`; os anteriores `layout-5-soft-hills` e `layout-4-godot` são preservados. Nenhum save anterior é apagado ou reaproveitado com alturas incompatíveis. Os sons de brisa e passos foram reutilizados de `assets/audio`, com ajuste de nível nas cópias Unity.
