# 光盘存档工具

将一个仍会更新目录中的文件，按从旧到新的时间顺序，分装到多个光盘中，实现备份功能。

## 特点

- 将目录中的文件平铺到光盘中

- 按从旧到新的时间顺序排序，而不是按目录，这可以让新的数据无需插入到旧的光盘中

- 在备份时平铺所有文件，恢复时能够重建目录结构

- 支持根据时间，备份任意修改时刻后的文件，实现接续导出

## 日志

### 20220809

搭建基本框架，完成基本的导出功能

### 20220810

新增导出文件出错时支持重试、跳过、中断

新增支持停止

优化文件包列表的样式，支持右键复制时间

增加了运行时的IsEnable调整

预留了其他功能面板

### 20220811

新增黑名单功能

新增导出ISO功能

新增重建分析，使用树状图显示原始目录结构

新增重建目录功能，基本完成重建页面