/**
* Author: Jeraldy Matara Deus | deusjeraldy@gmail.com
* Reusable Custom HTMLElements
*/
class AttachDialog extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="attachModal" style="left:4%">\
                <div class="modal-dialog" style="width:50%;">\
                <div class="modal-content panel-info">\
                <div class="modal-header panel-heading">\
                <a href="#" class="close" data-dismiss="modal">&times;</a>\
                <h3 class="modal-title">Revocation Details</h3>\
            </div>\
            <div class="modal-body">\
                <div>\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Remarks">\
                            Reasons <i class="fa fa-times" style="color:white" id="Remarks_1"></i></label>\
                        <div class="col-sm-9">\
                         <textarea style="width:300px" id="Remarks"/></textarea>\
                        </div>\
                    </div>\
                 <br />\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Beneficiary">\
                            Attachment <i class="fa fa-times" style="color:white" id="Attachment_1"></i></label>\
                        <div class="col-sm-9">\
                       <input type="file" style="width:300px" accept=".pdf" id="Attachment"/>\
                        </div>\
                    </div>\
          </div>\
              <br />\<br />\
            <div class="modal-footer">\
                <button class="btn btn-info" onclick="addFile()" id="saveForm">\
                    <i class="fa  fa-save"></i>Save\
                    <img src="/Content/img/loading.gif" id="saveLoader1" />\
                </button>\
                <button class="btn btn-info" data-dismiss="modal">\
                    <i class="fa  fa-times"></i>Close\
                </button>\
            </div>\
        </div>\
    </div>\
</div>'
    }
}

customElements.define('attach-dialog', AttachDialog);

var LoanPayeeId = 0
var AttachUrl = "";
function addFile() {
    if (formIsValid()) {
        var formData = new FormData()
        formData.append('LoanPayeeId', LoanPayeeId);
        formData.append('Remarks', $("#Remarks").val());
        formData.append('file', $("#Attachment")[0].files[0]);
        postFormData(formData);
    }
}

function postFormData(formData) {
    $("#saveForm").prop('disabled', true);
    $("#saveLoader1").toggle(true);
    $.ajax({
        url: AttachUrl,
        data: formData,
        type: 'POST',
        contentType: false,
        processData: false,
        success: function (response) {
            if (response == "Success") {
                swal("Saved Successfully!", { icon: "success" })
                .then((m) => {
                    window.location.reload()
                })
            } else {
                swal(response)
            }
            $("#saveForm").prop('disabled', false);
            $("#saveLoader1").toggle(false);
        },
        failure: function (error) {
            swal(error)
        }
    });
}

function formIsValid() {

    var isNotValid = validateInputs([
        '#Remarks',
        '#Attachment',
    ]);

    if (isNotValid) {
        $(isNotValid + "_1").attr("style", "color: red;");
        return false
    }
    return true
}

function validateInputs(parameterList) {
    var resetStyle = function (parameterList) {
        for (var i = 0; i < parameterList.length; i++) {
            $(parameterList[i] + "_1").attr("style", "color: white;");
        }
    }

    resetStyle(parameterList);
    for (var i = 0; i < parameterList.length; i++) {
        if (!$(parameterList[i]).val()) {
            return parameterList[i];
        }
    }
    return null;
}

function initParams(params) {
    LoanPayeeId = params.LoanPayeeId;
    AttachUrl = params.AttachUrl;
}