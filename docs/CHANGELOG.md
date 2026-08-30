# 更新记录

## v1.5.4

- 修复本地玩家死亡时 `IsInBattle` 先变为 false，导致 v1.5.2 的中断标记被提前清除、同房复活仍重置总伤的问题。
- 现在会识别 `PlayerAvatar.IsDead`，并在复活过渡阶段持续保留中断状态；同房复活后恢复统计并保留双方玩家排行、总伤与房间序号。
- 修复返回联机大厅后仍显示上一局结果的问题；本局失败时先在死亡结算阶段冻结结果，玩家回到大厅后再完整清零。
- 房间识别与伤害记录现在必须处于有效地下城整局中；攻击联机大厅假人不会进入“房间识别中”。
- 新房间、不同楼层或正常新一轮战斗仍会重置。
- 48 项本地房间规则测试与 112 项语言测试通过；已于 2026-08-30 完成双人联机游戏验证。
- Preserves results through death and the failure screen, clears them on returning to the lobby, and prevents lobby training dummies from starting room detection. Two-player multiplayer testing completed on 2026-08-30.

## v1.5.1

- 游戏选择繁体中文（`zh-TW`）时，DPS 面板与 F9 设置改为使用简体中文，简体中文及其他语言的行为不变。
- 112 项本地语言测试与 29 项房间规则回归测试通过；已于 2026-08-28 完成游戏内验证。
- Traditional Chinese now shares the Simplified Chinese overlay and F9 settings. Other languages still use English. In-game testing passed on 2026-08-28.

## v1.5.0

- DPS 面板与 F9 设置自动跟随游戏语言：仅简体中文 `zh-CN` 使用中文，其他语言统一使用英语。
- 支持运行中切换语言，不重置伤害、房间或本局计时；玩家名字保持原样，未命名玩家的占位名称随语言更新。
- 增加中英文 README 和语言切换链接；打包时包含两种语言的说明。
- 85 项语言测试和 29 项房间规则回归测试通过；编译通过，并于 2026-08-27 完成游戏内测试。
- Automatic Chinese/English UI selection, live language switching without resetting statistics, and bilingual documentation. In-game testing passed on 2026-08-27.

## v1.4.3

- 修复多人分别在不同房间战斗时，伤害与 DPS 混在同一面板的问题。
- 结合本地玩家楼层与游戏原生战斗区域识别当前房间；只计入同层且所属玩家、受击目标都在该房间内的伤害。
- 切换房间或楼层时，即使战斗状态未变化，也会开始新一轮统计；伤害回调前同步检查房间状态。
- 无法识别有效房间区域时暂停统计并显示提示，避免混入其他房间的反馈。
- 新增 29 项独立房间匹配规则测试；分房统计修复已于 2026-08-27 实机测试通过。

## v1.4.2

- F9 设置菜单背景不透明度固定为 75%，文字和控件保持清晰。

## v1.4.1

- 本局用时跟随游戏整局状态更新，结束后冻结，新局开始后更新。

## v1.4.0

- 新增 DPS 主面板 60%–120% 缩放，设置窗口尺寸独立。
- 修正列表右侧多余留白，统一标题与数值列的对齐方式。
- 玩家超过 6 人时为滚动条预留空间。

## v1.3.2

- 移除玩家整帧输入与指针动作拦截，修复鼠标进入面板时移动键卡住、左右键被吞的问题。

## v1.3.1

- 打开 F9 设置时使用系统光标，关闭时恢复原来的光标状态。

## 更早版本

- 按战斗房间统计伤害与 DPS，取消空闲超时重置。
- 增加本局计时、F9 设置、位置锁定与主面板背景不透明度调整。
- 更新深色界面、列对齐、关闭按钮和 G-Yoka 署名。
- 修复窗口活动状态产生的白圈背景。
