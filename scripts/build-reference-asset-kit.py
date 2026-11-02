"""Deterministic atlas slicing, inventory and visual QA; never edits application UI.

Run after slice-reference-ui.py and after generated PNG masters have been selected.
Image creation/restoration is by built-in imagegen; this script only crops, copies,
renders font specimens, and assembles explicitly labelled QA contact sheets.
"""
from pathlib import Path
import hashlib
import json
import math
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
KIT = ROOT / 'assets/reference-ui-v1'
READY = KIT / 'ready'
PREVIEW = KIT / 'previews'
RESAMPLE = Image.Resampling.LANCZOS if hasattr(Image, 'Resampling') else Image.LANCZOS
assets = []

def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def register(name, im, source, rect, group, provenance='ai-restored-atlas-crop', **extra):
    dest = READY / group / (name + '.png')
    dest.parent.mkdir(parents=True, exist_ok=True)
    im.save(dest)
    alpha = im.getchannel('A') if 'A' in im.getbands() else None
    item = dict(id=name, path=dest.relative_to(KIT).as_posix(), group=group,
                size=list(im.size), mode=im.mode, sha256=sha(dest), source=source,
                sourceRectXYWH=list(rect), provenance=provenance,
                bakedUiText=False, alphaRange=list(alpha.getextrema()) if alpha else None)
    item.update(extra)
    assets.append(item)
    return im

def crop(master, name, box, group, **extra):
    path = KIT / 'generated' / (master + '.png')
    im = Image.open(path)
    x0,y0,x1,y1 = box
    assert 0 <= x0 < x1 <= im.width and 0 <= y0 < y1 <= im.height, (master,name,box,im.size)
    return register(name, im.crop(box), path.relative_to(KIT).as_posix(),
                    (x0,y0,x1-x0,y1-y0), group, **extra)

def grid(master, names, cols, rows, group):
    im = Image.open(KIT / 'generated' / (master + '.png'))
    assert len(names) == cols*rows
    for i,name in enumerate(names):
        c,r=i%cols,i//cols
        box=(round(c*im.width/cols),round(r*im.height/rows),
             round((c+1)*im.width/cols),round((r+1)*im.height/rows))
        crop(master,name,box,group)

def whole(master, group, **extra):
    im=Image.open(KIT/'generated'/(master+'.png'))
    return crop(master,master,(0,0,im.width,im.height),group,
                provenance='ai-restored-or-extended-master',**extra)

def nine_slice(master, prefix, insets, group):
    im=Image.open(KIT/'generated'/(master+'.png'))
    left,top,right,bottom=insets
    xs=[0,left,im.width-right,im.width]
    ys=[0,top,im.height-bottom,im.height]
    for r in range(3):
        for c in range(3):
            if r==1 and c==1: continue  # alpha center intentionally omitted
            crop(master,f'{prefix}-{r}{c}',(xs[c],ys[r],xs[c+1],ys[r+1]),group,
                 nineSliceCell=[r,c],nineSliceInsetsLTRB=insets)

for master,group in [
    ('background-world-extended','backgrounds'),('background-world-ultrawide','backgrounds'),
    ('character-scene-extended','backgrounds'),('equipment-empty-scene','backgrounds'),
    ('panel-material-extended','materials'),('character-cutout','character'),
    ('outer-chrome-frame','frames'),('panel-frame-clean','frames'),
    ('character-card-frame','frames'),('contract-core-emblem','contract'),
    ('contract-orbit','contract'),('ritual-pedestal','contract')]:
    whole(master,group)

nine_slice('panel-frame-clean','panel-frame',[180,180,180,180],'frame-parts')
nine_slice('character-card-frame','character-frame',[210,200,210,200],'frame-parts')
# Outer title crown must remain independent; do not stretch the carved letters area.
for name,box in [
    ('outer-corner-tl',(0,0,300,200)),('outer-corner-tr',(1372,0,1672,200)),
    ('outer-corner-bl',(0,741,220,941)),('outer-corner-br',(1452,741,1672,941)),
    ('outer-title-crown',(450,0,1125,130)),
    ('outer-top-left-rail',(300,0,450,115)),('outer-top-right-rail',(1125,0,1372,115)),
    ('outer-left-rail',(0,200,85,741)),('outer-right-rail',(1587,200,1672,741)),
    ('outer-bottom-rail',(220,835,1452,941))]:
    crop('outer-chrome-frame',name,box,'frame-parts',layout='anchored original master coordinates')

grid('rune-glyph-atlas',[f'rune-{n}' for n in ['flesh','will','curse','whisper','fel','soul']],3,2,'runes')
grid('equipment-icon-atlas',[f'equipment-{n}' for n in ['head','shoulders','chest','bracers','hands','waist','legs','feet','accessory-1','accessory-2','mainhand','offhand']],4,3,'equipment')
grid('history-icon-atlas',[f'history-{n}' for n in ['fel-surge','void-will','blood-flesh','curse-bind','whisper-tongue','soul-fragment']],3,2,'history')
grid('node-frame-states',[f'node-{n}' for n in ['normal','hover','active','dormant']],2,2,'nodes')
grid('portal-control-icons',['icon-skill','icon-contract','icon-reputation','chevron-right','chevron-up','icon-refresh'],3,2,'controls')
for name,box in [('fx-mist',(0,512,512,1024)),('fx-embers',(512,512,1024,1024)),('fx-glint',(1024,512,1536,1024))]:
    crop('soul-fire-effects-clean',name,box,'effects')
for master,name in [('fire-wisp-left-contained','fx-fire-left'),('fire-wisp-right-contained','fx-fire-right'),('fire-ground-contained','fx-fire-ground')]:
    im=Image.open(KIT/'generated'/(master+'.png'))
    crop(master,name,(0,0,im.width,im.height),'effects',provenance='ai-restored-contained-fx')

# Atlas positions below were measured on actual output, not assumed from requested layout.
for name,box in [
    ('level-pennant',(120,100,510,700)),('equipment-empty-slot',(740,280,1130,660)),
    ('portal-row-empty',(10,880,670,1055)),('header-divider',(690,930,1220,1030))]:
    crop('small-ui-plates',name,box,'plates')

for state,y in [('normal',120),('hover',395),('pressed',669),('disabled',942)]:
    im=crop('button-states','button-'+state,(40,y,1214,y+220),'buttons',
            contentInsetsLTRB=[120,50,120,50],horizontalSliceInsetsLR=[120,120])
    for part,box in [('left',(40,y,160,y+220)),('middle',(160,y,1094,y+220)),('right',(1094,y,1214,y+220))]:
        crop('button-states',f'button-{state}-{part}',box,'button-parts')

for name,box,extra in [
    ('experience-empty',(0,120,1536,344),{'horizontalSliceInsetsLR':[200,200]}),
    ('experience-full',(0,335,1536,559),{'horizontalSliceInsetsLR':[200,200]}),
    ('experience-fill',(90,622,1448,716),{'usage':'clip horizontally; do not stretch end ornaments'}),
    ('stat-trough',(55,780,1480,895),{'horizontalSliceInsetsLR':[45,45]}),
    ('experience-left-cap',(0,120,200,344),{}),
    ('experience-right-cap',(1336,120,1536,344),{}),
    ('experience-middle-trough',(200,120,1336,344),{})]:
    crop('experience-bar-kit',name,box,'bars',**extra)

# Clean material/energy samples remain exact original pixels; no masking or redraw.
for name in ['material-empty-panel-clean','material-dark-equipment-clean','material-iron-clean',
             'bar-experience-fill-clean','bar-experience-unfilled-clean']:
    p=KIT/'raw-components'/(name+'.png'); im=Image.open(p)
    register('original-'+name,im,p.relative_to(KIT).as_posix(),(0,0,*im.size),'original-clean',
             provenance='exact-source-crop',usage='small original material sample; native-resolution only')

font_files={'chinese':'NotoSerifSC[wght].ttf','latin':'Cinzel[wght].ttf','compactNumbers':'RobotoCondensed[wght].ttf'}
roles={
    'mainTitle':dict(family='Noto Serif SC',weight=700,referencePx=44,color='#D6EE85'),
    'characterName':dict(family='Noto Serif SC',weight=600,referencePx=42,color='#B1E95B'),
    'panelTitle':dict(family='Noto Serif SC',weight=600,referencePx=23,color='#E4CA75'),
    'body':dict(family='Noto Serif SC',weight=400,referencePx=16,color='#C9C6AB'),
    'smallLabel':dict(family='Noto Serif SC',weight=500,referencePx=15,color='#C7B482'),
    'experienceTotal':dict(family='Noto Serif SC',weight=700,referencePx=37,color='#CEF16C'),
    'levelAndPercent':dict(family='Roboto Condensed',weight=700,referencePx=23,color='#C7F36A'),
    'latinOrnament':dict(family='Cinzel',weight=600,referencePx=22,color='#D6C080')}
font_manifest=dict(files=font_files,roles=roles,license='SIL Open Font License 1.1; accompanying per-family OFL files',
                   decision='Chinese titles/body follow the engraved Song-style serif reference; condensed sans only for compact level/percentage labels. Latin ceremonial text uses Cinzel.',
                   exactSourceFontIdentified=False,
                   sources={k:'https://github.com/google/fonts/tree/main/ofl/'+folder for k,folder in [('chinese','notoserifsc'),('latin','cinzel'),('compactNumbers','robotocondensed')]})
write_json(KIT/'fonts/font-roles.json',font_manifest)

def font(size=20,weight=400,family='chinese'):
    f=ImageFont.truetype(str(KIT/'fonts'/font_files[family]),size)
    axes=f.get_variation_axes()
    f.set_variation_by_axes([weight if a['name']==b'Weight' else a['default'] for a in axes])
    return f

def checker(size,light=(42,46,46),dark=(31,35,35),cell=16):
    im=Image.new('RGB',size,dark); d=ImageDraw.Draw(im)
    for y in range(0,size[1],cell):
        for x in range(0,size[0],cell):
            if (x//cell+y//cell)%2==0: d.rectangle((x,y,x+cell-1,y+cell-1),fill=light)
    return im

def sheet(entries,filename,cols=5,cw=300,ch=250,check=False):
    im=Image.new('RGB',(cols*cw,math.ceil(len(entries)/cols)*ch),(10,15,12)); d=ImageDraw.Draw(im)
    label=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
    for i,e in enumerate(entries):
        x,y=i%cols*cw,i//cols*ch
        tile=checker((cw-12,ch-48)) if check else Image.new('RGB',(cw-12,ch-48),(6,11,8))
        a=Image.open(KIT/e['path']).convert('RGBA');a.thumbnail((cw-24,ch-60),RESAMPLE)
        tile.paste(a,((tile.width-a.width)//2,(tile.height-a.height)//2),a)
        im.paste(tile,(x+6,y+6))
        d.text((x+7,y+ch-36),e['id'][:38],fill='#C6D2B0',font=label)
        d.text((x+7,y+ch-18),str(e['size'])+' / '+e['provenance'][:21],fill='#839778',font=label)
    im.save(PREVIEW/filename)

sheet([a for a in assets if a['group'] not in ['frame-parts','button-parts','original-clean']], 'ready-assets-contact.png')
sheet([a for a in assets if a['group'] in ['character','frames','runes','equipment','nodes','controls','effects']], 'alpha-contact.png',check=True)
overview_ids=['background-world-ultrawide','character-scene-extended','character-cutout','outer-chrome-frame',
              'panel-frame-clean','character-card-frame','contract-core-emblem','contract-orbit',
              'rune-flesh','rune-fel','equipment-head','equipment-chest',
              'node-active','button-hover','experience-full','fx-fire-ground']
sheet([next(a for a in assets if a['id']==n) for n in overview_ids], 'delivery-overview.png',cols=4,check=True)

# Font matching specimen: literal reference text, chosen roles and alternatives.
spec=Image.new('RGB',(1600,1180),(7,12,9)); d=ImageDraw.Draw(spec)
d.text((45,22),'字体匹配样张 · 原图字样与实际内容',font=font(32,600),fill='#D9D18D')
ref=Image.open(KIT/'raw-components/title-reference-main.png'); ref.thumbnail((600,90),RESAMPLE)
spec.paste(ref,(45,95));d.text((710,100),'原图主标题（原始像素）',font=font(20),fill='#A6AF91')
samples=[('主标题 / Noto Serif SC 700','角色仪表盘',44,700,'chinese'),
         ('角色名 / Noto Serif SC 600','无面者',42,600,'chinese'),
         ('面板标题 / Noto Serif SC 600','经验区域　暗影契约　当前装备　最近变化',25,600,'chinese'),
         ('任务与正文 / Noto Serif SC 400','在阴影中行走，聆听灵魂的低语。完成冥想修炼，追寻未知的深渊。',21,400,'chinese'),
         ('小标签 / Noto Serif SC 500','契约之日　距离下一级　3小时前　查看全部日志',18,500,'chinese'),
         ('经验总量 / Noto Serif SC 700','125,760 / 150,000 EXP',39,700,'chinese'),
         ('等级与百分比 / Roboto Condensed 700','Lv. 27    Lv. 19    83.7%    +300 EXP',36,700,'compactNumbers'),
         ('拉丁装饰标题 / Cinzel 600','LIFERPG · SOUL CONTRACT',32,600,'latin')]
for i,(label,sample,size,weight,family) in enumerate(samples):
    y=202+i*112
    d.text((45,y),label,font=font(17,400),fill='#839477')
    d.text((45,y+28),sample,font=font(size,weight,family),fill='#D5DB9F' if i not in [1,5,6] else '#C2EE6F')
spec.save(PREVIEW/'font-specimen.png')

# A composition for art-layer QA, not an application screenshot or integration claim.
comp=Image.new('RGBA',(1672,941),(5,8,5,255))
def place(name,box):
    e=next(a for a in assets if a['id']==name);im=Image.open(KIT/e['path']).convert('RGBA')
    im=im.resize((box[2],box[3]),RESAMPLE);comp.alpha_composite(im,(box[0],box[1]))
place('background-world-extended',(0,0,1672,941))
for box in [(475,109,568,238),(475,355,569,564),(1045,109,305,600),(1347,113,268,596),(1045,715,572,204)]:
    # Layer the source-derived dark material before the empty frame.
    place('panel-material-extended',box);place('panel-frame-clean',box)
place('character-scene-extended',(49,140,409,763))
place('character-cutout',(65,165,377,566))
place('character-card-frame',(44,131,422,788))
place('outer-chrome-frame',(0,0,1672,941))
place('contract-orbit',(545,430,414,414))
place('ritual-pedestal',(493,760,540,180))
place('contract-core-emblem',(641,520,210,211))
for n,(x,y) in zip(['flesh','will','curse','whisper','fel','soul'],[(609,411),(785,411),(532,565),(848,562),(605,704),(783,704)]):
    place('node-normal',(x,y,103,109));place('rune-'+n,(x+7,y+7,89,95))
place('experience-empty',(493,251,532,77));place('experience-fill',(548,273,348,26))
for i,n in enumerate(['head','shoulders','chest','bracers','hands','waist','legs','feet','accessory-1','accessory-2','mainhand','offhand']):
    place('equipment-empty-slot',(1067,160+i*44,49,43));place('equipment-'+n,(1070,161+i*44,43,40))
place('equipment-empty-scene',(1163,159,178,537))
for i,n in enumerate(['fel-surge','void-will','blood-flesh','curse-bind','whisper-tongue','soul-fragment']):
    place('history-'+n,(1358,177+i*78,52,52))
for i,n in enumerate(['skill','contract','reputation']):
    place('portal-row-empty',(1064,768+i*44,522,42));place('icon-'+n,(1070,772+i*44,34,34));place('chevron-right',(1547,775+i*44,29,29))
for name,box in [('button-normal',(92,793,158,45)),('button-normal',(263,793,158,45)),('button-hover',(90,843,328,50)),('button-normal',(1434,31,140,53))]:
    place(name,box)
cd=ImageDraw.Draw(comp)
def txt(x,y,s,size=20,weight=500,color='#D6C080',family='chinese'):
    cd.text((x,y),s,font=font(size,weight,family),fill=color,stroke_width=1,stroke_fill='#030703')
txt(688,20,'角色仪表盘',44,700,'#D7EE85')
txt(150,605,'无面者',42,600,'#B7EA64');txt(194,665,'暗影学徒',23,600)
txt(127,709,'在阴影中行走，聆听灵魂的低语。',16,400,'#C9C6AB')
txt(144,738,'追寻知识，探索未知的深渊。',16,400,'#C9C6AB')
txt(112,802,'选择头像',17);txt(285,802,'清除头像',17);txt(164,855,'编辑角色资料',17,600,'#B6E76E')
txt(729,123,'经验区域',23,600);txt(708,371,'暗影契约',24,600)
txt(570,164,'灵魂能量',17);txt(544,187,'125,760',37,700,'#CEF16C')
txt(729,164,'距离下一级',17);txt(725,197,'24,240 EXP',25,700,'#C6C1AA','compactNumbers')
txt(902,164,'契约之日',17);txt(900,188,'2024-01-15',16,500)
txt(729,271,'83.7%',21,700,'#F2EFDD','compactNumbers');txt(635,315,'125,760 / 150,000 EXP',17)
txt(1102,123,'当前装备',23,600);txt(1390,127,'最近变化',23,600)
txt(1110,728,'展开技能与 NPC/地图',22,600,'#B5DE60')
for i,s in enumerate(['技能','恶魔契约','势力声望']):txt(1118,773+i*44,s,20,600)
for i,s in enumerate(['邪能造物','虚空意志','暗影血肉','诅咒束缚','低语之舌','灵魂碎片']):
    txt(1412,183+i*78,s+' +XP',16,500,'#C9D584');txt(1412,210+i*78,'完成行动，铭刻成长',13,400,'#C9C6AB')
txt(1465,46,'刷新数据',16,500)
comp.convert('RGB').save(PREVIEW/'layer-composition-demo.png')

# Verify literal glyph coverage, source crop integrity and image decode/alpha.
cmaps={k:TTFont(KIT/'fonts'/v).getBestCmap() for k,v in font_files.items()}
missing=[]
for _,sample,_,_,family in samples:
    for c in sample:
        if not c.isspace() and ord(c) not in cmaps[family]:missing.append(c)
assert not missing,missing
slice_manifest=json.loads((KIT/'slice-manifest.json').read_text(encoding='utf-8'))
original=Image.open(KIT/slice_manifest['source']).convert('RGB')
for e in slice_manifest['rawComponents']:
    x,y,w,h=e['sourceRectXYWH']
    assert Image.open(KIT/e['path']).tobytes()==original.crop((x,y,x+w,y+h)).tobytes()
for a in assets:
    im=Image.open(KIT/a['path']);im.verify()
    x,y,w,h=a['sourceRectXYWH']
    assert Image.open(KIT/a['path']).tobytes()==Image.open(KIT/a['source']).crop((x,y,x+w,y+h)).tobytes(),a['id']
    if a['group'] in ['character','frames','runes','equipment','nodes','effects','controls']:
        assert a['alphaRange'] and a['alphaRange'][0]==0,(a['id'],a['alphaRange'])
masters=[]
for p in sorted((KIT/'generated').glob('*.png')):
    im=Image.open(p)
    masters.append(dict(id=p.stem,path=p.relative_to(KIT).as_posix(),size=list(im.size),
                        sha256=sha(p),prompt='prompts/'+p.stem+'.json',
                        status='superseded' if p.stem=='soul-fire-effects' else 'selected',
                        selectionNote='Only bottom-row mist/embers/glint selected; first-row flames replaced by contained individual sprites.' if p.stem=='soul-fire-effects-clean' else ''))
write_json(KIT/'ready-manifest.json',dict(version='1.0',scope='Raster assets only; app not integrated',
    coordinateConvention='top-left integer pixels; rectangles [x,y,width,height]; no resampling of source crops',
    sourceSha256=slice_manifest['sourceSha256'],masters=masters,assets=assets))
qa=dict(originalComponentCount=len(slice_manifest['rawComponents']),sourceCoverageTiles=len(slice_manifest['sourceGrid']),
        originalPixelReconstructionIdentical=Image.open(PREVIEW/'source-reconstructed.png').tobytes()==original.tobytes(),
        allRawCropsMatchOriginal=True,readyTextureCount=len(assets),selectedGeneratedMasterCount=sum(m['status']=='selected' for m in masters),
        imageDecodePassed=True,allReadyCropsMatchRecordedSourcePixels=True,requiredTransparencyPassed=True,fontSampleMissingGlyphs=missing,
        framesHaveTransparentCenter={n:Image.open(KIT/'generated'/(n+'.png')).getchannel('A').getpixel((Image.open(KIT/'generated'/(n+'.png')).width//2,Image.open(KIT/'generated'/(n+'.png')).height//2))==0 for n in ['outer-chrome-frame','panel-frame-clean','character-card-frame']},
        limitations=['AI-restored and outpainted regions are authored interpretations, not original hidden pixels.',
                    'Exact source typeface cannot be identified conclusively from a raster; font choices are visual matches.',
                    'This kit covers the supplied dashboard reference; it does not invent unrelated application pages.',
                    'Composition demo is a static art-layer QA image, not a running application screenshot.',
                    'Requested generation sizes are not guaranteed; manifests record actual PNG dimensions.'])
write_json(KIT/'qa-report.json',qa)
print(json.dumps(qa,ensure_ascii=False,indent=2))
