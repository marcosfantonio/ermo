# Modelagem modular do Atacarejo.
# Roda com:
#   blender --background --python gerar_atacarejo.py -- piso
#   blender --background --python gerar_atacarejo.py -- todos
#
# 1 unidade = 1 metro. Malha em Shade Flat, sem subdivisão.
# Exportação FBX: Scale 1, Apply Scalings "All Local" (enum FBX_SCALE_NONE),
# Forward -Z, Up Y, Apply Transform, só Mesh.
# Eixos: o exportador manda Blender +Y para FBX -Z, e a Unity converte o FBX
# para mão esquerda espelhando X. Resultado: Unity X = -Blender X,
# Unity Y = Blender Z, Unity Z = -Blender Y. As funções abaixo recebem medidas
# em coordenadas da Unity e fazem essa conversão (FLIP). A Unity confere a
# orientação em Ermo/Rebuild Atacarejo Prefabs (frente da placa 13 em +X).

import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

ERMO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = os.path.join(ERMO, "Modelos", "Atacarejo")
FBX_DIR = os.path.join(ERMO, "Modelos", "Atacarejo")
KIT_PATH = os.path.join(MODEL_DIR, "kit.json")

TOL = 5e-4
FLIP = True


def bx(x):
    return -x if FLIP else x


def bz(z):
    return -z if FLIP else z


def hex_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) / 255.0 for i in (0, 2, 4))


def make_material(name, hex_color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    color = (*hex_rgb(hex_color), 1.0)
    mat.diffuse_color = color
    nodes = mat.node_tree.nodes
    principled = nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Metallic"].default_value = 0.0
        principled.inputs["Roughness"].default_value = 0.9
    return mat


def prepare_scene():
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    collection = bpy.data.collections.get("Atacarejo")
    if collection is None:
        collection = bpy.data.collections.new("Atacarejo")
        scene.collection.children.link(collection)
    return collection


def purge():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)


def box(bm, x0, x1, y0, y1, z0, z1, material):
    """Caixa em coordenadas da Unity: X largura, Y altura, Z profundidade."""
    if x1 < x0 or y1 < y0 or z1 < z0:
        raise RuntimeError("caixa invertida")
    if (x1 - x0) <= 0 or (y1 - y0) <= 0 or (z1 - z0) <= 0:
        raise RuntimeError("caixa degenerada")
    before = set(bm.faces)
    geom = bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, verts=geom["verts"], vec=(x1 - x0, z1 - z0, y1 - y0))
    bmesh.ops.translate(
        bm,
        verts=geom["verts"],
        vec=(bx((x0 + x1) * 0.5), bz((z0 + z1) * 0.5), (y0 + y1) * 0.5),
    )
    for face in bm.faces:
        if face not in before:
            face.material_index = material
            face.smooth = False


def lathe(bm, rings, segments, material_of_band):
    """Sólido de revolução. rings = [(y, raio)]. Eixo Y da Unity."""
    columns = []
    for y, radius in rings:
        column = []
        for index in range(segments):
            angle = math.tau * index / segments
            column.append(bm.verts.new((bx(radius * math.cos(angle)), bz(radius * math.sin(angle)), y)))
        columns.append(column)
    for band in range(len(rings) - 1):
        material = material_of_band[band]
        for index in range(segments):
            nxt = (index + 1) % segments
            face = bm.faces.new(
                (
                    columns[band][index],
                    columns[band][nxt],
                    columns[band + 1][nxt],
                    columns[band + 1][index],
                )
            )
            face.material_index = material
            face.smooth = False
    bottom = bm.faces.new(list(reversed(columns[0])))
    bottom.material_index = material_of_band[0]
    bottom.smooth = False
    top = bm.faces.new(list(columns[-1]))
    top.material_index = material_of_band[-1]
    top.smooth = False


def segment(bm, center_z, center_y, width_z, height_y, x0, x1, material):
    box(
        bm,
        x0,
        x1,
        center_y - height_y * 0.5,
        center_y + height_y * 0.5,
        center_z - width_z * 0.5,
        center_z + width_z * 0.5,
        material,
    )


def digit_bar_1(bm, center_z, x0, x1):
    segment(bm, center_z + 0.045, 0.0, 0.05, 0.24, x0, x1, 1)


def digit_3(bm, center_z, x0, x1):
    for z_off, y_off, wz, hy in (
        (0.0, 0.10, 0.16, 0.045),
        (0.0, 0.0, 0.16, 0.04),
        (0.0, -0.10, 0.16, 0.045),
        (0.058, 0.05, 0.045, 0.10),
        (0.058, -0.05, 0.045, 0.10),
    ):
        segment(bm, center_z + z_off, y_off, wz, hy, x0, x1, 1)


def build_piso(bm):
    box(bm, -2.0, 2.0, -0.08, 0.0, -2.0, 2.0, 0)


def build_forro(bm):
    box(bm, -2.0, 2.0, 0.0, 0.08, -2.0, 2.0, 0)


def build_luminaria(bm):
    box(bm, -0.6, 0.6, -0.03, 0.0, -0.15, 0.15, 0)
    box(bm, -0.6, -0.52, -0.12, 0.0, -0.15, 0.15, 0)
    box(bm, 0.52, 0.6, -0.12, 0.0, -0.15, 0.15, 0)
    box(bm, -0.52, 0.52, -0.12, -0.03, -0.15, -0.11, 0)
    box(bm, -0.52, 0.52, -0.12, -0.03, 0.11, 0.15, 0)
    box(bm, -0.48, 0.48, -0.10, -0.05, -0.035, 0.035, 1)


def build_pilar(bm):
    box(bm, -0.2, 0.2, 0.0, 5.5, -0.2, 0.2, 0)


def build_gondola(bm):
    box(bm, -0.6, 0.6, 0.0, 0.16, -1.0, 1.0, 0)
    box(bm, -0.6, -0.54, 0.16, 2.2, -1.0, 1.0, 0)
    box(bm, 0.50, 0.6, 2.04, 2.2, -1.0, 1.0, 0)
    for height in (0.42, 1.02, 1.62):
        box(bm, -0.52, 0.46, height, height + 0.03, -0.98, 0.98, 1)


def build_bandeja(bm):
    box(bm, -0.55, 0.55, 0.0, 0.025, -1.0, 1.0, 0)
    box(bm, 0.50, 0.55, 0.025, 0.04, -1.0, 1.0, 0)


def build_testeira(bm):
    box(bm, -0.6, 0.6, 0.0, 2.2, -0.075, 0.03, 0)
    box(bm, -0.6, 0.6, 0.0, 0.18, 0.03, 0.075, 0)
    box(bm, -0.6, 0.6, 2.02, 2.2, 0.03, 0.075, 0)
    box(bm, -0.6, -0.48, 0.18, 2.02, 0.03, 0.075, 0)
    box(bm, 0.48, 0.6, 0.18, 2.02, 0.03, 0.075, 0)
    box(bm, -0.48, 0.48, 0.18, 2.02, 0.03, 0.055, 1)


def build_palete(bm):
    for center_z in (-0.38, 0.0, 0.38):
        box(bm, -0.6, 0.6, 0.0, 0.095, center_z - 0.045, center_z + 0.045, 0)
    gap = 0.125
    width = 0.14
    start = -0.6
    for index in range(5):
        x0 = start + index * (width + gap)
        box(bm, x0, x0 + width, 0.095, 0.14, -0.5, 0.5, 0)


def build_caixa_papelao(bm):
    box(bm, -0.3, 0.3, 0.0, 0.34, -0.225, 0.225, 0)
    box(bm, -0.3, 0.3, 0.34, 0.4, -0.225, 0.225, 1)


def build_porta_palete(bm):
    post_x = ((-0.55, -0.47), (0.47, 0.55))
    post_z = ((-1.1, -1.02), (1.02, 1.1))
    for x0, x1 in post_x:
        for z0, z1 in post_z:
            box(bm, x0, x1, 0.0, 4.2, z0, z1, 0)
    for y0, y1 in ((0.20, 0.28), (1.40, 1.48), (2.60, 2.68), (3.80, 3.88)):
        for x0, x1 in post_x:
            box(bm, x0, x1, y0, y1, -1.02, 1.02, 0)
    for y0, y1 in ((1.40, 1.48), (3.80, 3.88)):
        for z0, z1 in ((-1.02, -0.94), (0.94, 1.02)):
            box(bm, -0.47, 0.47, y0, y1, z0, z1, 0)


def build_fardo(bm):
    box(bm, -0.25, 0.25, 0.0, 0.12, -0.15, 0.15, 0)
    box(bm, -0.25, 0.25, 0.12, 0.23, -0.15, 0.15, 1)
    box(bm, -0.25, 0.25, 0.23, 0.35, -0.15, 0.15, 0)


def build_cartucho(bm):
    box(bm, -0.125, 0.125, 0.0, 0.24, -0.06, 0.06, 0)
    box(bm, -0.125, 0.125, 0.24, 0.30, -0.06, 0.06, 1)


def build_garrafao(bm):
    rings = (
        (0.00, 0.10),
        (0.02, 0.125),
        (0.27, 0.125),
        (0.325, 0.04),
        (0.35, 0.036),
        (0.365, 0.036),
        (0.365, 0.052),
        (0.40, 0.052),
    )
    bands = (0, 0, 0, 0, 0, 1, 1)
    lathe(bm, rings, 8, bands)


def build_pacote(bm):
    box(bm, -0.2, 0.2, 0.0, 0.11, -0.175, 0.175, 0)
    box(bm, -0.2, 0.2, 0.11, 0.15, -0.175, 0.175, 1)


def build_caixa_atendimento(bm):
    box(bm, -0.4, 0.4, 0.0, 0.78, -0.8, 0.8, 0)
    box(bm, -0.4, -0.05, 0.78, 0.9, -0.8, 0.8, 0)
    box(bm, 0.30, 0.4, 0.78, 0.9, -0.8, 0.8, 0)
    box(bm, -0.05, 0.30, 0.78, 0.9, -0.8, 0.45, 0)
    box(bm, -0.05, 0.30, 0.78, 0.9, 0.45, 0.8, 1)


def build_portal(bm):
    box(bm, -0.15, 0.15, 0.0, 3.2, -2.0, -1.55, 0)
    box(bm, -0.15, 0.15, 0.0, 3.2, 1.55, 2.0, 0)
    box(bm, -0.15, 0.15, 2.4, 3.2, -1.55, 1.55, 0)


def build_ilha_freezer(bm):
    box(bm, -0.5, 0.5, 0.0, 0.75, -1.0, 1.0, 0)
    box(bm, -0.5, -0.38, 0.75, 0.9, -1.0, 1.0, 0)
    box(bm, 0.38, 0.5, 0.75, 0.9, -1.0, 1.0, 0)
    box(bm, -0.38, 0.38, 0.75, 0.9, -1.0, -0.88, 0)
    box(bm, -0.38, 0.38, 0.75, 0.9, 0.88, 1.0, 0)
    box(bm, -0.36, 0.36, 0.78, 0.86, -0.84, 0.84, 1)


def build_freezer_vertical(bm):
    box(bm, -0.375, 0.30, 0.0, 2.0, -0.6, 0.6, 0)
    box(bm, 0.30, 0.375, 1.88, 2.0, -0.6, 0.6, 0)
    box(bm, 0.30, 0.375, 0.0, 0.12, -0.6, 0.6, 0)
    box(bm, 0.30, 0.375, 0.12, 1.88, -0.04, 0.04, 0)
    box(bm, 0.30, 0.375, 0.12, 1.88, -0.6, -0.52, 0)
    box(bm, 0.30, 0.375, 0.12, 1.88, 0.52, 0.6, 0)
    box(bm, 0.325, 0.35, 0.20, 1.80, -0.50, -0.08, 1)
    box(bm, 0.325, 0.35, 0.20, 1.80, 0.08, 0.50, 1)
    box(bm, 0.35, 0.375, 0.85, 1.25, -0.08, -0.03, 0)
    box(bm, 0.35, 0.375, 0.85, 1.25, 0.03, 0.08, 0)


def build_parede(bm):
    box(bm, -0.1, 0.1, 0.15, 5.5, -2.0, 2.0, 0)
    box(bm, -0.1, 0.1, 0.0, 0.15, -2.0, 2.0, 1)


def build_porta_camara(bm):
    box(bm, -0.1, 0.06, 0.0, 2.4, -0.8, 0.8, 0)
    box(bm, 0.06, 0.1, 1.85, 2.4, -0.8, 0.8, 0)
    box(bm, 0.06, 0.1, 0.0, 1.15, -0.8, 0.8, 0)
    box(bm, 0.06, 0.1, 1.15, 1.85, -0.8, -0.28, 0)
    box(bm, 0.06, 0.1, 1.15, 1.85, 0.28, 0.8, 0)
    box(bm, 0.065, 0.085, 1.20, 1.78, -0.24, 0.24, 1)
    box(bm, 0.08, 0.1, 1.00, 1.35, 0.42, 0.55, 0)


def build_placa_setor(bm):
    box(bm, -0.025, 0.012, -0.2, 0.2, -0.6, 0.6, 0)
    digit_bar_1(bm, -0.20, 0.012, 0.025)
    digit_3(bm, 0.18, 0.012, 0.025)


def build_porta_saida(bm):
    box(bm, -0.06, 0.035, 0.0, 2.4, -0.7, 0.7, 0)
    box(bm, 0.035, 0.06, 2.05, 2.4, -0.7, 0.7, 0)
    box(bm, 0.035, 0.06, 0.0, 0.18, -0.7, 0.7, 0)
    box(bm, 0.035, 0.06, 0.18, 2.05, -0.7, -0.58, 0)
    box(bm, 0.035, 0.06, 0.18, 2.05, 0.58, 0.7, 0)
    box(bm, 0.04, 0.055, 0.28, 1.95, -0.55, 0.55, 0)
    box(bm, 0.045, 0.06, 1.05, 1.18, -0.28, 0.28, 0)


def build_placa_corredor(bm):
    box(bm, -0.025, 0.01, -0.175, 0.175, -0.6, 0.6, 0)
    box(bm, 0.01, 0.025, -0.175, -0.12, -0.6, 0.6, 0)
    box(bm, 0.01, 0.025, 0.12, 0.175, -0.6, 0.6, 0)
    box(bm, 0.01, 0.025, -0.12, 0.12, -0.6, -0.48, 0)
    box(bm, 0.01, 0.025, -0.12, 0.12, 0.48, 0.6, 0)
    box(bm, 0.01, 0.02, -0.10, 0.10, -0.46, 0.46, 0)


def prism(bm, points, x0, x1, material):
    """Prisma convexo. points = [(z, y)] em coordenadas da Unity, extrudado de x0 a x1."""
    near = [bm.verts.new((bx(x0), bz(z), y)) for z, y in points]
    far = [bm.verts.new((bx(x1), bz(z), y)) for z, y in points]
    faces = [bm.faces.new(near), bm.faces.new(list(reversed(far)))]
    count = len(points)
    for index in range(count):
        nxt = (index + 1) % count
        faces.append(bm.faces.new((near[index], near[nxt], far[nxt], far[index])))
    for face in faces:
        face.material_index = material
        face.smooth = False


def glyph(bm, rows, z_left, y_top, cell, x0, x1, material, mirror=False):
    """Letra em grade. Colunas crescem para a direita de quem lê a face.
    mirror=True para a face de trás (direita do leitor = -Z)."""
    for r, row in enumerate(rows):
        k = 0
        while k < len(row):
            if row[k] != "#":
                k += 1
                continue
            j = k
            while j < len(row) and row[j] == "#":
                j += 1
            if mirror:
                z0, z1 = z_left - j * cell, z_left - k * cell
            else:
                z0, z1 = z_left + k * cell, z_left + j * cell
            box(bm, x0, x1, y_top - (r + 1) * cell, y_top - r * cell, z0, z1, material)
            k = j


GLYPHS = {
    "S": ("###", "#..", "###", "..#", "###"),
    "A": ("###", "#.#", "###", "#.#", "#.#"),
    "I": ("#", "#", "#", "#", "#"),
    "D": ("##.", "#.#", "#.#", "#.#", "##."),
    "1": (".#.", "##.", ".#.", ".#.", "###"),
    "3": ("###", "..#", ".##", "..#", "###"),
}


def mirrored(rows):
    return tuple(row[::-1] for row in rows)


def arrow_z(bm, z0, z1, x0, x1, material, half=0.045, head=0.13, head_len=0.17):
    """Seta apontando para +Z (Unity) entre z0 e z1, centrada em y = 0."""
    box(bm, x0, x1, -half, half, z0, z1 - head_len + 0.01, material)
    prism(bm, ((z1 - head_len, -head), (z1, 0.0), (z1 - head_len, head)), x0, x1, material)


def arrow_up(bm, y0, y1, x0, x1, material, half=0.045, head=0.14, head_len=0.16):
    box(bm, x0, x1, y0, y1 - head_len + 0.01, -half, half, material)
    prism(bm, ((-head, y1 - head_len), (head, y1 - head_len), (0.0, y1)), x0, x1, material)


def wires(bm, z_offsets, y0, y1, material):
    for z in z_offsets:
        box(bm, -0.008, 0.008, y0, y1, z - 0.008, z + 0.008, material)


def build_luminaria_morta(bm):
    box(bm, -0.6, 0.6, -0.03, 0.0, -0.15, 0.15, 0)
    box(bm, -0.6, -0.52, -0.12, 0.0, -0.15, 0.15, 0)
    box(bm, 0.52, 0.6, -0.12, 0.0, -0.15, 0.15, 0)
    box(bm, -0.52, 0.52, -0.12, -0.03, -0.15, -0.11, 0)
    box(bm, -0.52, 0.52, -0.12, -0.03, 0.11, 0.15, 0)
    # Um tubo só, mais curto, encardido. O soquete do outro ficou vazio.
    box(bm, -0.48, 0.31, -0.105, -0.055, -0.07, -0.02, 1)
    box(bm, 0.40, 0.48, -0.09, -0.06, 0.02, 0.06, 1)


def build_seta_corredor(bm):
    box(bm, -0.02, 0.02, -0.25, 0.25, -0.35, 0.35, 0)
    for x0, x1 in ((0.02, 0.032), (-0.032, -0.02)):
        arrow_up(bm, -0.19, 0.19, x0, x1, 1)
    wires(bm, (-0.3, 0.3), 0.25, 2.2, 2)


def build_placa_13(bm):
    box(bm, -0.02, 0.02, -0.25, 0.25, -0.75, 0.75, 0)
    cell = 0.068
    top = 0.17
    # Frente (+X): "13" e seta para a direita do leitor (+Z).
    glyph(bm, GLYPHS["1"], -0.64, top, cell, 0.02, 0.032, 1)
    glyph(bm, GLYPHS["3"], -0.38, top, cell, 0.02, 0.032, 1)
    arrow_z(bm, 0.02, 0.66, 0.02, 0.032, 1)
    # Verso (-X): seta à esquerda do leitor (+Z) e "13" à direita dele.
    arrow_z(bm, 0.02, 0.66, -0.032, -0.02, 1)
    glyph(bm, GLYPHS["1"], -0.10, top, cell, -0.032, -0.02, 1, mirror=True)
    glyph(bm, GLYPHS["3"], -0.36, top, cell, -0.032, -0.02, 1, mirror=True)
    wires(bm, (-0.6, 0.6), 0.25, 2.2, 2)


def build_placa_saida(bm):
    # Verga sobre a porta, do topo da porta ao forro, com a caixa "SAÍDA".
    box(bm, -0.1, 0.1, 0.0, 3.1, -0.7, 0.7, 0)
    box(bm, 0.1, 0.2, 0.12, 0.58, -0.66, 0.66, 1)
    cell = 0.06
    top = 0.50
    x0, x1 = 0.2, 0.215
    z = -0.51
    for letter in "SAIDA":
        rows = GLYPHS[letter]
        glyph(bm, rows, z, top, cell, x0, x1, 2)
        if letter == "I":
            box(bm, x0, x1, top + 0.012, top + 0.05, z + 0.015, z + cell + 0.02, 2)
        z += len(rows[0]) * cell + 0.06


def build_evaporador(bm):
    box(bm, 0.0, 0.4, -0.3, 0.3, -0.8, 0.8, 0)
    for zc in (-0.4, 0.4):
        box(bm, 0.4, 0.41, -0.2, 0.2, zc - 0.2, zc + 0.2, 2)
        box(bm, 0.41, 0.45, 0.19, 0.23, zc - 0.23, zc + 0.23, 1)
        box(bm, 0.41, 0.45, -0.23, -0.19, zc - 0.23, zc + 0.23, 1)
        box(bm, 0.41, 0.45, -0.19, 0.19, zc - 0.23, zc - 0.19, 1)
        box(bm, 0.41, 0.45, -0.19, 0.19, zc + 0.19, zc + 0.23, 1)
        box(bm, 0.41, 0.44, -0.02, 0.02, zc - 0.19, zc + 0.19, 1)
        box(bm, 0.41, 0.44, -0.19, 0.19, zc - 0.02, zc + 0.02, 1)


def unity_bounds(obj):
    xs, ys, zs = [], [], []
    for corner in obj.bound_box:
        world = obj.matrix_world @ Vector(corner)
        xs.append(bx(world.x))
        ys.append(world.z)
        zs.append(bz(world.y))
    lower = (min(xs), min(ys), min(zs))
    upper = (max(xs), max(ys), max(zs))
    size = tuple(upper[i] - lower[i] for i in range(3))
    return lower, upper, size


def export_fbx(obj, path):
    if tuple(round(v, 6) for v in obj.scale) != (1.0, 1.0, 1.0):
        raise RuntimeError("escala do objeto não está em 1,1,1")
    if any(abs(v) > 1e-8 for v in obj.rotation_euler):
        raise RuntimeError("rotação do objeto não está em 0")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    with bpy.context.temp_override(active_object=obj, selected_objects=[obj]):
        bpy.ops.export_scene.fbx(
            filepath=path,
            use_selection=True,
            object_types={"MESH"},
            apply_scale_options="FBX_SCALE_NONE",
            apply_unit_scale=True,
            use_space_transform=True,
            bake_space_transform=True,
            axis_forward="-Z",
            axis_up="Y",
            global_scale=1.0,
            mesh_smooth_type="FACE",
            use_mesh_modifiers=False,
            use_subsurf=False,
            add_leaf_bones=False,
            bake_anim=False,
            path_mode="STRIP",
        )


def fbx_child(elem, name):
    for child in elem.elems:
        if child.id == name:
            yield child


def fbx_prop(props_elem, name):
    for prop in props_elem.elems:
        if prop.id == b"P" and prop.props and prop.props[0] == name:
            return prop.props
    return None


def parse_fbx_bounds(path):
    addon = os.path.join(
        os.path.dirname(bpy.app.binary_path),
        "5.3",
        "scripts",
        "addons_core",
    )
    addon = os.path.normpath(addon)
    if addon not in sys.path:
        sys.path.append(addon)
    from io_scene_fbx.parse_fbx import parse

    root, _version = parse(path, use_namedtuple=True)
    unit = 1.0
    for settings in fbx_child(root, b"GlobalSettings"):
        for props in fbx_child(settings, b"Properties70"):
            found = fbx_prop(props, b"UnitScaleFactor")
            if found is not None:
                unit = float(found[4])
    geometries = {}
    models = {}
    for obj in fbx_child(root, b"Objects"):
        for geom in fbx_child(obj, b"Geometry"):
            vertices = None
            for child in geom.elems:
                if child.id == b"Vertices":
                    vertices = child.props[0]
            if vertices is None:
                continue
            geometries[geom.props[0]] = vertices
        for model in fbx_child(obj, b"Model"):
            translation = (0.0, 0.0, 0.0)
            rotation = (0.0, 0.0, 0.0)
            scale = (1.0, 1.0, 1.0)
            for props in fbx_child(model, b"Properties70"):
                found = fbx_prop(props, b"Lcl Translation")
                if found is not None:
                    translation = tuple(float(v) for v in found[-3:])
                found = fbx_prop(props, b"Lcl Rotation")
                if found is not None:
                    rotation = tuple(float(v) for v in found[-3:])
                found = fbx_prop(props, b"Lcl Scaling")
                if found is not None:
                    scale = tuple(float(v) for v in found[-3:])
            models[model.props[0]] = {
                "name": model.props[1],
                "translation": translation,
                "rotation": rotation,
                "scale": scale,
            }
    connections = []
    for conn_root in fbx_child(root, b"Connections"):
        for conn in fbx_child(conn_root, b"C"):
            if len(conn.props) >= 3 and conn.props[0] == b"OO":
                connections.append((conn.props[1], conn.props[2]))
    if len(geometries) != 1 or len(models) != 1:
        raise RuntimeError(
            "FBX deveria ter 1 malha e 1 modelo, veio %d malhas e %d modelos"
            % (len(geometries), len(models))
        )
    model = next(iter(models.values()))
    if any(abs(v) > 1e-4 for v in model["rotation"]):
        raise RuntimeError("rotação do nó FBX não é zero: %s" % (model["rotation"],))
    if any(abs(v - 1.0) > 1e-4 for v in model["scale"]):
        raise RuntimeError("escala do nó FBX não é 1: %s" % (model["scale"],))
    vertices = next(iter(geometries.values()))
    tx, ty, tz = model["translation"]
    xs, ys, zs = [], [], []
    factor = unit / 100.0
    for index in range(0, len(vertices), 3):
        px = (float(vertices[index]) + tx) * factor
        py = (float(vertices[index + 1]) + ty) * factor
        pz = (float(vertices[index + 2]) + tz) * factor
        # Unity espelha X do FBX. Com FLIP o gerador já compensou isso.
        xs.append(-px if FLIP else px)
        ys.append(py)
        zs.append(pz if FLIP else -pz)
    lower = (min(xs), min(ys), min(zs))
    upper = (max(xs), max(ys), max(zs))
    size = tuple(upper[i] - lower[i] for i in range(3))
    return {
        "unit_scale_factor": unit,
        "translation_cm": model["translation"],
        "min": lower,
        "max": upper,
        "size": size,
        "connections": len(connections),
    }


ASSETS = (
    {
        "file": "piso_concreto",
        "prefab": "Piso",
        "folder": "Arquitetura",
        "materials": (("Concreto", "8A8478"),),
        "pivot_y": 0.0,
        "build": build_piso,
        "bounds": ((-2, -0.08, -2), (2, 0, 2)),
        "colliders": [{"center": [0, -0.04, 0], "size": [4, 0.08, 4]}],
    },
    {
        "file": "forro",
        "prefab": "Forro",
        "folder": "Arquitetura",
        "materials": (("Forro", "B7B2A6"),),
        "pivot_y": 5.5,
        "build": build_forro,
        "bounds": ((-2, 5.5, -2), (2, 5.58, 2)),
        "colliders": [],
    },
    {
        "file": "luminaria",
        "prefab": "Luminaria",
        "folder": "Arquitetura",
        "materials": (("Luz", "C8CBCF"), ("Tubo", "E8F6D4")),
        "pivot_y": 5.5,
        "build": build_luminaria,
        "bounds": ((-0.6, 5.38, -0.15), (0.6, 5.5, 0.15)),
        "colliders": [],
    },
    {
        "file": "pilar",
        "prefab": "Pilar",
        "folder": "Arquitetura",
        "materials": (("Concreto", "8A8478"),),
        "pivot_y": 0.0,
        "build": build_pilar,
        "bounds": ((-0.2, 0, -0.2), (0.2, 5.5, 0.2)),
        "colliders": [{"center": [0, 2.75, 0], "size": [0.4, 5.5, 0.4]}],
    },
    {
        "file": "gondola",
        "prefab": "Gondola",
        "folder": "Estacoes",
        "materials": (("Gondola", "C4622D"), ("Bandeja", "E6DECF")),
        "pivot_y": 0.0,
        "build": build_gondola,
        "bounds": ((-0.6, 0, -1), (0.6, 2.2, 1)),
        "colliders": [{"center": [0, 1.1, 0], "size": [1.2, 2.2, 2]}],
    },
    {
        "file": "bandeja",
        "prefab": "Bandeja",
        "folder": "Props",
        "materials": (("Bandeja", "E6DECF"),),
        "pivot_y": 0.0,
        "build": build_bandeja,
        "bounds": ((-0.55, 0, -1), (0.55, 0.04, 1)),
        "colliders": [],
    },
    {
        "file": "testeira",
        "prefab": "Testeira",
        "folder": "Estacoes",
        "materials": (("Gondola", "C4622D"), ("Bandeja", "E6DECF")),
        "pivot_y": 0.0,
        "build": build_testeira,
        "bounds": ((-0.6, 0, -0.075), (0.6, 2.2, 0.075)),
        "colliders": [{"center": [0, 1.1, 0], "size": [1.2, 2.2, 0.15]}],
    },
    {
        "file": "palete",
        "prefab": "Palete",
        "folder": "Props",
        "materials": (("Madeira", "8C6B3E"),),
        "pivot_y": 0.0,
        "build": build_palete,
        "bounds": ((-0.6, 0, -0.5), (0.6, 0.14, 0.5)),
        "colliders": [{"center": [0, 0.07, 0], "size": [1.2, 0.14, 1]}],
    },
    {
        "file": "caixa_papelao",
        "prefab": "CaixaPapelao",
        "folder": "Props",
        "materials": (("Papelao", "B58962"), ("PapelaoTampa", "8D6948")),
        "pivot_y": 0.0,
        "build": build_caixa_papelao,
        "bounds": ((-0.3, 0, -0.225), (0.3, 0.4, 0.225)),
        "colliders": [{"center": [0, 0.2, 0], "size": [0.6, 0.4, 0.45]}],
    },
    {
        "file": "porta_palete",
        "prefab": "PortaPalete",
        "folder": "Estacoes",
        "materials": (("Metal", "5C636A"),),
        "pivot_y": 0.0,
        "build": build_porta_palete,
        "bounds": ((-0.55, 0, -1.1), (0.55, 4.2, 1.1)),
        "colliders": [{"center": [0, 2.1, 0], "size": [1.1, 4.2, 2.2]}],
    },
    {
        "file": "fardo",
        "prefab": "Fardo",
        "folder": "Props",
        "materials": (("Fardo", "E7E1D6"), ("Faixa", "2C4C86")),
        "pivot_y": 0.0,
        "build": build_fardo,
        "bounds": ((-0.25, 0, -0.15), (0.25, 0.35, 0.15)),
        "colliders": [{"center": [0, 0.175, 0], "size": [0.5, 0.35, 0.3]}],
    },
    {
        "file": "cartucho",
        "prefab": "Cartucho",
        "folder": "Props",
        "materials": (("Cartucho", "D4A24C"), ("CartuchoTopo", "8C3A32")),
        "pivot_y": 0.0,
        "build": build_cartucho,
        "bounds": ((-0.125, 0, -0.06), (0.125, 0.3, 0.06)),
        "colliders": [{"center": [0, 0.15, 0], "size": [0.25, 0.3, 0.12]}],
    },
    {
        "file": "garrafao",
        "prefab": "Garrafao",
        "folder": "Props",
        "materials": (("Garrafao", "2C6E9C"), ("Tampa", "E6E4DE")),
        "pivot_y": 0.0,
        "build": build_garrafao,
        "bounds": ((-0.125, 0, -0.125), (0.125, 0.4, 0.125)),
        "colliders": [{"center": [0, 0.2, 0], "size": [0.25, 0.4, 0.25]}],
    },
    {
        "file": "pacote",
        "prefab": "Pacote",
        "folder": "Props",
        "materials": (("Pacote", "C24E44"), ("PacoteFaixa", "F2E6C4")),
        "pivot_y": 0.0,
        "build": build_pacote,
        "bounds": ((-0.2, 0, -0.175), (0.2, 0.15, 0.175)),
        "colliders": [{"center": [0, 0.075, 0], "size": [0.4, 0.15, 0.35]}],
    },
    {
        "file": "caixa_atendimento",
        "prefab": "CaixaAtendimento",
        "folder": "Estacoes",
        "materials": (("Atendimento", "4A5560"), ("Registro", "2A2E32")),
        "pivot_y": 0.0,
        "build": build_caixa_atendimento,
        "bounds": ((-0.4, 0, -0.8), (0.4, 0.9, 0.8)),
        "colliders": [{"center": [0, 0.45, 0], "size": [0.8, 0.9, 1.6]}],
    },
    {
        "file": "portal_entrada",
        "prefab": "PortalEntrada",
        "folder": "Arquitetura",
        "materials": (("Portal", "2A2F34"),),
        "pivot_y": 0.0,
        "build": build_portal,
        "bounds": ((-0.15, 0, -2), (0.15, 3.2, 2)),
        "colliders": [
            {"center": [0, 1.6, -1.775], "size": [0.3, 3.2, 0.45]},
            {"center": [0, 1.6, 1.775], "size": [0.3, 3.2, 0.45]},
        ],
    },
    {
        "file": "ilha_freezer",
        "prefab": "IlhaFreezer",
        "folder": "Estacoes",
        "materials": (("Freezer", "E8E6E0"), ("Vidro", "8EA0AA")),
        "pivot_y": 0.0,
        "build": build_ilha_freezer,
        "bounds": ((-0.5, 0, -1), (0.5, 0.9, 1)),
        "colliders": [{"center": [0, 0.45, 0], "size": [1, 0.9, 2]}],
    },
    {
        "file": "freezer_vertical",
        "prefab": "FreezerVertical",
        "folder": "Estacoes",
        "materials": (("Freezer", "E8E6E0"), ("Vidro", "8EA0AA")),
        "pivot_y": 0.0,
        "build": build_freezer_vertical,
        "bounds": ((-0.375, 0, -0.6), (0.375, 2, 0.6)),
        "colliders": [{"center": [0, 1, 0], "size": [0.75, 2, 1.2]}],
    },
    {
        "file": "parede",
        "prefab": "Parede",
        "folder": "Arquitetura",
        "materials": (("Parede", "C8C2B4"), ("Rodape", "6F6B64")),
        "pivot_y": 0.0,
        "build": build_parede,
        "bounds": ((-0.1, 0, -2), (0.1, 5.5, 2)),
        "colliders": [{"center": [0, 2.75, 0], "size": [0.2, 5.5, 4]}],
    },
    {
        "file": "porta_camara",
        "prefab": "PortaCamara",
        "folder": "Arquitetura",
        "materials": (("Porta", "3C4450"), ("Vidro", "8EA0AA")),
        "pivot_y": 0.0,
        "build": build_porta_camara,
        "bounds": ((-0.1, 0, -0.8), (0.1, 2.4, 0.8)),
        "colliders": [{"center": [0, 1.2, 0], "size": [0.2, 2.4, 1.6]}],
    },
    {
        "file": "placa_setor_13",
        "prefab": "PlacaSetor13",
        "folder": "Arquitetura",
        "materials": (("PlacaSetor", "E0B33A"), ("Digito", "16181A")),
        "pivot_y": 0.0,
        "build": build_placa_setor,
        "bounds": ((-0.025, -0.2, -0.6), (0.025, 0.2, 0.6)),
        "colliders": [],
    },
    {
        "file": "porta_saida",
        "prefab": "PortaSaida",
        "folder": "Arquitetura",
        "materials": (("Porta", "3C4450"),),
        "pivot_y": 0.0,
        "build": build_porta_saida,
        "bounds": ((-0.06, 0, -0.7), (0.06, 2.4, 0.7)),
        "colliders": [{"center": [0, 1.2, 0], "size": [0.12, 2.4, 1.4]}],
    },
    {
        "file": "placa_corredor",
        "prefab": "PlacaCorredor",
        "folder": "Arquitetura",
        "materials": (("PlacaCorredor", "1E6A58"),),
        "pivot_y": 0.0,
        "build": build_placa_corredor,
        "bounds": ((-0.025, -0.175, -0.6), (0.025, 0.175, 0.6)),
        "colliders": [],
    },
    {
        "file": "luminaria_morta",
        "prefab": "LuminariaMorta",
        "folder": "Arquitetura",
        "materials": (("Luz", "C8CBCF"), ("TuboMorto", "5E615A")),
        "pivot_y": 5.5,
        "build": build_luminaria_morta,
        "bounds": ((-0.6, 5.38, -0.15), (0.6, 5.5, 0.15)),
        "colliders": [],
    },
    {
        "file": "seta_corredor",
        "prefab": "SetaCorredor",
        "folder": "Arquitetura",
        "materials": (("VerdeSaida", "1F7A45"), ("LetraSaida", "F2F5EE"), ("Metal", "5C636A")),
        "pivot_y": 0.0,
        "build": build_seta_corredor,
        "bounds": ((-0.032, -0.25, -0.35), (0.032, 2.2, 0.35)),
        "colliders": [],
    },
    {
        "file": "placa_13",
        "prefab": "Placa13",
        "folder": "Arquitetura",
        "materials": (("Placa13Azul", "2C5E8C"), ("LetraSaida", "F2F5EE"), ("Metal", "5C636A")),
        "pivot_y": 0.0,
        "build": build_placa_13,
        "bounds": ((-0.032, -0.25, -0.75), (0.032, 2.2, 0.75)),
        "colliders": [],
    },
    {
        "file": "placa_saida",
        "prefab": "PlacaSaida",
        "folder": "Arquitetura",
        "materials": (("Parede", "C8C2B4"), ("VerdeSaida", "1F7A45"), ("LetraSaida", "F2F5EE")),
        "pivot_y": 0.0,
        "build": build_placa_saida,
        "bounds": ((-0.1, 0.0, -0.7), (0.215, 3.1, 0.7)),
        "colliders": [],
    },
    {
        "file": "evaporador",
        "prefab": "Evaporador",
        "folder": "Estacoes",
        "materials": (("Freezer", "E8E6E0"), ("Metal", "5C636A"), ("Registro", "2A2E32")),
        "pivot_y": 0.0,
        "build": build_evaporador,
        "bounds": ((0.0, -0.3, -0.8), (0.45, 0.3, 0.8)),
        "colliders": [],
    },
)

# Materiais que brilham na Unity (emissão = cor x força).
EMISSIVE = (
    ("Tubo", 1.0),
    ("LetraSaida", 1.6),
    ("VerdeSaida", 0.45),
    ("Placa13Azul", 0.4),
)


def nearly(a, b):
    return all(abs(a[i] - b[i]) <= TOL for i in range(3))


def build_one(collection, spec):
    purge()
    materials = [make_material(name, color) for name, color in spec["materials"]]
    mesh = bpy.data.meshes.new(spec["file"])
    obj = bpy.data.objects.new(spec["file"], mesh)
    collection.objects.link(obj)
    for material in materials:
        mesh.materials.append(material)
    bm = bmesh.new()
    spec["build"](bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    if hasattr(mesh, "shade_flat"):
        mesh.shade_flat()
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    mesh.calc_loop_triangles()
    obj.location = (0.0, 0.0, spec["pivot_y"])
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    lower, upper, size = unity_bounds(obj)
    expect_min, expect_max = spec["bounds"]
    if not nearly(lower, expect_min) or not nearly(upper, expect_max):
        raise RuntimeError(
            "%s fora da medida. min=%s max=%s esperado min=%s max=%s"
            % (spec["file"], lower, upper, expect_min, expect_max)
        )
    if spec["file"] == "portal_entrada":
        for vertex in mesh.vertices:
            unity_y = vertex.co.z
            unity_z = vertex.co.y
            if abs(unity_z) < 1.549 and unity_y < 2.399:
                raise RuntimeError("portal bloqueia o vão em %s" % (vertex.co,))
    blend_path = os.path.join(MODEL_DIR, spec["file"] + ".blend")
    fbx_path = os.path.join(FBX_DIR, spec["file"] + ".fbx")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    export_fbx(obj, fbx_path)
    exported = parse_fbx_bounds(fbx_path)
    if not nearly(exported["min"], expect_min) or not nearly(exported["max"], expect_max):
        raise RuntimeError(
            "%s FBX fora da medida. min=%s max=%s unit=%s translation=%s"
            % (
                spec["file"],
                exported["min"],
                exported["max"],
                exported["unit_scale_factor"],
                exported["translation_cm"],
            )
        )
    record = {
        "file": spec["file"],
        "prefab": spec["prefab"],
        "folder": spec["folder"],
        "materials": [name for name, _color in spec["materials"]],
        "pivot": [0, spec["pivot_y"], 0],
        "colliders": spec["colliders"],
        "triangles": len(mesh.loop_triangles),
        "min": [round(v, 5) for v in exported["min"]],
        "max": [round(v, 5) for v in exported["max"]],
        "size": [round(v, 5) for v in exported["size"]],
        "unit_scale_factor": exported["unit_scale_factor"],
    }
    print(
        "OK %s size=%s min=%s max=%s tris=%d unit=%s"
        % (
            spec["file"],
            tuple(round(v, 5) for v in exported["size"]),
            tuple(round(v, 5) for v in exported["min"]),
            tuple(round(v, 5) for v in exported["max"]),
            record["triangles"],
            exported["unit_scale_factor"],
        )
    )
    return record


def main():
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else ["piso"]
    wanted = argv[0]
    if wanted == "todos":
        selected = ASSETS
    elif wanted == "piso":
        selected = (ASSETS[0],)
    else:
        selected = tuple(item for item in ASSETS if item["file"] == wanted)
        if not selected:
            raise SystemExit("modelo desconhecido: %s" % wanted)
    os.makedirs(MODEL_DIR, exist_ok=True)
    os.makedirs(FBX_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    collection = prepare_scene()
    records = [build_one(collection, spec) for spec in selected]
    if wanted == "todos":
        colors = {}
        for spec in ASSETS:
            for name, color in spec["materials"]:
                colors[name] = color
        palette = [{"name": name, "hex": color} for name, color in sorted(colors.items())]
        emissive = [{"name": name, "strength": strength} for name, strength in EMISSIVE]
        with open(KIT_PATH, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(
                {"assets": records, "colors": colors, "palette": palette, "emissive": emissive, "flip": FLIP},
                handle,
                indent=2,
            )
            handle.write("\n")
        print("KIT %s" % KIT_PATH)


if __name__ == "__main__":
    main()
