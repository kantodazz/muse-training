/**
* Author: Jeraldy Matara Deus | deusjeraldy@gmail.com
* Reusable Custom HTMLElements
*/
class ViewDialog extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="viewModal" style="left:4%">\
           <div class="modal-dialog" style="width:60%;">\
           <div class="modal-content panel-info">\
           <div class="modal-header panel-heading">\
                <a href="#" class="close" data-dismiss="modal">&times;</a>\
                <h3 class="modal-title">Loan Application Information</h3>\
           </div>\
           <div class="modal-body">\
                <div>\
                    <div class="widget-body ">\
                        <fieldset>\
                            <strong>Loan Details</strong>\
                            <div style="padding-left:90px;padding-right:150px">\
                             <table class="table table-condensed\
                                 table-bordered1" width="50%" > \
                                     <tr>\
                                     <td style="font-weight:bold; width: 250px">Customer Name</td>\
                                     <td id="CustomerName"></td>\
                                  </tr>\
                                     <tr>\
                                     <td style="font-weight:bold">Applied Amount</td>\
                                     <td id="AppliedAmount"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Loan Purpose</td>\
                                    <td id="LoanPurpose"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Repayment Source</td>\
                                    <td id="RepaymentSource"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Loan Disbursement Mode</td>\
                                    <td id="LoanDisbursementMode"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Approval Attachment </td>\
                                    <td id="ApprovalAttachment"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Loan Disbursement Channel</td>\
                                    <td id="LoanDisbursementChannel"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Disbursed Amount</td>\
                                    <td id="DisbursedAmount"></td>\
                                  </tr>\
                                </table>\
                            </div>\
                        </fieldset>\
                        <br />\
                        <fieldset>\
                            <strong>Meeting Details</strong>\
                             <div style="padding-left:90px;padding-right:150px">\
                             <table class="table table-condensed table-bordered1" width="50%">\
                                  <tr>\
                                      <td style="font-weight:bold; width: 250px">Meeting No</td>\
                                     <td id="MeetingNumber"></td>\
                                  </tr>\
                                  <tr>\
                                     <td style="font-weight:bold">Approved Amount</td>\
                                    <td id="LoanApprovedAmount"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Meeting Date </td>\
                                    <td id="MeetingDate"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Agender No </td>\
                                    <td id="MeetingAgendaNo"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Approval Attachment </td>\
                                    <td id="ApprovalAttachmentUrl"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Approval Authority</td>\
                                    <td id="LoanApprovalAuthority"></td>\
                                  </tr>\
                            </table>\
                           </div>\
                        </fieldset>\
                        <br />\
                        <fieldset>\
                           <strong>Appraisal Details</strong>\
                           <div style="padding-left:90px;padding-right:150px">\
                         <table class="table table-condensed width="50%"> \
                                   <tr>\
                                      <td style="font-weight:bold; width: 250px">Spouse Comments</td>\
                                     <td id="SpouseComments"></td>\
                                  </tr>\
                                   <tr>\
                                     <td style="font-weight:bold">LocalLeader Comments</td>\
                                     <td id="LocalLeaderComments"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Credit History</td>\
                                    <td id="CreditHistory"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Comments On CreditHistory</td>\
                                    <td id="CommentsOnCreditHistory"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">General Comments On Character</td>\
                                    <td id="GeneralCommentsOnCharacter"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Other Sources Of Income</td>\
                                    <td id="OtherSourcesOfIncome"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Financial Performance</td>\
                                    <td id="FinancialPerformance"></td>\
                                  </tr>\
                                  <tr>\
                                    <td style="font-weight:bold">Tangible And Intangible Assets</td>\
                                    <td id="TangibleAndIntangibleAssets"></td>\
                                  </tr>\
                                </table>\
                        </div>\
                        <br />\
                        <fieldset>\
                           <strong>Attachments</strong>\
                           <div style="padding:10px">\
                       <table class="table table-striped table-bordered table-hover table-condensed table-view-details" \
                         id="dt_view_attachments">\
                           <thead>\
                             <tr>\
                               <th style="width:50px">#</th>\
                               <th>Attachment Title</th>\
                               <th>File</th>\
                               <th>Attached By</th>\
                               <th>Attached At</th>\
                             </tr>\
                         </thead>\
                     </table>\
                        </div>\
                        </fieldset>\
                        <br />\
                            <div class="payee-entry remarks">\
                               <div class="entry-label">Remarks</div>\
                                 <div style="padding:10px">\
                                    <table id="dt_view_remarks" class="table table-striped table-bordered \
                                     table-hover table-condensed table-view-details" width="100%">\
                                     <thead>\
                                        <tr>\
                                            <th style="width:10px">#</th>\
                                            <th>Level</th>\
                                            <th>CreatedBy</th>\
                                            <th>Remark</th>\
                                            <th>Status</th>\
                                            <th>CreatedAt</th>\
                                       </tr>\
                                      </thead>\
                                   </table>\
                                 </div>\
                            </div>\
                        <br />\
                    </div>\
                </div>\
                <div class="modal-footer">\
                    <button class="btn btn-info" data-dismiss="modal" style="width:100px;">\
                        <i class="fa fa-times"></i>Close\
                    </button>\
                </div>\
            </div>\
        </div>\
    </div>\
</div>'
    }
}
customElements.define('view-dialog', ViewDialog);

class ViewDialogActions extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="viewActionModal" style="left:4%">\
                <div class="modal-dialog" style="width:50%;">\
                <div class="modal-content panel-info">\
                <div class="modal-header panel-heading">\
                <a href="#" class="close" data-dismiss="modal">&times;</a>\
               </div>\
               <div class="modal-body">\
               <div>\
                    <div class="widget-body ">\
                        <div class="payee-entry1">\
                            <div style="padding-left:20px">\
                               <table style="margin-left: 5%">\
                                        <tr>\
                                            <td class="form-label">\
                                                Remarks\
                                                <i class="fa fa-times" style="color:white" id="Remarks_1"></i>\
                                            </td>\
                                            <td>\
                                                <div class="name-input-container">\
                                                    <textarea type="text" id="Remarks" style="width:300px" rows="4" cols="50"/></textarea>\
                                                </div>\
                                            </td>\
                                        </tr>\
                                   </table>\
                            </div>\
                        </div>\
                        <br />\
                    </div>\
                </div>\
                <div class="modal-footer">\
                    <button class="btn btn-info" style="width:100px;" onclick="confirmSave()">\
                        <i class="fa fa-save"></i>Save\
                    </button>\
                    <button class="btn btn-info" data-dismiss="modal" style="width:100px;">\
                        <i class="fa fa-times"></i>Close\
                    </button>\
                </div>\
            </div>\
        </div>\
    </div>\
</div>'
    }
}
customElements.define('view-dialog-actions', ViewDialogActions);

class MainTable extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<table id="dt_table" \
                  class="table table-bordered table-condensed table-amount">\
                      <thead>\
                         <tr>\
                             <th>#</th>\
                             <th>Application No</th>\
                             <th>Applicant Name</th>\
                             <th>Approved Amount</th>\
                             <th>Disbursed Amount</th>\
                             <th>Product Type</th>\
                             <th>OverAll Status</th>\
                             <th>Created At</th>\
                             <th style="width:20px"></th>\
                         </tr>\
                    </thead>\
               </table>'
    }
}
customElements.define('main-table', MainTable);

class AttachDialog extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="attachModal" style="left:4%">\
                <div class="modal-dialog" style="width:80%;">\
                <div class="modal-content panel-info">\
                <div class="modal-header panel-heading">\
                <a href="#" class="close" data-dismiss="modal">&times;</a>\
                <h3 class="modal-title">Attachments</h3>\
            </div>\
            <div class="modal-body">\
                <div class="row">\
                   <div class="col-sm-5">\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Beneficiary">\
                            Title <i class="fa fa-times" style="color:white" id="AttachmentTitle_1"></i></label>\
                        <div class="col-sm-9">\
                         <input type="text" style="width:300px" id="AttachmentTitle"/>\
                        </div>\
                    </div>\
                </div>\
                   <div class="col-sm-5">\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Beneficiary">\
                            File <i class="fa fa-times" style="color:white" id="Attachment_1"></i></label>\
                        <div class="col-sm-9">\
                       <input type="file" style="width:300px" accept=".pdf" id="Attachment"/>\
                        </div>\
                    </div>\
                </div>\
                    <div class="col-sm-2">\
                        <button type="submit" class="btn btn-info" style="width:100px" onclick="addFile()"\
                             id="saveForm">\
                            <i class="fa fa-plus"></i>Add\
                            <img src="/Content/img/loading.gif" id="saveLoader1" />\
                        </button>\
                    </div>\
                </div>\
                <hr />\
                <table class="table" id="dt_attach">\
                    <thead>\
                        <tr>\
                            <th style="width:50px">#</th>\
                            <th>Attachment Title</th>\
                            <th>File</th>\
                            <th style="width:80px">Remove</th>\
                        </tr>\
                    </thead>\
                    <tbody>\</tbody>\
                </table>\
            </div>\
            <div class="modal-footer">\
                <button class="btn btn-info" data-dismiss="modal">\
                    <i class="fa  fa-save"></i>Save\
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

var dt_table = $('#dt_table').dataTable({
    "fnDrawCallback": function (oSettings) {
        $(".loading-gif").toggle(false)
    }
});
$("#dt_table_wrapper .dt-toolbar").remove();
$("#searchbox").on("keyup search input paste cut", function () {
    dt_table.DataTable().search(this.value).draw();
});

var motorRegUrl = "";
var AmendBenUrl = '';


function actionTypes(i, action) {

    switch (action) {
        case 'Confirm':
            var needsMotorVehicle = data[i].c.RequireMotorRegistration;
            if (needsMotorVehicle == 'true') {
                return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="' + AmendBenUrl + '/?Id=' + data[i].a.LoanApplicationId+ '">Add/Amend Beneficiary</a></li>\
                                <li><a href="' + motorRegUrl + '/?Id=' + data[i].a.LoanApplicationId + '" >Register Motor Vehicle</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Confirm</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
            }
            return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="' + AmendBenUrl + '/?Id=' + data[i].a.LoanApplicationId + '">Add/Amend Beneficiary</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Confirm</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';

        case 'Approve':
    
            if (data[i].a.OverAllStatus == 'Confirmed in Loan Disbursement') {
                return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Approve</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
            }

            if (data[i].a.OverAllStatus == 'Approved in Loan Disbursement') {
                return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="printLoanDisb(' + data[i].a.LoanApplicationId + ')">Print Loan Dis.</a></li>\
                                <li><a href="#" onclick="attach(' + i + ')">Attach Loan Dis.</a></li>\
                                <li><a href="#" onclick="generateVoucher(' + i + ')">Generate Voucher</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
            } 
            default:
            return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick= "attach(' + i + ')">Attach</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Confirm</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
    }
}

var data = []
let context = {};
var selectedItemId = 0

function loadMainData(params) {
    context = params;
    AmendBenUrl = context.AmendBenUrl;
    motorRegUrl = context.motorRegUrl;

    $.ajax({
        type: "get",
        url: context.dataUrl,
        data: {
            status1: context.getStatus.status1,
            status2: context.getStatus.status2,
            status3: context.getStatus.status3 || '',
            status4: context.getStatus.status4 || '',
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {

            data = response.data;
            dt_table.fnClearTable();
            for (var i = 0; i < data.length; i++) {
                dt_table.fnAddData([i + 1,
                 data[i].a.ApplicationNo,
                 data[i].b.CustomerName,
                 toLabel(data[i].a.LoanApprovedAmount),
                 toLabel(data[i].a.DisbursedAmount?data[i].a.DisbursedAmount:0),
                 data[i].a.RepaymentSource,
                 data[i].a.OverAllStatus,
                 data[i].a.FCreatedAt,
                 data[i].a.OverAllStatus == 'Waiting for Down Payment' ?
                 '<a href="#" onclick= "view(' + i + ')">View</a>'
                 : actionTypes(i, context.caller)
                ]);
                $("#saveLoader-" + i + "").toggle(false)
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

var confirmRowId = 0;
function confirm(i) {
    confirmRowId = i;
    $('#viewActionModal').modal('show');
}

var shouldGenVoucher = false;

function generateVoucher(i) {
    shouldGenVoucher = true;
    confirmRowId = i;
    $('#viewActionModal').modal('show');
}

function confirmSave() {
    if ($("#Remarks").val() == '') {
        $("#Remarks_1").attr("style", "color: red;");
    } else {
        swal({
            title: context.caller == 'Approve' ? "Submit this Item?" : (context.caller + ' Loan Request?'),
            buttons: [
              'NO',
              'YES'
            ],
            dangerMode: true,
        }).then(function (isConfirmed) {
            if (isConfirmed) {
                $("#saveLoader-" + confirmRowId + "").toggle(true)
                $("#drop-" + confirmRowId + "").toggle(false)
                confirmPost(data[confirmRowId].a.LoanApplicationId);
            } else {
                swal("Cancelled", "No change was made");
            }
        });
    }
}

function confirmPost(Id) {
    $.ajax({
        type: "post",
        url: context.actionsUrl,
        data: {
            Id, status: shouldGenVoucher?"GenerateVoucher":context.actionStatus,
            remarks: $("#Remarks").val(),
            level: context.level
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            if (response == "Success") {
                swal("Saved Successfully!", { icon: "success" })
                .then((e) => {
                    window.location.reload();
                });
            }
            else {
                swal(response);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

function view(i) {
    window.location.href = `${context.ViewUrl}/${data[i].a.LoanApplicationId}`;
}
var ViewItemsId = 0
function view1(i) {
    selectedItemId = data[i].a.LoanApplicationId
    $.ajax({
        type: "GET",
        url: context.allAttachmentsUrl,
        data: { SourceModuleId: data[i].a.LoanApplicationId, sourceModule: 'LoanAuthorization-' + context.caller },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            var d2 = response.data;
            dt_view_attachments.fnClearTable();
            for (var i = 0; i < d2.length; i++) {
                dt_view_attachments.fnAddData([i + 1,
                  d2[i]['AttachmentTitle'],
                  '<a href="/Content/uploads/' + d2[i]['AttachmentName'] + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>',
                  d2[i]['CreatedBy'],
                  d2[i]['FCreatedAt'],
                ]);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
    loadRemarks(i)
    populateData(i)
    $("#viewModal").modal('show');
}

function populateData(i) {
    // LOAN DETAILS
    $("#CustomerName").text(data[i].b.CustomerName);
    $("#LoanPurpose").text(data[i].a.LoanPurpose);
    $("#LoanDisbursementMode").text(data[i].a.LoanDisbursementMode);
    $("#LoanDisbursementChannel").text(data[i].a.LoanDisbursementChannel);
    $("#AppliedAmount").text(data[i].a.AppliedAmount);
    $("#RepaymentSource").text(data[i].a.RepaymentSource);
    $("#ApprovalAttachment").text(data[i].a.ApprovalAttachment);
    $("#DisbursedAmount").text(toLabel(data[i].a.DisbursedAmount || ''));
    // Appraisal Details
    //$("#SpouseComments").text(data[i].b.SpouseComments);
    //$("#CreditHistory").text(data[i].b.CreditHistory);
    //$("#GeneralCommentsOnCharacter").text(data[i].b.GeneralCommentsOnCharacter);
    //$("#FinancialPerformance").text(data[i].b.FinancialPerformance);
    //$("#LocalLeaderComments").text(data[i].b.LocalLeaderComments);
    //$("#CommentsOnCreditHistory").text(data[i].b.CommentsOnCreditHistory);
    //$("#OtherSourcesOfIncome").text(data[i].b.OtherSourcesOfIncome);
    //$("#TangibleAndIntangibleAssets").text(data[i].b.TangibleAndIntangibleAssets);

    // Meeting Details
    $.ajax({
        type: "GET",
        url: context.meetingInfoUrl,
        data: { Id: data[i].a.LoanApplicationId },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            var d2 = response.data;
            if (d2) {
                $("#MeetingNumber").text(d2.MeetingNumber);
                $("#MeetingDate").text(d2.FMeetingDate);
                $("#MeetingAgendaNo").text(d2.MeetingAgendaNo);
                $("#LoanApprovalAuthority").text(d2.LoanApprovalAuthority);
                $("#LoanApprovedAmount").text(d2.LoanApprovedAmount);
                $("#OfferLetterAttachmentUrl").html('<a href="/Content/uploads/' + d2.OfferLetterAttachmentUrl + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>');
                $("#ApprovalAttachmentUrl").html('<a href="/Content/uploads/' + d2.ApprovalAttachmentUrl + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>');

            }
        },
        failure: function (error) {
            swal(error);
        }
    });

}
function attach(i) {
    selectedItemId = data[i].a.LoanApplicationId
    loadAttachments(selectedItemId)
    $("#attachModal").modal("show");
}

function loadRemarks(i) {
    $.ajax({
        type: "GET",
        url: context.remarksUrl,
        data: { SourceModuleId: data[i].a.LoanApplicationId, sourceModule: 'LoanDisbursement' },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            var d2 = response.data;
            dt_view_remarks.fnClearTable();
            for (var i = 0; i < d2.length; i++) {
                dt_view_remarks.fnAddData([i + 1,
                  d2[i]['RemarkLevel'],
                  d2[i]['CreatedBy'],
                  d2[i]['Remarks'],
                  d2[i]['OverallStatus'] == 'Accepted' ?
                  '<span style="color:green">' + d2[i]['OverallStatus'] + '</span>'
                  : '<span style="color:red">' + d2[i]['OverallStatus'] + '</span>',
                  d2[i]['FCreatedAt'],
                ]);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

function loadAttachments1(i) {
    console.log("I here")
    $.ajax({
        type: "GET",
        url: context.allAttachmentsUrl,
        data: {
            SourceModuleId: data[i]['LoanAppraisalId'],
            sourceModule: 'LoanDisbursement'
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            console.log(response)
            var d2 = response.data;
            dt_view_attachments.fnClearTable();
            for (var i = 0; i < d2.length; i++) {
                dt_view_attachments.fnAddData([i + 1,
                  d2[i]['AttachmentTitle'],
                  d2[i]['AttachmentName'],
                  d2[i]['CreatedBy'],
                  d2[i]['CreatedAt'],
                ]);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

function reject(i) {
    swal({
        text: 'Rejection Reason',
        content: "input",
        button: {
            text: "SAVE",
            closeModal: true,
        },
    }).then(remark => {
        if (!remark) {
            swal("No reason was provided");
        } else {
            $("#saveLoader-" + i + "").toggle(true)
            $("#drop-" + i + "").toggle(false)
            confirmRejection(data[i].a.LoanApplicationId, remark);
        };
    });
}


function confirmRejection(Id, remarks) {
    $.ajax({
        type: "post",
        url: context.actionsUrl,
        data: {
            Id, remarks,
            status: context.rejectionStatus,
            level: context.level
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {

            if (response == "Success") {
                swal("Rejected Successfully!", { icon: "success" })
                .then((e) => {
                    window.location.reload();
                });
            }
            else {
                swal(response);
            }

        },
        failure: function (error) {
            swal(error);
        }
    });
}

function addFile() {
    if (formIsValid()) {
        var formData = new FormData()
        formData.append('SourceModule', 'LoanDisbursement-' + context.caller);
        formData.append('SourceModuleId', selectedItemId);
        formData.append('AttachmentTitle', $("#AttachmentTitle").val());
        formData.append('file', $("#Attachment")[0].files[0]);
        postFormData(formData);
    }
}

function postFormData(formData) {
    $("#saveForm").prop('disabled', true);
    $("#saveLoader1").toggle(true);
    $.ajax({
        url: context.attachUrl,
        data: formData,
        type: 'POST',
        contentType: false,
        processData: false,
        success: function (response) {
            var d2 = response.data;
            if (!d2.includes("Error:")) {
                dt_attach.fnClearTable();
                for (var i = 0; i < d2.length; i++) {
                    dt_attach.fnAddData([i + 1,
                      d2[i]['AttachmentTitle'],
                      '<a href="/Content/uploads/' + d2[i]['AttachmentName'] + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>',
                      '<a href="#" onclick="removeFile(' + d2[i]['LoanAttachmentId'] + ')"><i class="fa fa-trash-o"></i></a>',
                    ]);
                }
                $("#AttachmentTitle").val('')
                $("#Attachment").val('')
            } else {
                swal(response.data)
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
        '#Attachment',
        '#AttachmentTitle',
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

function removeFile(Id) {
    $.ajax({
        url: context.deleteAttachmentUrl,
        data: { Id },
        type: 'POST',
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            console.log(response)
            var d2 = response.data;
            dt_attach.fnClearTable();
            for (var i = 0; i < d2.length; i++) {
                dt_attach.fnAddData([i + 1,
                  d2[i]['AttachmentTitle'],
                  '<a href="/Content/uploads/' + d2[i]['AttachmentName'] + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>',
                  '<a href="#" onclick="removeFile(' + d2[i]['LoanAttachmentId'] + ')"><i class="fa fa-trash-o"></i></a>',
                ]);
            }
        },
        failure: function (error) {
            swal(error)
        }
    });
}

function loadAttachments(SourceModuleId) {
    $.ajax({
        type: "GET",
        url: context.attachmentsUrl,
        data: { SourceModuleId, sourceModule: 'LoanDisbursement-' + context.caller },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            var d2 = response.data;
            dt_attach.fnClearTable();
            for (var i = 0; i < d2.length; i++) {
                dt_attach.fnAddData([i + 1,
                  d2[i]['AttachmentTitle'],
                  '<a href="/Content/uploads/' + d2[i]['AttachmentName'] + '" target="_blank"><i class="fa fa-file-pdf-o"></i></a>',
                  '<a href="#" onclick="removeFile(' + d2[i]['LoanAttachmentId'] + ')"><i class="fa fa-trash-o"></i></a>',
                ]);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

function approve(i) {
    swal({
        title: 'Approve this Item ?',
        buttons: ['NO', 'YES'],
        dangerMode: true,
    }).then(function (yes) {
        if (yes) {
            approvePost(data[i].LoanAppraisalId)
        } else {
            swal("Cancelled", "No change was made");
        }
    });
}

function approvePost(Id) {
    $.ajax({
        type: "post",
        url: context.actionsUrl,
        data: {
            Id, status: context.actionStatus,
            remarks: "Approved",
            level: context.level
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            if (response == "Success") {
                swal("Saved Successfully!", { icon: "success" })
                    .then((e) => {
                    window.location.reload();
                });
            }
            else {
                swal(response);
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

