# Unity 四元数旋转变换演示

本项目使用 Unity 2022.3 LTS 与 C# 实现四元数旋转教学演示。程序能够根据旋转轴和角度构造单位四元数，通过 Slerp 平滑插值旋转，并直观展示三维旋转的非交换性。

## 开发环境

- Unity：2022.3.62f3c1
- 编程语言：C#
- UI：uGUI + TextMeshPro
- 目标平台：Windows x86-64
- 视频：1280×720，30 fps，H.264 MP4

## 主要功能

- 调整旋转轴 X、Y、Z 分量和旋转角度。
- 调整 Slerp 插值参数 `t`。
- 播放、立即应用和重置旋转。
- 绕 X、Y、Z 轴旋转 90°的快捷预设。
- 任意轴旋转预设。
- 对比“先 X 后 Y”和“先 Y 后 X”，验证旋转乘法不满足交换律。
- 实时显示旋转轴、四元数、欧拉角、模长和插值参数。
- 显示局部坐标轴、旋转轴和 Slerp 方向轨迹。
- 自动播放完整教学流程，方便课堂演示和录制视频。

## 运行方法

1. 使用 Unity Hub 打开本项目。
2. 打开 `Assets/Scenes/QuaternionDemo.unity`。
3. 点击 Unity 顶部的 Play 按钮。

也可以直接运行已经构建的 Windows 程序：

```text
Builds/Windows/QuaternionRotationDemo.exe
```

## 操作说明

| 操作 | 功能 |
|---|---|
| Axis X/Y/Z | 修改旋转轴分量 |
| Angle | 修改旋转角度 |
| Interpolation | 手动修改 Slerp 参数 |
| PLAY SLERP | 从当前方向平滑旋转到目标方向 |
| APPLY | 立即应用目标旋转 |
| RESET | 恢复单位四元数 |
| X/Y/Z 90 | 绕对应坐标轴旋转 90° |
| ARBITRARY | 使用任意轴 `(1,1,0.5)` 旋转 |
| X THEN Y | 先绕 X 轴，再绕 Y 轴 |
| Y THEN X | 先绕 Y 轴，再绕 X 轴 |
| AUTO DEMO | 重新播放完整演示 |
| 数字键 1/2/3 | X/Y/Z 轴旋转预设 |
| Space | 播放当前 Slerp |
| R | 重置 |

## 数学原理

由单位旋转轴 `axis` 和角度 `theta` 构造四元数：

```text
q = [axis × sin(theta / 2), cos(theta / 2)]
```

使用四元数旋转向量：

```text
v' = q × (v, 0) × q^-1
```

球面线性插值：

```text
q(t) = Slerp(qA, qB, t),  0 <= t <= 1
```

Unity 中 `lhs * rhs` 表示先执行 `rhs`，再执行 `lhs`。一般情况下：

```text
qX × qY != qY × qX
```

因此三维旋转的执行顺序会影响最终姿态。

## 正确性测试

EditMode 自动测试位于 `Assets/Tests/EditMode/QuaternionMathTests.cs`，覆盖：

- 单位四元数不改变向量。
- 绕 X 轴 90°的旋转结果。
- 手动轴角公式与 `Quaternion.AngleAxis` 一致。
- 手动 Hamilton product 与 Unity 四元数乘法一致。
- `q` 与 `-q` 表示同一旋转。
- Slerp 端点和中点保持单位长度。
- 四元数旋转乘法不满足交换律。

测试报告保存在 `TestResults/EditModeResults.xml`。

## 演示视频

![演示画面](DemoVideo/preview.png)

[点击查看演示视频](DemoVideo/QuaternionRotationDemo.mp4)

## 主要代码

```text
Assets/Scripts/QuaternionMath.cs
Assets/Scripts/QuaternionRotationDemo.cs
Assets/Scripts/QuaternionAutoDemo.cs
Assets/Scripts/DemoFrameRecorder.cs
Assets/Editor/QuaternionDemoProjectBuilder.cs
```

场景中的模型和界面全部由程序自动生成，不依赖外部模型或贴图资源。

## 上传到 GitHub

在 GitHub 创建空仓库后执行：

```powershell
git remote add origin https://github.com/你的用户名/QuaternionRotationDemo.git
git push -u origin main
```
