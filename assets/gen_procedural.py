# -*- coding: utf-8 -*-
"""
程序化补生成：模型额度受限时用噪声/径向渐变合成特效贴图
- fx_fog    绿雾（多倍频值噪声 + 径向衰减）
- fx_embers 火星（随机亮点 + 高斯模糊 + 亮度转 alpha）
- fx_vignette 暗角（径向渐变，直接可用）
"""
import os
import random

from PIL import Image, ImageFilter

OUT = r"D:\Projects\LifeRPG\app\src\assets\textures"


def value_noise(size, cells, seed):
    """简易值噪声：低频网格 + 双线性插值"""
    rnd = random.Random(seed)
    w, h = size
    grid = [[rnd.random() for _ in range(cells + 1)] for _ in range(cells + 1)]

    def lerp(a, b, t):
        return a + (b - a) * t

    def smooth(t):
        return t * t * (3 - 2 * t)

    img = Image.new("L", size)
    px = img.load()
    for y in range(h):
        gy = y / h * cells
        y0 = int(gy)
        ty = smooth(gy - y0)
        for x in range(w):
            gx = x / w * cells
            x0 = int(gx)
            tx = smooth(gx - x0)
            top = lerp(grid[y0][x0], grid[y0][x0 + 1], tx)
            bot = lerp(grid[y0 + 1][x0], grid[y0 + 1][x0 + 1], tx)
            px[x, y] = int(lerp(top, bot, ty) * 255)
    return img


def gen_fog(size=(1024, 576)):
    w, h = size
    acc = Image.new("L", size, 0)
    for i, (cells, weight, seed) in enumerate([(4, 1.0, 11), (8, 0.55, 22), (16, 0.28, 33), (32, 0.14, 44)]):
        n = value_noise(size, cells, seed)
        n = n.point(lambda v, w=weight: int(v * w))
        acc = Image.blend(acc, n, 0.5) if i else n
    # 径向衰减，中心亮四周暗
    mask = Image.new("L", size, 0)
    mpx = mask.load()
    cx, cy = w / 2, h / 2
    maxd = (cx ** 2 + cy ** 2) ** 0.5
    for y in range(h):
        for x in range(w):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 / maxd
            v = max(0.0, 1.0 - d * 1.15)
            mpx[x, y] = int(255 * (v ** 1.6))
    alpha = Image.composite(acc, Image.new("L", size, 0), mask).filter(ImageFilter.GaussianBlur(6))
    rgba = Image.new("RGBA", size, (124, 255, 31, 0))
    rgba.putalpha(alpha)
    # 绿色渐变填充
    grad = Image.new("RGBA", size)
    gpx = grad.load()
    for y in range(h):
        for x in range(w):
            t = y / h
            r = int(60 + 70 * (1 - t))
            g = int(200 + 55 * (1 - t))
            b = int(20 + 40 * (1 - t))
            gpx[x, y] = (r, g, b, 255)
    grad.putalpha(alpha)
    return grad


def gen_embers(size=(768, 432), count=260):
    w, h = size
    layer = Image.new("L", size, 0)
    px = layer.load()
    rnd = random.Random(2026)
    for _ in range(count):
        cx = rnd.randint(0, w - 1)
        cy = rnd.randint(0, h - 1)
        # 下半部更密集，模拟上升火星
        if rnd.random() < 0.65 and cy < h * 0.35:
            cy = rnd.randint(int(h * 0.35), h - 1)
        rad = rnd.choice([1, 1, 1, 2, 2, 3])
        bright = rnd.randint(120, 255)
        for dy in range(-rad, rad + 1):
            for dx in range(-rad, rad + 1):
                x, y = cx + dx, cy + dy
                if 0 <= x < w and 0 <= y < h:
                    d = (dx * dx + dy * dy) ** 0.5
                    v = int(bright * max(0.0, 1 - d / (rad + 0.6)))
                    if v > px[x, y]:
                        px[x, y] = v
    layer = layer.filter(ImageFilter.GaussianBlur(1.2))
    rgba = Image.new("RGBA", size, (200, 255, 150, 0))
    rgba.putalpha(layer)
    return rgba


def gen_vignette(size=(1024, 576)):
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    px = img.load()
    cx, cy = w / 2, h / 2
    maxd = (cx ** 2 + cy ** 2) ** 0.5
    for y in range(h):
        for x in range(w):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 / maxd
            a = int(255 * max(0.0, (d - 0.45) / 0.55) ** 1.5)
            px[x, y] = (0, 0, 0, a)
    return img


def save(img, name, quality=88):
    path = os.path.join(OUT, name + ".webp")
    img.save(path, "WEBP", quality=quality, method=6)
    print("OK   %-14s %5d KB %s" % (name, os.path.getsize(path) // 1024, "%dx%d" % img.size))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    save(gen_fog(), "fx_fog")
    save(gen_embers(), "fx_embers")
    save(gen_vignette(), "fx_vignette")
    print("DONE")
