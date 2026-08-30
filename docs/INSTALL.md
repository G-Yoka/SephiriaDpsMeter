# Sephiria DPS Meter v1.5.4 安装说明

中文使用说明见 README.md；English installation and usage: README.en.md.

此版本已通过编译、自动测试与双人联机游戏验证（2026-08-30）。

作者：G-Yoka · Steam AppID：2436940

需要：Windows 版 Sephiria、BepInEx 5（Unity Mono）。本包不附带加载器。

## 安装 / 更新

1. 退出游戏。
2. 在 Steam 库中右键《赛菲莉娅》→ 管理 → 浏览本地文件。
3. 将压缩包内的 `BepInEx` 文件夹合并到游戏根目录。
4. 确认最终路径是 `游戏目录/BepInEx/plugins/SephiriaDpsMeter.dll`。
5. 启动游戏，按 F9 打开设置；进入战斗后自动开始统计。

已安装旧版本时覆盖同名 DLL 即可；保留已有配置，不要同时放置多个版本。

若未安装 BepInEx，请先按 https://docs.bepinex.dev/articles/user_guide/installation/index.html 安装 BepInEx 5，启动一次游戏后再放入插件。请勿混装 BepInEx 5 与 6。

## 使用

- F9：显示 / 关闭设置菜单。
- 自动语言：游戏为简体中文或繁体中文时均显示简体中文，其他语言显示英语；游戏内切换语言后立即跟随，无需重启。
- 主面板：显示当前房间玩家伤害、占比、DPS、命中计数、房间用时与本局用时。
- 分房统计：其他房间或楼层的队友伤害不会混入；房间无法识别时暂停统计并显示提示。
- 死亡后保留：死亡时冻结结果，同一房间复活后继续累计；整局失败时保留到死亡结算，回到联机大厅后清零。
- 大厅隔离：联机大厅假人不会启动房间识别或伤害统计。
- 设置：主面板显示开关、固定位置、25%–100% 不透明度、60%–120% 缩放。
- 未固定位置时拖动主面板标题栏；设置菜单不暂停游戏，请在安全区域调整。
- 完整统计口径与多人限制见包内 README.md。

## 卸载

退出游戏后仅删除 `BepInEx/plugins/SephiriaDpsMeter.dll`，无需删除其他模组或加载器。

## 下载与反馈

https://github.com/G-Yoka/SephiriaDpsMeter

https://github.com/G-Yoka/SephiriaDpsMeter/releases/latest
