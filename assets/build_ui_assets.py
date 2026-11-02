# -*- coding: utf-8 -*-
"""
程序化合成 UI 框架贴图：把生成的金属材质加工成可九宫格拉伸的浮雕边框、
按钮底板、进度条槽与填充。解决「细线网页感」，做出铸铁厚边 + 倒角高光。
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageChops

TEX = r"D:\Projects\LifeRPG\app\src\assets\textures"
OUT = TEX


def load(name, size):
    p = os.path.join(TEX, name + ".webp")
    im = Image.open(p).convert("RGB")
    return im.resize((size, size), Image.LANCZOS)


def tint(im, color, strength=0.55):
    solid = Image.new("RGB", im.size, color)
    return Image.blend(im, solid, strength)


def noise(im, amount=10):
    n = Image.effect_noise(im.size, amount).convert("L")
    return ImageChops.add(im, Image.merge("RGB", (n, n, n)), scale=1, offset=-amount // 2)


def bevel_box(draw, box, light=(198, 170, 96), dark=(18, 14, 6), t=3):
    """在矩形内画倒角：上/左亮，下/右暗"""
    x0, y0, x1, y1 = box
    for i in range(t):
        a = int(255 * (1 - i / max(1, t)) * 0.85)
        # 上、左
        draw.line([(x0 + i, y0 + i), (x1 - i, y0 + i)], fill=light + (a,) if len(light) == 3 else light, width=1)
        draw.line([(x0 + i, y0 + i), (x0 + i, y1 - i)], fill=light, width=1)
        # 下、右
        draw.line([(x0 + i, y1 - i), (x1 - i, y1 - i)], fill=dark, width=1)
        draw.line([(x1 - i, y0 + i), (x1 - i, y1 - i)], fill=dark, width=1)


def make_frame(size=120, border=30, tex="mat_metal_bronze", name="frame_panel"):
    base = load(tex, size)
    base = tint(base, (44, 36, 16), 0.62)
    base = noise(base, 8)
    img = base.convert("RGBA")
    d = ImageDraw.Draw(img)

    # 外描边（近黑）
    d.rectangle([0, 0, size - 1, size - 1], outline=(10, 8, 3, 255), width=2)
    # 外倒角
    bevel_box(d, (2, 2, size - 3, size - 3), light=(148, 126, 64), dark=(8, 6, 2), t=3)
    # 中段金线
    d.rectangle([6, 6, size - 7, size - 7], outline=(104, 86, 42, 200), width=1)
    # 内倒角（反向：内凹）
    bevel_box(d, (border - 2, border - 2, size - border + 1, size - border + 1), light=(120, 102, 52), dark=(6, 5, 2), t=2)
    # 内描边暗线
    d.rectangle([border, border, size - border - 1, size - border - 1], outline=(6, 8, 4, 255), width=2)

    # 四角铆钉
    r = 3
    for cx, cy in [(11, 11), (size - 12, 11), (11, size - 12), (size - 12, size - 12)]:
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(160, 138, 74, 255))
        d.ellipse([cx - r + 1, cy - r + 1, cx + r - 1, cy + r - 1], fill=(88, 72, 34, 255))
        d.ellipse([cx - 1, cy - 1, cx + 1, cy + 1], fill=(186, 164, 104, 255))

    # 中心挖空（保留 alpha=0，让 CSS 背景透出）
    inner = Image.new("RGBA", (size - 2 * border, size - 2 * border), (0, 0, 0, 0))
    img.paste(inner, (border, border))

    # 轻微外发光
    glow = img.split()[3].filter(ImageFilter.GaussianBlur(2))
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    halo = Image.new("RGBA", img.size, (124, 255, 31, 0))
    halo.putalpha(glow.point(lambda v: int(v * 0.22)))
    out = Image.alpha_composite(out, halo)
    out = Image.alpha_composite(out, img)

    path = os.path.join(OUT, name + ".webp")
    out.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, out.size[0], out.size[1]))


def make_bar(size=(96, 24), tex="mat_metal_iron", name="bar_trough"):
    w, h = size
    im = tint(load(tex, w), (40, 36, 18), 0.55).crop((0, 0, w, h))
    img = im.convert("RGBA")
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w - 1, h - 1], outline=(8, 8, 4, 255), width=1)
    # 内凹：上暗下亮
    d.line([(1, 1), (w - 2, 1)], fill=(6, 6, 3, 255))
    d.line([(1, 2), (w - 2, 2)], fill=(30, 26, 12, 255))
    d.line([(1, h - 2), (w - 2, h - 2)], fill=(120, 100, 50, 160))
    d.line([(1, h - 3), (w - 2, h - 3)], fill=(70, 58, 28, 140))
    path = os.path.join(OUT, name + ".webp")
    img.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, w, h))


def make_fill(size=(96, 24), name="bar_fill"):
    w, h = size
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    for x in range(w):
        for y in range(h):
            t = y / max(1, h - 1)
            # 上亮下暗的魂火渐变
            if t < 0.18:
                c = (233, 255, 176)
            elif t < 0.5:
                c = (150, 255, 70)
            elif t < 0.82:
                c = (105, 220, 25)
            else:
                c = (52, 130, 12)
            px[x, y] = c + (255,)
    d = ImageDraw.Draw(img)
    # 高光带
    d.line([(0, 2), (w, 2)], fill=(255, 255, 255, 170))
    d.line([(0, 1), (w, 1)], fill=(255, 255, 255, 90))
    # 竖向流动条纹
    for x in range(0, w, 6):
        d.line([(x, 0), (x, h)], fill=(255, 255, 255, 26))
    path = os.path.join(OUT, name + ".webp")
    img.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, w, h))


def make_button(size=(64, 32), tex="mat_metal_iron", name="btn_plate"):
    w, h = size
    im = tint(load(tex, w), (34, 30, 14), 0.6).crop((0, 0, w, h))
    img = im.convert("RGBA")
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w - 1, h - 1], outline=(9, 8, 4, 255), width=1)
    d.line([(1, 1), (w - 2, 1)], fill=(150, 128, 66, 200))
    d.line([(1, 1), (1, h - 2)], fill=(120, 102, 52, 170))
    d.line([(1, h - 2), (w - 2, h - 2)], fill=(14, 12, 6, 255))
    d.line([(w - 2, 1), (w - 2, h - 2)], fill=(18, 16, 8, 255))
    d.rectangle([2, 2, w - 3, h - 3], outline=(96, 80, 38, 120), width=1)
    path = os.path.join(OUT, name + ".webp")
    img.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, w, h))


def make_rune_watermark(name="rune_watermark", size=512, feather=0.62):
    """空状态用的符文阵水印：单色化 + 径向羽化，杜绝矩形硬边"""
    src = Image.open(os.path.join(TEX, "orn_magic_circle.webp")).convert("RGBA")
    src = src.resize((size, size), Image.LANCZOS)
    alpha = src.split()[3]

    # 径向羽化遮罩：中心 1，到 feather 半径开始衰减，边缘归零
    mask = Image.new("L", (size, size), 0)
    mpx = mask.load()
    cx = cy = size / 2.0
    maxd = (cx * cx + cy * cy) ** 0.5
    for y in range(size):
        dy = y - cy
        for x in range(size):
            dx = x - cx
            d = ((dx * dx + dy * dy) ** 0.5) / maxd
            if d <= feather:
                v = 255
            else:
                t = (d - feather) / (1.0 - feather)
                v = int(255 * max(0.0, 1.0 - t) ** 1.6)
            mpx[x, y] = v

    from PIL import ImageChops
    feathered = ImageChops.multiply(alpha, mask)

    green = Image.new("RGBA", (size, size), (124, 255, 31, 255))
    green.putalpha(feathered.point(lambda v: int(v * 0.38)))
    path = os.path.join(OUT, name + ".webp")
    green.save(path, "WEBP", quality=90, method=6)
    print("OK   %-18s %5d KB %dx%d (feathered)" % (name, os.path.getsize(path) // 1024, size, size))


def make_corner(name="orn_corner", size=64):
    """哥特四角包角铸件：切除黑底、提取 L 臂并置于左上角，供 4 向翻转复用"""
    raw_path = os.path.join(r"D:\Projects\LifeRPG\assets\raw", "orn_corner.png")
    if not os.path.exists(raw_path):
        return
    raw_corner = Image.open(raw_path).convert("RGB")
    corner_rgba = raw_corner.convert("RGBA")
    alpha = corner_rgba.convert("L").point(lambda v: min(255, int(v * 1.25)))
    corner_rgba.putalpha(alpha)
    bbox = alpha.point(lambda v: 255 if v > 25 else 0).getbbox()
    if bbox:
        corner_rgba = corner_rgba.crop(bbox)
    # 翻转让角尖位于左上 (0, 0)
    corner_tl = corner_rgba.transpose(Image.FLIP_TOP_BOTTOM)
    out = corner_tl.resize((size, size), Image.LANCZOS)
    path = os.path.join(OUT, name + ".webp")
    out.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, size, size))


def make_fluid_fill(size=(96, 24), name="bar_fill"):
    """流体进度条：使用生成的魂火纹理替换纯色渐变"""
    raw_path = os.path.join(r"D:\Projects\LifeRPG\assets\raw", "tex_soulfluid.png")
    w, h = size
    if os.path.exists(raw_path):
        raw_fluid = Image.open(raw_path).convert("RGB")
        img = raw_fluid.resize((w, h), Image.LANCZOS).convert("RGBA")
    else:
        # 降级兜底
        make_fill(size, name)
        return
    d = ImageDraw.Draw(img)
    d.line([(0, 1), (w, 1)], fill=(255, 255, 255, 120))
    d.line([(0, 2), (w, 2)], fill=(220, 255, 160, 80))
    d.line([(0, h - 1), (w, h - 1)], fill=(10, 30, 8, 140))
    path = os.path.join(OUT, name + ".webp")
    img.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d (fluid)" % (name, os.path.getsize(path) // 1024, w, h))


def make_skill_plate(name="plate_skill", size=80):
    """技能图标底框：镂空中部，暗铁倒角外框"""
    raw_path = os.path.join(r"D:\Projects\LifeRPG\assets\raw", "plate_skill.png")
    if not os.path.exists(raw_path):
        return
    raw_plate = Image.open(raw_path).convert("RGB")
    plate_rgba = raw_plate.convert("RGBA")
    alpha = plate_rgba.convert("L").point(lambda v: min(255, int(v * 1.3)))
    plate_rgba.putalpha(alpha)
    bbox = alpha.point(lambda v: 255 if v > 20 else 0).getbbox()
    if bbox:
        plate_rgba = plate_rgba.crop(bbox)
    out = plate_rgba.resize((size, size), Image.LANCZOS)
    path = os.path.join(OUT, name + ".webp")
    out.save(path, "WEBP", quality=92, method=6)
    print("OK   %-18s %5d KB %dx%d" % (name, os.path.getsize(path) // 1024, size, size))


if __name__ == "__main__":
    make_frame()
    make_frame(name="frame_panel_iron", tex="mat_metal_iron")
    # 挂件专用：小尺寸边框，避免通用 120px 图在 340px 宽被压扁
    make_frame(size=72, border=18, name="frame_panel_sm")
    make_bar()
    make_fill()
    make_button()
    make_rune_watermark()
    make_corner()
    make_fluid_fill()
    make_skill_plate()
    print("DONE")
