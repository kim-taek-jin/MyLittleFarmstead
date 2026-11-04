"""Generate all pixel art for My Little Farmstead.

Usage (from repo root):  python3 Tools/pixelart/generate.py

Writes PNGs + Unity .meta files to Assets/Resources/PixelArt/<Group>/<name>.png
(point filter, 32 pixels per unit, no compression) and a preview sheet to Docs/.
GUIDs are derived from the file path, so re-running keeps Unity references intact.
"""
import hashlib
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import items  # noqa: E402
import crops  # noqa: E402
import world  # noqa: E402
import preview  # noqa: E402
import backgrounds  # noqa: E402
import farmmap  # noqa: E402
import villagers  # noqa: E402
import spirit  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'PixelArt')

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid_for(rel_path):
    return hashlib.md5(('mylittlefarmstead/' + rel_path).encode('utf-8')).hexdigest()


def write_folder_meta(path):
    rel = os.path.relpath(path, ROOT).replace(os.sep, '/')
    meta = path + '.meta'
    if not os.path.exists(meta):
        with open(meta, 'w', newline='\n') as f:
            f.write(FOLDER_META.format(guid=guid_for(rel)))


def export(group, name, canvas):
    return export_image(group, name, canvas.image())


def export_image(group, name, img):
    folder = os.path.join(OUT, group)
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name + '.png')
    img.save(path)
    rel = os.path.relpath(path, ROOT).replace(os.sep, '/')
    g = guid_for(rel)
    with open(path + '.meta', 'w', newline='\n') as f:
        f.write(META.format(guid=g, sprite_id=hashlib.md5(g.encode()).hexdigest()))
    return name, img


def main():
    bgs = []
    exported = {'Items': [], 'Crops': [], 'Tiles': [], 'World': [], 'Player': [], 'Villagers': [], 'Spirit': []}
    for name, fn in items.ITEMS.items():
        exported['Items'].append(export('Items', name, fn()))
    for crop_id, stages in crops.CROPS.items():
        for i, fn in enumerate(stages):
            exported['Crops'].append(export('Crops', f'{crop_id}_{i}', fn()))
    for name, fn in crops.TILES.items():
        exported['Tiles'].append(export('Tiles', name, fn()))
    for name, fn in world.WORLD.items():
        exported['World'].append(export('World', name, fn()))
    for name, canvas in world.player_frames().items():
        exported['Player'].append(export('Player', name, canvas))
    for vid, fn in villagers.VILLAGERS.items():
        for i, canvas in enumerate(villagers.stages(fn())):
            exported['Villagers'].append(export('Villagers', f'{vid}_{i}', canvas))
    exported['World'].append(export('World', 'bubble', villagers.bubble()))
    for name, canvas in spirit.spirit_sprites().items():
        exported['Spirit'].append(export('Spirit', name, canvas))
    exported['Items'].append(export('Items', 'star_fragment', spirit.star_fragment()))
    exported['World'].append(export('World', 'donation_box', spirit.donation_box()))
    exported['World'].append(export('World', 'star_ladder', spirit.star_ladder()))
    bgs.append(export_image('World', 'bg_starhill', spirit.bg_starhill()))

    # ---- heart seasons: seasonal backgrounds and world-object variants ----
    for season in backgrounds.SEASONS:
        bgs.append(export_image('World', f'bg_farm_{season}', backgrounds.farm(season)))
        bgs.append(export_image('World', f'bg_market_{season}', backgrounds.market(season)))
    larch = [(90, 58, 16, 255), (160, 102, 26, 255), (216, 160, 48, 255), (246, 212, 112, 255)]
    exported['World'].append(export('World', 'tree_autumn', world.tree(larch)))
    for name in ('tree', 'rock', 'well', 'storage_box', 'exit_sign'):
        exported['World'].append(export('World', f'{name}_winter', backgrounds.snow_cap(world.WORLD[name]())))
    # ---- expanded farm map (drawn from Assets/Resources/Maps/farm.json) and the things you clear on it ----
    for season in backgrounds.SEASONS:
        bgs.append(export_image('World', f'bg_farmmap_{season}', farmmap.farmmap(season)))
    for name, fn in farmmap.DEBRIS.items():
        exported['World'].append(export('World', name, fn()))
    for name, canvas in farmmap.debris_variants().items():
        exported['World'].append(export('World', name, canvas))
    for name, fn in backgrounds.BACKGROUNDS.items():
        bgs.append(export_image('World', name, fn()))

    # folder metas (Resources/, Resources/PixelArt/, groups)
    for p in [os.path.join(ROOT, 'Assets', 'Resources'), OUT] + [os.path.join(OUT, g) for g in exported]:
        write_folder_meta(p)

    docs = os.path.join(ROOT, 'Docs')
    os.makedirs(docs, exist_ok=True)
    every = [img for group in exported.values() for img in group]
    preview.sheet(every, os.path.join(docs, 'pixelart_preview.png'), zoom=3, cols=12)
    # backgrounds: one 2x image per scene
    for name, img in bgs:
        img.resize((img.width * 2, img.height * 2), 0).save(os.path.join(docs, f'pixelart_{name}.png'))
    print('exported', sum(len(v) for v in exported.values()), 'sprites to', OUT)


if __name__ == '__main__':
    main()
