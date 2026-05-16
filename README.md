# ScreenGlow

一个 Windows 托盘小工具，用来控制 ESPHome/ESP8266 上的屏幕灯亮度。

## 使用方式

- 左键单击托盘图标：弹出亮度面板。
- 每个灯一个独立横向滑块，范围：`0-100`。
- 某个滑块设为 `0` 时，只关闭对应的灯。
- 某个滑块设为 `1-100` 时，只调节对应灯，并自动换算为 ESPHome 的 `1-255` 亮度。
- 滑块左侧显示每个灯的 emoji。
- 右键托盘图标：配置设备、实体、开机启动和退出。

## 右键菜单

右键托盘图标可以直接查看和修改配置：

- 开机自动启动，点击切换。
- 设备设置：包含 ESPHome 地址和实体名列表。
- 实体设置：只有填写实体名后才显示，每个实体可修改显示名称，支持 emoji。
- 退出。

开机启动会同时写入当前用户的注册表启动项和“启动”文件夹快捷方式，提升登录后自动启动的可靠性。如果移动了 `ScreenGlow.exe` 的位置，重新运行一次程序后会自动修复开机启动路径；也可以右键托盘图标点击 `开机自动启动（点击修复路径）` 手动修复。

设备地址例如：

```text
http://your-device.local/
```

实体名示例：

```text
left_light, right_light
```

## ESPHome 接口

程序使用 ESPHome Web Server 的 HTTP 接口：

```text
POST /light/<entity>/turn_on?brightness=<1-255>
POST /light/<entity>/turn_off
```

## 构建

```powershell
dotnet build -c Release
```

本地最小发布：

```powershell
dotnet publish ScreenGlow.csproj -c Release -r win-x64 --self-contained false -o publish\ScreenGlow /p:PublishSingleFile=true /p:DebugType=None /p:DebugSymbols=false
```

发布产物为单个 exe：

```text
publish\ScreenGlow\ScreenGlow.exe
```

## 自动发布

仓库包含 GitHub Actions workflow：

```text
.github/workflows/release.yml
```

推送到 `main` 分支时会自动构建 `win-x64` 版本，并上传 `ScreenGlow-win-x64.zip` 到 GitHub Release。发布包不会包含 PDB、日志或本地配置文件。
