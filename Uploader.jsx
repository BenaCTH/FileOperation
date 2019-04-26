import * as React from 'react';
import * as GuiCommonServices from '../../Services/GuiCommon.Services';

$$.uploader.refresh = function (id) {
    $$.uploader.cache.forEach((cache) => {
        if (cache.value.indexOf(id) != -1) {
            cache.uploader.refreshFiles();
        }
    });
}
var uuid = 0;
export class Uploader extends React.Component {
    spliteCount = window.CommonUtil.uploadFileSplitCount || 20;
    id = "uploader-" + uuid++;
    cacheIndex = -1;
    cache = { key: this.id, value: [], uploader: this };
    constructor(props) {
        super(props);
        this.file = {};
        this.initState()
            .initBinding();
    }
    initState() {
        this.state = {
            files: [],
            isValidate: true,
            maxFileSize: this.props.maxFileSize || '5120 MB'
        };
        return this;
    }
    componentDidMount() {
        this.handleGetFilesByIds(this.props.files);
        this.handleGetFileSetting();
        $$.uploader.cache.push(this.cache);
        this.cacheIndex = $$.uploader.cache.findIndex(item => item.key === this.id);
    }
    componentWillUnmount() {
        $$.uploader.cache.splice(this.cacheIndex, 1);
    }
    updateCache() {
        $$.uploader.cache[this.cacheIndex].value = this.state.files.map((m) => { return m.fileId; });
    }
    handleGetFileSetting() {
        if (!this.props.maxFileSize) {
            try {
                GuiCommonServices.GetFileSetting({
                    host: this.props.host,
                    token: this.props.token
                }).then((data) => {
                    let maxFileSize = data && data.maxUploadSizeInMb ? data.maxUploadSizeInMb + ' MB' : DEFAULTMAXFILESIZE;
                    this.setState({
                        maxFileSize: maxFileSize
                    })
                });
            } catch (e) {
                $$.error("Uploader GetFileSetting Error:" + e);
            }

        }
    }
    handleGetFilesByIds(propFiles, isRefresh) {
        if (propFiles.length === 0) {
            this.setState({
                files: [],
                isValidate: true
            });
            return;
        }
        let fileIdList = [];
        propFiles.map((file) => { fileIdList.push(file.fileId) });
        if (!this.props.category instanceof Array || this.props.scopePath instanceof Array) {
            $$.warn("Uploader props category or scopePath must be Array .");
        }
        let param = {
            url: this.props.queryFilesByIdsUrl,
            host: this.props.host,
            token: this.props.token,
            data: {
                category: this.props.category || [],
                scopePath: this.props.scopePath || [],
                fileIds: fileIdList
            }
        }
        GuiCommonServices.QueryUploadFilesByIds(param).then((data) => {
            this.convertFiles(data, isRefresh);
        });
    }
    componentWillReceiveProps(nextProps) {
        if (this.props.files != nextProps.files) {
            this.handleGetFilesByIds(nextProps.files);
        }
        if (this.props.maxFileSize != nextProps.maxFileSize && nextProps.maxFileSize != this.state.maxFileSize) {
            this.setState({
                maxFileSize: nextProps.maxFileSize
            });
        }
    }
    convertFiles(data, isRefresh) {
        if (Array.isArray(data)) {
            this.file.disabled = false;
            for (var i = 0; i < data.length; i++) {
                data[i].fileSize = CommonUtil.convertFileSize(data[i].fileSize);
            }
            if (isRefresh || data.length === 0) {
                this.state.files = [];
            }
            if (this.props.isMultiple) {
                data.forEach((item, index) => {
                    item.disabled = false;
                    this.state.files.push(item)
                });
            } else {
                this.state.files = [];
                if (data.length > 0) {
                    $.extend(true, this.file, data[0]);
                    this.state.files.push(this.file);
                }
            }
            this.setState({
                isValidate: true,
                msg: "",
                files: [].concat(this.state.files)
            });
        }
        this.updateCache();
    }
    initBinding() {
        const eventsArr = [
            "handleUpload",
            "handleSuccess",
            "handleError",
            "handleGetFilesByIds",
            'handleRemove',
            'refreshFiles'
        ];
        eventsArr.forEach((ev) => { this[ev] = this[ev].bind(this); });
    }
    handleUpload(e, args) {
        let files = args.newValue;
        let data = new FormData();
        this.file = files && files.length > 0 ? files[files.length - 1] : {};
        this.props.isShowLoading && $$.loading(true);
        this.ajaxFile(this.file.file, 0, "");
    }
    ajaxFile(file, i, id) {
        var self = this,
            name = file.name,
            size = file.size,
            //Calculate the size of the splice file
            shardSize = this.spliteCount * 1024 * 1024,
            //Calculate the number of file splices
            shardCount = Math.ceil(size / shardSize);
        if (i >= shardCount) {
            return;
        }
        //Calculate the start and end positions of each slice
        var start = i * shardSize,
            end = Math.min(size, start + shardSize);
        let category = this.props.category instanceof Array && this.props.category.length > 0 ? this.props.category[0] : '',
            scopePath = this.props.scopePath instanceof Array && this.props.scopePath.length > 0 ? this.props.scopePath[0] : '';
        var data = new FormData();
        data.append("category", category);
        data.append("scopePath", scopePath);
        //The slice method is used to cut out part of a file
        data.append("file", file.slice(start, end), name);
        data.append("moduleId", this.props.moduleId);
        data.append("correlationId", id || "");
        data.append("fileName", name);
        data.append("totalCount", shardCount);
        //current file index
        data.append("index", i + 1);
        let param = {
            url: this.props.submitFileUrl,
            data: data,
            success: function (data) {
                $$.log("index:", i + 1);
                self.handleSuccess(data, file, shardCount);
            },
            error: this.handleError,
            host: this.props.host,
            token: this.props.token,
            category: category,
            scopePath: scopePath,
            beforeSend: this.props.beforeSend,
            complete: this.props.complete
        };
        GuiCommonServices.SubmitFileToServer(param);
    }
    handleSuccess(data, file, shardCount) {
        if (data != null) {
            let i = data.fileNumber++;
            $$.log('ajaxfile index:', i);
            var num = Math.ceil(i * 100 / shardCount);
            $$.log('percent:', num + '%');
            try {
                this.ajaxFile(file, i, data.info.correlationId);
                if (data.mergeResult) {
                    this.props.isShowLoading && $$.loading(false);
                    if (data.responseFiles instanceof Array && data.responseFiles.length > 0) {
                        this.convertFiles(data.responseFiles);
                        this.props.upload && this.props.upload(data.responseFiles, this.state.files);
                    } else {
                        this.uploadErrorSetting(data);
                    }
                }
            } catch (e) {
                this.uploadErrorSetting(data);
            }
        }
    }
    uploadErrorSetting(data) {
        this.setState({
            isValidate: false,
            msg: data.msg || I18N.getGUICommonValue("GC_AUI_Uploader_Filed_Message"),
            files: [].concat(this.state.files)
        }, () => {
            this.props.uploadError && this.props.uploadError(data);
        });
    }

    handleError(data) {
        this.props.isShowLoading && $$.loading(false);
        this.state.files.splice(this.state.files.length - 1, 1);
        this.setState({
            isValidate: false,
            msg: data.msg || I18N.getGUICommonValue("GC_AUI_Uploader_Filed_Message"),
            files: [].concat(this.state.files)
        }, () => {
            this.props.uploadError && this.props.uploadError(data);
        });
    }
    handleRemove(e, args) {
        let removeFile = args && args.newValue ? args.newValue.removeFile : '';
        if (removeFile) {
            let files = [].concat(this.state.files),
                data = {
                    removeFile: removeFile,
                    files: []
                };
            if (files && files.length > 0) {
                files.forEach((f) => {
                    if (f.fileId != removeFile.fileId) {
                        data.files.push(f);
                    }
                });
            }
            try {
                GuiCommonServices.DeleteFile({
                    host: this.props.host,
                    token: this.props.token,
                    fileId: removeFile.fileId
                }).then(() => {
                    if (args.parameters === "remove") {
                        this.setState({
                            files: data.files
                        }, () => {
                            this.updateCache();
                            this.props.remove && this.props.remove(data)
                        });
                    } else {
                        this.updateCache();
                        this.props.remove && this.props.remove(data);
                    }
                });
            } catch (e) {
                $$.error("Uploader Remove File Error:", e);
            }
        }
    }
    refreshFiles() {
        this.handleGetFilesByIds(this.state.files, true);
    }
    render() {
        let host = this.props.host,
            commonDownloadUrl = '/api/common/DownloadFile';
        return (
            <React.Fragment>
                <R.Uploader
                    popupStyle={this.props.popupStyle || ''}
                    isRequired={this.props.isRequired || false}
                    maxFileSize={this.state.maxFileSize}
                    uptoText={this.props.uptoText}
                    files={this.state.files}
                    fileTypes={this.props.fileTypes}
                    isMultiple={this.props.isMultiple}
                    canDownload={this.props.canDownload === false ? false : true}
                    downloadUrl={(this.props.downloadFileUrl || host ? host + commonDownloadUrl : SAComponentsUrl.URL_GUICOMMON + commonDownloadUrl) + '?fileId='}
                    textAlign={this.props.textAlign || "left"}
                    mode={this.props.mode}
                    onUpload={this.handleUpload}
                    onRemove={this.handleRemove}
                    disabled={this.props.disabled}
                    isShowType={this.props.isShowType}
                    disabledFile={true}
                />
                <R.ValidationPanel
                    validations={[{
                        type: 'checked',
                        msg: this.state.msg
                    }]}
                    importantMsg={this.state.msg}
                    autoChecking
                    widget="checkbox"
                >
                    <input
                        className="hide"
                        data-part="vtWidget"
                        type="checkbox"
                        readOnly
                        checked={this.state.isValidate}
                    />
                </R.ValidationPanel>
            </React.Fragment>
        );
    }
}