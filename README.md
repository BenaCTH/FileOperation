# FileOperation
大文件拆分上传，下载

中心思想：
前台利用 file.slice() 将文件按照指定大小拆分
data.append("file", file.slice(start, end), name);

在后台接收拆分的文件，

download 时 当文件超出指定大小后按照HttpWebRequest 的当时请求文件。
