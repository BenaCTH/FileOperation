# FileOperation
大文件拆分上传，下载

中心思想：
前台利用 file.slice() 将文件按照指定大小拆分
data.append("file", file.slice(start, end), name);

在后台接收拆分的文件，

download 时 当文件超出指定大小后按照HttpWebRequest 的当时请求文件。

# Flex 实现多个div 等分
      .parent {
            display: flex;
        }

        .child {
            flex: 1 1 0;
            height: 30px;
            overflow:hidden;
            white-space:nowrap;
        }

            .child + .child {
                margin-left: 10px;
            }
           <div class="parent" style="background-color: lightgrey;">
                <div class="child" style="background-color: lightblue;">1sdadawsdfasfasdfeqw asdasdasdweqe dadsa dad as</div>
                <div class="child" style="background-color: lightblue;">2</div>
                <div class="child" style="background-color: lightblue;">3</div>
                <div class="child" style="background-color: lightblue;">4</div>
            </div>
            <input type="button" value="Add" id="add" />
