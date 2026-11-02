"""Lossless pixel-coordinate slicing of the user-supplied reference; no redraw or resizing."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/reference-ui-v1'
SOURCE=ROOT/'app/src/assets/theme/master_dashboard.png'
image=Image.open(SOURCE).convert('RGB')
assert image.size==(1672,941)
items=[]

def cut(name,box,kind='component',note='Reference pixels; may contain baked text or neighboring artwork.'):
    x0,y0,x1,y1=box
    assert 0<=x0<x1<=1672 and 0<=y0<y1<=941,(name,box)
    asset=image.crop(box)
    path=OUT/'raw-components'/(name+'.png')
    asset.save(path)
    assert asset.tobytes()==image.crop(box).tobytes()
    items.append({'id':name,'path':path.relative_to(OUT).as_posix(),'sourceRectXYWH':[x0,y0,x1-x0,y1-y0],'size':[x1-x0,y1-y0],'provenance':'exact-source-crop','kind':kind,'note':note})

regions={
 'region-character-card':(44,131,466,919),
 'region-experience-panel':(475,109,1043,347),
 'region-contract-panel':(475,355,1044,919),
 'region-equipment-panel':(1045,109,1350,709),
 'region-history-panel':(1347,113,1615,709),
 'region-portals-panel':(1045,715,1617,919),
 'region-header-crown':(465,0,1065,112),
 'region-character-art':(63,150,446,619),
 'region-character-floor-nameplate':(60,575,450,704),
 'region-contract-wheel':(496,405,1021,840),
 'region-contract-pedestal':(494,823,1032,914),
 'region-equipment-silhouette':(1158,155,1343,698),
 'outer-corner-tl':(0,0,215,155),
 'outer-corner-tr':(1457,0,1672,155),
 'outer-corner-bl':(0,761,180,941),
 'outer-corner-br':(1492,761,1672,941),
 'outer-edge-top-left':(210,0,470,36),
 'outer-edge-top-right':(1060,0,1462,34),
 'outer-edge-left':(0,150,45,765),
 'outer-edge-right':(1615,150,1672,765),
 'outer-edge-bottom':(172,907,1499,941),
 'title-reference-main':(682,20,952,78),
 'title-reference-experience':(720,121,819,151),
 'title-reference-contract':(699,376,811,404),
 'title-reference-character-name':(190,602,364,659),
 'title-reference-character-role':(192,665,367,700),
 'title-reference-description':(118,706,396,784),
 'number-reference-level':(85,174,126,241),
 'number-reference-total-xp':(540,188,681,227),
 'number-reference-next-xp':(712,193,834,226),
 'number-reference-node-level':(548,459,605,481),
 'button-avatar-select':(91,795,254,837),
 'button-avatar-clear':(262,795,427,837),
 'button-character-edit':(94,843,429,895),
 'button-refresh':(1435,32,1577,82),
 'button-history-all':(1371,656,1572,697),
 'badge-level-pennant':(76,153,139,260),
 'bar-experience-complete':(494,252,1025,309),
 'bar-experience-left-cap':(491,251,550,309),
 'bar-experience-right-cap':(970,251,1029,309),
 'bar-experience-fill-clean':(552,272,727,292),
 'bar-experience-unfilled-clean':(880,273,984,291),
 'panel-character-header-edge':(50,130,464,156),
 'panel-character-left-edge':(44,155,67,895),
 'panel-character-right-edge':(438,155,466,895),
 'panel-character-bottom-edge':(61,895,450,920),
 'panel-experience-header':(477,108,1042,159),
 'panel-experience-bottom-edge':(487,335,1033,349),
 'panel-contract-header':(477,355,1042,415),
 'panel-equipment-header':(1048,111,1348,155),
 'panel-history-header':(1347,119,1613,169),
 'panel-portals-header':(1047,716,1614,769),
 'portal-row-skill':(1063,769,1601,810),
 'portal-row-contract':(1063,813,1601,854),
 'portal-row-reputation':(1063,858,1601,901),
 'portal-icon-skill':(1072,773,1114,807),
 'portal-icon-contract':(1072,814,1114,851),
 'portal-icon-reputation':(1072,858,1114,895),
 'portal-chevron-right':(1553,779,1573,801),
 'portal-chevron-up':(1319,733,1344,757),
 'icon-refresh':(1452,45,1478,72),
 'crest-title-left':(635,25,677,71),
 'crest-title-right':(965,28,1008,70),
 'material-empty-panel-clean':(495,160,526,240),
 'material-dark-equipment-clean':(1170,175,1205,262),
 'material-iron-clean':(15,309,33,444),
 'separator-history-clean':(1360,246,1586,256),
}
for name,box in regions.items():
    cut(name,box,'typography-reference' if name.startswith(('title-','number-')) else 'component')

nodes=[
 ('flesh',(609,411,710,518),(637,443,678,483),(511,505,613,529)),
 ('will',(785,411,886,518),(812,444,852,483),(902,506,1005,530)),
 ('curse',(532,565,635,674),(564,603,606,643),(497,663,605,688)),
 ('whisper',(848,562,949,672),(876,597,918,641),(901,667,1008,691)),
 ('fel',(605,704,709,812),(634,739,677,783),(521,805,626,831)),
 ('soul',(783,704,887,813),(812,738,853,783),(895,808,1003,833)),
]
for name,frame,glyph,bar in nodes:
    cut('node-'+name+'-frame',frame,'rune-frame')
    cut('node-'+name+'-glyph',glyph,'rune-glyph')
    cut('node-'+name+'-bar',bar,'mini-progress')
cut('contract-center-emblem',(642,520,850,731),'core-emblem')

slots=['head','shoulders','chest','bracers','hands','waist','legs','feet','accessory-1','accessory-2','mainhand','offhand']
for index,name in enumerate(slots):
    y=160+index*44
    cut('equipment-slot-'+name,(1066,y,1116,y+44),'equipment-slot')
    cut('equipment-icon-'+name,(1073,y+6,1108,y+38),'equipment-icon')

for index,name in enumerate(['fel-surge','void-will','blood-flesh','curse-bind','whisper-tongue','soul-fragment']):
    y=183+index*80
    cut('history-row-'+name,(1357,y,1601,min(y+78,658)),'history-row')
    cut('history-icon-'+name,(1362,y+2,1409,y+52),'history-icon')

# Source coverage tiles cover every source pixel exactly once and reconstruct it losslessly.
xs=[0,44,466,475,1044,1347,1615,1672]
ys=[0,110,131,347,355,709,715,919,941]
canvas=Image.new('RGB',image.size)
tiles=[]
for row,(y0,y1) in enumerate(zip(ys,ys[1:])):
    for col,(x0,x1) in enumerate(zip(xs,xs[1:])):
        tile=image.crop((x0,y0,x1,y1))
        name=f'r{row:02d}-c{col:02d}.png'
        tile.save(OUT/'source-grid'/name)
        canvas.paste(tile,(x0,y0))
        tiles.append({'path':'source-grid/'+name,'sourceRectXYWH':[x0,y0,x1-x0,y1-y0]})
assert canvas.tobytes()==image.tobytes()
canvas.save(OUT/'previews/source-reconstructed.png')

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',12)
preview=Image.new('RGB',(1200,((len(items)+5)//6)*132),(14,19,14))
d=ImageDraw.Draw(preview)
for index,entry in enumerate(items):
    x=(index%6)*200;y=(index//6)*132
    im=Image.open(OUT/entry['path'])
    im.thumbnail((188,101),Image.Resampling.LANCZOS if hasattr(Image,'Resampling') else Image.LANCZOS)
    preview.paste(im,(x+(194-im.width)//2,y+(104-im.height)//2))
    d.text((x+5,y+107),entry['id'][:28],font=font,fill=(199,205,153))
    d.text((x+5,y+120),str(entry['sourceRectXYWH']),font=font,fill=(146,147,113))
preview.save(OUT/'previews/raw-slices-contact.png')

# Precise original-glyph montage for reference-led cleaning; preserves each crop's pixels.
for kind,names,columns,cell in [
 ('rune-icons-reference',[f'node-{n[0]}-glyph' for n in nodes],3,(128,128)),
 ('equipment-icons-reference',[f'equipment-icon-{n}' for n in slots],4,(96,96)),
 ('history-icons-reference',[f'history-icon-{n}' for n in ['fel-surge','void-will','blood-flesh','curse-bind','whisper-tongue','soul-fragment']],3,(128,128)),
]:
    atlas=Image.new('RGB',(columns*cell[0],((len(names)+columns-1)//columns)*cell[1]),(5,8,5))
    for i,name in enumerate(names):
        im=Image.open(OUT/'raw-components'/(name+'.png'))
        atlas.paste(im,((i%columns)*cell[0]+(cell[0]-im.width)//2,(i//columns)*cell[1]+(cell[1]-im.height)//2))
    atlas.save(OUT/'source'/(kind+'.png'))

manifest={'source':'source/reference.png','sourceSize':list(image.size),'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'pixelDataSha256':hashlib.sha256(image.tobytes()).hexdigest(),'coordinateConvention':'integer pixels; origin top-left; rectangles use [x,y,width,height]; crops are not rescaled','rawComponents':items,'sourceGrid':tiles,'validation':{'everyPixelCovered':True,'reconstructionPixelIdentical':True,'rawComponentCount':len(items),'sourceTileCount':len(tiles)},'warning':'Raw crops deliberately preserve baked source text and occlusions. They are provenance/reference assets; clean reusable layers are listed separately in ready-manifest.json.'}
(OUT/'slice-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest['validation']))

if __name__=='__main__':
    pass
