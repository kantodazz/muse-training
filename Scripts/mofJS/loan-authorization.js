/**
* Author: Jeraldy Matara Deus | deusjeraldy@gmail.com
* Reusable Custom HTMLElements
*/
class LegalDialog extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="legalModalx" style="left:4%">\
        <div class="modal-dialog" style="width:80%;">\
        <div class="modal-content panel-info">\
            <div class="modal-header panel-heading">\
                <a href="#" class="close" data-dismiss="modal">&times;</a>\
                <h3 class="modal-title">Credit Agreement</h3>\
            </div>\
            <div class="modal-body">\
                <div class="row">\
                    <div class="col-sm-6">\
                        <div style="padding-left:5%;padding-right:1%">\
                            <table style="margin-left: 2%">\
                                <tr>\
                                    <td class="form-label" >\
                                        Registration No\
                                        <i class="fa fa-times" style="color:white" id="RegistrationNo_1"></i>\
                                    </td>\
                                    <td>\
                                        <div class="name-input-container">\
                                            <input type="text" style="width:300px" id="RegistrationNo" />\
                                        </div>\
                                    </td>\
                                </tr>\
                                <tr>\
                                    <td class="form-label">\
                                        Registration Date\
                                        <i class="fa fa-times" style="color:white" id="RegistrationDate_1"></i>\
                                    </td>\
                                    <td>\
                                    <div class="flatpickr date-group">\
                                        <input type="text" placeholder="Select Date.."\
                                          data-input\
                                          style="width:260px;padding-left:10px;border:none"\
                                          autocomplete="off" id="RegistrationDate">\
                                         <a class="input-button" title="open" data-toggle href="#">\
                                         <span class="input-group-addon" style="height:32px"><i class="fa fa-calendar"></i></span>\
                                         </a>\
                                     </div>\
                                    </td>\
                                </tr>\
                            </table>\
                        </div>\
                    </div>\
                        <div class="col-sm-6">\
                            <table>\
                                  <tr>\
                                    <td class="form-label">\
                                        Remarks\
                                        <i class="fa fa-times" style="color:white" id="Remarks1_1"></i>\
                                    </td>\
                                    <td>\
                                        <div class="name-input-container">\
                                              <textarea type="text" id="Remarks1" style="width:300px" rows="4" cols="50"/></textarea>\
                                        </div>\
                                    </td>\
                                </tr>\
                            </table>\
                        </div>\
                    </div>\
                   <br/>\
                      <label style="padding-left:4%"> Attachments </label>\
                   <table class="table" id="dt_legal" style="width:70%;position:relative;left:18%">\
                          <tbody>\
                              <tr>\
                                   <td>Loan agreement\
                                      <i class="fa fa-times" style="color:white" id="LoanAgreement_1"></i>\
                                    </td>\
                                   <td><input type="file" id="LoanAgreement"></td>\
                              </tr>\
                              <tr>\
                                   <td>Mortgage of the right of occupancy\
                                    <i class="fa fa-times" style="color:white" id="Mortgage_1"></i>\</td>\
                                   <td><input type="file" id="Mortgage"></td>\
                              </tr>\
                              <tr>\
                                   <td>Personal Guarantee\
                                     <i class="fa fa-times" style="color:white" id="Guarantee_1"></i>\</td>\
                                   <td><input type="file" id="Guarantee"></td>\
                              </tr>\
                              <tr>\
                                   <td>Spouse consent\
                                      <i class="fa fa-times" style="color:white" id="SpouseConsent_1"></i>\</td>\
                                   <td><input type="file" id="SpouseConsent"></td>\
                              </tr>\
                              <tr>\
                                   <td>Borrower’s affidavit\
                                      <i class="fa fa-times" style="color:white" id="Affidavit_1"></i>\</td>\
                                    <td><input type="file" id="Affidavit"></td>\
                              </tr>\
                              <tr>\
                                   <td>Original Certificate of right of occupancy\
                                   <i class="fa fa-times" style="color:white" id="Certificate_1"></i></td>\
                                   <td><input type="file" id="Certificate"></td>\
                              </tr>\
                          </tbody>\
                  </table>\
            </div>\
            <div class="modal-footer">\
                <button class="btn btn-info" onclick="saveLegalInfo()" id="saveForm">\
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

customElements.define('legal-dialog', LegalDialog);

class ViewDialog extends HTMLElement {

    constructor() {super()}

    connectedCallback() {this.innerHTML = this._renderCustomElement()}

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
                                  </tr>\
                                     <tr>\
                                     <td style="font-weight:bold">Customer Location</td>\
                                     <td id="CustomerLocation"></td>\
                                  </tr>\
                               </table>\
                            </div>\
                        <br />\
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

class ViewDialogActionsAttach extends HTMLElement {

    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = this._renderCustomElement();
    }

    _renderCustomElement() {
        return '<div class="modal fade" id="viewActionAttachModal" style="left:4%">\
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
                               <table style="margin-left: 12%">\
                                        <tr>\
                                            <td class="form-label">\
                                                Remarks1\
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
customElements.define('view-dialog-actions-attach', ViewDialogActionsAttach);

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
                               <table style="margin-left: 8%">\
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
                             <th>BatchNo</th>\
                             <th>Customer Name</th>\
                             <th>Applied Loan Amount</th>\
                             <th>Loan Purpose</th>\
                             <th>Repayment Source</th>\
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
                   <div class="col-sm-4">\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Beneficiary">\
                            Title <i class="fa fa-times" style="color:white" id="AttachmentTitle_1"></i></label>\
                        <div class="col-sm-9">\
                         <input type="text" style="width:300px" id="AttachmentTitle"/>\
                        </div>\
                    </div>\
                </div>\
                 <div class="col-sm-2"></div>\
                <div class="col-sm-4">\
                    <div class="form-group">\
                        <label class="control-label col-sm-3" for="Beneficiary">\
                         <i class="fa fa-times" style="color:white" id="Attachment_1"></i></label>\
                        <div class="col-sm-9">\
                       <input type="file" style="width:150px" accept=".pdf" id="Attachment"/>\
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
                <button class="btn btn-info" data-dismiss="modal" onclick="window.location.reload()">\
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

var AddMeetingInfoUrl = "";
var UpdateMeetingInfoUrl = "";
function actionTypes(i, action) {
    var Id = data[i].a.LoanApplicationId;
    var hasMeeting = data[i].a.HasMeetingInfo
    var OverallStatus = data[i].a.OverAllStatus

    switch (action) {
        case 'Confirm':
            if (hasMeeting) {
                return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                 <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                 <li><a href="' + UpdateMeetingInfoUrl + '/?Id=' + Id + '">Edit Meeting Info</a></li>\
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
                                <li><a href="' + AddMeetingInfoUrl + '/?Id=' + Id + '">Add Meeting Info</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';

        case 'Examine':
            return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick= "view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Examine</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
        case 'Legal Review':
            return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick="view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="addLegalInfo(' + i + ')">Submit</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';
        case 'Approve':
            if (OverallStatus == 'Approved - Waiting for Attachment') {
                return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick="view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="printOfferLatter(' + Id + ')">Print Offer Letter</a></li>\
                                <li><a href="#" onclick="attach(' + i + ')">Attach Offer Letter</a></li>\
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
                                <li><a href="#" onclick="view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="confirm(' + i + ')">Approve</a></li>\
                                <li><a href="#" onclick="reject(' + i + ')">Reject</a></li>\
                            </ul>\
                        </div>\
                  <img src="/Content/img/loading.gif" id="saveLoader-' + i + '" class="loading-gif"/>';

        default:
            return '<div class="btn-group" id="drop-' + i + '">\
                           <button type="button" class="btn btn-info btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">\
                                 <span class="caret"></span\
                                     <span class="sr-only"></span>\
                            </button>\
                            <ul class="dropdown-menu">\
                                <li><a href="#" onclick="view(' + i + ')">View</a></li>\
                                <li><a href="#" onclick="attach(' + i + ')">Attach Offer Letter</a></li>\
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
    AddMeetingInfoUrl = context.entryUrl;
    UpdateMeetingInfoUrl = context.editUrl;

    $.ajax({
        type: "get",
        url: context.dataUrl,
        data: {
            status1: context.getStatus.status1,
            status2: context.getStatus.status2,
            status3: context.getStatus.status3 || '',
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
           console.log(response)
           data = response.data;
            console.log(data)
            dt_table.fnClearTable();
            for (var i = 0; i < data.length; i++) {
                dt_table.fnAddData([i + 1,
                 data[i].a.ApplicationNo,
                 data[i].a.BatchNo,
                 data[i].b.CustomerName,
                 toLabel(data[i].a.AppliedLoanAmount),
                 data[i].a.LoanPurpose,
                 data[i].a.RepaymentSource,
                 data[i].a.OverAllStatus,
                 data[i].a.FCreatedAt,
                 actionTypes(i, context.caller)
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


function confirmSave() {
    if ($("#Remarks").val() == '') {
        $("#Remarks_1").attr("style", "color: red;");
    } else {
        swal({
            title: context.caller + ' Loan Request?',
            buttons: [
              'NO',
              'YES'
            ],
            dangerMode: true,
        }).then(function (isConfirmed) {
            if (isConfirmed) {
                $("#saveLoader-" + confirmRowId + "").toggle(true)
                $("#drop-" + confirmRowId + "").toggle(false)
                confirmPost( data[confirmRowId].a.LoanApplicationId);
            } else {
                swal("Cancelled", "No change was made");
            }
        });
    }
}
function downloadURI(uri, name) {
    var link = document.createElement("a");
    link.download = name;
    link.href = uri;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    delete link;
}
function confirmPost(Id) {
    console.log(context.level)
    if (context.level == 'Loan Auth. Approve'
        && data[confirmRowId].a.OverAllStatus
        == 'Examined in Loan Auth.') {
        context.actionStatus = 'Approved - Waiting for Attachment';
    }

    $.ajax({
        type: "post",
        url: context.actionsUrl,
        data: {
            Id,
            status: context.actionStatus,
            remarks: $("#Remarks").val(),
            level: context.level
        },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            if (response == "Success") {
                swal("Saved Successfully!", { icon: "success" }).then((e) => {
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


var ViewItemsId = 0

function view(i) {
    window.location.href = `${context.ViewUrl}/${data[i].a.LoanApplicationId}`;
}

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
    console.log(data[i])
    // LOAN DETAILS
    $("#CustomerName").text(data[i].b.CustomerName);
    //$("#LoanPurpose").text(data[i].a.LoanPurpose);
    $("#LoanDisbursementMode").text(data[i].a.LoanDisbursementMode);
    $("#CustomerLocation").text(data[i].b.CustomerLocation);
    $("#AppliedAmount").text(toLabel(data[i].a.AppliedLoanAmount));
    $("#ApprovalAttachment").text(data[i].a.ApprovalAttachment);
    $("#DisbursedAmount").text(toLabel(data[i].a.DisbursedAmount || ''));
    // Appraisal Details
    $("#SpouseComments").text(data[i].b.SpouseComments);
    $("#CreditHistory").text(data[i].b.CreditHistory);
    $("#GeneralCommentsOnCharacter").text(data[i].b.GeneralCommentsOnCharacter);
    $("#FinancialPerformance").text(data[i].b.FinancialPerformance);
    $("#LocalLeaderComments").text(data[i].b.LocalLeaderComments);
    $("#CommentsOnCreditHistory").text(data[i].b.CommentsOnCreditHistory);
    $("#OtherSourcesOfIncome").text(data[i].b.OtherSourcesOfIncome);
    $("#TangibleAndIntangibleAssets").text(data[i].b.TangibleAndIntangibleAssets);

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
    loadAttachments(i)
    $("#attachModal").modal("show");
}

var isUnsecuredLoan = false;
function addLegalInfo(i) {
    selectedItemId = data[i].a.LoanApplicationId
    isUnsecuredLoan = data[i].a.LoanSecurityId == 2
    loanLegalAttachments(selectedItemId)
    $("#legalModal").modal("show");
}


function loadRemarks(i) {
    $.ajax({
        type: "GET",
        url: context.remarksUrl,
        data: { SourceModuleId: data[i].a.LoanApplicationId, sourceModule: 'LoanAuthorization' },
        contenttype: "application/json; charset=utf-8",
        datatype: "json",
        success: function (response) {
            var d2 = response.data;
            if(d2){
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
            }
        },
        failure: function (error) {
            swal(error);
        }
    });
}

function loadAttachments(i) {
    $.ajax({
        type: "GET",
        url: context.allAttachmentsUrl,
        data: { SourceModuleId: data[i].a.LoanApplicationId, sourceModule: 'LoanAuthorization-' + context.caller },
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
        formData.append('SourceModule', 'LoanAuthorization-' + context.caller);
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
        data: {Id},
        type: 'POST',
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
            swal(error)
        }
    });
}

function loadAttachments1(SourceModuleId) {
    $.ajax({
        type: "GET",
        url: context.attachmentsUrl,
        data: { SourceModuleId, sourceModule: 'LoanAuthorization-' + context.caller },
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

function legalFormIsValid() {
    var isNotValid = validateInputs([
           '#RegistrationNo',
           '#RegistrationDate',
           '#Remarks1',
    ]);

    if (isNotValid) {
        $(isNotValid + "_1").attr("style", "color: red;");
        return false
    }

    for (var i = 0; i < requiredAttachments.length; i++) {
        if (requiredAttachments[i].IsRequired) {
            var find = uploadedAttachments.filter(v=>v.Title == requiredAttachments[i].AttachmentName)
            if (find.length == 0) {
                swal(`Attachment ${requiredAttachments[i].AttachmentName} is Missing`)
                return false
            }
        }
    }

    if (uploadedAttachments.length == 0) {
        swal("No attachemnts found..!")
        return false
    }
    return true
}

function saveLegalInfo() {
    if (legalFormIsValid()) {
        var formData = new FormData()
        formData.append('_AttachItems', JSON.stringify(uploadedAttachments))
        formData.append('RegistrationNo', $("#RegistrationNo").val());
        formData.append('RegistrationDate', $("#RegistrationDate").val());
        formData.append('Remarks', $("#Remarks1").val());
        formData.append('LoanApplicationId', selectedItemId);
        postLegalFormData(formData);
    }
}

function postLegalFormData(formData) {
    $("#saveForm").prop('disabled', true);
    $("#saveLoader1").toggle(true);
    $.ajax({
        url: context.attachUrl,
        data: formData,
        type: 'POST',
        contentType: false,
        processData: false,
        success: function (response) {
            if (response == "Success") {
                swal("Saved Successfully!", { icon: "success" })
                .then((e) => {
                    window.location.reload();
                });
            } else {
                swal(response);
            }
            $("#saveForm").prop('disabled', false);
            $("#saveLoader1").toggle(false);
        },
        failure: function (error) {
            swal(error)
        }
    });
}
