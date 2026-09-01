# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 规范，
版本号遵循 [语义化版本（Semantic Versioning）](https://semver.org/lang/zh-CN/)。

## [Unreleased]

## [1.0.0] - 2026-09-01

### Added

- 首次发布：本地文件锁工具产品版本。
- 加锁/解锁核心逻辑（`FileLocker.cs`），支持通过复制、重命名与字节差异校验实现文档加锁与还原。
- 图形界面主窗体（`MainForm.cs`），支持源文件/文件夹选择、输出目录与实时进度显示。
- 命令行模式（`Program.cs`），支持 `-lock` / `-unlock` / `-output` 等参数，无参数时默认进入图形界面。
- 文件夹批量递归处理，自动跳过解锁中间产物 `.backup`。
- 图形界面模式下自动隐藏控制台窗口。
- 项目工程与解决方案（`net48`，产物名 `wps.exe`）。
- 使用文档 `README.md` 与许可证。

[Unreleased]: https://github.com/shihongxin1997/LocalFileLocker/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/shihongxin1997/LocalFileLocker/releases/tag/v1.0.0