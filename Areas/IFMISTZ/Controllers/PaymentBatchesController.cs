using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using IFMIS.Services;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Xml.Linq;
using Serilog;
using System.Text;
using System.Xml;
using System.Web.Mvc;
using System.Transactions;


namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class PaymentBatchesController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();
        private readonly IFundBalanceServices fundBalanceServices;
        private readonly IServiceManager serviceManager;
        public PaymentBatchesController()
        {

        }

        public PaymentBatchesController(
            IFundBalanceServices fundBalanceServices,
             IServiceManager serviceManager
            )
        {
            this.fundBalanceServices = fundBalanceServices;
            this.serviceManager = serviceManager;
        }


        // GET: IFMISTZ/PaymentBatches
        public ActionResult PaymentBatchList()
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var sublevellist = serviceManager.GetSubLevel(User.Identity.GetUserId());
            List<PaymentBatch> paymentbatchList = new List<PaymentBatch>();
            //using (var t = new TransactionScope(TransactionScopeOption.Required,
            //    new TransactionOptions
            //    {
            //        IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
            //    }))
            //{
            paymentbatchList = db.PaymentBatches
            .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            && a.PaymentCategory == "PAYMENT"
            //&& a.SubLevelCode == userPaystation.SubLevelCode
            && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")
            ).ToList();
            // }

            //var paymentbatchList = db.PaymentBatches
            //.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            //&& a.PaymentCategory=="PAYMENT"
            ////&& sublevellist.Contains(a.SubLevelCode)
            //&& a.SubLevelCode==userPaystation.SubLevelCode 
            //&& (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")
            //).ToList();

            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatchList)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    RejectedReason = item.RejectedReason,
                    GLstatus = item.GLstatus,
                    UploadStatus = item.UploadStatus,
                    MsgID = item.MsgID,
                    BulkPaymentMethod = item.BulkPaymentMethod,


                };

                data.Add(vm);

                int id = (data.Select(a => a.PaymentBatchID)).FirstOrDefault();
                decimal? totalBatchAmount = 0;

                if (totalBatchAmount == null)
                {
                    ViewBag.totalBatchAmount = 0;
                }
                else
                {
                    totalBatchAmount = (data.Where(a => a.PaymentBatchID == id).Select(a => a.TotalAmount)).FirstOrDefault();
                    ViewBag.totalBatchAmount = totalBatchAmount;
                    ViewBag.payment = data.Where(a => a.PaymentBatchID == id).ToArray();
                }

                if (id == null)
                {
                    ViewBag.GLCoaSum = 0;
                }
                else
                {
                    ViewBag.GLCoaSum = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == id).Sum(a => a.OperationalAmount);
                }
            }


            return View(data);
        }


        // GET: IFMISTZ/PaymentBatches/Create
        public ActionResult CreatePaymentBatch()
        {
            PaymentBatchVM vm = new PaymentBatchVM();
            string userId = User.Identity.GetUserId();

            var InstitutionCodeList = serviceManager.GetInstitutionList(User.Identity.GetUserId());
            vm.InstitutionNameList = new SelectList(InstitutionCodeList, "InstitutionId", "InstitutionCodeInstitutionName");
            var List = serviceManager.GetPaymentList();
            vm.PaymentCategoryList = new SelectList(List, "CategoryName", "CategoryName");

            return View(vm);
        }

        // POST: IFMISTZ/PaymentBatches/Create
        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePaymentBatch(PaymentBatchVM paymentBatchVM)
        {

            int financialyear = serviceManager.GetFinancialYear(DateTime.Now);
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());


            //var restrictionFinancialYear = db.RestrictionFinancialYears
            //    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            //    && a.OverallStatus == "Active").FirstOrDefault();

            //if (restrictionFinancialYear != null)
            //{
            //    if (restrictionFinancialYear.FinancialYearCode != ServiceManager.GetFinancialYear(db, DateTime.Now))
            //    {
            //        return Content("Invalid Apply date!");
            //    }
            //}


            if (ModelState.IsValid)
            {
                PaymentBatch paymentBatch = new PaymentBatch()
                {
                    BatchNo = paymentBatchVM.BatchNo,
                    BatchDesc = paymentBatchVM.BatchDesc,
                    InstitutionId = (int)paymentBatchVM.InstitutionId,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    SubLevelCode = userPaystation.SubLevelCode,
                    PaymentCategory = paymentBatchVM.PaymentCategory,
                    OverallStatus = "Pending",
                    PaymentVoucherStatus = "Pending",
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    GLstatus = "No",
                    UploadStatus = "No",
                    NumRejections = 0,
                    NumSubmissions = 0,
                    NumSubmissionsAtRejection = 0,
                    StPaymentFlag = false,
                    Financialyear = financialyear,
                    paymentOfficeId = userPaystation.Institution.PaymentOfficeId,
                };

                db.PaymentBatches.Add(paymentBatch);

                db.SaveChanges();
                int id = paymentBatch.PaymentBatchID;
                var numRejections = db.PaymentBatches.Where(a => a.PaymentBatchID == id).Select(a => a.NumRejections).FirstOrDefault();

                string MsgIdMask = Properties.Settings.Default.MsgIdMask.Replace("MUS", "MUB");
                string MsgId = MsgIdMask + financialyear.ToString().Substring(2, 2) + numRejections.ToString().PadLeft(2, '0') + id.ToString().PadLeft(7, '0');
                paymentBatch.MsgID = MsgId;

                db.SaveChanges();

                if (paymentBatchVM.PaymentCategory == "PAYMENT")
                {

                    return RedirectToAction("PaymentBatchList");
                }
                else
                {
                    return RedirectToAction("UnappliedBatchList", "UnappliedBatches", new { area = "IFMISTZ" });
                }

            }
            string userId = User.Identity.GetUserId();
            var InstitutionCodeList = serviceManager.GetInstitutionList(User.Identity.GetUserId());
            paymentBatchVM.InstitutionNameList = new SelectList(InstitutionCodeList, "InstitutionId", "InstitutionCodeInstitutionName");

            return View(paymentBatchVM);
        }

        public string GetPaymentBatchNoPerInstitution(int id, string paymentCategory)
        {

            if (id > 0)
            {
                int maxValue = db.PaymentBatches
                        .Where(a => a.InstitutionId == id)
                        .Select(a => a.PaymentBatchID)
                        .DefaultIfEmpty(0)
                        .Max();

                maxValue = ++maxValue;
                var userPaystation = db.Institution.Find(id);
                string batchNo = null;
                if (paymentCategory == "PAYMENT")
                {
                    batchNo = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, "B", maxValue);
                }
                else if (paymentCategory == "UNAPPLIED")
                {
                    batchNo = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, "UN", maxValue);
                }
                return batchNo;
            }
            return "";
        }


        public ActionResult EditPaymentBatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PaymentBatch paymentBatch = db.PaymentBatches.Find(id);
            if (paymentBatch == null)
            {
                return HttpNotFound();
            }

            var paymentBatchVM = new PaymentBatchVM
            {
                PaymentBatchID = paymentBatch.PaymentBatchID,
                BatchNo = paymentBatch.BatchNo,
                BatchDesc = paymentBatch.BatchDesc,
                InstitutionId = paymentBatch.InstitutionId,
                OverallStatus = paymentBatch.OverallStatus,
                PaymentCategory = paymentBatch.PaymentCategory,
            };
            string userId = User.Identity.GetUserId();
            var InstitutionCodeList = serviceManager.GetInstitutionList(User.Identity.GetUserId());
            paymentBatchVM.InstitutionNameList = new SelectList(InstitutionCodeList, "InstitutionId", "InstitutionCodeInstitutionName", paymentBatchVM.InstitutionId);

            var List = serviceManager.GetPaymentList();
            paymentBatchVM.PaymentCategoryList = new SelectList(List, "CategoryName", "CategoryName", paymentBatchVM.PaymentCategory);

            return View(paymentBatchVM);

        }

        // POST: IFMISTZ/PaymentBatches/Edit/5
        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPaymentBatch([Bind(Include = "PaymentBatchID,InstitutionId,BatchNo,BatchDesc,PaymentCategory,CreatedBy,CreatedAt,CreatedStatus,VerifiedBy,VerifiedAt,VerificationStatus,ApprovedBy,ApprovedAt,ApprovalStatus,OverallStatus,CancelledBy,CancelledAt,CancelledStatus,CancelledReason,PrefundingRef,PrefundingDate,PrefundingType,PrefundingBy,PrefundingAt,PrefundedAccountNo,PrefundedAccountName,PrefundedBankName,PrefundedBIC,SourceAccountNo,SourceAccountName,SourceBIC,AmountFunded,NoTrx,TotalAmount,SourceBankName,BotSlipFileName,MsgID,PayrollId")] PaymentBatchVM paymentBatchVM)
        {
            if (ModelState.IsValid)
            {
                var paymentBatch = db.PaymentBatches.Find(paymentBatchVM.PaymentBatchID);
                paymentBatch.BatchDesc = paymentBatchVM.BatchDesc;
                paymentBatch.InstitutionId = (int)paymentBatchVM.InstitutionId;
                paymentBatch.PaymentCategory = paymentBatchVM.PaymentCategory;
                //paymentBatch.PaymentCategory = paymentBatchVM.PaymentCategories.ToString();
                paymentBatch.OverallStatus = "Pending";
                paymentBatch.CreatedBy = User.Identity.Name;
                paymentBatch.CreatedAt = DateTime.Now;

                db.SaveChanges();

                if (paymentBatchVM.PaymentCategory == "PAYMENT")
                {
                    return RedirectToAction("PaymentBatchList");
                }
                else if (paymentBatchVM.PaymentCategory == "UNAPPLIED")
                {
                    return RedirectToAction("PaymentBatchList");
                    //return RedirectToAction("UnappliedBatchList", "UnappliedBatches", new { area = "IFMISTZ" });
                }

                //return RedirectToAction("PaymentBatchList");
            }
            string userId = User.Identity.GetUserId();
            var InstitutionCodeList = serviceManager.GetInstitutionList(User.Identity.GetUserId());
            paymentBatchVM.InstitutionNameList = new SelectList(InstitutionCodeList, "InstitutionId", "InstitutionCodeInstitutionName", paymentBatchVM.InstitutionId);

            var List = serviceManager.GetPaymentList();
            paymentBatchVM.PaymentCategoryList = new SelectList(List, "CategoryName", "CategoryName", paymentBatchVM.PaymentCategory);

            return View(paymentBatchVM);
        }



        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        public ActionResult DeleteConfirmed(int id)
        {
            string response = "Success";


            using (TransactionScope scope = new TransactionScope())
            {

                try
                {
                    PaymentBatch paymentBatch = db.PaymentBatches.Find(id);
                    paymentBatch.OverallStatus = "Cancelled";

                    var bulkpayment = db.BulkPayments.Where(a => a.PaymentBatchID == id).ToList();
                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Cancelled";
                    }

                    db.SaveChanges();

                    //var PaymentBatchCoaCount = db.PaymentBatchCoas
                    //    .Where(a => a.PaymentBatchId == id).Count();

                    //if (PaymentBatchCoaCount > 0)
                    //{
                    //    ProcessResponse postingStatus = GlService.CancelTransaction(paymentBatch.BatchNo, paymentBatch.JournalTypeCode);
                    //    if (postingStatus.OverallStatus != "Success")
                    //    {
                    //        // Log posting error to table
                    //        //Call Clean up routine
                    //    }
                    //}

                    response = fundBalanceServices.CancelTransaction(paymentBatch.BatchNo, paymentBatch.PaymentBatchID, User.Identity.Name);
                    if (response == "Success")
                    {
                        var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentBatch.BatchNo) };
                        db.Database.ExecuteSqlCommand("dbo.reverse_cancelledBalky_entires_p @PVNo", parameters);

                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();

                }

            }

            return Content(response);
        }



        [HttpGet]
        public ActionResult PaymentConfirmation()
        {
            var paymentbatch = db.PaymentBatches.Where(a => a.OverallStatus == "Pending").ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus
                };
                data.Add(vm);
            }
            return View(data);

        }

        public ActionResult PaymentConfirmationDetails(int? id)
        {
            var bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")
                ).ToList();
            ViewBag.Batch = db.PaymentBatches.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")).FirstOrDefault();
            return View(bulkpayment);
        }


        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        public JsonResult PaymentConfirmation(int? id)
        {
            string response = "";

            PaymentBatch paymentBatches = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id && a.OverallStatus != "Cancelled")
                .FirstOrDefault();

            if (paymentBatches.NoTrx < 10 && paymentBatches.PaymentCategory == "PAYMENT")
            {
                response = "The bulk payment process require at least ten transactions";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            var totalBatchAmount = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id)
                .Select(a => a.TotalAmount)
                .FirstOrDefault();

            var GLCoaSum = db.PaymentBatchCoas
                .Where(a => a.PaymentBatchId == id)
                .Sum(a => a.OperationalAmount);

            if (totalBatchAmount != GLCoaSum)
            {
                response = "Total GL item amount and total batch amount do not match";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            if (paymentBatches != null)
            {
                paymentBatches.OverallStatus = "Confirmed";
            }

            List<BulkPayment> bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id && a.OverallStatus != "Cancelled")
                .ToList();

            if (bulkpayment != null)
            {
                foreach (var item in bulkpayment)
                {
                    item.OverallStatus = "Confirmed";
                }
            }

            db.SaveChanges();

            response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);

            response = "Success";

            return Json(response, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddGLitem(int? id)
        {
            string institutioncategory = null;
            string levelonecode = null;

            var bulkpayment = (from a in db.BulkPayments
                               where a.PaymentBatchID == id
                               where (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")
                               select a
                              ).ToList();

            var paymentbatch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Pending"
                || a.OverallStatus == "Rejected in Verification")
                ).FirstOrDefault();

            ViewBag.batchnumber = paymentbatch.BatchNo;
            ViewBag.batchDescription = paymentbatch.BatchDesc;
            ViewBag.NoOfTrans = paymentbatch.NoTrx;
            ViewBag.totalAmount = paymentbatch.TotalAmount;
            var category = paymentbatch.PaymentCategory;

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            if (category == "PAYMENT")
            {
                var subBudgetClassList = db.CurrencyRateViews.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                          && a.SubBudgetClass != null
                                          && a.SubBudgetClass != "303")
                                          .OrderBy(a => a.SubBudgetClass).ToList();
                ViewBag.subBudgetClassList = subBudgetClassList;
            }
            else
            {
                var subBudgetClassList = db.CurrencyRateViews
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                              && a.SubBudgetClass != null
                              && a.SubBudgetClass == "303")
                              .OrderBy(a => a.SubBudgetClass).ToList();
                ViewBag.subBudgetClassList = subBudgetClassList;
            }
            ViewBag.PayeeTypesList = db.PayeeTypes.ToList();

            ViewBag.PaymentBatchMethod = db.PaymentBatchMethods.ToList();
            return View();
        }



        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        public ActionResult AddGLitem(PaymentBatchVM paymentBatch)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            db.Database.CommandTimeout = 1200;
            string response = "Success";

            //using (var trans = db.Database.BeginTransaction())
            using (TransactionScope trxscope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                List<PaymentBatchCoa> paymentButchCoaList = new List<PaymentBatchCoa>();
                try
                {

                    PaymentBatch paymentbatches = db.PaymentBatches
                        .Where(a => a.BatchNo == paymentBatch.BatchNo
                        && a.OverallStatus == "Pending")
                        .FirstOrDefault();

                    if (paymentBatch.BulkPaymentMethod == "Different Account")
                    {
                        var sourceAccountNum = db.InstitutionAccounts
                          .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                          && a.InstitutionCode == userPaystation.InstitutionCode
                          && a.OverallStatus == "Active"
                          && a.IsTSA == false
                          ).FirstOrDefault();

                        if (sourceAccountNum == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentBatch.SubBudgetClass + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        var voucherpayerBank = db.InstitutionAccounts
                        .Where(a => a.AccountNumber == sourceAccountNum.AccountNumber
                        && a.InstitutionCode == userPaystation.InstitutionCode
                        && a.SubBudgetClass == paymentBatch.SubBudgetClass
                        && a.OverallStatus == "Active"
                        && a.IsTSA == false
                        ).FirstOrDefault();

                        if (voucherpayerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no Deposit account number '" + paymentBatch.BankAccountNo + "' for Institution Code'" + userPaystation.InstitutionCode + "'. Please consult Administrator!";

                            return Content(response);
                        }

                        var payerBank = db.InstitutionAccounts
                        .Where(a => a.AccountNumber == paymentBatch.BankAccountNo
                        && a.InstitutionCode == userPaystation.InstitutionCode
                        && a.OverallStatus == "Active"
                        && a.IsTSA == false
                        ).FirstOrDefault();

                        if (payerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no Deposit account number '" + paymentBatch.BankAccountNo + "' for Institution Code'" + userPaystation.InstitutionCode + "'. Please consult Administrator!";
                            return Content(response);
                        }


                        var payee = db.Payees
                               .Where(a => a.PayeeCode == paymentBatch.PayeeCode
                               && a.OverallStatus == "ACTIVE")
                               .FirstOrDefault();

                        if (payee == null)
                        {
                            response = "There is no payee '" + paymentBatch.PayeeName + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        var payeeDetails = db.PayeeDetails
                            .Where(a => a.PayeeId == payee.PayeeId
                            && a.Accountnumber == paymentBatch.BankAccountNo
                            && a.IsActive == true)
                            .FirstOrDefault();

                        if (payeeDetails == null)
                        {
                            response = "There is no payee with Account number  '" + paymentBatch.BankAccountNo + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        var payeeType = db.PayeeTypes
                            .Where(a => a.PayeeTypeCode.ToUpper() == payeeDetails.PayeeType.ToUpper())
                            .FirstOrDefault();

                        if (payeeType == null)
                        {
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentBatch.PayeeName + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        var crCodes = db.JournalTypeViews
                            .Where(a => a.CrGfsCode == payeeType.GfsCode
                            && a.SubBudgetClass == paymentBatch.SubBudgetClass
                            && a.InstitutionCode == userPaystation.InstitutionCode)
                            .FirstOrDefault();
                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentBatch.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        //var unappliedAccount = db.InstitutionAccounts
                        //       .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                        //     //  && a.AccountType.ToUpper() == "UNAPPLIED"
                        //     && a.AccountNumber==payerBank.AccountNumber
                        //     && a.UnnappliedAccountNumber != null
                        //       && a.OverallStatus == "Active"
                        //       && a.IsTSA == false
                        //       ).FirstOrDefault();


                        var unappliedAccount = ServiceManager.GetUnappliedAccount(db, userPaystation.InstitutionCode, paymentBatch.SubBudgetClass);
                        if (unappliedAccount == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        CurrencyRateView currencyRateView = db.CurrencyRateViews
                            .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                            && a.InstitutionId == userPaystation.InstitutionId)
                            .FirstOrDefault();

                        if (currencyRateView == null)
                        {
                            response = "Currency Rate Setup is Incomplete";
                            return Content(response);
                        }
                        var baseAmount = paymentBatch.OperationalAmount * currencyRateView.OperationalExchangeRate;

                        var glPostingDetail = GlService.GetGlPostingDetail(userPaystation.InstitutionCode, paymentBatch.SubBudgetClass, voucherpayerBank.GlAccount, paymentBatch.ParentInstitutionCode);

                        if (glPostingDetail.OverallStatus == "Error")
                        {
                            return Content(glPostingDetail.OverallStatusDescription);
                        }

                        if (paymentbatches != null)
                        {
                            paymentbatches.SourceModule = "Bulk Different";
                            paymentbatches.SourceModuleReferenceNo = "NA";
                            paymentbatches.PayeeType = paymentBatch.PayeeType;
                            paymentbatches.PayeeDetailId = paymentBatch.PayeeDetailId;
                            paymentbatches.PayeeCode = paymentBatch.PayeeCode;
                            paymentbatches.Payeename = paymentBatch.PayeeName;
                            paymentbatches.PayeeBankAccount = paymentBatch.BankAccountNo;
                            paymentbatches.PayeeBankName = paymentBatch.BankName;
                            paymentbatches.PayeeAccountName = paymentBatch.PayeeName;
                            paymentbatches.PayeeBIC = paymentBatch.PayeeBIC;
                            paymentbatches.PaymentDesc = paymentBatch.PaymentDescription;
                            paymentbatches.OperationalAmount = paymentBatch.OperationalAmount;
                            paymentbatches.BaseAmount = baseAmount;
                            paymentbatches.BaseCurrency = currencyRateView.BaseCurrencyCode;
                            paymentbatches.OperationalCurrency = currencyRateView.OperationalCurrencyCode;
                            paymentbatches.ExchangeRate = currencyRateView.OperationalExchangeRate;
                            paymentbatches.CreatedBy = User.Identity.Name;
                            paymentbatches.CreatedAt = DateTime.Now;
                            paymentbatches.OverallStatus = "Pending";
                            paymentbatches.Book = "MAIN";
                            paymentbatches.InstitutionId = userPaystation.InstitutionId;
                            paymentbatches.InstitutionCode = userPaystation.InstitutionCode;
                            paymentbatches.InstitutionName = userPaystation.Institution.InstitutionName;
                            paymentbatches.PaystationId = userPaystation.InstitutionSubLevelId;
                            paymentbatches.SubLevelCategory = userPaystation.SubLevelCategory;
                            paymentbatches.SubLevelCode = userPaystation.SubLevelCode;
                            paymentbatches.SubLevelDesc = userPaystation.SubLevelDesc;

                            paymentbatches.InstitutionLevel = userPaystation.Institution.InstitutionLevel;
                            paymentbatches.Level1Code = userPaystation.Institution.Level1Code;
                            paymentbatches.Level1Desc = userPaystation.Institution.Level1Desc;
                            paymentbatches.InstitutionLogo = userPaystation.Institution.InstitutionLogo;

                            paymentbatches.SubBudgetClass = paymentBatch.SubBudgetClass;
                            paymentbatches.JournalTypeCode = "BPV";
                            paymentbatches.InstitutionAccountId = payerBank.InstitutionAccountId;

                            paymentbatches.SourceAccountNo = sourceAccountNum.AccountNumber;
                            paymentbatches.SourceAccountName = sourceAccountNum.AccountName;
                            paymentbatches.SourceBankName = sourceAccountNum.BankName;
                            paymentbatches.SourceBIC = sourceAccountNum.BIC;

                            paymentbatches.PayerBankAccount = payerBank.AccountNumber;
                            paymentbatches.PayerBankName = payerBank.AccountName;
                            paymentbatches.PayerBIC = payerBank.BIC;

                            paymentbatches.PayerCashAccount = sourceAccountNum.GlAccount;
                            paymentbatches.PayerAccountType = sourceAccountNum.AccountType;
                            paymentbatches.PayableGlAccount = crCodes.CrCoa;

                            paymentbatches.UnappliedAccount = unappliedAccount.AccountNumber;
                            paymentbatches.GLstatus = "YES";
                            paymentbatches.BulkPaymentMethod = paymentBatch.BulkPaymentMethod;

                            // St Payment
                            paymentbatches.StPaymentFlag = paymentBatch.IsStPayment;
                            paymentbatches.ParentInstitutionCode = paymentBatch.ParentInstitutionCode;
                            paymentbatches.ParentInstitutionName = paymentBatch.ParentInstitutionName;
                            paymentbatches.SubWarrantCode = paymentBatch.SubWarrantCode;
                            paymentbatches.SubWarrantDescription = paymentBatch.SubWarrantDescription;

                            //Sub TSA
                            paymentbatches.SubTSAAccountNumber = payerBank.SubTSAAccountNumber;
                            paymentbatches.SubTsaBankAccountName = payerBank.SubTsaBankAccountName;
                            paymentbatches.SubTsaCashAccount = payerBank.SubTSAGlAccount;


                            // Gl Posting Detail
                            paymentbatches.StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode;
                            paymentbatches.StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa;
                            paymentbatches.StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc;
                            paymentbatches.DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode;
                            paymentbatches.DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa;
                            paymentbatches.DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc;
                            paymentbatches.GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode;
                            paymentbatches.GrantCoa = glPostingDetail.glPostingDetail.GrantCoa;
                            paymentbatches.GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc;
                            //paymentbatches.InstitutionLevel = glPostingDetail.glPostingDetail.institutionlevel;
                            //paymentbatches.Level1Code = glPostingDetail.glPostingDetail.level1code;
                            //paymentbatches.Level1Desc = glPostingDetail.glPostingDetail.level1desc;
                            // paymentbatches.InstitutionLogo = glPostingDetail.glPostingDetail.institutionLogo;


                        }



                        //List<PaymentBatchCoa> paymentButchCoaList = new List<PaymentBatchCoa>();
                        foreach (PaymentBatchCoaVm PaymentBatchCoaVm in paymentBatch.PaymentBatchCoa)
                        {
                            COA coa = db.COAs.Where(a => a.GlAccount == PaymentBatchCoaVm.ExpenditureLineItem).FirstOrDefault();
                            var baseAmount2 = PaymentBatchCoaVm.ExpenseAmount * currencyRateView.OperationalExchangeRate;
                            PaymentBatchCoa paymentBatchCoa = new PaymentBatchCoa
                            {
                                PaymentBatchId = paymentbatches.PaymentBatchID,
                                JournalTypeCode = "BPV",
                                DrGlAccount = PaymentBatchCoaVm.ExpenditureLineItem,
                                DrGlAccountDesc = PaymentBatchCoaVm.ItemDescription,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = PaymentBatchCoaVm.FundingReference,
                                OperationalAmount = PaymentBatchCoaVm.ExpenseAmount,
                                BaseAmount = baseAmount2,
                                GfsCode = coa.GfsCode,
                                FundingSourceDesc = coa.FundingSourceDesc,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                VoteDesc = coa.VoteDesc,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TR = coa.TR,
                                TrDesc = coa.TrDesc,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutputDesc,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                Level1Code = userPaystation.Institution.Level1Code,
                                InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                                Level1Desc = coa.Level1Desc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                SourceModuleRefNo = paymentbatches.BatchNo
                            };
                            paymentButchCoaList.Add(paymentBatchCoa);
                        }
                        db.PaymentBatchCoas.AddRange(paymentButchCoaList);
                        db.SaveChanges();
                    }
                    else
                    {

                        var payerBank = db.InstitutionAccounts
                         .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                         && a.InstitutionCode == userPaystation.InstitutionCode
                         && a.IsTSA == false
                         && a.OverallStatus != "Cancelled"
                         ).FirstOrDefault();

                        if (payerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentBatch.SubBudgetClass + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode == "Employee").FirstOrDefault();

                        if (payeeType == null)
                        {
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentBatch.PayeeType + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == paymentBatch.SubBudgetClass && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentBatch.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        //var unappliedAccount = db.InstitutionAccounts
                        //       .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                        //       && a.AccountType.ToUpper() == "UNAPPLIED"
                        //       && a.IsTSA == false
                        //       && a.OverallStatus != "Cancelled"
                        //       ).FirstOrDefault();

                        //if (unappliedAccount == null)
                        //{
                        //    response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                        //    return Content(response);
                        //}

                        var unappliedAccount = ServiceManager.GetUnappliedAccount(db, userPaystation.InstitutionCode, paymentBatch.SubBudgetClass);
                        if (unappliedAccount == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                            return Content(response);
                        }


                        CurrencyRateView currencyRateView = db.CurrencyRateViews.Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                                 && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();

                        if (currencyRateView == null)
                        {
                            response = "Currency Rate Setup is Incomplete";
                            return Content(response);
                        }
                        var baseAmount = paymentBatch.OperationalAmount * currencyRateView.OperationalExchangeRate;

                        var glPostingDetail = GlService.GetGlPostingDetail(userPaystation.InstitutionCode, paymentBatch.SubBudgetClass, payerBank.GlAccount, paymentBatch.ParentInstitutionCode);

                        if (glPostingDetail.OverallStatus == "Error")
                        {
                            return Content(glPostingDetail.OverallStatusDescription);
                        }

                        if (paymentbatches != null)
                        {
                            paymentbatches.SourceModule = "Bulk Same";
                            paymentbatches.SourceModuleReferenceNo = "NA";
                            paymentbatches.PayeeType = paymentBatch.PayeeType;
                            paymentbatches.PayeeDetailId = paymentBatch.PayeeDetailId;
                            paymentbatches.PayeeCode = paymentBatch.PayeeCode;
                            paymentbatches.Payeename = paymentBatch.PayeeName;
                            paymentbatches.PayeeBankAccount = paymentBatch.BankAccountNo;
                            paymentbatches.PayeeBankName = paymentBatch.BankName;
                            paymentbatches.PayeeAccountName = paymentBatch.PayeeName;
                            paymentbatches.PayeeBIC = paymentBatch.PayeeBIC;
                            paymentbatches.PaymentDesc = paymentBatch.PaymentDescription;
                            paymentbatches.OperationalAmount = paymentBatch.OperationalAmount;
                            paymentbatches.BaseAmount = baseAmount;
                            paymentbatches.BaseCurrency = currencyRateView.BaseCurrencyCode;
                            paymentbatches.OperationalCurrency = currencyRateView.OperationalCurrencyCode;
                            paymentbatches.ExchangeRate = currencyRateView.OperationalExchangeRate;
                            //FinancialYear = serviceManager.GetFinancialYear(db, paymentBatch.ApplyDate),
                            paymentbatches.CreatedBy = User.Identity.Name;
                            paymentbatches.CreatedAt = DateTime.Now;
                            paymentbatches.OverallStatus = "Pending";
                            paymentbatches.Book = "MAIN";
                            paymentbatches.InstitutionId = userPaystation.InstitutionId;
                            paymentbatches.InstitutionCode = userPaystation.InstitutionCode;
                            paymentbatches.InstitutionName = userPaystation.Institution.InstitutionName;

                            paymentbatches.InstitutionLevel = userPaystation.Institution.InstitutionLevel;
                            paymentbatches.Level1Code = userPaystation.Institution.Level1Code;
                            paymentbatches.Level1Desc = userPaystation.Institution.Level1Desc;
                            paymentbatches.InstitutionLogo = userPaystation.Institution.InstitutionLogo;

                            paymentbatches.PaystationId = userPaystation.InstitutionSubLevelId;
                            paymentbatches.SubLevelCategory = userPaystation.SubLevelCategory;
                            paymentbatches.SubLevelCode = userPaystation.SubLevelCode;
                            paymentbatches.SubLevelDesc = userPaystation.SubLevelDesc;
                            paymentbatches.SubBudgetClass = paymentBatch.SubBudgetClass;
                            paymentbatches.JournalTypeCode = "BPT";
                            paymentbatches.InstitutionAccountId = payerBank.InstitutionAccountId;

                            paymentbatches.SourceAccountNo = payerBank.AccountNumber;
                            paymentbatches.SourceAccountName = payerBank.AccountName;
                            paymentbatches.SourceBankName = payerBank.BankName;
                            paymentbatches.SourceBIC = payerBank.BIC;

                            paymentbatches.PayerBankAccount = payerBank.AccountNumber;
                            paymentbatches.PayerBankName = payerBank.AccountName;
                            paymentbatches.PayerBIC = payerBank.BIC;
                            paymentbatches.PayerCashAccount = payerBank.GlAccount;
                            paymentbatches.PayableGlAccount = crCodes.CrCoa;
                            paymentbatches.UnappliedAccount = unappliedAccount.AccountNumber;
                            paymentbatches.PayerAccountType = payerBank.AccountType;
                            paymentbatches.GLstatus = "YES";
                            paymentbatches.BulkPaymentMethod = paymentBatch.BulkPaymentMethod;

                            // St Payment
                            paymentbatches.StPaymentFlag = paymentBatch.IsStPayment;
                            paymentbatches.ParentInstitutionCode = paymentBatch.ParentInstitutionCode;
                            paymentbatches.ParentInstitutionName = paymentBatch.ParentInstitutionName;
                            paymentbatches.SubWarrantCode = paymentBatch.SubWarrantCode;
                            paymentbatches.SubWarrantDescription = paymentBatch.SubWarrantDescription;

                            //Sub TSA
                            paymentbatches.SubTSAAccountNumber = payerBank.SubTSAAccountNumber;
                            paymentbatches.SubTsaBankAccountName = payerBank.SubTsaBankAccountName;
                            paymentbatches.SubTsaCashAccount = payerBank.SubTSAGlAccount;

                            // Gl Posting Detail
                            paymentbatches.StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode;
                            paymentbatches.StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa;
                            paymentbatches.StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc;
                            paymentbatches.DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode;
                            paymentbatches.DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa;
                            paymentbatches.DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc;
                            paymentbatches.GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode;
                            paymentbatches.GrantCoa = glPostingDetail.glPostingDetail.GrantCoa;
                            paymentbatches.GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc;


                        }
                        // List<PaymentBatchCoa> paymentButchCoaList = new List<PaymentBatchCoa>();
                        foreach (PaymentBatchCoaVm PaymentBatchCoaVm in paymentBatch.PaymentBatchCoa)
                        {
                            COA coa = db.COAs.Where(a => a.GlAccount == PaymentBatchCoaVm.ExpenditureLineItem).FirstOrDefault();
                            var baseAmount2 = PaymentBatchCoaVm.ExpenseAmount * currencyRateView.OperationalExchangeRate;
                            PaymentBatchCoa paymentBatchCoa = new PaymentBatchCoa
                            {
                                PaymentBatchId = paymentbatches.PaymentBatchID,
                                JournalTypeCode = "BPT",
                                DrGlAccount = PaymentBatchCoaVm.ExpenditureLineItem,
                                DrGlAccountDesc = PaymentBatchCoaVm.ItemDescription,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = PaymentBatchCoaVm.FundingReference,
                                OperationalAmount = PaymentBatchCoaVm.ExpenseAmount,
                                BaseAmount = baseAmount2,
                                FundingSourceDesc = coa.FundingSourceDesc,
                                GfsCode = coa.GfsCode,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                VoteDesc = coa.VoteDesc,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TR = coa.TR,
                                TrDesc = coa.TrDesc,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutputDesc,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                Level1Code = userPaystation.Institution.Level1Code,
                                InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                                Level1Desc = coa.Level1Desc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                SourceModuleRefNo = paymentbatches.BatchNo

                            };
                            paymentButchCoaList.Add(paymentBatchCoa);
                        }

                        db.PaymentBatchCoas.AddRange(paymentButchCoaList);
                        db.SaveChanges();
                    }


                    string voucherSubLevelCode = userPaystation.SubLevelCode;
                    string voucherInstitutionCode = userPaystation.InstitutionCode;
                    string voucherInstitutionName = userPaystation.Institution.InstitutionName;

                    //if (paymentbatches.StPaymentFlag)
                    //{
                    //    voucherSubLevelCode = paymentbatches.SubWarrantCode;
                    //    voucherInstitutionCode = paymentbatches.ParentInstitutionCode;
                    //    voucherInstitutionName = paymentbatches.ParentInstitutionName;
                    //}

                    if (paymentbatches.StPaymentFlag && !paymentbatches.SubBudgetClass.StartsWith("3"))
                    {
                        voucherSubLevelCode = paymentbatches.SubWarrantCode;
                        voucherInstitutionCode = paymentbatches.ParentInstitutionCode;
                        voucherInstitutionName = paymentbatches.ParentInstitutionName;
                    }

                    else if (paymentbatches.SubBudgetClass.StartsWith("3") && paymentbatches.StPaymentFlag)
                    {
                        voucherSubLevelCode = paymentbatches.SubWarrantCode;
                    }
                    List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                    // var paymentBatchDetails = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == paymentbatches.PaymentBatchID).ToList();

                    foreach (var paymentBatchDetail in paymentButchCoaList)
                    {

                        TransactionLogVM transactionLogVM = new TransactionLogVM()
                        {
                            SourceModuleId = paymentbatches.PaymentBatchID,
                            SourceModule = paymentbatches.SourceModule,
                            LegalNumber = paymentbatches.BatchNo,
                            FundingRerenceNo = paymentBatchDetail.FundingReferenceNo,
                            FundingSourceDesc = paymentBatchDetail.FundingSourceDesc,
                            InstitutionCode = voucherInstitutionCode,
                            InstitutionName = voucherInstitutionName,
                            JournalTypeCode = paymentbatches.JournalTypeCode,
                            GlAccount = paymentBatchDetail.DrGlAccount,
                            GlAccountDesc = paymentBatchDetail.DrGlAccountDesc,
                            GfsCode = paymentBatchDetail.GfsCode,
                            GfsCodeCategory = paymentBatchDetail.GfsCodeCategory,
                            TransactionCategory = "Expenditure",
                            VoteDesc = paymentBatchDetail.VoteDesc,
                            GeographicalLocationDesc = paymentBatchDetail.GeographicalLocationDesc,
                            TR = paymentBatchDetail.TR,
                            TrDesc = paymentBatchDetail.TrDesc,
                            SubBudgetClass = paymentbatches.SubBudgetClass,
                            SubBudgetClassDesc = paymentBatchDetail.SubBudgetClassDesc,
                            ProjectDesc = paymentBatchDetail.ProjectDesc,
                            ServiceOutputDesc = paymentBatchDetail.ServiceOutputDesc,
                            ActivityDesc = paymentBatchDetail.ActivityDesc,
                            FundTypeDesc = paymentBatchDetail.FundTypeDesc,
                            CofogDesc = paymentBatchDetail.CofogDesc,
                            SubLevelCode = voucherSubLevelCode,
                            FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                            OperationalAmount = paymentBatchDetail.OperationalAmount,
                            BaseAmount = paymentBatchDetail.BaseAmount,
                            Currency = paymentbatches.OperationalCurrency,
                            CreatedAt = DateTime.Now,
                            CreatedBy = paymentbatches.CreatedBy,
                            ApplyDate = paymentbatches.CreatedAt,
                            PayeeCode = paymentbatches.PayeeCode,
                            PayeeName = paymentbatches.Payeename,
                            TransactionDesc = paymentbatches.BatchDesc,
                            Facility = paymentBatchDetail.Facility,
                            FacilityDesc = paymentBatchDetail.FacilityDesc,
                            CostCentre = paymentBatchDetail.CostCentre,
                            CostCentreDesc = paymentBatchDetail.CostCentreDesc,
                            Level1Code = userPaystation.Institution.Level1Code,
                            Level1Desc = paymentBatchDetail.Level1Desc,
                            SubVote = paymentBatchDetail.SubVote,
                            SubVoteDesc = paymentBatchDetail.SubVoteDesc,
                            SourceModuleRefNo = paymentBatchDetail.SourceModuleRefNo,
                            InstitutionLevel = paymentbatches.InstitutionLevel,
                            OverallStatus = paymentbatches.OverallStatus,
                            OverallStatusDesc = paymentbatches.OverallStatus
                        };

                        transactionLogVMs.Add(transactionLogVM);
                    }

                    response = fundBalanceServices.PostTransaction(transactionLogVMs);

                    if (response == "Success")
                    {
                        trxscope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                    trxscope.Dispose();
                }

            }
            return Content(response);
        }

        public JsonResult GetFundBalance(string subBudgetClass)
        {

            db.Database.CommandTimeout = 120;

            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>();
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                BalanceResponse fundBalanceResponse = serviceManager.GetFundBalance(userPaystation.InstitutionCode, subBudgetClass, DateTime.Now);
                if (fundBalanceResponse.overallStatus == "Error")
                {
                    //Handle Errors Here
                    //Default is to return an empty list
                    return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
                }

                fundBalanceList = fundBalanceResponse.FundBalanceViewList
                             .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                             && a.SubBudgetClass == subBudgetClass
                             && a.SublevelCode == userPaystation.SubLevelCode
                             && a.SourceModule == "AP"
                             && a.FundBalance > 0
                             && a.JournalTypeCode == "PV")
                             .ToList();
            }
            catch (Exception ex)
            {
                //Handle Errors Here
                //Default is to return an empty list
                return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditGLitem(int? id)
        {
            string institutioncategory = null;

            var bulkpayment = (from a in db.BulkPayments
                               where a.PaymentBatchID == id
                               where (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification")
                               select a
                              ).ToList();

            var paymentbatch = db.PaymentBatches
             .Where(a => a.PaymentBatchID == id
             && (a.OverallStatus == "Pending"
             || a.OverallStatus == "Rejected in Verification")
             ).FirstOrDefault();

            ViewBag.batchnumber = paymentbatch.BatchNo;
            ViewBag.batchDescription = paymentbatch.BatchDesc;
            ViewBag.NoOfTrans = paymentbatch.NoTrx;
            ViewBag.totalAmount = paymentbatch.TotalAmount;
            ViewBag.ParentInstitutionName = paymentbatch.ParentInstitutionName;
            ViewBag.SubWarrantCode = paymentbatch.SubWarrantCode;
            ViewBag.stPaymentFlag = paymentbatch.StPaymentFlag;

            ViewBag.ParentInstitutionCodeName = paymentbatch.ParentInstitutionCode + '-' + paymentbatch.ParentInstitutionName;
            ViewBag.SubWarrantCodeName = paymentbatch.SubWarrantCode + '-' + paymentbatch.SubWarrantDescription;

            ViewBag.PaymentBatchCoaList = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == id).ToList();
            ViewBag.GLCoaSum = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == id).Sum(a => a.OperationalAmount);

            InstitutionSubLevel userPaystation = serviceManager
              .GetUserPayStation(User.Identity.GetUserId());

            List<CurrencyRateView> subBudgetClassList = db.CurrencyRateViews.ToList();
            if (paymentbatch.PaymentCategory == "PAYMENT")
            {
                subBudgetClassList = db.CurrencyRateViews.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                          && a.SubBudgetClass != null
                                          && a.SubBudgetClass != "303")
                                          .OrderBy(a => a.SubBudgetClass).ToList();
            }
            else
            {
                subBudgetClassList = db.CurrencyRateViews.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                               && a.SubBudgetClass != null
                               && a.SubBudgetClass == "303")
                               .OrderBy(a => a.SubBudgetClass).ToList();

            }
            ViewBag.subBudgetClassList = subBudgetClassList;


            institutioncategory = userPaystation.Institution.InstitutionCategory;
            if (institutioncategory == "Ministry"
                || institutioncategory == "Region"
                || institutioncategory == "Sub Treasury Offices"
                || institutioncategory == "Independent Government Department")
            {
                ViewBag.PaymentBatchMethod = db.PaymentBatchMethods
                .Where(a => a.Method == "Different Account")
                .ToList();
            }
            else
            {
                ViewBag.PaymentBatchMethod = db.PaymentBatchMethods.ToList();
                //ViewBag.PaymentBatchMethod = db.PaymentBatchMethods.ToList();
            }

            ViewBag.PayeeTypesList = db.PayeeTypes.ToList();
            var paymentBatches = db.PaymentBatches.Find(id);
            ViewBag.paymentBatches = paymentBatches;
            List<PaymentBatchCoa> PaymentBatchCoaDetails = paymentBatches.PaymentBatchCoa.ToList();

            ViewBag.PaymentBatchCoaDetails = PaymentBatchCoaDetails;
            ViewBag.PaymentBatchId = id;

            ViewBag.subBudgetClass = subBudgetClassList
                .Where(a => a.SubBudgetClass == paymentBatches.SubBudgetClass)
                .First();

            ViewBag.PaymentBatchCoaList = db.PaymentBatchCoas
                .Where(a => a.PaymentBatchId == id).ToList();

            return View();
        }
        public JsonResult GetPaymentBatchCoasDetails(int id)
        {
            var list = db.PaymentBatchCoas
                .Where(a => a.PaymentBatchId == id);
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Bulk Payment Entry")]
        public ActionResult EditGLitem(PaymentBatchVM paymentBatch, int id)
        {
            List<PaymentBatchCoa> paymentButchCoaList = new List<PaymentBatchCoa>();

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            db.Database.CommandTimeout = 1200;
            string response = "Success";

            using (TransactionScope trxscope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {

                try
                {
                    PaymentBatch Batch = db.PaymentBatches.Find(id);

                    if (paymentBatch.BulkPaymentMethod != "Same Account")
                    {

                        var sourceAccountNum = db.InstitutionAccounts
                           .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                           && a.InstitutionCode == userPaystation.InstitutionCode
                           && a.IsTSA == false
                           ).FirstOrDefault();

                        if (sourceAccountNum == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentBatch.SubBudgetClass + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        var voucherpayerBank = db.InstitutionAccounts
                        .Where(a => a.AccountNumber == sourceAccountNum.AccountNumber
                        && a.InstitutionCode == userPaystation.InstitutionCode
                        && a.SubBudgetClass == paymentBatch.SubBudgetClass
                        && a.IsTSA == false
                        ).FirstOrDefault();

                        if (voucherpayerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no Deposit account number '" + paymentBatch.BankAccountNo + "' for Institution Code'" + userPaystation.InstitutionCode + "'. Please consult Administrator!";

                            return Content(response);
                        }

                        InstitutionAccount payerBank = new InstitutionAccount();
                        if (paymentBatch.BankAccountNo != null)
                        {
                            payerBank = db.InstitutionAccounts
                             .Where(a => a.AccountNumber == paymentBatch.BankAccountNo
                             && a.InstitutionCode == userPaystation.InstitutionCode
                             && a.IsTSA == false
                             ).FirstOrDefault();

                        }
                        else
                        {
                            payerBank = db.InstitutionAccounts
                            .Where(a => a.AccountNumber == Batch.PayerBankAccount
                            && a.InstitutionCode == userPaystation.InstitutionCode
                            && a.IsTSA == false
                            ).FirstOrDefault();

                        }

                        if (payerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no Deposit account number '" + paymentBatch.BankAccountNo + "' for Institution Code'" + userPaystation.InstitutionCode + "'. Please consult Administrator!";

                            return Content(response);
                        }

                        var payeeType = db.PayeeTypes
                            .Where(a => a.PayeeTypeCode == "Employee")
                            .FirstOrDefault();

                        if (payeeType == null)
                        {
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentBatch.PayeeType + "'. Please contact Administrator!";
                            return Content(response);
                        }
                        var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == paymentBatch.SubBudgetClass && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentBatch.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        //var unappliedAccount = db.InstitutionAccounts
                        //    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                        //    && a.AccountType.ToUpper() == "UNAPPLIED"
                        //    && a.IsTSA == false
                        //    ).FirstOrDefault();

                        //if (unappliedAccount == null)
                        //{
                        //    response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                        //    return Content(response);
                        //}

                        var unappliedAccount = ServiceManager.GetUnappliedAccount(db, userPaystation.InstitutionCode, paymentBatch.SubBudgetClass);

                        if (unappliedAccount == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        CurrencyRateView currencyRateView = db.CurrencyRateViews
                            .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                            && a.InstitutionId == userPaystation.InstitutionId)
                            .FirstOrDefault();

                        if (currencyRateView == null)
                        {
                            response = "Currency Rate Setup is Incomplete";
                            return Content(response);
                        }
                        var baseAmount = paymentBatch.OperationalAmount * currencyRateView.OperationalExchangeRate;
                        var glPostingDetail = GlService.GetGlPostingDetail(userPaystation.InstitutionCode, paymentBatch.SubBudgetClass, voucherpayerBank.GlAccount, paymentBatch.ParentInstitutionCode);

                        if (glPostingDetail.OverallStatus == "Error")
                        {
                            return Content(glPostingDetail.OverallStatusDescription);
                        }

                        if (paymentBatch.PayeeName == null)
                        {
                            //Batch.PayeeType = paymentBatch.PayeeType;
                            Batch.SourceModule = "Bulk Different";
                            Batch.SourceModuleReferenceNo = "NA";
                            Batch.PayeeDetailId = Batch.PayeeDetailId;
                            Batch.PayeeCode = Batch.PayeeCode;
                            Batch.Payeename = Batch.PayeeAccountName;
                            Batch.PayeeBankAccount = Batch.PayeeBankAccount;
                            Batch.PayeeBankName = Batch.PayeeBankName;
                            Batch.PayeeAccountName = Batch.PayeeAccountName;
                            Batch.PayeeBIC = Batch.PayeeBIC;
                            Batch.PaymentDesc = paymentBatch.PaymentDescription;
                            Batch.OperationalAmount = paymentBatch.OperationalAmount;
                            Batch.BaseAmount = baseAmount;
                            Batch.BaseCurrency = currencyRateView.BaseCurrencyCode;
                            Batch.OperationalCurrency = currencyRateView.OperationalCurrencyCode;
                            Batch.ExchangeRate = currencyRateView.OperationalExchangeRate;
                            Batch.SubBudgetClass = paymentBatch.SubBudgetClass;

                            // Batch.FinancialYear = serviceManager.GetFinancialYear(db, DateTime.Now);
                            Batch.CreatedBy = User.Identity.Name;
                            Batch.CreatedAt = DateTime.Now;
                            Batch.OverallStatus = "Pending";
                            Batch.Book = "MAIN";
                            Batch.InstitutionId = userPaystation.InstitutionId;
                            Batch.InstitutionCode = userPaystation.InstitutionCode;
                            Batch.InstitutionName = userPaystation.Institution.InstitutionName;
                            Batch.PaystationId = userPaystation.InstitutionSubLevelId;
                            Batch.SubLevelCategory = userPaystation.SubLevelCategory;
                            Batch.SubLevelCode = userPaystation.SubLevelCode;
                            Batch.SubLevelDesc = userPaystation.SubLevelDesc;
                            Batch.SubBudgetClass = paymentBatch.SubBudgetClass;
                            Batch.JournalTypeCode = "BPV";

                            Batch.InstitutionAccountId = sourceAccountNum.InstitutionAccountId;
                            Batch.SourceAccountNo = sourceAccountNum.AccountNumber;
                            Batch.SourceAccountName = sourceAccountNum.AccountName;
                            Batch.SourceBankName = sourceAccountNum.BankName;
                            Batch.SourceBIC = sourceAccountNum.BIC;

                            Batch.PayerBankAccount = payerBank.AccountNumber;
                            Batch.PayerBankName = payerBank.AccountName;
                            Batch.PayerBIC = payerBank.BIC;

                            Batch.PayerCashAccount = sourceAccountNum.GlAccount;
                            Batch.PayerAccountType = sourceAccountNum.AccountType;
                            Batch.PayableGlAccount = crCodes.CrCoa;

                            Batch.UnappliedAccount = unappliedAccount.AccountNumber;
                            Batch.PayerAccountType = payerBank.AccountType;

                            Batch.GLstatus = "YES";
                            Batch.BulkPaymentMethod = "Different Account"; //paymentBatch.BulkPaymentMethod;

                            // St Payment
                            Batch.StPaymentFlag = paymentBatch.IsStPayment;
                            Batch.ParentInstitutionCode = paymentBatch.ParentInstitutionCode;
                            Batch.ParentInstitutionName = paymentBatch.ParentInstitutionName;
                            Batch.SubWarrantCode = paymentBatch.SubWarrantCode;
                            Batch.SubWarrantDescription = paymentBatch.SubWarrantDescription;

                            //Sub TSA

                            Batch.SubTSAAccountNumber = payerBank.SubTSAAccountNumber;
                            Batch.SubTsaBankAccountName = payerBank.SubTsaBankAccountName;
                            Batch.SubTsaCashAccount = payerBank.SubTSAGlAccount;

                            // Gl Posting Detail
                            Batch.StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode;
                            Batch.StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa;
                            Batch.StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc;
                            Batch.DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode;
                            Batch.DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa;
                            Batch.DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc;
                            Batch.GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode;
                            Batch.GrantCoa = glPostingDetail.glPostingDetail.GrantCoa;
                            Batch.GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc;


                        }
                        else
                        {
                            Batch.SourceModule = "Bulk Different";
                            Batch.SourceModuleReferenceNo = "NA";
                            Batch.PayeeType = paymentBatch.PayeeType;
                            Batch.PayeeDetailId = paymentBatch.PayeeDetailId;
                            Batch.PayeeCode = paymentBatch.PayeeCode;
                            Batch.Payeename = paymentBatch.PayeeName;
                            Batch.PayeeBankAccount = paymentBatch.BankAccountNo;
                            Batch.PayeeBankName = paymentBatch.BankName;
                            Batch.PayeeAccountName = paymentBatch.PayeeName;
                            Batch.PayeeBIC = paymentBatch.PayeeBIC;
                            Batch.PaymentDesc = paymentBatch.PaymentDescription;
                            Batch.OperationalAmount = paymentBatch.OperationalAmount;
                            Batch.BaseAmount = baseAmount;
                            Batch.BaseCurrency = currencyRateView.BaseCurrencyCode;
                            Batch.OperationalCurrency = currencyRateView.OperationalCurrencyCode;
                            Batch.ExchangeRate = currencyRateView.OperationalExchangeRate;
                            //Batch.FinancialYear = serviceManager.GetFinancialYear(db, DateTime.Now);
                            Batch.CreatedBy = User.Identity.Name;
                            Batch.CreatedAt = DateTime.Now;
                            Batch.OverallStatus = "Pending";
                            Batch.Book = "MAIN";
                            Batch.InstitutionId = userPaystation.InstitutionId;
                            Batch.InstitutionCode = userPaystation.InstitutionCode;
                            //Batch.InstitutionLevel = userPaystation.Institution.InstitutionLevel;

                            Batch.InstitutionLevel = userPaystation.Institution.InstitutionLevel;
                            Batch.Level1Code = userPaystation.Institution.Level1Code;
                            Batch.Level1Desc = userPaystation.Institution.Level1Desc;
                            Batch.InstitutionLogo = userPaystation.Institution.InstitutionLogo;

                            Batch.InstitutionName = userPaystation.Institution.InstitutionName;
                            Batch.PaystationId = userPaystation.InstitutionSubLevelId;
                            Batch.SubLevelCategory = userPaystation.SubLevelCategory;
                            Batch.SubLevelCode = userPaystation.SubLevelCode;
                            Batch.SubLevelDesc = userPaystation.SubLevelDesc;
                            Batch.SubBudgetClass = paymentBatch.SubBudgetClass;
                            Batch.JournalTypeCode = "BPV";

                            Batch.InstitutionAccountId = sourceAccountNum.InstitutionAccountId;
                            Batch.SourceAccountNo = sourceAccountNum.AccountNumber;
                            Batch.SourceAccountName = sourceAccountNum.AccountName;
                            Batch.SourceBankName = sourceAccountNum.BankName;
                            Batch.SourceBIC = sourceAccountNum.BIC;

                            Batch.PayerBankAccount = payerBank.AccountNumber;
                            Batch.PayerBankName = payerBank.AccountName;
                            Batch.PayerBIC = payerBank.BIC;

                            Batch.PayerCashAccount = sourceAccountNum.GlAccount;
                            Batch.PayerAccountType = sourceAccountNum.AccountType;
                            Batch.PayableGlAccount = crCodes.CrCoa;

                            Batch.UnappliedAccount = unappliedAccount.AccountNumber;
                            Batch.PayerAccountType = payerBank.AccountType;
                            Batch.GLstatus = "YES";
                            Batch.BulkPaymentMethod = "Different Account"; //paymentBatch.BulkPaymentMethod;

                            // St Payment
                            Batch.StPaymentFlag = paymentBatch.IsStPayment;
                            Batch.ParentInstitutionCode = paymentBatch.ParentInstitutionCode;
                            Batch.ParentInstitutionName = paymentBatch.ParentInstitutionName;
                            Batch.SubWarrantCode = paymentBatch.SubWarrantCode;
                            Batch.SubWarrantDescription = paymentBatch.SubWarrantDescription;

                            //Sub TSA

                            Batch.SubTSAAccountNumber = payerBank.SubTSAAccountNumber;
                            Batch.SubTsaBankAccountName = payerBank.SubTsaBankAccountName;
                            Batch.SubTsaCashAccount = payerBank.SubTSAGlAccount;

                            // Gl Posting Detail
                            Batch.StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode;
                            Batch.StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa;
                            Batch.StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc;
                            Batch.DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode;
                            Batch.DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa;
                            Batch.DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc;
                            Batch.GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode;
                            Batch.GrantCoa = glPostingDetail.glPostingDetail.GrantCoa;
                            Batch.GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc;


                        }

                        db.SaveChanges();

                        List<PaymentBatchCoa> datalist = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == id).ToList();
                        foreach (var item in datalist)
                        {
                            db.PaymentBatchCoas.Remove(db.PaymentBatchCoas.Find(item.PaymentBatchCoaId));
                        }


                        foreach (PaymentBatchCoaVm PaymentBatchCoaVm in paymentBatch.PaymentBatchCoa)
                        {
                            var baseAmount2 = PaymentBatchCoaVm.ExpenseAmount * currencyRateView.OperationalExchangeRate;
                            COA coa = db.COAs
                                    .Where(a => a.GlAccount == PaymentBatchCoaVm.ExpenditureLineItem)
                                    .FirstOrDefault();

                            PaymentBatchCoa paymentBatchCoa = new PaymentBatchCoa
                            {
                                PaymentBatchId = id,
                                JournalTypeCode = "BPV",
                                DrGlAccount = PaymentBatchCoaVm.ExpenditureLineItem,
                                DrGlAccountDesc = PaymentBatchCoaVm.ItemDescription,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = PaymentBatchCoaVm.FundingReference,
                                OperationalAmount = PaymentBatchCoaVm.ExpenseAmount,
                                BaseAmount = baseAmount2,
                                GfsCode = coa.GfsCode,
                                FundingSourceDesc = coa.FundingSourceDesc,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                VoteDesc = coa.VoteDesc,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TR = coa.TR,
                                TrDesc = coa.TrDesc,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutputDesc,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                Level1Code = userPaystation.Institution.Level1Code,
                                InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                                Level1Desc = coa.Level1Desc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                SourceModuleRefNo = Batch.BatchNo
                            };
                            paymentButchCoaList.Add(paymentBatchCoa);
                        }
                        db.PaymentBatchCoas.AddRange(paymentButchCoaList);
                        db.SaveChanges();

                    }
                    else
                    {

                        var payerBank = db.InstitutionAccounts
                         .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                         && a.InstitutionCode == userPaystation.InstitutionCode
                         && a.IsTSA == false
                         ).FirstOrDefault();

                        if (payerBank == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentBatch.SubBudgetClass + "'. Please consult Administrator!";
                            return Content(response);
                        }
                        var payeeType = db.PayeeTypes
                                .Where(a => a.PayeeTypeCode == "Employee")
                                .FirstOrDefault();

                        if (payeeType == null)
                        {
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentBatch.PayeeType + "'. Please contact Administrator!";
                            return Content(response);
                        }
                        var crCodes = db.JournalTypeViews
                                .Where(a => a.CrGfsCode == payeeType.GfsCode
                                && a.SubBudgetClass == paymentBatch.SubBudgetClass
                                && a.InstitutionCode == userPaystation.InstitutionCode)
                                .FirstOrDefault();

                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentBatch.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        //var unappliedAccount = db.InstitutionAccounts
                        //    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                        //    && a.AccountType.ToUpper() == "UNAPPLIED"
                        //    && a.IsTSA == false
                        //    ).FirstOrDefault();

                        //if (unappliedAccount == null)
                        //{
                        //    response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                        //    return Content(response);
                        //}

                        var unappliedAccount = ServiceManager.GetUnappliedAccount(db, userPaystation.InstitutionCode, paymentBatch.SubBudgetClass);
                        if (unappliedAccount == null)
                        {
                            response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                            return Content(response);
                        }

                        CurrencyRateView currencyRateView = db.CurrencyRateViews
                                .Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass
                                          && a.InstitutionId == userPaystation.InstitutionId)
                                .FirstOrDefault();

                        if (currencyRateView == null)
                        {
                            response = "Currency Rate Setup is Incomplete";
                            return Content(response);
                        }
                        var baseAmount = paymentBatch.OperationalAmount * currencyRateView.OperationalExchangeRate;

                        var glPostingDetail = GlService.GetGlPostingDetail(userPaystation.InstitutionCode, paymentBatch.SubBudgetClass, payerBank.GlAccount, paymentBatch.ParentInstitutionCode);

                        if (glPostingDetail.OverallStatus == "Error")
                        {
                            return Content(glPostingDetail.OverallStatusDescription);
                        }

                        Batch.SourceModule = "Bulk Same";
                        Batch.SourceModuleReferenceNo = "NA";
                        Batch.PayeeType = paymentBatch.PayeeType;
                        Batch.PayeeDetailId = paymentBatch.PayeeDetailId;
                        Batch.PayeeCode = paymentBatch.PayeeCode;
                        Batch.Payeename = paymentBatch.PayeeName;
                        Batch.PayeeBankAccount = paymentBatch.BankAccountNo;
                        Batch.PayeeBankName = paymentBatch.BankName;
                        Batch.PayeeAccountName = paymentBatch.PayeeName;
                        Batch.PayeeBIC = paymentBatch.PayeeBIC;
                        Batch.PaymentDesc = paymentBatch.PaymentDescription;
                        Batch.OperationalAmount = paymentBatch.OperationalAmount;
                        Batch.BaseAmount = baseAmount;
                        Batch.BaseCurrency = currencyRateView.BaseCurrencyCode;
                        Batch.OperationalCurrency = currencyRateView.OperationalCurrencyCode;
                        Batch.ExchangeRate = currencyRateView.OperationalExchangeRate;
                        //Batch.FinancialYear = serviceManager.GetFinancialYear(db, DateTime.Now);
                        Batch.CreatedBy = User.Identity.Name;
                        Batch.CreatedAt = DateTime.Now;
                        Batch.OverallStatus = "Pending";
                        Batch.Book = "MAIN";
                        Batch.InstitutionId = userPaystation.InstitutionId;
                        Batch.InstitutionCode = userPaystation.InstitutionCode;
                        Batch.InstitutionName = userPaystation.Institution.InstitutionName;

                        Batch.InstitutionLevel = userPaystation.Institution.InstitutionLevel;
                        Batch.Level1Code = userPaystation.Institution.Level1Code;
                        Batch.Level1Desc = userPaystation.Institution.Level1Desc;
                        Batch.InstitutionLogo = userPaystation.Institution.InstitutionLogo;

                        Batch.PaystationId = userPaystation.InstitutionSubLevelId;
                        Batch.SubLevelCategory = userPaystation.SubLevelCategory;
                        Batch.SubLevelCode = userPaystation.SubLevelCode;
                        Batch.SubLevelDesc = userPaystation.SubLevelDesc;
                        Batch.SubBudgetClass = paymentBatch.SubBudgetClass;
                        Batch.JournalTypeCode = "BPT";
                        Batch.InstitutionAccountId = payerBank.InstitutionAccountId;
                        Batch.SourceAccountNo = payerBank.AccountNumber;
                        Batch.SourceAccountName = payerBank.AccountName;
                        Batch.SourceBankName = payerBank.BankName;
                        Batch.SourceBIC = payerBank.BIC;

                        Batch.PayerBankAccount = payerBank.AccountNumber;
                        Batch.PayerBankName = payerBank.AccountName;
                        Batch.PayerBIC = payerBank.BIC;
                        Batch.PayerCashAccount = payerBank.GlAccount;
                        Batch.PayableGlAccount = crCodes.CrCoa;
                        Batch.UnappliedAccount = unappliedAccount.AccountNumber;
                        Batch.PayerAccountType = payerBank.AccountType;
                        Batch.GLstatus = "YES";
                        Batch.BulkPaymentMethod = "Same Account";


                        // St Payment
                        Batch.StPaymentFlag = paymentBatch.IsStPayment;
                        Batch.ParentInstitutionCode = paymentBatch.ParentInstitutionCode;
                        Batch.ParentInstitutionName = paymentBatch.ParentInstitutionName;
                        Batch.SubWarrantCode = paymentBatch.SubWarrantCode;
                        Batch.SubWarrantDescription = paymentBatch.SubWarrantDescription;

                        //Sub TSA

                        Batch.SubTSAAccountNumber = payerBank.SubTSAAccountNumber;
                        Batch.SubTsaBankAccountName = payerBank.SubTsaBankAccountName;
                        Batch.SubTsaCashAccount = payerBank.SubTSAGlAccount;

                        // Gl Posting Detail
                        Batch.StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode;
                        Batch.StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa;
                        Batch.StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc;
                        Batch.DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode;
                        Batch.DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa;
                        Batch.DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc;
                        Batch.GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode;
                        Batch.GrantCoa = glPostingDetail.glPostingDetail.GrantCoa;
                        Batch.GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc;



                        db.SaveChanges();

                        List<PaymentBatchCoa> datalist = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == id).ToList();
                        foreach (var item in datalist)
                        {
                            db.PaymentBatchCoas.Remove(db.PaymentBatchCoas.Find(item.PaymentBatchCoaId));
                        }

                        foreach (PaymentBatchCoaVm PaymentBatchCoaVm in paymentBatch.PaymentBatchCoa)
                        {
                            var baseAmount2 = PaymentBatchCoaVm.ExpenseAmount * currencyRateView.OperationalExchangeRate;

                            COA coa = db.COAs
                                .Where(a => a.GlAccount == PaymentBatchCoaVm.ExpenditureLineItem)
                                .FirstOrDefault();

                            PaymentBatchCoa paymentBatchCoa = new PaymentBatchCoa
                            {
                                PaymentBatchId = id,
                                JournalTypeCode = "BPT",
                                DrGlAccount = PaymentBatchCoaVm.ExpenditureLineItem,
                                DrGlAccountDesc = PaymentBatchCoaVm.ItemDescription,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = PaymentBatchCoaVm.FundingReference,
                                OperationalAmount = PaymentBatchCoaVm.ExpenseAmount,
                                BaseAmount = baseAmount2,
                                FundingSourceDesc = coa.FundingSourceDesc,
                                GfsCode = coa.GfsCode,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                VoteDesc = coa.VoteDesc,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TR = coa.TR,
                                TrDesc = coa.TrDesc,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutputDesc,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                Level1Code = userPaystation.Institution.Level1Code,
                                InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                                Level1Desc = coa.Level1Desc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                SourceModuleRefNo = Batch.BatchNo
                            };
                            paymentButchCoaList.Add(paymentBatchCoa);
                        }

                        db.PaymentBatchCoas.AddRange(paymentButchCoaList);
                        db.SaveChanges();
                    }

                    string voucherSubLevelCode = userPaystation.SubLevelCode;
                    string voucherInstitutionCode = userPaystation.InstitutionCode;
                    string voucherInstitutionName = userPaystation.Institution.InstitutionName;

                    //if (paymentBatch.IsStPayment)
                    //{
                    //    voucherSubLevelCode = paymentBatch.SubWarrantCode;
                    //    voucherInstitutionCode = paymentBatch.ParentInstitutionCode;
                    //    voucherInstitutionName = paymentBatch.ParentInstitutionName;
                    //}

                    if (paymentBatch.IsStPayment && !paymentBatch.SubBudgetClass.StartsWith("3"))
                    {
                        voucherSubLevelCode = paymentBatch.SubWarrantCode;
                        voucherInstitutionCode = paymentBatch.ParentInstitutionCode;
                        voucherInstitutionName = paymentBatch.ParentInstitutionName;
                    }

                    else if (paymentBatch.SubBudgetClass.StartsWith("3") && paymentBatch.IsStPayment)
                    {
                        voucherSubLevelCode = paymentBatch.SubWarrantCode;
                    }

                    //Edit transaction
                    List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();


                    foreach (var paymentBatchDetail in paymentButchCoaList)
                    {
                        TransactionLogVM transactionLogVM = new TransactionLogVM()
                        {
                            SourceModuleId = Batch.PaymentBatchID,
                            SourceModule = Batch.SourceModule,
                            LegalNumber = Batch.BatchNo,
                            FundingRerenceNo = paymentBatchDetail.FundingReferenceNo,
                            FundingSourceDesc = paymentBatchDetail.FundingSourceDesc,
                            InstitutionCode = voucherInstitutionCode,
                            InstitutionName = voucherInstitutionName,
                            JournalTypeCode = Batch.JournalTypeCode,
                            GlAccount = paymentBatchDetail.DrGlAccount,
                            GlAccountDesc = paymentBatchDetail.DrGlAccountDesc,
                            GfsCode = paymentBatchDetail.GfsCode,
                            GfsCodeCategory = paymentBatchDetail.GfsCodeCategory,
                            TransactionCategory = "Expenditure",
                            VoteDesc = paymentBatchDetail.VoteDesc,
                            GeographicalLocationDesc = paymentBatchDetail.GeographicalLocationDesc,
                            TR = paymentBatchDetail.TR,
                            TrDesc = paymentBatchDetail.TrDesc,
                            SubBudgetClass = Batch.SubBudgetClass,
                            SubBudgetClassDesc = paymentBatchDetail.SubBudgetClassDesc,
                            ProjectDesc = paymentBatchDetail.ProjectDesc,
                            ServiceOutputDesc = paymentBatchDetail.ServiceOutputDesc,
                            ActivityDesc = paymentBatchDetail.ActivityDesc,
                            FundTypeDesc = paymentBatchDetail.FundTypeDesc,
                            CofogDesc = paymentBatchDetail.CofogDesc,
                            SubLevelCode = voucherSubLevelCode,
                            FinancialYear = Batch.Financialyear,
                            OperationalAmount = paymentBatchDetail.OperationalAmount,
                            BaseAmount = paymentBatchDetail.BaseAmount,
                            Currency = Batch.OperationalCurrency,
                            CreatedAt = DateTime.Now,
                            CreatedBy = Batch.CreatedBy,
                            ApplyDate = Batch.CreatedAt,
                            PayeeCode = Batch.PayeeCode,
                            PayeeName = Batch.Payeename,
                            TransactionDesc = Batch.BatchDesc,
                            Facility = paymentBatchDetail.Facility,
                            FacilityDesc = paymentBatchDetail.FacilityDesc,
                            CostCentre = paymentBatchDetail.CostCentre,
                            CostCentreDesc = paymentBatchDetail.CostCentreDesc,
                            Level1Code = userPaystation.Institution.Level1Code,
                            InstitutionLevel = Batch.InstitutionLevel,
                            Level1Desc = paymentBatchDetail.Level1Desc,
                            SubVote = paymentBatchDetail.SubVote,
                            SubVoteDesc = paymentBatchDetail.SubVoteDesc,
                            OverallStatus = Batch.OverallStatus,
                            OverallStatusDesc = Batch.OverallStatus,
                            SourceModuleRefNo = paymentBatchDetail.SourceModuleRefNo
                        };

                        transactionLogVMs.Add(transactionLogVM);
                    }
                    response = fundBalanceServices.EditTransaction(transactionLogVMs);

                    if (response == "Success")
                    {
                        trxscope.Complete();
                    }

                }

                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                    trxscope.Dispose();
                }
                return Content(response);
            }
        }





        [HttpGet, Authorize(Roles = "Bulk Payment Verification")]
        public ActionResult PaymentBatchVerification()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            && (a.OverallStatus == "Confirmed" || a.OverallStatus == "Rejected in Approval")
            ).ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo
                };
                data.Add(vm);
            }
            return View(data);
        }

        [HttpGet, Authorize(Roles = "Bulk Payment Verification")]
        public ActionResult PaymentVerificationDetails(int? id)
        {
            var bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Confirmed" || a.OverallStatus == "Rejected in Approval"))
                .ToList();

            ViewBag.Batch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Confirmed" || a.OverallStatus == "Rejected in Approval"))
                .FirstOrDefault();

            return View(bulkpayment);
        }


        [HttpPost, Authorize(Roles = "Bulk Payment Verification")]
        public JsonResult PaymentBatchVerification(int? id)
        {
            string response = "";

            var paymentBatches = (from a in db.PaymentBatches
                                  where a.PaymentBatchID == id
                                  where (a.OverallStatus == "Confirmed"
                                  || a.OverallStatus == "Rejected in Approval")
                                  select a
                                  ).FirstOrDefault();

            var bulkpayment = (from a in db.BulkPayments
                               where a.PaymentBatchID == id
                               where (a.OverallStatus == "Confirmed"
                                  || a.OverallStatus == "Rejected in Approval")
                               select a
                               ).ToList();

            if (paymentBatches.NoTrx < 10 && paymentBatches.PaymentCategory == "PAYMENT")
            {
                response = "The bulk payment process require at least ten transactions";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            if (paymentBatches != null)
            {
                paymentBatches.OverallStatus = "Verified";
                //paymentBatches.VerificationStatus = "Verified";
                paymentBatches.RejectedReason = "";
                paymentBatches.VerifiedBy = User.Identity.Name;
                paymentBatches.VerifiedAt = DateTime.Now;
            }

            foreach (var item in bulkpayment)
            {
                item.OverallStatus = "Verified";
                item.RejectedReason = "";
                item.VerifiedBy = User.Identity.Name;
                item.VerifiedAt = DateTime.Now;
            }

            db.SaveChanges();

            response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);

            response = "Success";
            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [HttpGet, Authorize(Roles = "Bulk Payment Approval")]
        public ActionResult PaymentBatchApproval()
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());


            List<PaymentBatch> paymentbatchList = null;

            var paymentcount = db.InstitutionConfigs
                .Where(a => a.InstitutionId == userPaystation.InstitutionId
                && a.ConfigName == "SkipExamination")
                .Count();

            ViewBag.Count = paymentcount;

            if (paymentcount > 0)
            {
                paymentbatchList = db.PaymentBatches
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                     //&& a.SubLevelCode == userPaystation.SubLevelCode
                     && (a.OverallStatus == "Confirmed" || a.OverallStatus == "Verified"
                     || a.OverallStatus == "Rejected in Submission"
                     || a.OverallStatus == "Rejected in Payment Office"
                     || a.OverallStatus == "Rejected in Payment Voucher")
                     ).ToList();
            }
            else
            {

                paymentbatchList = db.PaymentBatches
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                     //&& a.SubLevelCode == userPaystation.SubLevelCode
                     && (a.OverallStatus == "Verified"
                     || a.OverallStatus == "Rejected in Submission"
                     || a.OverallStatus == "Rejected in Payment Office"
                     || a.OverallStatus == "Rejected in Payment Voucher")
                     ).ToList();

            }

            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatchList)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo,
                    OverallStatus = item.OverallStatus
                };
                data.Add(vm);
            }
            return View(data);

        }

        [HttpGet, Authorize(Roles = "Bulk Payment Approval")]
        public ActionResult PaymentApprovalDetails(int? id)
        {
            var bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Confirmed"
                || a.OverallStatus == "Verified"
                || a.OverallStatus == "Rejected in Submission"
                || a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher"
                )
            ).ToList();

            ViewBag.Batch = db.PaymentBatches.Where(a => a.PaymentBatchID == id
            && (a.OverallStatus == "Confirmed"
            || a.OverallStatus == "Verified"
            || a.OverallStatus == "Rejected in Submission"
            || a.OverallStatus == "Rejected in Payment Office"
            || a.OverallStatus == "Rejected in Payment Voucher"
            )
            ).FirstOrDefault();

            return View(bulkpayment);
        }

        [HttpPost, Authorize(Roles = "Bulk Payment Approval")]
        public ActionResult PaymentBatchApproval(int id)
        {
            string response = "Success";
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            db.Database.CommandTimeout = 1200;
            //using (TransactionScope trxscope = new TransactionScope(TransactionScopeOption.RequiresNew))
            //{

            try
            {
                PaymentBatch paymentBatches = db.PaymentBatches
                    .Where(a => a.PaymentBatchID == id
                    && (a.OverallStatus == "Confirmed"
                    || a.OverallStatus == "Verified"
                    || a.OverallStatus == "Rejected in Submission"
                    || a.OverallStatus == "Rejected in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Voucher")
                    ).FirstOrDefault();

                if (paymentBatches.NoTrx < 10 && paymentBatches.PaymentCategory == "PAYMENT")
                {
                    response = "The bulk payment process require at least ten transactions";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                var bulkpayment = (from a in db.BulkPayments
                                   where a.PaymentBatchID == id
                                   where (a.OverallStatus == "Verified"
                                   || a.OverallStatus == "Rejected in Submission"
                                   || a.OverallStatus == "Rejected in Payment Office"
                                   || a.OverallStatus == "Rejected in Payment Voucher"
                                   )
                                   select a
                                   ).ToList();


                if (paymentBatches != null)
                {
                    if (paymentBatches.BulkPaymentMethod == "Same Account")
                    {
                        paymentBatches.OverallStatus = "Approved";
                        paymentBatches.RejectedReason = "";
                        paymentBatches.ApprovedBy = User.Identity.Name;
                        paymentBatches.ApprovedAt = DateTime.Now;
                        paymentBatches.PrefundingRef = paymentBatches.BatchNo;
                        paymentBatches.PaymentVoucherStatus = "N/A";

                        foreach (var item in bulkpayment)
                        {
                            item.OverallStatus = "Approved";
                            item.RejectedReason = "";
                            item.ApprovedBy = User.Identity.Name;
                            item.ApprovedAt = DateTime.Now;
                        }

                        response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);

                    }
                    else if (paymentBatches.BulkPaymentMethod == "Different Account" && paymentBatches.PaymentCategory == "UNAPPLIED")
                    {
                        paymentBatches.JournalTypeCode = "BPT";
                        paymentBatches.SourceModule = "Bulk Payment Transfer";

                        //string journalTypeCode = "BPT";
                        //var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalTypeCode) };
                        //db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);

                        paymentBatches.OverallStatus = "Approved";
                        paymentBatches.RejectedReason = "";
                        paymentBatches.ApprovedBy = User.Identity.Name;
                        paymentBatches.ApprovedAt = DateTime.Now;

                        foreach (var item in bulkpayment)
                        {
                            item.OverallStatus = "Approved";
                            item.RejectedReason = "";
                            item.ApprovedBy = User.Identity.Name;
                            item.ApprovedAt = DateTime.Now;
                        }

                        response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);
                    }
                    else if (paymentBatches.BulkPaymentMethod == "Different Account" && paymentBatches.PVNo == null)
                    {

                        string journalTypeCode = "BPV";
                        ProcessResponse voucherStatus = serviceManager.GeneratePaymentVoucher(journalTypeCode, id, User);
                        if (voucherStatus.OverallStatus == "Success")
                        {
                            paymentBatches.OverallStatus = "Approved";
                            paymentBatches.RejectedReason = "";
                            paymentBatches.ApprovedBy = User.Identity.Name;
                            paymentBatches.ApprovedAt = DateTime.Now;

                            foreach (var item in bulkpayment)
                            {
                                item.OverallStatus = "Approved";
                                item.RejectedReason = "";
                                item.ApprovedBy = User.Identity.Name;
                                item.ApprovedAt = DateTime.Now;
                            }

                            response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);
                        }
                        else
                        {
                            response = voucherStatus.OverallStatusDescription;
                            return Content(response);
                        }

                    }
                    else
                    {

                        paymentBatches.OverallStatus = "Approved";
                        paymentBatches.RejectedReason = "";
                        paymentBatches.ApprovedBy = User.Identity.Name;
                        paymentBatches.ApprovedAt = DateTime.Now;

                        foreach (var item in bulkpayment)
                        {
                            item.OverallStatus = "Approved";
                            item.RejectedReason = "";
                            item.ApprovedBy = User.Identity.Name;
                            item.ApprovedAt = DateTime.Now;
                        }

                        response = fundBalanceServices.UpdateTransaction(paymentBatches.BatchNo, paymentBatches.PaymentBatchID, paymentBatches.OverallStatus);

                    }

                    db.SaveChanges();
                    //trxscope.Complete();

                    var parameters = new SqlParameter[] {
                           new SqlParameter("@PaymentNo",  paymentBatches.BatchNo),};
                    var gLProcessStatusVM = db.Database.SqlQuery<GLProcessStatusVM>("dbo.GlPostPaymentBatch_p @PaymentNo", parameters).FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
                // trxscope.Dispose();
            }

            response = "Success";
            //}

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectVerifier(int ids, string reason)
        {
            string response = "";
            try

            {
                if (reason == "") { response = "emptyreason"; }
                else
                {
                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                && (a.OverallStatus == "Confirmed"
                || a.OverallStatus == "Rejected in Approval")).FirstOrDefault();

                    batch.OverallStatus = "Rejected in Verification";
                    batch.RejectedReason = reason;
                    batch.RejectedBy = User.Identity.GetUserName();
                    batch.RejectedAt = DateTime.Now;

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Confirmed"
                                       || a.OverallStatus == "Rejected in Approval"
                                        )
                                       select a).ToList();

                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Rejected in Verification";
                        item.RejectedReason = reason;
                        item.RejectedBy = User.Identity.GetUserName();
                        item.RejectedAt = DateTime.Now;
                    }

                    response = fundBalanceServices.UpdateTransaction(batch.BatchNo, batch.PaymentBatchID, batch.OverallStatus);

                    db.SaveChanges();
                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectApproval(int ids, string reason)
        {
            string response = "Success";
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    if (reason == "")
                    {
                        response = "emptyreason";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }

                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                    && (a.OverallStatus == "Rejected in Submission"
                    || a.OverallStatus == "Rejected in Payment Voucher"
                    || a.OverallStatus == "Verified"
                    || a.OverallStatus == "Approved")).FirstOrDefault();

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Rejected in Submission"
                                       || a.OverallStatus == "Rejected in Payment Voucher"
                                       || a.OverallStatus == "Verified"
                                       || a.OverallStatus == "Approved"
                                       )
                                       select a).ToList();

                    if (batch.BulkPaymentMethod == "Different Account" && batch.PaymentVoucherId != 0)
                    {

                        PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(batch.PaymentVoucherId);
                        if (paymentVoucher != null)
                        {
                            if (!(paymentVoucher.OverallStatus == "Pending" || paymentVoucher.OverallStatus == "Cancelled"))
                            {
                                response = "You can not Reject the Payment Batch Number:  " + batch.BatchNo + " while the payment Voucher Number  " + paymentVoucher.PVNo + " is " + paymentVoucher.OverallStatus;
                                return Content(response);
                            }


                            paymentVoucher.OverallStatus = "Cancelled";
                            paymentVoucher.CancelledAt = DateTime.Now;
                            paymentVoucher.CancelledBy = User.Identity.GetUserName();

                            db.SaveChanges();

                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);
                            if (response == "Success")
                            {
                                var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                                db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", parameters);

                                // trans.Commit();

                            }
                            else
                            {
                                trans.Rollback();
                            }


                        }

                        batch.OverallStatus = "Rejected in Approval";
                        batch.RejectedReason = reason;
                        batch.RejectedBy = User.Identity.GetUserName();
                        batch.RejectedAt = DateTime.Now;
                        batch.PVNo = null;
                        batch.PaymentVoucherId = 0;
                        batch.PaymentVoucherStatus = null;



                        foreach (var item in bulkpayment)
                        {
                            item.OverallStatus = "Rejected in Approval";
                            item.RejectedReason = reason;
                            item.RejectedBy = User.Identity.GetUserName();
                            item.RejectedAt = DateTime.Now;
                        }

                    }
                    else
                    {

                        batch.OverallStatus = "Rejected in Approval";
                        batch.RejectedReason = reason;
                        batch.RejectedBy = User.Identity.GetUserName();
                        batch.RejectedAt = DateTime.Now;
                        batch.PVNo = null;
                        batch.PaymentVoucherId = 0;
                        batch.PaymentVoucherStatus = null;



                        foreach (var item in bulkpayment)
                        {
                            item.OverallStatus = "Rejected in Approval";
                            item.RejectedReason = reason;
                            item.RejectedBy = User.Identity.GetUserName();
                            item.RejectedAt = DateTime.Now;
                        }

                    }

                    response = fundBalanceServices.UpdateTransaction(batch.BatchNo, batch.PaymentBatchID, batch.OverallStatus);

                    db.SaveChanges();
                    trans.Commit();

                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                    trans.Rollback();
                }

            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectSubmission2(int ids, string reason)
        {
            string response = "";
            try
            {

                if (reason == "") { response = "emptyreason"; }
                else
                {
                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                && (a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher"
                || a.OverallStatus == "Approved"
                || a.OverallStatus == "Slip Uploaded")).FirstOrDefault();

                    batch.OverallStatus = "Rejected in Submission";
                    batch.RejectedReason = reason;
                    batch.RejectedBy = User.Identity.GetUserName();
                    batch.RejectedAt = DateTime.Now;

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Approved"
                                       || a.OverallStatus == "Slip Uploaded"
                                       || a.OverallStatus == "Rejected in Payment Office"
                                       || a.OverallStatus == "Rejected in Payment Voucher")
                                       select a).ToList();

                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Rejected in Submission";
                        item.RejectedReason = reason;
                        item.RejectedBy = User.Identity.GetUserName();
                        item.RejectedAt = DateTime.Now;
                    }

                    response = fundBalanceServices.UpdateTransaction(batch.BatchNo, batch.PaymentBatchID, batch.OverallStatus);

                    db.SaveChanges();
                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SubmissionPaymentOffice()
        {
            return View();
        }

        public JsonResult getSubmissionList()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
             //&& a.SubLevelCode == userPaystation.SubLevelCode
             && (a.OverallStatus == "Approved"
             || a.OverallStatus == "Slip Uploaded"
             || a.OverallStatus == "Rejected in Payment Office"
             || a.OverallStatus == "Rejected in Payment Voucher"
             )).ToList();
            List<PaymentBatchVM> paymentBatchList = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    PaymentVoucherStatus = item.PaymentVoucherStatus,
                    PVNo = item.PVNo,
                    UploadSlip = item.UploadSlip,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo

                };
                paymentBatchList.Add(vm);
            }

            return Json(new { data = paymentBatchList }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Bulk Payment Submission")]
        public JsonResult AttachmentPOSlip(PaymentBatchVM paymentBatchVM)
        {
            string response = "";
            string fileExtension = "";
            string filename = "";
            string fileLocation = "";

            try
            {
                PaymentBatch model = db.PaymentBatches
                    .Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                    && (a.OverallStatus == "Approved"
                    || a.OverallStatus == "Slip Uploaded"
                    || a.OverallStatus == "Rejected in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Voucher"
                    )).FirstOrDefault();
                List<BulkPayment> data = db.BulkPayments
                    .Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                    && (a.OverallStatus == "Approved"
                    || a.OverallStatus == "Slip Uploaded"
                    || a.OverallStatus == "Rejected in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Voucher"
                    )).ToList();

                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);
                filename = System.IO.Path.GetFileName(Request.Files["FileName"].FileName);
                filename = model.PVNo + DateTime.Now.ToString("_yyyyMMddHHmmss") + ".pdf";

                if (Request.Files["FileName"].ContentLength <= 0 || Request.Files["FileName"].ContentLength > 2000000)
                {
                    response = "Failed to upload due to invalid file size";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                if (fileExtension != ".pdf")
                {
                    response = "Failed to upload due to invalid file format uploaded ";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                fileLocation = Server.MapPath("~/Media/Payments/") + filename;
                if (System.IO.File.Exists(fileLocation))
                {
                    System.IO.File.Delete(fileLocation);
                }
                Request.Files["FileName"].SaveAs(fileLocation);

                if (model != null)
                {
                    model.UploadSlip = "Slip Uploaded";
                    model.OverallStatus = "Slip Uploaded";
                    model.RejectedReason = "";
                    model.FileName = filename;
                    model.UploadedBy = User.Identity.Name;
                    model.UploadedAt = DateTime.Now;
                }
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        item.OverallStatus = "Slip Uploaded";
                        item.RejectedReason = "";
                    }
                }

                db.SaveChanges();
                response = "Success";
            }

            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult RejectSubmission(PaymentBatchVM paymentBatchVM)
        {
            string response = "";
            try
            {
                PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                && (a.OverallStatus == "Approved"
                || a.OverallStatus == "Slip Uploaded"
                || a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher"
               )).FirstOrDefault();

                batch.OverallStatus = "Rejected in Submission";
                batch.RejectedReason = paymentBatchVM.RejectedReason;
                batch.UploadSlip = "";
                batch.RejectedBy = User.Identity.GetUserName();
                batch.RejectedAt = DateTime.Now;

                var bulkpayment = (from a in db.BulkPayments
                                   where a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                                   where (a.OverallStatus == "Approved"
                                   || a.OverallStatus == "Slip Uploaded"
                                   || a.OverallStatus == "Rejected in Payment Office"
                                   || a.OverallStatus == "Rejected in Payment Voucher"
                                   )
                                   select a).ToList();

                foreach (var item in bulkpayment)
                {
                    item.OverallStatus = "Rejected in Submission";
                    item.RejectedReason = paymentBatchVM.RejectedReason;
                    item.RejectedBy = User.Identity.GetUserName();
                    item.RejectedAt = DateTime.Now;
                }

                response = fundBalanceServices.UpdateTransaction(batch.BatchNo, batch.PaymentBatchID, batch.OverallStatus);

                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }




        [HttpGet]
        public ActionResult SubmissionPaymentOffice1(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                if (response == "Success")
                {
                    ViewBag.Message = "Success";
                }
                else
                {
                    ViewBag.Message = "Failed";
                }
            }
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
             //&& a.SubLevelCode == userPaystation.SubLevelCode
             && (a.OverallStatus == "Approved"
             || a.OverallStatus == "Slip Uploaded"
             || a.OverallStatus == "Rejected in Payment Office"
             || a.OverallStatus == "Rejected in Payment Voucher")).ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    PaymentVoucherStatus = item.PaymentVoucherStatus,
                    PVNo = item.PVNo

                };
                data.Add(vm);
            }
            return View(data);
        }

        [HttpGet, Authorize(Roles = "Bulk Payment Submission")]
        public ActionResult SubmissionPaymentOfficeDetails(int? id)
        {
            var bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Approved"
                || a.OverallStatus == "Slip Uploaded"
                || a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher")
                ).ToList();
            ViewBag.Batch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Approved"
                || a.OverallStatus == "Slip Uploaded"
                || a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher")
                ).FirstOrDefault();

            return View(bulkpayment);
        }


        public ActionResult PaymentDetails(int? id)
        {
            var bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && a.OverallStatus != "Rejected"
                && a.OverallStatus != "Cancelled").ToList();

            ViewBag.Batch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && a.OverallStatus != "Rejected"
                && a.OverallStatus != "Cancelled").FirstOrDefault();

            return View(bulkpayment);
        }

        [HttpGet, Authorize(Roles = "Bulk Payment Submission")]
        public ActionResult PrintPaymentSlip(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PaymentBatch paymentBatch = db.PaymentBatches.Find(id);

            if (paymentBatch == null)
            {
                return HttpNotFound();
            }
            InstitutionSubLevel userPaystation = serviceManager
              .GetUserPayStation(User.Identity.GetUserId());
            ViewBag.institutioncategory = userPaystation.Institution.InstitutionCategory;

            ViewBag.Date = DateTime.Now.ToString("dd/MM/yyyy hh:mm");
            return new Rotativa.PartialViewAsPdf("_Bulkpaymentslip", paymentBatch)
            {
                FileName = "POSlip" + paymentBatch.PaymentBatchID + ".pdf",
                CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
            };
        }

        [HttpPost, Authorize(Roles = "Bulk Payment Submission")]
        public ActionResult AttachSlip(PaymentBatchVM paymentBatchVM)
        {
            string response = "";
            string fileExtension = "";
            string filename = "";
            string fileLocation = "";

            try
            {
                PaymentBatch model = db.PaymentBatches
                    .Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                    && (a.OverallStatus == "Approved"
                    || a.OverallStatus == "Slip Uploaded"
                    || a.OverallStatus == "Rejected in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Voucher")
                    ).FirstOrDefault();

                List<BulkPayment> data = db.BulkPayments
                    .Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID
                    && (a.OverallStatus == "Approved"
                    || a.OverallStatus == "Slip Uploaded"
                    || a.OverallStatus == "Rejected in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Voucher")
                    ).ToList();

                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);
                filename = System.IO.Path.GetFileName(Request.Files["FileName"].FileName);
                filename = model.PVNo + DateTime.Now.ToString("_yyyyMMddHHmmss") + ".pdf";

                if (Request.Files["FileName"].ContentLength <= 0 || Request.Files["FileName"].ContentLength > 2000000)
                {
                    response = "Failed to upload due to invalid file size";
                }
                else
                {
                    if (fileExtension != ".pdf")
                    {
                        response = "Failed to upload due to invalid file format uploaded ";
                    }
                    else
                    {
                        fileLocation = Server.MapPath("~/Media/Payments/") + filename;
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                        Request.Files["FileName"].SaveAs(fileLocation);

                        if (model != null)
                        {
                            model.UploadSlip = "Slip Uploaded";
                            model.OverallStatus = "Slip Uploaded";
                            model.RejectedReason = "";
                            model.FileName = filename;
                            model.UploadedBy = User.Identity.Name;
                            model.UploadedAt = DateTime.Now;
                        }
                        if (data != null)
                        {
                            foreach (var item in data)
                            {
                                item.OverallStatus = "Slip Uploaded";
                                item.RejectedReason = "";
                            }
                        }
                        db.SaveChanges();
                        response = "Success";
                    }
                }
            }

            catch (Exception ex)
            {
                response = "Fail";
            }
            return RedirectToAction("SubmissionPaymentOffice", new { response = response });
        }


        [HttpPost, Authorize(Roles = "Bulk Payment Submission")]
        public JsonResult SendtoPaymentOffice(int? id)
        {
            string response = "";

            List<BulkPayment> data = db.BulkPayments.Where(a => a.PaymentBatchID == id
            && (a.OverallStatus == "Slip Uploaded"
            || a.OverallStatus == "Rejected in Payment Office"
            || a.OverallStatus == "Rejected in Payment Voucher")
            ).ToList();

            PaymentBatch paymentBatch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Slip Uploaded"
                || a.OverallStatus == "Rejected in Payment Office"
                || a.OverallStatus == "Rejected in Payment Voucher")
                ).FirstOrDefault();


            if (paymentBatch.BulkPaymentMethod == "Different Account")
            {

                PaymentVoucher paymentVoucher = db.PaymentVouchers
                .Where(a => a.PVNo == paymentBatch.PVNo)
                .FirstOrDefault();

                if (paymentVoucher.OverallStatus == "Cancelled")
                {
                    response = "You can not Submit  the Payment Batch Number:  " + paymentBatch.BatchNo + " to payment office  while the payment Voucher Number  " + paymentVoucher.PVNo + " is " + paymentVoucher.OverallStatus;
                    return Json(response, JsonRequestBehavior.AllowGet);

                }


            }

            if (paymentBatch != null)
            {
                paymentBatch.OverallStatus = "Sent to Payment Office";
                paymentBatch.ButchStatus = "Sent to Payment Office";
                paymentBatch.RejectedReason = "";
                paymentBatch.SentToPaymentOfficeBy = User.Identity.Name;
                paymentBatch.SentToPaymentOfficeAt = DateTime.Now;
            }

            if (data != null)
            {
                foreach (var item in data)
                {
                    item.OverallStatus = "Sent to Payment Office";
                    item.RejectedReason = "";
                    item.SentToPaymentOfficeBy = User.Identity.Name;
                    item.SentToPaymentOfficeAt = DateTime.Now;
                }
            }

            db.SaveChanges();
            response = "Success";
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DownloadPaymentSummary(int id)
        {
            var fileName = db.PaymentBatches.Find(id).FileName;

            return File("~/Media/Payments/" + fileName, "application/pdf", Server.UrlEncode(fileName));
        }


        [HttpGet, Authorize(Roles = "Bulk Payment Submission")]
        public ActionResult SentToPaymentList()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            && a.SubLevelCode == userPaystation.SubLevelCode
            && (a.ButchStatus == "Sent to Payment Office")
            ).ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus
                };
                data.Add(vm);
            }
            return View(data);

        }

        // Payment office
        [HttpGet, Authorize(Roles = "Payment Office Verification")]
        public ActionResult Verification()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches
               .Where(a => a.paymentOfficeId == userPaystation.Institution.PaymentOfficeId
               && (a.OverallStatus == "Sent to Payment Office"
               || a.OverallStatus == "Rejected in Payment Office Approval")
               && (a.PaymentVoucherStatus == "Processed"
               || a.PaymentVoucherStatus == "Settled"
               || a.BulkPaymentMethod == "Same Account"
               || a.TransferStatus == "PROCESSED")
               ).ToList();

            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    PaymentVoucherStatus = item.PaymentVoucherStatus,
                    PVNo = item.PVNo,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo
                };
                data.Add(vm);
            }
            return View(data);
        }

        [HttpGet, Authorize(Roles = "Payment Office Verification")]
        public ActionResult VerificationDetails(int? id)
        {
            var Bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Approval")
                ).ToList();
            ViewBag.Batch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Approval")
                ).FirstOrDefault();
            return View(Bulkpayment);
        }

        [HttpPost, Authorize(Roles = "Payment Office Verification")]
        public JsonResult Verification(int? id)
        {
            string response = "";

            PaymentBatch paymentBatches = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Approval")
                ).FirstOrDefault();

            if (paymentBatches != null)
            {
                paymentBatches.OverallStatus = "Verified in Payment Office";
                paymentBatches.PaymentOfficeStatus = "Verified in Payment Office";
                paymentBatches.RejectedReason = "";
                paymentBatches.PaymentOfficeVerifiedBy = User.Identity.Name;
                paymentBatches.PaymentOfficeVerifiedAt = DateTime.Now;
            }

            List<BulkPayment> bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Sent to Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Approval")
                ).ToList();

            if (bulkpayment != null)
            {
                foreach (var item in bulkpayment)
                {
                    item.OverallStatus = "Verified in Payment Office";
                    item.PaymentOfficeStatus = "Verified in Payment Office";
                    item.RejectedReason = "";
                    item.PaymentOfficeVerifiedBy = User.Identity.Name;
                    item.PaymentOfficeVerifiedAt = DateTime.Now;
                }
            }
            db.SaveChanges();
            response = "Success";
            return Json(response, JsonRequestBehavior.AllowGet);
        }



        [HttpGet, Authorize(Roles = "Payment Office Approval")]
        public ActionResult Approval()
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var paymentbatch = db.PaymentBatches
                .Where(a => a.paymentOfficeId == userPaystation.Institution.PaymentOfficeId
            //&& a.SubLevelCode == userPaystation.SubLevelCode
            && (a.OverallStatus == "Verified in Payment Office"
            || a.OverallStatus == "Rejected in Payment Office Submission")
            ).ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    PaymentVoucherStatus = item.PaymentVoucherStatus,
                    PVNo = item.PVNo,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo
                };
                data.Add(vm);
            }
            return View(data);
        }
        public ActionResult ApprovalDetails(int? id)
        {
            var Bulkpayment = db.BulkPayments
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Verified in Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Submission")
                ).ToList();

            ViewBag.Batch = db.PaymentBatches
                .Where(a => a.PaymentBatchID == id
                && (a.OverallStatus == "Verified in Payment Office"
                || a.OverallStatus == "Rejected in Payment Office Submission")
                ).FirstOrDefault();

            return View(Bulkpayment);
        }

        [HttpPost, Authorize(Roles = "Payment Office Approval")]
        public JsonResult Approval(int id)
        {
            string response = "";

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            try
            {
                PaymentBatch model = db.PaymentBatches.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Verified in Payment Office" || a.OverallStatus == "Rejected in Payment Office Submission")).FirstOrDefault();
                if (model != null)
                {
                    model.OverallStatus = "Approved in Payment Office";
                    model.PaymentOfficeStatus = "Approved in Payment Office";
                    model.RejectedReason = "";
                    model.PaymentOfficeApprovedAt = DateTime.Now;
                    model.PaymentOfficeApprovedBy = User.Identity.Name;
                    model.JournalTypeCode = "PDB";
                }

                List<BulkPayment> Bulkpayment = db.BulkPayments.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Verified in Payment Office" || a.OverallStatus == "Rejected in Payment Office Submission")).ToList();
                if (Bulkpayment != null)
                {
                    foreach (var item in Bulkpayment)
                    {
                        item.OverallStatus = "Approved in Payment Office";
                        item.PaymentOfficeStatus = "Approved in Payment Office";
                        item.RejectedReason = "";
                        item.PaymentOfficeApprovedBy = User.Identity.Name;
                        item.PaymentOfficeApprovedAt = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            db.SaveChanges();
            response = "Success";
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Submission(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                if (response == "Success")
                {
                    ViewBag.Message = "Success";
                }
                else
                {
                    ViewBag.Message = "Failed";
                }
            }
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.paymentOfficeId == userPaystation.Institution.PaymentOfficeId
            && (a.OverallStatus == "Approved in Payment Office"
            || a.OverallStatus == "Slip Uploaded in Payment Office"
            || a.OverallStatus.ToUpper() == "REJECTED")).ToList();

            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus,
                    SubBudgetClass = item.SubBudgetClass,
                    PayerBankAccount = item.PayerBankAccount,
                    SourceAccountNo = item.SourceAccountNo
                };
                data.Add(vm);
            }
            return View(data);
        }
        public ActionResult SubmissionDetails(int? id)
        {
            var Bulkpayment = db.BulkPayments.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Approved in Payment Office" || a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus.ToUpper() == "REJECTED")).ToList();
            ViewBag.Batch = db.PaymentBatches.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Approved in Payment Office" || a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus.ToUpper() == "REJECTED")).FirstOrDefault();
            return View(Bulkpayment);
        }


        [HttpPost]
        public JsonResult Submission(int id)
        {
            string response = "";

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            try
            {
                PaymentBatch model = db.PaymentBatches.Where(a => a.PaymentBatchID == id
                && a.OverallStatus == "Slip Uploaded in Payment Office").FirstOrDefault();
                if (model != null)
                {
                    model.OverallStatus = "Sent To BoT";
                    model.PaymentOfficeStatus = "Sent To BoT";
                    model.PaymentOfficeApprovedAt = DateTime.Now;
                    model.PaymentOfficeApprovedBy = User.Identity.Name;
                }

                List<BulkPayment> Bulkpayment = db.BulkPayments.Where(a => a.PaymentBatchID == id
                && a.OverallStatus == "Slip Uploaded in Payment Office").ToList();
                if (Bulkpayment != null)
                {
                    foreach (var item in Bulkpayment)
                    {
                        item.OverallStatus = "Sent To BoT";
                        item.PaymentOfficeStatus = "Sent To BoT";
                        item.PaymentOfficeApprovedBy = User.Identity.Name;
                        item.PaymentOfficeApprovedAt = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            db.SaveChanges();
            response = "Success";
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PaymentSlip(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PaymentBatch paymentBatch = db.PaymentBatches.Find(id);

            if (paymentBatch == null)
            {
                return HttpNotFound();
            }
            InstitutionSubLevel userPaystation = serviceManager
              .GetUserPayStation(User.Identity.GetUserId());
            if (paymentBatch.paymentOfficeId != 0) {

                var paymentOfficeName = db.PaymentOfficeSetups
                    .Where(a => a.PaymentOfficeId == paymentBatch.paymentOfficeId && a.Status == "Active")
                    .Select(a => a.PaymentOfficeDescription).FirstOrDefault();

                ViewBag.PaymentOfficeName = paymentOfficeName;
            }
            else
            {
                ViewBag.PaymentOfficeName = paymentBatch.InstitutionName;
            }
            
            DateTime paymentOfficeApprovedAt = (DateTime)paymentBatch.PaymentOfficeApprovedAt;
            ViewBag.Date = paymentOfficeApprovedAt.ToString("dd/MM/yyyy hh:mm");
            //ViewBag.Date = DateTime.Now.ToString("dd/MM/yyyy hh:mm");

            return new Rotativa.PartialViewAsPdf("_Paymentslip", paymentBatch)

            {
                FileName = "Slip" + paymentBatch.PaymentBatchID + ".pdf",
                CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
            };
        }


        [HttpPost]
        public ActionResult AttachSlipinPo(PaymentBatchVM paymentBatchVM)
        {
            string response = "";
            string fileExtension = "";
            string filename = "";
            string fileLocation = "";

            try
            {
                PaymentBatch model = db.PaymentBatches.Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID && (a.OverallStatus == "Approved in Payment Office" || a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus == "REJECTED")).FirstOrDefault();
                List<BulkPayment> data = db.BulkPayments.Where(a => a.PaymentBatchID == paymentBatchVM.PaymentBatchID && (a.OverallStatus == "Approved in Payment Office" || a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus == "REJECTED")).ToList();

                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);
                filename = System.IO.Path.GetFileName(Request.Files["FileName"].FileName);
                filename = model.PVNo + DateTime.Now.ToString("_yyyyMMddHHmmss") + ".pdf";

                if (Request.Files["FileName"].ContentLength <= 0 || Request.Files["FileName"].ContentLength > 2000000)
                {
                    response = "Size";
                }
                else
                {
                    if (fileExtension != ".pdf")
                    {
                        response = "Format";
                    }
                    else
                    {
                        fileLocation = Server.MapPath("~/Media/Payments/") + filename;
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                        Request.Files["FileName"].SaveAs(fileLocation);

                        if (model != null)
                        {
                            model.UploadStatusInPaymentOffice = "Slip Uploaded in Payment Office";
                            model.OverallStatus = "Slip Uploaded in Payment Office";
                            model.PaymentOfficeStatus = "Slip Uploaded in Payment Office";
                            model.RejectedReason = "";
                            model.FileName2 = filename;
                            model.PaymentOfficeUploadedBy = User.Identity.Name;
                            model.PaymentOfficeUploadedAt = DateTime.Now;
                        }

                        if (data != null)
                        {
                            foreach (var item in data)
                            {
                                item.OverallStatus = "Slip Uploaded in Payment Office";
                                item.PaymentOfficeStatus = "Slip Uploaded in Payment Office";
                                item.RejectedReason = "";
                            }
                        }

                        db.SaveChanges();
                        response = "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                response = "Fail";
            }
            return RedirectToAction("Submission", new { response = response });
        }

        public ActionResult DownloadPaymentSlipinPo(int id)
        {
            var fileName = db.PaymentBatches.Find(id).FileName2;

            return File("~/Media/Payments/" + fileName, "application/pdf", Server.UrlEncode(fileName));
        }

        [HttpGet]
        public ActionResult SentToBoTList()
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var paymentbatch = db.PaymentBatches.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && (a.OverallStatus == "Sent To BoT")).ToList();
            List<PaymentBatchVM> data = new List<PaymentBatchVM>();
            foreach (var item in paymentbatch)
            {
                var vm = new PaymentBatchVM
                {
                    PaymentBatchID = item.PaymentBatchID,
                    InstitutionId = item.InstitutionId,
                    InstitutionCode = item.InstitutionCode,
                    BatchNo = item.BatchNo,
                    BatchDesc = item.BatchDesc,
                    PaymentCategory = item.PaymentCategory,
                    NoTrx = item.NoTrx,
                    TotalAmount = item.TotalAmount,
                    OverallStatus = item.OverallStatus
                };
                data.Add(vm);
            }
            return View(data);

        }

        public ActionResult PaymentStatus()
        {

            return View();
        }

        public JsonResult GetPaymentFile(string OverallStatus)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<PaymentBatch> paymentFileList = db.PaymentBatches
                .Where(a => a.OverallStatus == OverallStatus

                  && a.InstitutionCode == userPaystation.InstitutionCode)
                .OrderByDescending(a => a.PaymentBatchID)
                .ToList();

            return Json(new { data = paymentFileList }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Payment Office Submission")]
        public ActionResult SendtoBoT(int? id)
        {
            var schemaPath = "";
            var receiverBic = "";
            var receiverUrl = "";
            string response = "Success";
            string certPass = "";
            string certStorePath = "";
            var clientCertStorePath = "";
            var clientCertPass = "";

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            try
            {
                db.Database.CommandTimeout = 1200;
                PaymentBatch PaymentBatchFile = db.PaymentBatches.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus.ToUpper() == "REJECTED")).FirstOrDefault();
                List<BulkPayment> BulkyPaymentList = db.BulkPayments.Where(a => a.PaymentBatchID == id && (a.OverallStatus == "Slip Uploaded in Payment Office" || a.OverallStatus.ToUpper() == "REJECTED")).ToList();

                //if (Properties.Settings.Default.HostingEnvironment != "Live")
                //{
                //    if (PaymentBatchFile != null)
                //    {
                //        PaymentBatchFile.OverallStatus = "PROCESSED";
                //        PaymentBatchFile.OverallStatusDescription = "Processed Succesfully";
                //        PaymentBatchFile.SentToBotBy = User.Identity.Name;
                //        PaymentBatchFile.SentToBotAt = DateTime.Now;
                //        PaymentBatchFile.NumSubmissions = PaymentBatchFile.NumSubmissions + 1;
                //    }

                //    if (BulkyPaymentList != null)
                //    {
                //        foreach (var item in BulkyPaymentList)
                //        {
                //            item.OverallStatus = "PROCESSED";
                //            item.SettledDescription = "Processed Succesfully";
                //            item.ApprovedBy = User.Identity.Name;
                //            item.ApprovedAt = DateTime.Now;
                //        }
                //    }

                //    db.SaveChanges();

                //    //if (PaymentBatchFile.BulkPaymentMethod == "Same Account" && PaymentBatchFile.NumSubmissions == 1) //If first time submission. Post to GL
                //    //{
                //    //    string journalTypeCode = "PDB";
                //    //    var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalTypeCode) };
                //    //    db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);

                //    //}

                //    if (PaymentBatchFile.NumSubmissions == 1)
                //    {
                //        var parameters = new SqlParameter[] {
                //           new SqlParameter("@PaymentNo",  PaymentBatchFile.BatchNo),};
                //        var gLProcessStatusVM = db.Database.SqlQuery<GLProcessStatusVM>("dbo.GlPostPaymentBatch_p @PaymentNo", parameters).FirstOrDefault();
                //    }

                //    return Content(response);
                //}


                var apiClient = db.ApiClients
                    .Where(a => a.ClientId == PaymentBatchFile.PayerBIC && a.MessageType == "Payment")
                    .FirstOrDefault();

                if (apiClient == null)
                {
                    response = "Api client is not Found!";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                clientCertStorePath = apiClient.ClientPublicKey;
                clientCertPass = apiClient.ClientPassword;
                receiverUrl = apiClient.ClientUrl;
                receiverBic = apiClient.ClientId;

                if (receiverBic != "TANZTZTX")
                {
                    var data = GetData(BulkyPaymentList, PaymentBatchFile, receiverBic);

                    XDocument xmlData = XDocument.Parse(data);

                    schemaPath = db.SystemConfigs.Where(a => a.ConfigName == "XMLSchemaPath").Select(a => a.ConfigValue).FirstOrDefault();
                    schemaPath = schemaPath + "schema_block_payment.xsd";

                    //var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
                    //if (!isSchemaValid.ValidationStatus)
                    //{
                    //    response = isSchemaValid.ValidationDesc;
                    //    return Content(response);
                    //}

                    certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
                    certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;
                    var hashSignature = DigitalSignature.GenerateSignature(data, certPass, certStorePath);
                    
                    var signedData = data + "|" + hashSignature;

                    //log request
                    Log.Information(signedData + "{Name}!", "OutgoingMessages");

                    PaymentBatchFile.XmlData = signedData;
                    if (signedData.Length > 65000)
                        PaymentBatchFile.XmlData = signedData.Substring(1, 65000);

                    db.SaveChanges();

                    HttpWebResponse httpResponse = serviceManager.SendToCommercialBank(signedData, receiverUrl);

                    if (httpResponse == null)
                    {
                        response = "Error on getting response from remote server. Contact system support";
                        return Content(response);
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader sr = new StreamReader(httpResponse.GetResponseStream());
                        var xmlString = @sr.ReadToEnd().Trim().ToString();
                        //log response
                        Log.Information(xmlString + "{Name}!", "IncomingResponses");
                        var dataArray = xmlString.Split('|');
                        var dataPart = dataArray[0];
                        var dataSignature = dataArray[1];

                        XDocument xDocResponse = XDocument.Parse(dataPart, LoadOptions.None);
                        // Start of temporary check to simulate NMB issues
                        if (Properties.Settings.Default.HostingEnvironment != "Live")
                        {
                            xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                               new XElement("Document",
                                  new XElement("Header",
                                     new XElement("Sender", "NMIBTZTZ"),
                                     new XElement("Receiver", "MOFPTZTZ"),
                                     new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                                     new XElement("PaymentType", "P108"),
                                     new XElement("MessageType", "RESPONSE")),
                                  new XElement("ResponseSummary",
                                     new XElement("OrgMsgId", PaymentBatchFile.MsgID),
                                     new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                                  new XElement("ResponseDetails",
                                     new XElement("PaymentRef", "NA"),
                                     new XElement("RespStatus", "ACCEPTED"),
                                     new XElement("Description", "Accepted Successfully")))));

                            XmlWriterSettings settings = new XmlWriterSettings();
                            settings.OmitXmlDeclaration = true;
                            StringWriter sw = new StringWriter();
                            using (XmlWriter xw = XmlWriter.Create(sw, settings))
                            // or to write to a file...
                            //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
                            {
                                xDocResponse.Save(xw);
                            }
                        }
                        // End of temporary check to simulate NMB issues

                        //check schema
                        //schemaPath = db.SystemConfigs
                        //    .Where(a => a.ConfigName == "XMLSchemaPath")
                        //    .Select(a => a.ConfigValue)
                        //    .FirstOrDefault();

                        //schemaPath = schemaPath + "schema_block_response.xsd";
                        //isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
                        //if (!isSchemaValid.ValidationStatus)
                        //{
                        //    return Content("File submission failed," + isSchemaValid.ValidationDesc);
                        //}

                        //validate signature
                        var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
                        if (!isSignatureValid)
                        {
                            return Content("File submission failed, response signature is invalid");
                        }
                        response = SaveResponse(xDocResponse, PaymentBatchFile, BulkyPaymentList);

                        return Content(response);
                    }
                }
                //Special filter for payment offices 
                int userPaymentOfficeId = db.Institution.Find(userPaystation.InstitutionId).PaymentOfficeId;
                var paymentOfficeStatus = db.PaymentOfficeSubmissionStatus
                                        .Where(a => a.PaymentOfficeId == userPaymentOfficeId)
                                        .Where(a => a.Status == "Active").Any();

                if (!paymentOfficeStatus)
                {
                    response = "Error sending file to BOT. Plese contact System Administrator!";
                    return Content(response);
                }
                ProcessResponse sendToBotStatus = serviceManager.CreateBulkPaymentEFT(BulkyPaymentList, PaymentBatchFile);
                if (sendToBotStatus.OverallStatus != "Success")
                {
                    response = sendToBotStatus.OverallStatusDescription;
                    return Content(response);
                }

                if (sendToBotStatus.StrReturnId != PaymentBatchFile.MsgID)
                {
                    PaymentBatchFile.MsgID = sendToBotStatus.StrReturnId;
                }

                //if (PaymentBatchFile.BulkPaymentMethod == "Same Account" && PaymentBatchFile.OverallStatus.ToUpper() != "REJECTED")
                //{
                //    string journalTypeCode = "PDB";
                //    var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalTypeCode) };
                //    db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);
                //}

                if (PaymentBatchFile.OverallStatus.ToUpper() != "REJECTED")
                {
                    var parameters = new SqlParameter[] {
                           new SqlParameter("@PaymentNo",  PaymentBatchFile.BatchNo),};
                    var glProcessStatusvm = db.Database.SqlQuery<GLProcessStatusVM>("dbo.GlPostPaymentBatch_p @PaymentNo", parameters).FirstOrDefault();
                }


                if (PaymentBatchFile != null)
                {
                    PaymentBatchFile.OverallStatus = "Sent to BoT";
                    PaymentBatchFile.OverallStatusDescription = "Sent to BoT Successfully";
                    PaymentBatchFile.SentToBotBy = User.Identity.Name;
                    PaymentBatchFile.SentToBotAt = DateTime.Now;
                    PaymentBatchFile.NumSubmissions = PaymentBatchFile.NumSubmissions + 1;
                }

                if (BulkyPaymentList != null)
                {
                    foreach (var item in BulkyPaymentList)
                    {
                        item.OverallStatus = "Sent to BoT";
                        item.SettledDescription = "Sent to BoT Successfully";
                        item.ApprovedBy = User.Identity.Name;
                        item.ApprovedAt = DateTime.Now;
                    }
                }
                db.SaveChanges();

                string strBoTFileUrl = serviceManager.GetSystemConfig("BoTFileUrl");
                string slipfilePath = Server.MapPath("~/Media/Payments/") + PaymentBatchFile.FileName2;
                string sendFileStatus = "OK";
                if (Properties.Settings.Default.HostingEnvironment == "Live")
                {
                    sendFileStatus = ServiceManager.sendBotPaymentSlip(slipfilePath, PaymentBatchFile.MsgID, strBoTFileUrl);
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Content(response);
        }

        private string GetData(List<BulkPayment> BulkyPaymentList, PaymentBatch PaymentBatchFile, string receiverBic)
        {
            var xml_text = new StringBuilder();
            ProcessResponse eftStatus = new ProcessResponse();
            eftStatus.OverallStatus = "Pending";
            string xml_line = "";
            try
            {
                int NbOfTxs = (int)PaymentBatchFile.NoTrx;
                decimal TotalAmount = (decimal)PaymentBatchFile.TotalAmount;
                //MessageId
                int financialyear = serviceManager.GetFinancialYear(DateTime.Now);
                //string MsgId = PaymentBatchFile.MsgID;
                //eftStatus.StrReturnId = MsgId;

                string MsgIdMask = Properties.Settings.Default.MsgIdMask.Replace("MUS", "MUB");
                string MsgId = MsgIdMask + financialyear.ToString().Substring(2, 2) + PaymentBatchFile.NumRejections.ToString().PadLeft(2, '0') + PaymentBatchFile.PaymentBatchID.ToString().PadLeft(7, '0');
                PaymentBatchFile.MsgID = MsgId;
                eftStatus.StrReturnId = MsgId;


                xml_line = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>";
                xml_line += "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_block_payment.xsd\" >";
                xml_line += "<Header>";

                xml_line += "<Sender>MOFPTZTZ</Sender>";
                xml_line += "<Receiver>" + receiverBic + "</Receiver>";

                xml_line += "<MsgId>" + MsgId + "</MsgId>";
                xml_line += "<PaymentType>" + "P120" + "</PaymentType>";
                xml_line += "<MessageType>Payment</MessageType>";
                xml_line += "</Header>";

                /*** Begin Payment Block ****/
                xml_line += "<BlockPayment>";

                xml_line += "<MsgSummary>";
                xml_line += "<TransferRef>" + PaymentBatchFile.PrefundingRef + "</TransferRef>";
                xml_line += "<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</CreDtTm>";
                xml_line += "<NbOfTxs>" + NbOfTxs.ToString() + "</NbOfTxs>";
                xml_line += "<Currency>TZS</Currency>";
                xml_line += "<TotalAmount>" + TotalAmount.ToString("0.00") + "</TotalAmount>";
                xml_line += "<PayerName>" + PaymentBatchFile.PayerBankName.Replace("+", "").Replace("'", "").Replace("-", "").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "") + "</PayerName>";
                xml_line += "<PayerAcct>" + PaymentBatchFile.PayerBankAccount + "</PayerAcct>";
                xml_line += "<RegionCode>" + "TZDO" + "</RegionCode>";
                xml_line += "</MsgSummary>";


                xml_text.Append(xml_line);
                string priority = "0";
                string endToEndId = "";
                string disbNum = "";
                foreach (var bulkpayment in BulkyPaymentList)
                {
                    //endToEndId = PaymentBatchFile.InstitutionCode + "E" + bulkpayment.BulkPaymentID.ToString().PadLeft(6, '0');
                    endToEndId = bulkpayment.InstitutionId.ToString() + "E" + bulkpayment.BulkPaymentID.ToString().PadLeft(11, '0');
                    disbNum = bulkpayment.BulkPaymentID.ToString().PadLeft(6, '0');
                    xml_line = "<TrxRecord>";
                    xml_line += "<Priority>" + priority + "</Priority>";
                    if (bulkpayment.BeneficiaryCode != null && bulkpayment.BeneficiaryCode != "")
                        xml_line += "<VendorNo>" + bulkpayment.BeneficiaryCode.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "").Replace(" ", "").Replace("-", "") + "</VendorNo>";
                    xml_line += "<EndToEndId>" + endToEndId + "</EndToEndId>";
                    xml_line += "<TrxAmount>" + ((decimal)bulkpayment.Amount).ToString("0.00") + "</TrxAmount>";
                    xml_line += "<BenName>" + bulkpayment.BeneficiaryName.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenName>";
                    xml_line += "<BenAcct>" + bulkpayment.BeneficiaryAccountNo.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenAcct>";
                    xml_line += "<BenBic>" + bulkpayment.BankBic.Replace("/", "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace(",", "") + "</BenBic>";
                    xml_line += "<Description>" + bulkpayment.PaymentDescription.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</Description>";
                    xml_line += "<DisbNum>" + disbNum + "</DisbNum>";
                    xml_line += "<UnappliedAccount>" + PaymentBatchFile.UnappliedAccount + "</UnappliedAccount>";
                    xml_line += "<DetailsOfCharges>" + "SHA" + "</DetailsOfCharges>";

                    xml_line += "</TrxRecord>";
                    xml_text.Append(xml_line);

                }
                xml_line = "</BlockPayment>";
                /**** End of Payment Block ****/
                xml_text.Append(xml_line);
                xml_line = "</Document>";
                xml_text.Append(xml_line);


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return xml_text.ToString();
        }


        public string SaveResponse(XDocument xDoc, PaymentBatch PaymentBatchFile, List<BulkPayment> BulkyPaymentList)
        {
            var sender = "";
            var receiver = "";
            var msgId = "";
            var paymentType = "";
            var messageType = "";
            var orgMsgId = "";
            var creDtTm = "";
            var paymentRef = "";
            var respStatus = "";
            var desc = "";

            try
            {
                var header = (from u in xDoc.Descendants("Header")
                              select new
                              {
                                  Sender = (string)u.Element("Sender"),
                                  Receiver = (string)u.Element("Receiver"),
                                  MsgId = (string)u.Element("MsgId"),
                                  PaymentType = (string)u.Element("PaymentType"),
                                  MessageType = (string)u.Element("MessageType"),
                              }).FirstOrDefault();

                if (header != null)
                {
                    sender = header.Sender;
                    receiver = header.Receiver;
                    msgId = header.MsgId;
                    paymentType = header.PaymentType;
                    messageType = header.MessageType;
                }

                var responseSummary = (from u in xDoc.Descendants("ResponseSummary")
                                       select new
                                       {
                                           OrgMsgId = (string)u.Element("OrgMsgId"),
                                           CreDtTm = (string)u.Element("CreDtTm")
                                       }).FirstOrDefault();

                if (responseSummary != null)
                {
                    orgMsgId = responseSummary.OrgMsgId;
                    creDtTm = responseSummary.CreDtTm;
                }

                var responseDetails = (from u in xDoc.Descendants("ResponseDetails")
                                       select new
                                       {
                                           PaymentRef = (string)u.Element("PaymentRef"),
                                           RespStatus = (string)u.Element("RespStatus"),
                                           Description = (string)u.Element("Description")
                                       }).FirstOrDefault();

                if (responseDetails != null)
                {
                    paymentRef = responseDetails.PaymentRef;
                    respStatus = responseDetails.RespStatus;
                    desc = responseDetails.Description;
                }

                var incomingMessage = new IncomingMessage
                {
                    MsgID = header.MsgId,
                    PaymentRef = paymentRef,
                    PaymentType = header.PaymentType,
                    messageProcessStatus = "ACCEPTED",
                    messageProcessStatusDescription = "Valid XML",
                    MessageType = header.MessageType,
                    OriginalMsgID = orgMsgId,
                    MessageTimeStamp = DateTime.Now,
                    DatabaseUpdateStatus = "Pending",
                    BotResponse = respStatus,
                    BotResponseDescription = desc,
                    PaymentUpdateStatus = "Pending",
                    XmlContent = xDoc.ToString()
                };

                db.IncomingMessages.Add(incomingMessage);

                if (PaymentBatchFile != null)
                {
                    PaymentBatchFile.OverallStatus = respStatus;
                    PaymentBatchFile.OverallStatusDescription = desc;
                    PaymentBatchFile.SentToBotAt = DateTime.Now;
                    PaymentBatchFile.SentToBotBy = User.Identity.Name;
                    PaymentBatchFile.NumSubmissions += 1;

                    if (respStatus.ToUpper() == "ACCEPTED")
                    {
                        List<BulkPayment> BulkpaymentList2 = db.BulkPayments.Where(ps => ps.PaymentBatchID == PaymentBatchFile.PaymentBatchID && ps.OverallStatus != "Cancelled").ToList();
                        foreach (var item in BulkpaymentList2)
                        {
                            item.OverallStatus = respStatus;
                            item.SentToBotAt = DateTime.Now;
                            item.SentToBotBy = User.Identity.Name;
                        }
                        PaymentBatch PaymentBatchFilelist = db.PaymentBatches.Where(a => a.PaymentBatchID == PaymentBatchFile.PaymentBatchID).FirstOrDefault();
                        if (PaymentBatchFilelist != null)
                        {
                            PaymentBatchFilelist.OverallStatus = respStatus;
                            PaymentBatchFilelist.SentToBotBy = User.Identity.Name;
                            PaymentBatchFilelist.SentToBotAt = DateTime.Now;
                        }
                    }
                }

                db.SaveChanges();

                if (PaymentBatchFile.BulkPaymentMethod == "Same Account" && PaymentBatchFile.OverallStatus.ToUpper() != "REJECTED")

                {
                    string journalTypeCode = "PDB";
                    var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalTypeCode) };
                    db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                Log.Information(ex + "{Name}!", "SaveIncomingResponseExceptions");
                Log.Information(xDoc + "{Name}!", "IncomingResponseExceptions");
            }

            return "Success";
        }


        public ActionResult RejectPaymentOfficeVerifier(int ids, string reason)
        {
            string response = "";
            try
            {
                if (reason == "") { response = "emptyreason"; }
                else
                {

                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                    && (a.OverallStatus == "Sent to Payment Office"
                    || a.OverallStatus == "Rejected in Payment Office Approval")).FirstOrDefault();

                    batch.OverallStatus = "Rejected in Payment Office";
                    batch.RejectedReason = reason;
                    batch.RejectedBy = User.Identity.GetUserName();
                    batch.RejectedAt = DateTime.Now;

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Sent to Payment Office"
                                       || a.OverallStatus == "Rejected in Payment Office Approval"
                                       )
                                       select a).ToList();

                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Rejected in Payment Office";
                        item.RejectedReason = reason;
                        item.RejectedBy = User.Identity.GetUserName();
                        item.RejectedAt = DateTime.Now;
                    }
                    db.SaveChanges();
                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectPaymentOfficeAproval(int ids, string reason)
        {
            string response = "";
            try
            {
                if (reason == "") { response = "emptyreason"; }
                else
                {
                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                    && (a.OverallStatus == "Verified in Payment Office"
                    || a.OverallStatus == "Approved in Payment Office"
                    || a.OverallStatus == "Rejected in Payment Office Submission")).FirstOrDefault();

                    batch.OverallStatus = "Rejected in Payment Office Approval";
                    batch.RejectedReason = reason;
                    batch.RejectedBy = User.Identity.GetUserName();
                    batch.RejectedAt = DateTime.Now;

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Verified in Payment Office"
                                       || a.OverallStatus == "Approved in Payment Office"
                                       || a.OverallStatus == "Rejected in Payment Office Submission")
                                       select a).ToList();

                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Rejected in Payment Office Approval";
                        item.RejectedReason = reason;
                        item.RejectedBy = User.Identity.GetUserName();
                        item.RejectedAt = DateTime.Now;
                    }
                    db.SaveChanges();
                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public ActionResult RejectPaymentOfficeSubmission(int ids, string reason)
        {
            string response = "";
            try
            {
                if (reason == "") { response = "emptyreason"; }
                else
                {
                    PaymentBatch batch = db.PaymentBatches.Where(a => a.PaymentBatchID == ids
                && (a.OverallStatus == "Verified in Payment Office"
                || a.OverallStatus == "Approved in Payment Office"
                || a.OverallStatus.ToUpper() == "REJECTED"
                || a.OverallStatus == "Slip Uploaded in Payment Office")).FirstOrDefault();

                    batch.OverallStatus = "Rejected in Payment Office Submission";
                    batch.PaymentOfficeStatus = "Rejected in Payment Office Submission";
                    batch.RejectedReason = reason;
                    batch.RejectedBy = User.Identity.GetUserName();
                    batch.RejectedAt = DateTime.Now;

                    var bulkpayment = (from a in db.BulkPayments
                                       where a.PaymentBatchID == ids
                                       where (a.OverallStatus == "Verified in Payment Office"
                                       || a.OverallStatus == "Approved in Payment Office"
                                       || a.OverallStatus.ToUpper() == "REJECTED"
                                       || a.OverallStatus == "Slip Uploaded in Payment Office")
                                       select a).ToList();

                    foreach (var item in bulkpayment)
                    {
                        item.OverallStatus = "Rejected in Payment Office Submission";
                        item.PaymentOfficeStatus = "Rejected in Payment Office Submission";
                        item.RejectedReason = reason;
                        item.RejectedBy = User.Identity.GetUserName();
                        item.RejectedAt = DateTime.Now;
                    }
                    db.SaveChanges();
                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = "DbException" + ex;
                response = ex.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        ////</ End Payment Office>

        //public ActionResult GetBulkPaymentMethod()
        // {
        //     return View();
        // }


        public JsonResult GetBulkPaymentMethod(string subBudgetClass)
        {
            string method;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var payerbank = db.InstitutionAccounts
              .Where(a => a.SubBudgetClass == subBudgetClass
              && a.InstitutionCode == userPaystation.InstitutionCode
              && a.IsTSA == false
              ).FirstOrDefault();

            if (payerbank.AccountNumber == "9921180001")
            {
                method = "Different Account";
                ViewBag.PaymentBatchMethod = db.PaymentBatchMethods
                .Where(a => a.Method == "Different Account")
                .ToList();
            }
            else
            {
                method = "Same Account";
                ViewBag.PaymentBatchMethod = db.PaymentBatchMethods
                .Where(a => a.Method == "Same Account")
                .ToList();
            }
            return Json(new { method }, JsonRequestBehavior.AllowGet);
            //return Json(new { success = true, method = method });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
