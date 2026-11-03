"""Build lossless runtime textures and offline fonts from the reviewed reference kit."""
from pathlib import Path
from PIL import Image
from fontTools.ttLib import TTFont
import hashlib,json

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT/'assets/reference-ui-v1'
OUT=ROOT/'app/src/assets/reference'
OUT.mkdir(parents=True,exist_ok=True)
m=json.loads((KIT/'ready-manifest.json').read_text(encoding='utf-8'))
ids=['background-world-extended','background-world-ultrawide','character-scene-extended',
     'equipment-empty-scene','panel-material-extended','character-cutout','outer-chrome-frame',
     'panel-frame-clean','character-card-frame','contract-core-emblem','contract-orbit',
     'ritual-pedestal','level-pennant','equipment-empty-slot','portal-row-empty','header-divider',
     'experience-empty','experience-fill','stat-trough']
ids += ['button-'+s for s in ['normal','hover','pressed','disabled']]
ids += ['node-'+s for s in ['normal','hover','active','dormant']]
ids += ['rune-'+s for s in ['flesh','will','curse','whisper','fel','soul']]
ids += ['history-'+s for s in ['fel-surge','void-will','blood-flesh','curse-bind','whisper-tongue','soul-fragment']]
ids += ['equipment-'+s for s in ['head','shoulders','chest','bracers','hands','waist','legs','feet','accessory-1','accessory-2','mainhand','offhand']]
ids += ['icon-'+s for s in ['skill','contract','reputation','refresh']]+['chevron-right','chevron-up']
records=[]
for name in ids:
    entry=next(a for a in m['assets'] if a['id']==name)
    source=KIT/entry['path']
    assert hashlib.sha256(source.read_bytes()).hexdigest()==entry['sha256'],name
    im=Image.open(source)
    box=(0,0,*im.size)
    # Tight crops keep the actual glyph at 24-32px, instead of scaling its empty cell.
    if name.startswith(('rune-','history-','equipment-','icon-','chevron-')) and name not in ['equipment-empty-scene','equipment-empty-slot']:
        if 'A' in im.getbands():
            bounds=im.getchannel('A').point(lambda a:255 if a>45 else 0).getbbox()
            if bounds:
                box=(max(0,bounds[0]-12),max(0,bounds[1]-12),min(im.width,bounds[2]+12),min(im.height,bounds[3]+12))
    im=im.crop(box)
    target=OUT/(name+'.webp');im.save(target,lossless=True,method=6)
    decoded=Image.open(target).convert('RGBA');expected=im.convert('RGBA')
    # Fully transparent RGB is not meaningful and may be canonicalized by WebP.
    assert decoded.getchannel('A').tobytes()==expected.getchannel('A').tobytes(),name
    assert Image.alpha_composite(Image.new('RGBA',im.size,(5,8,5,255)),decoded).tobytes()==Image.alpha_composite(Image.new('RGBA',im.size,(5,8,5,255)),expected).tobytes(),name
    records.append(dict(id=name,source=entry['path'],sourceSha256=entry['sha256'],cropXYXY=box,
                        size=list(im.size),file=target.name,sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
fonts=[]
for source,name in [('NotoSerifSC[wght].ttf','noto-serif-sc'),('RobotoCondensed[wght].ttf','roboto-condensed'),('Cinzel[wght].ttf','cinzel')]:
    ft=TTFont(KIT/'fonts'/source);ft.flavor='woff2';p=OUT/(name+'.woff2');ft.save(p)
    assert TTFont(p).getBestCmap()==TTFont(KIT/'fonts'/source).getBestCmap()
    fonts.append(dict(file=p.name,source=source,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
for family in ['notoserifsc','robotocondensed','cinzel']:
    (OUT/(family+'-OFL.txt')).write_bytes((KIT/'fonts'/(family+'-OFL.txt')).read_bytes())
(OUT/'manifest.json').write_text(json.dumps(dict(assets=records,fonts=fonts,method='Lossless WebP; integer crop only. Original PNG masters retained in the reference kit.'),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'textures':len(records),'fonts':len(fonts),'bytes':sum(p.stat().st_size for p in OUT.iterdir())}))
