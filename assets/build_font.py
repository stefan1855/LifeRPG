# -*- coding: utf-8 -*-
"""
把中文衬线字体按「界面实际用到的字」做子集，输出 woff2。
避免整包 13MB 塞进绿色包，同时保证离线可用。
"""
import os
import subprocess
import sys

ROOT = r"D:\Projects\LifeRPG\app\src"
SRC_FONT = r"D:\Projects\LifeRPG\assets\fonts-src\LXGWWenKaiLite-Regular.ttf"
OUT_FONT = r"D:\Projects\LifeRPG\app\src\assets\fonts\wenkai-subset.woff2"

# 界面里可能动态出现、但源码里没有的字（分类名、提示语、时间单位等）
EXTRA = (
    "日常工作日学习健康财务默认未设置其他"
    "一二三四五六七八九十百千万亿零年月日时分秒天周星期"
    "上午下午昨天今天明天本周上周本月下月"
    "完成已待办进行中逾期撤销删除编辑保存取消确认"
    "重要紧急不轻缓高中有无"
    "体魄心智纪律表达创造资源"
    "等级经验契约灵魂暗影绿火毁灭术哥特"
    "记录图片证据复盘归档设置统计"
    "备份恢复导出导入目录路径选择"
    "分钟小时天前刚刚"
    "，。、；：？！“”‘’（）《》【】—…·￥%＋－×÷＝"
    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    " !\"#$&'()*+,-./:;<=>?@[\\]^_`{|}~"
)


def collect_chars():
    chars = set(EXTRA)
    for dirpath, _dirnames, filenames in os.walk(ROOT):
        for fn in filenames:
            if not fn.endswith((".vue", ".js", ".css", ".html")):
                continue
            p = os.path.join(dirpath, fn)
            try:
                with open(p, "r", encoding="utf-8") as f:
                    chars.update(f.read())
            except Exception:
                pass
    # 只保留可见字符，去掉控制符
    return "".join(sorted(c for c in chars if c.isprintable() and not c.isspace())) + " "


def main():
    text = collect_chars()
    tmp = os.path.join(os.path.dirname(SRC_FONT), "_charset.txt")
    with open(tmp, "w", encoding="utf-8") as f:
        f.write(text)
    print("chars:", len(text))

    os.makedirs(os.path.dirname(OUT_FONT), exist_ok=True)
    cmd = [
        sys.executable, "-m", "fontTools.subset", SRC_FONT,
        "--text-file=" + tmp,
        "--flavor=woff2",
        "--layout-features=*",
        "--no-hinting",
        "--desubroutinize",
        "--output-file=" + OUT_FONT,
    ]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        print("SUBSET FAILED")
        print(r.stdout[-2000:])
        print(r.stderr[-2000:])
        return 1
    size = os.path.getsize(OUT_FONT)
    print("OK  %s  %.1f KB" % (OUT_FONT, size / 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
