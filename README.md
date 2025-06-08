<h1>Avalonia.SVirtualizingWrapPanel Project Description</h1>

<h2>Project Introduction</h2>
This project develops a custom virtualizing layout control for the Avalonia UI framework, named Avalonia.SVirtualizingWrapPanel. It should be noted that, due to the upcoming official VirtualizingWrapPanel from the Avalonia Accelerate program, the prefix "SVirtualizingWrapPanel" is used to avoid naming conflicts.

<h2>Development Background</h2>
As an amateur programming enthusiast, I developed this control to meet the needs of a virtualizing wrap layout in my personal projects. In the absence of existing solutions, I implemented this layout logic independently based on the design ideas of Avalonia's VirtualizingStackPanel (without directly referencing the WrapPanel source code). It should be emphasized:
<ul>
<li>While the control operates stably, there are inevitable gaps in performance optimization and code quality compared to implementations by professional teams.</li>
<li>If your project has strict performance requirements, it is recommended to wait for the official VirtualizingWrapPanel release.</li>
</ul>

<h2>Usage Instructions</h2>
Currently, the project provides a source-code-level integration solution (simply copy the SVirtualizingWrapPanel class into your project to use it), and no NuGet package has been released yet. Please note:
<ul>
<li>The GetIrregularSnapPoints and GetRegularSnapPoints methods are not yet implemented.</li>
<li>Maintenance and updates will be driven by the actual needs of my personal projects.</li>
</ul>

<h2>Open Source Statement</h2>
This project is fully open source under the MIT license, and we welcome:
<ul>
<li>Secondary development and optimization through forking.</li>
<li>Submitting issues to report problems.</li>
</ul>

Due to limited personal time and energy, I cannot provide professional-level maintenance support, but I promise to continuously fix significant issues discovered during my own use. I hope this project can serve as a temporary solution for community developers, and I appreciate your understanding and support.

Demonstration effect：
<img src="https://private-user-images.githubusercontent.com/30720036/378710908-840a6e62-488d-440d-80e6-ed3c901707d8.gif?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NDkzNzEzNzcsIm5iZiI6MTc0OTM3MTA3NywicGF0aCI6Ii8zMDcyMDAzNi8zNzg3MTA5MDgtODQwYTZlNjItNDg4ZC00NDBkLTgwZTYtZWQzYzkwMTcwN2Q4LmdpZj9YLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFWQ09EWUxTQTUzUFFLNFpBJTJGMjAyNTA2MDglMkZ1cy1lYXN0LTElMkZzMyUyRmF3czRfcmVxdWVzdCZYLUFtei1EYXRlPTIwMjUwNjA4VDA4MjQzN1omWC1BbXotRXhwaXJlcz0zMDAmWC1BbXotU2lnbmF0dXJlPWQyNjFlMDQxMzc4ZTgyNGZmYjNlNmI5MWU5NWI3N2RjODk3ZjRiOTk2NDg5YTNlOWU3NWJiMmJkNzQyMWE2MzEmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0In0.hLs7E7Xx-vy83FNp6Nb-6q50RRfKDxO3ve-3FgaIXE0"></img>
<hr>
<h1>Avalonia.SVirtualizingWrapPanel 项目描述</h1>

<h2>项目介绍</h2>
本项目为 Avalonia UI 框架开发了一款自定义虚拟化布局控件，命名为 Avalonia.SVirtualizingWrapPanel。需要说明的是，由于 Avalonia Accelerate 计划未来推出官方的 VirtualizingWrapPanel，为避免命名冲突，特此采用 "SVirtualizingWrapPanel" 作为前缀标识。

<h2>开发背景</h2>
作为一名业余编程爱好者，我开发此控件的初衷是为了满足个人项目中虚拟化换行布局的需求。在未找到现有实现方案的情况下，我基于 Avalonia 的 VirtualizingStackPanel 设计思路，独立实现了这套布局逻辑（未直接参考 WrapPanel 源码）。需要特别说明：
<ul>
<li>该控件虽能稳定运行，但在性能优化和代码质量方面与专业团队的实现必然存在差距</li>
<li>若您的项目对性能有严格要求，建议等待官方 VirtualizingWrapPanel 的发布</li>
</ul>

<h2>使用说明</h2>
当前提供源码级集成方案（直接复制 SVirtualizingWrapPanel 类到项目即可使用），暂未发布 NuGet 包。需注意：
<ul>
<li>GetIrregularSnapPoints 和 GetRegularSnapPoints 方法暂未实现</li>
<li>维护更新将基于个人项目中的实际使用需求进行</li>
</ul>

<h2>开源声明</h2>
本项目采用 MIT 协议完全开源，欢迎：
<ul>
<li>通过 Fork 进行二次开发与优化</li>
<li>提交 Issue 反馈问题</li>
</ul>

受限于个人时间精力，我无法提供专业级的维护支持，但承诺会持续修复个人使用过程中发现的重大问题。期待这个项目能为社区开发者提供临时解决方案，也感谢您的理解与支持。
