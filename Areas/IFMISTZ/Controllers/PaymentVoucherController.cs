using Audit.Mvc;
using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Extensions;
using IFMIS.Libraries;
using IFMIS.Services;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class PaymentVoucherController : Controller
    {
        private readonly IFMISTZDbContext db;
        private readonly IFundBalanceServices fundBalanceServices;
        private readonly IManageRetentionServices manageRetentionServices;
        private readonly IServiceManager serviceManager;
        public PaymentVoucherController()
        {

        }

        public PaymentVoucherController(
            IFundBalanceServices fundBalanceServices,
            IManageRetentionServices manageRetentionServices,
            IServiceManager serviceManager,
            IFMISTZDbContext db
            )
        {
            this.fundBalanceServices = fundBalanceServices;
            this.manageRetentionServices = manageRetentionServices;
            this.serviceManager = serviceManager;
            this.db = db;
        }

        [HttpGet, Authorize(Roles = "Voucher Entry")]
        public ActionResult List() { return View(); }

        [HttpGet, Authorize(Roles = "Voucher Entry")]
        public ActionResult PendingVoucher()
        { 
            return View();
        }
        [HttpGet, Authorize(Roles = "Voucher Entry")]
        public ActionResult PendingAccrualVoucher()
        {
            return View();
        }

        public ActionResult SimpleVoucherEdit(int id = 0)
        {
            return View();
        }

        public JsonResult GetPaymentVoucher(string overallStatus = "Pending", int paymentSummaryId = 0)
        {
            db.Database.CommandTimeout = 230000;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<PaymentVoucher> paymentVoucherList = null;
            if (overallStatus == "Generated")
            {
                if (paymentSummaryId != 0)
                {
                    PaymentSummary paymentSummary = db.PaymentSummaries.Find(paymentSummaryId);
                    if (paymentSummary != null)
                    {
                        paymentVoucherList = db.PaymentVouchers
                            .Where(a => a.OverallStatus == overallStatus
                             && a.PaymentSummaryId == paymentSummaryId
                            )
                            .OrderByDescending(a => a.PaymentVoucherId)
                            .AsNoTracking().ToList();
                    }
                }
            }
            else
            {
                if (overallStatus == "Pending")
                {
                    List<InstitutionSubLevel> allUserPaystations = serviceManager.GetAllUserPayStations(User.Identity.GetUserId());
                    List<string> userPaystationList = allUserPaystations.Select(a => a.SubLevelCode).ToList();
                    paymentVoucherList = db.PaymentVouchers
                        .Where(a => 
                        (a.OverallStatus == "Pending"
                         || a.OverallStatus == "Rejected By Examiner"
                         || (a.ExaminedBy == "NA" && a.OverallStatus == "Rejected"))
                         && a.InstitutionCode == userPaystation.InstitutionCode
                         && userPaystationList.Contains(a.SubLevelCode)
                         && a.ReversalFlag == false)
                        .OrderByDescending(a => a.PaymentVoucherId)
                        .AsNoTracking().ToList();   
                }
                else if (overallStatus == "Confirmed")
                {
                    paymentVoucherList = db.PaymentVouchers
                       .Where(a => (a.OverallStatus == "Confirmed"
                        || a.OverallStatus == "Rejected"
                        ) && a.ExaminedBy != "NA"
                        && a.InstitutionCode == userPaystation.InstitutionCode
                        && a.ReversalFlag == false)
                       .OrderByDescending(a => a.PaymentVoucherId)
                       .AsNoTracking().ToList();
                }
                else
                {
                    if (overallStatus == "Examined")
                    {
                        paymentVoucherList = db.PaymentVouchers
                              .Where(a => (a.OverallStatus == overallStatus
                                 || a.OverallStatus == "Rejected in Voucher Generation"
                                 || (a.ExaminedBy == "NA" && a.OverallStatus == "Confirmed"))
                                 && a.InstitutionCode == userPaystation.InstitutionCode
                                 && a.ReversalFlag == false)
                              .OrderByDescending(a => a.PaymentVoucherId)
                              .AsNoTracking().ToList();
                    }
                    else if (overallStatus == "Cheque")
                    {
                        paymentVoucherList = db.PaymentVouchers
                        .Where(a => a.PaymentSummaryId == paymentSummaryId
                         && a.OverallStatus != "Cancelled")
                        .OrderByDescending(a => a.PaymentVoucherId)
                        .AsNoTracking().ToList();
                    }
                    else
                    {
                        paymentVoucherList = db.PaymentVouchers
                        .Where(a => a.OverallStatus == overallStatus
                         && a.InstitutionCode == userPaystation.InstitutionCode
                         && a.ReversalFlag == false)
                        .OrderByDescending(a => a.PaymentVoucherId)
                        .AsNoTracking().ToList();
                    }
                }
            }
            var response = Json(new { data = paymentVoucherList.Where(a => a.OverallStatus != "Cancelled").ToList() }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
        }

        public JsonResult GetPaymentAccrualVoucher()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var data = db.PaymentVouchers
                .Where(a =>
                  a.OverallStatus == "Approved - Waiting for Payment"
                  && a.InstitutionCode == userPaystation.InstitutionCode
                  && a.ReversalFlag == false)
               .OrderByDescending(a => a.PaymentVoucherId)
               .ToList();
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPaymentVoucherGenerate(string subBudgetClass = "UNKNOWN")
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<PaymentVoucher> paymentVoucherList = null;
            if (subBudgetClass == "UNKNOWN")
            {
                paymentVoucherList = db.PaymentVouchers
                                     .Where(a => a.OverallStatus == "Approved"
                                      && a.InstitutionCode == userPaystation.InstitutionCode
                                      && a.ReversalFlag == false)
                                     .OrderByDescending(a => a.PaymentVoucherId)
                                     .AsNoTracking()
                                     .ToList();
            }
            else
            {
                paymentVoucherList = db.PaymentVouchers
                                     .Where(a => a.OverallStatus == "Approved"
                                      && a.InstitutionCode == userPaystation.InstitutionCode
                                      && a.SubBudgetClass == subBudgetClass
                                      && a.ReversalFlag == false)
                                     .OrderByDescending(a => a.PaymentVoucherId)
                                     .AsNoTracking()
                                     .ToList();
            }

            var response = Json(new { data = paymentVoucherList }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
        }

        // public JsonResult GetCashBookBalance(
        //     string accountNo,
        //     string SBC
        //     )
        // {
        //     CashBalanceView data = new CashBalanceView();
        //     string status = "Success";
        //     try
        //     {
        //         db.Database.CommandTimeout = 1200;
        //         InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(db, User.Identity.GetUserId());
        //         int financialYear = serviceManager.GetFinancialYear(db, DateTime.Now);
        //         data = db.CashBalanceViews
        //          .Where(a => a.BankAccountNumber == accountNo
        //           && a.SubBudgetClass == SBC
        //           && a.InstitutionCode == userPaystation.InstitutionCode
        //           && a.FinancialYear == financialYear)
        //          .FirstOrDefault();

        //         if (data == null)
        //         {
        //             data = new CashBalanceView
        //             {
        //                 CashBookBalance = 0,
        //                 CommittedAmount = 0,
        //                 AvailableBalance = 0
        //             };
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         status = ex.Message;
        //     }

        //     return Json(new { data }, JsonRequestBehavior.AllowGet);
        // }
        public JsonResult GetCashBookBalance(
            string accountNo,
            string SBC
            )
        {
            CashBalanceView data = new CashBalanceView
            {
                CashBookBalance = 0,
                CommittedAmount = 0,
                BankAccountNumber = accountNo,
                SubBudgetClass = SBC,
                AvailableBalance = 0
            };
            try
            {
                db.Database.CommandTimeout = 1200;
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                int financialYear = serviceManager.GetFinancialYear(DateTime.Now);

                BalanceResponse fundBalanceResponse = serviceManager.GetCashBookBalance(accountNo, SBC, userPaystation.InstitutionCode, financialYear.ToString());

                if (fundBalanceResponse.overallStatus == "Error")
                {
                    return Json(new { data, status = fundBalanceResponse.overallStatusDescription }, JsonRequestBehavior.AllowGet);
                }
                if (fundBalanceResponse.CashBalanceViewList == null)
                {
                    return Json(new { data, status = "Success" }, JsonRequestBehavior.AllowGet);
                }
                var _data = fundBalanceResponse.CashBalanceViewList.FirstOrDefault();
                if (_data != null)
                {
                    data = _data;
                }
            }
            catch (Exception ex)
            {
                return Json(new { data, status = "Cash Book Balance: Execution Timeout Expired :" + ex.Message.ToString() }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data, status = "Success" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPaymentVoucher2(int paymentSummaryId)
        {
            List<PaymentVoucher> list = db.PaymentVouchers
              .Where(a => a.PaymentSummaryId == paymentSummaryId)
              .ToList();
            return Json(new { data = list.Where(a => a.OverallStatus != "Cancelled").ToList() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Authorize(Roles = "Voucher Entry")]
        public ActionResult CreateVoucher()
        {
            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());

            var userInst = db.Institution.Find(userPaystation.InstitutionId);
            ViewBag.isEmbassy = userInst.InstitutionCategory.Trim() == "Embassy/Commission";

            var InstPeriod = db.InstitutionPostPeriods
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && a.OverallStatus != "Cancelled")
                .FirstOrDefault();

            var subBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                  && a.SubBudgetClass != null
                  && a.SubBudgetClass != "303")
                .OrderBy(a => a.SubBudgetClass)
                .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            ViewBag.ApplyDate = InstPeriod == null ? DateTime.Now.ToString("yyyy-MM-dd") : InstPeriod.MinimumPostingDateFormatted;
            ViewBag.PayeeTypesList = db.PayeeTypes.Where(a => a.Status != "Cancelled").ToList();
            return View();
        }

        [HttpPost, Authorize(Roles = "Voucher Entry")]
        //[ValidateAntiForgeryToken]
        public ActionResult CreateVoucher(PaymentVoucherVM paymentVoucherVM)
        {
            string response = "Success";
            var paymentVoucherId = 0;
            //var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId()); Old
            var institutionId = Convert.ToInt32(User.GetInstitutionId());
            var institutionCode = User.GetInstitutionCode();
            var institutionName = User.GetInstitutionName();
            var institutionLevel = Convert.ToByte(User.GetInstitutionLevel());
            var institutionCategory = User.GetInstitutionCategory();
            var level1Code = User.GetLevel1Code();
            var subLevelCode = User.GetSubLevelCode();
            var subLevelDesc = User.GetSubLevelDesc();
            var subLevelCategory = User.GetSubLevelCategory();
            var payStationId = Convert.ToInt32(User.GetPayStationId());

            db.Database.CommandTimeout = 1200;
            try
            {
                DateTime applyDate = DateTime.Now;
                if(paymentVoucherVM.IsAccrualVoucher)
                {
                    applyDate = paymentVoucherVM.ApplyDate;
                }

                string cuttOffStatus = serviceManager.GetCutOffStatus(applyDate, institutionCode );
                if (cuttOffStatus != "success")
                {
                    return Content(cuttOffStatus);
                }
                if (serviceManager.GetAtSourceDummyControlBalance(institutionCode) > 0)
                {
                    return Content("You must post all at source dummy journals before you can capture new vouchers");
                }  

                var voucherDetailsAmount = paymentVoucherVM.voucherDetails.Sum(a => a.ExpenseAmount);

                if (voucherDetailsAmount != paymentVoucherVM.OperationalAmount)
                {
                    return Content("Total Voucher Amount: " + paymentVoucherVM.OperationalAmount
                        + " Differs with Voucher Details Amount: " + voucherDetailsAmount);
                }

                foreach (var vd in paymentVoucherVM.voucherDetails)   
                {
                    if (vd.FundingReference == null || vd.FundingReference == "")
                    {
                        return Content("Funding Reference is required for gl Item(" + vd.ExpenditureLineItem + ")" +
                            "Please contact system admin.");
                    }
                }

                var payerBank = db.InstitutionAccounts
                    .Where(a => a.SubBudgetClass == paymentVoucherVM.SubBudgetClass
                      && a.InstitutionCode == institutionCode
                      && a.IsTSA == false
                      && a.OverallStatus != "Cancelled"
                    ).FirstOrDefault();
                if (payerBank == null)
                {
                    response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentVoucherVM.SubBudgetClass + "'. Please consult Administrator!";
                    return Content(response);
                }
                var payeeType = db.PayeeTypes
                    .Where(a => a.PayeeTypeCode == paymentVoucherVM.PayeeType
                      && a.Status != "Cancelled")
                    .FirstOrDefault();

                if (payeeType == null)
                {
                    response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentVoucherVM.PayeeType + "'. Please contact Administrator!";
                    return Content(response);
                }

                var crCodes = db.JournalTypeViews
                    .Where(a => a.CrGfsCode == payeeType.GfsCode
                     && a.SubBudgetClass == paymentVoucherVM.SubBudgetClass
                     && a.InstitutionCode == institutionCode)
                    .FirstOrDefault();
                if (crCodes == null)
                {
                    response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentVoucherVM.SubBudgetClass + "' is missing. Please contact Administrator!";
                    return Content(response);
                }

                var unappliedAccount = serviceManager.GetUnappliedAccount(
                    institutionCode,
                    paymentVoucherVM.SubBudgetClass
                    );

                if (unappliedAccount == null)
                {
                    response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + institutionName + "' SBC: '" + paymentVoucherVM.SubBudgetClass + "'. Please consult Administrator!";
                    return Content(response);
                }

                if (!serviceManager.IsDateValid(new List<DateTime> { paymentVoucherVM.InvoiceDate, paymentVoucherVM.ApplyDate }))
                {
                    response = "Invalid date please check your computer date settings.";
                    return Content(response);
                }

                if (paymentVoucherVM.hasWithHolding)
                {
                    var withHoldingCoa = db.JournalTypeViews
                        .Where(a => a.CrGfsCode == payeeType.WithheldGfsCode
                          && a.SubBudgetClass == paymentVoucherVM.SubBudgetClass
                          && a.InstitutionCode == institutionCode)
                        .FirstOrDefault();

                    if (withHoldingCoa == null)
                    {
                        response = "Withholding Generation Failed. Chart of Account setup is incomplete. Withholding COA with GFS '" + payeeType.WithheldGfsCode + "' for subbudget class '" + paymentVoucherVM.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return Content(response);
                    }
                }

                CurrencyRateView currencyRateView = db.CurrencyRateViews
                    .Where(a => a.SubBudgetClass == paymentVoucherVM.SubBudgetClass
                          && a.InstitutionId == institutionId).FirstOrDefault();

                if (!paymentVoucherVM.IsAccrualVoucher)
                {
                    paymentVoucherVM.ApplyDate = DateTime.Now;
                    paymentVoucherVM.InvoiceDate = DateTime.Now;
                }

                if (serviceManager.GetFinancialYear(paymentVoucherVM.ApplyDate) == -1)
                {
                    return Content("The given apply date does not exist in the current Financial Year");
                }

                if (serviceManager.GetFinancialYear(paymentVoucherVM.ApplyDate)
                    < serviceManager.GetFinancialYear(DateTime.Now))
                {
                    var modal = db.RestrictionFinancialYears
                        .Where(a => a.InstitutionCode == institutionCode
                        && a.OverallStatus == "Active").FirstOrDefault();
                    if (modal != null)
                    {
                        if (modal.FinancialYearCode != serviceManager.GetFinancialYear(paymentVoucherVM.ApplyDate))
                        {
                            return Content("Invalid Apply date! " + paymentVoucherVM.ApplyDate);
                        }
                    }
                    else
                    {
                        return Content("Invalid Apply date! " + paymentVoucherVM.ApplyDate);
                    }
                }

                var glPostingDetail = GlService.GetGlPostingDetail(
                    institutionCode,
                    paymentVoucherVM.SubBudgetClass,
                    payerBank.GlAccount,
                    paymentVoucherVM.ParentInstitutionCode);

                if (glPostingDetail.OverallStatus == "Error")
                {
                    return Content(glPostingDetail.OverallStatusDescription);
                }
                string voucherSubLevelCode = subLevelCode;
                string voucherInstitutionCode = institutionCode;
                string voucherInstitutionName = institutionName;
                if (paymentVoucherVM.IsStPayment && !paymentVoucherVM.SubBudgetClass.StartsWith("3"))
                {
                    voucherSubLevelCode = paymentVoucherVM.SubWarrantCode;
                    voucherInstitutionCode = paymentVoucherVM.ParentInstitutionCode;
                    voucherInstitutionName = paymentVoucherVM.ParentInstitutionName;
                }
                else if (paymentVoucherVM.SubBudgetClass.StartsWith("3") && paymentVoucherVM.IsStPayment)
                {
                    voucherSubLevelCode = paymentVoucherVM.SubWarrantCode;
                }

                PaymentVoucher paymentVoucher = new PaymentVoucher
                {
                    SourceModule = paymentVoucherVM.IsAccrualVoucher ? "Accrual Voucher" : "Normal Voucher",
                    SourceModuleReferenceNo = "NA",
                    PayeeType = paymentVoucherVM.PayeeType,
                    InvoiceNo = paymentVoucherVM.InvoiceNo,
                    InvoiceDate = paymentVoucherVM.InvoiceDate,
                    PayeeDetailId = paymentVoucherVM.PayeeDetailId,
                    PayeeCode = paymentVoucherVM.PayeeCode,
                    Payeename = paymentVoucherVM.PayeeName,
                    PayeeBankAccount = paymentVoucherVM.BankAccountNo,
                    PayeeBankName = paymentVoucherVM.BankName,
                    PayeeAccountName = paymentVoucherVM.PayeeAccountName,
                    PayeeAddress = paymentVoucherVM.Address,
                    PayeeBIC = paymentVoucherVM.PayeeBIC,
                    Narration = paymentVoucherVM.Comments,
                    ControlNumber = paymentVoucherVM.ControlNumber,
                    PaymentDesc = paymentVoucherVM.PaymentDescription,
                    OperationalAmount = paymentVoucherVM.OperationalAmount,
                    BaseAmount = paymentVoucherVM.OperationalAmount * currencyRateView.OperationalExchangeRate,
                    BaseCurrency = currencyRateView.BaseCurrencyCode,
                    OperationalCurrency = paymentVoucherVM.OperationalCurrencyCode,
                    OperationalCurrencyId = currencyRateView.OperationalCurrencyId,
                    BaseCurrencyId = currencyRateView.BasecurrencyId,
                    ExchangeRate = currencyRateView.OperationalExchangeRate,
                    ApplyDate = paymentVoucherVM.ApplyDate,
                    PaymentMethod = paymentVoucherVM.PaymentMethod,
                    FinancialYear = serviceManager.GetFinancialYear(paymentVoucherVM.ApplyDate),
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    OverallStatus = "Pending",
                    Book = "MAIN",
                    InstitutionId = institutionId,
                    InstitutionCode = institutionCode,
                    InstitutionName = institutionName,
                    PaystationId = payStationId,
                    SubLevelCategory = subLevelCategory,
                    SubLevelCode = subLevelCode,
                    SubLevelDesc = subLevelDesc,
                    SubBudgetClass = paymentVoucherVM.SubBudgetClass,
                    JournalTypeCode = "PV",
                    InstitutionAccountId = payerBank.InstitutionAccountId,
                    PayerBankAccount = payerBank.AccountNumber,
                    PayerBankName = payerBank.AccountName,
                    PayerBIC = payerBank.BIC,
                    PayerCashAccount = payerBank.GlAccount,
                    PayableGlAccount = crCodes.CrCoa,
                    UnappliedAccount = unappliedAccount.AccountNumber,
                    PayerAccountType = payerBank.AccountType,
                    IsAccrualPayed = false,
                    IsAccrualVoucher = paymentVoucherVM.IsAccrualVoucher,
                    PVNo = institutionCode + ":" + DateTime.Now.ToString("yyyyMMddHHmmss"),

                    //Sub TSA
                    SubTsaBankAccount = payerBank.SubTSAAccountNumber,
                    SubTsaCashAccount = payerBank.SubTSAGlAccount,

                    // St Payment
                    StPaymentFlag = paymentVoucherVM.IsStPayment,
                    ParentInstitutionCode = paymentVoucherVM.ParentInstitutionCode,
                    ParentInstitutionName = paymentVoucherVM.ParentInstitutionName,
                    SubWarrantCode = paymentVoucherVM.SubWarrantCode,
                    SubWarrantDescription = paymentVoucherVM.SubWarrantDescription,

                    MiscDeduction = paymentVoucherVM.MiscDeduction,
                    MiscDeductionDescription = paymentVoucherVM.MiscDeductionDescription,
                    MiscDeductionPayeeDetailsId = paymentVoucherVM.MiscDeductionPayeeDetailsId,
                    MiscDeductionPayeeName = paymentVoucherVM.MiscDeductionPayeeName,

                    // Gl Posting Detail
                    StReceivableGfsCode = glPostingDetail.glPostingDetail.StReceivableGfsCode,
                    StReceivableCoa = glPostingDetail.glPostingDetail.StReceivableCoa,
                    StReceivableCoaDesc = glPostingDetail.glPostingDetail.StReceivableCoaDesc,
                    DeferredGfsCode = glPostingDetail.glPostingDetail.DeferredGfsCode,
                    DeferredCoa = glPostingDetail.glPostingDetail.DeferredCoa,
                    DeferredCoaDesc = glPostingDetail.glPostingDetail.DeferredCoaDesc,
                    GrantGfsCode = glPostingDetail.glPostingDetail.GrantGfsCode,
                    GrantCoa = glPostingDetail.glPostingDetail.GrantCoa,
                    GrantCoaDesc = glPostingDetail.glPostingDetail.GrantCoaDesc,
                    InstitutionLevel = glPostingDetail.glPostingDetail.institutionlevel,
                    Level1Code = glPostingDetail.glPostingDetail.level1code,
                    Level1Desc = glPostingDetail.glPostingDetail.level1desc,
                    InstitutionLogo = glPostingDetail.glPostingDetail.institutionLogo,
                    ExaminedBy = "Default"
                };

                if (paymentVoucherVM.hasWithHolding)
                {
                    paymentVoucher.ServiceAmount = paymentVoucherVM.ServiceAmount;
                    paymentVoucher.VATOnService = paymentVoucherVM.VATOnService;
                    paymentVoucher.GoodsAmount = paymentVoucherVM.GoodsAmount;
                    paymentVoucher.VATOnGoods = paymentVoucherVM.VATOnGoods;
                    paymentVoucher.hasWithHolding = paymentVoucherVM.hasWithHolding;
                    paymentVoucher.OperationalWithHoldingAmount = paymentVoucherVM.OperationalWithHoldingAmount;
                    paymentVoucher.BaseWithHoldingAmount = paymentVoucherVM.BaseWithHoldingAmount;
                    paymentVoucher.OtherWithholdingAmount = paymentVoucherVM.OtherWithholdingAmount;
                    paymentVoucher.OtherWithholdingPercent = paymentVoucherVM.OtherWithholdingPercent;
                }
                if (!paymentVoucherVM.IsAccrualVoucher)
                {
                    paymentVoucher.ApplyDate = DateTime.Now;
                }
                db.PaymentVouchers.Add(paymentVoucher);
                db.SaveChanges();
                paymentVoucherId = paymentVoucher.PaymentVoucherId;

                List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                foreach (VoucherDetailVm voucherDetailVm in paymentVoucherVM.voucherDetails)
                {
                    COA coa = db.COAs.Where(a => a.GlAccount == voucherDetailVm.ExpenditureLineItem && a.Status != "Cancelled").FirstOrDefault();
                    VoucherDetail voucherDetail = new VoucherDetail
                    {
                        PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                        JournalTypeCode = "PV",
                        DrGlAccount = voucherDetailVm.ExpenditureLineItem,
                        DrGlAccountDesc = voucherDetailVm.ItemDescription,
                        CrGlAccount = crCodes.CrCoa,
                        CrGlAccountDesc = crCodes.CrCoaDesc,
                        FundingReferenceNo = voucherDetailVm.FundingReference,
                        OperationalAmount = voucherDetailVm.ExpenseAmount,
                        BaseAmount = voucherDetailVm.BaseAmountDetail,
                        GfsCode = coa.GfsCode,
                        GfsCodeCategory = coa.GfsCodeCategory,
                        VoteDesc = coa.VoteDesc,
                        GeographicalLocationDesc = coa.GeographicalLocationDesc,
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
                        Level1Code = level1Code ?? "MISSING",
                        InstitutionLevel = institutionLevel,
                        Level1Desc = coa.Level1Desc ?? "MISSING",
                        TR = coa.TR,
                        SubVote = coa.SubVote,
                        SubVoteDesc = coa.SubVoteDesc,
                        SourceModuleRefNo = paymentVoucher.PVNo,
                        FundingSourceDesc = coa.FundingSourceDesc
                    };

                    voucherDetailList.Add(voucherDetail);
                }

                db.VoucherDetails.AddRange(voucherDetailList);
                db.SaveChanges();

                paymentVoucher.PVNo = serviceManager.GetLegalNumber(institutionCode, "V", paymentVoucher.PaymentVoucherId);

                if (paymentVoucher.PVNo.Contains("Error") || paymentVoucher.PVNo == null)
                {
                    paymentVoucher.OverallStatus = "Cancelled";
                    paymentVoucher.OverallStatusDesc = "Cancelled due to null PVNo";
                    paymentVoucher.CancelledAt = DateTime.Now;
                    paymentVoucher.CancelledBy = "System";
                    db.SaveChanges();
                    return Content("Payment Voucher Could not be saved. Please try again.EC1122");
                }
                db.SaveChanges();

                //Post transaction 
                List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                var vourcherDetails = db.VoucherDetails
                    .Where(a => a.PaymentVoucherId == paymentVoucher.PaymentVoucherId)
                    .ToList();


                foreach (var voucherDetail in vourcherDetails)
                {
                    TransactionLogVM transactionLogVM = new TransactionLogVM()
                    {
                        SourceModuleId = paymentVoucher.PaymentVoucherId,
                        LegalNumber = paymentVoucher.PVNo,
                        SourceModule = "Normal Voucher",
                        OverallStatus = paymentVoucher.OverallStatus,
                        OverallStatusDesc = paymentVoucher.OverallStatusDesc,
                        FundingRerenceNo = voucherDetail.FundingReferenceNo,
                        InstitutionCode = voucherInstitutionCode,
                        InstitutionName = voucherInstitutionName,
                        JournalTypeCode = paymentVoucher.JournalTypeCode,
                        GlAccount = voucherDetail.DrGlAccount,
                        GlAccountDesc = voucherDetail.DrGlAccountDesc,
                        GfsCode = voucherDetail.GfsCode,
                        GfsCodeCategory = voucherDetail.GfsCodeCategory,
                        TransactionCategory = "Expenditure",
                        VoteDesc = voucherDetail.VoteDesc,
                        GeographicalLocationDesc = voucherDetail.GeographicalLocationDesc,
                        TrDesc = voucherDetail.TrDesc,
                        SubBudgetClass = paymentVoucher.SubBudgetClass,
                        SubBudgetClassDesc = voucherDetail.SubBudgetClassDesc,
                        ProjectDesc = voucherDetail.ProjectDesc,
                        ServiceOutputDesc = voucherDetail.ServiceOutputDesc,
                        ActivityDesc = voucherDetail.ActivityDesc,
                        FundTypeDesc = voucherDetail.FundTypeDesc,
                        CofogDesc = voucherDetail.CofogDesc,
                        SubLevelCode = voucherSubLevelCode,
                        FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                        OperationalAmount = voucherDetail.OperationalAmount,
                        BaseAmount = voucherDetail.BaseAmount,
                        Currency = paymentVoucher.OperationalCurrency,
                        CreatedAt = DateTime.Now,
                        CreatedBy = paymentVoucher.CreatedBy,
                        ApplyDate = paymentVoucher.ApplyDate,
                        PayeeCode = paymentVoucher.PayeeCode,
                        PayeeName = paymentVoucher.Payeename,
                        TransactionDesc = paymentVoucher.PaymentDesc,
                        TR = voucherDetail.TR,
                        Facility = voucherDetail.Facility,
                        FacilityDesc = voucherDetail.FacilityDesc,
                        SourceModuleRefNo = paymentVoucher.SourceModuleReferenceNo,
                        CostCentre = voucherDetail.CostCentre,
                        CostCentreDesc = voucherDetail.CostCentreDesc,
                        Level1Code = level1Code,
                        InstitutionLevel = institutionLevel,
                        Level1Desc = voucherDetail.Level1Desc,
                        SubVote = voucherDetail.SubVote,
                        SubVoteDesc = voucherDetail.SubVoteDesc,
                        FundingSourceDesc = voucherDetail.FundingSourceDesc,
                    };
                    transactionLogVMs.Add(transactionLogVM);
                }
                response = fundBalanceServices.PostTransaction(transactionLogVMs);
                if (response != "Success")
                {
                    RollBack(paymentVoucherId);
                    return Content("Sorry! Payment Voucher could not be saved. Please try again later. If the problem persists please contact your system support.EC1133");
                }
            }
            catch (Exception ex)
            {
                RollBack(paymentVoucherId);
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Content("Sorry! Payment Voucher could not be saved. Please try again later. If the problem persists please contact your system support.EC1144");
            }
            return Content(response);
        }

        public void RollBack(int pvId)
        {
            try
            {
                if (pvId != 0)
                {
                    var voucher = db.PaymentVouchers.Find(pvId);
                    if (voucher != null)
                    {
                        voucher.OverallStatus = "Cancelled";
                        voucher.CancelledAt = DateTime.Now;
                        voucher.OverallStatusDesc = "Cancelled due to saving failure";
                        voucher.CancelledBy = "system";
                        db.SaveChanges();

                        var trxLog = db.TransactionLogs
                            .Where(a => a.SourceModuleId == pvId && a.SourceModule == "Normal Voucher")
                            .ToList();
                        foreach (var t in trxLog)
                        {
                            t.OverallStatus = "Cancelled";
                            t.CancelledAt = DateTime.Now;
                            t.OverallStatusDesc = "Cancelled due to saving failure";
                            t.CancelledBy = "system";
                        }
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
        }

        public ActionResult ApproveAccrualVoucher(PaymentVoucherVM paymentVoucher)
        {
            var pv = db.PaymentVouchers.Find(paymentVoucher.PaymentVoucherId);

            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());
            var payeeType = db.PayeeTypes
                  .Where(a => a.PayeeTypeCode == pv.PayeeType
                    && a.Status != "Cancelled")
                  .FirstOrDefault();

            if (payeeType == null)
            {
                var response = "Vendor setup is incomplete. There is no payee type setup for '" + pv.PayeeType + "'. Please contact Administrator!";
                return Content(response);
            }
            var crCodes = db.JournalTypeViews
               .Where(a => a.CrGfsCode == payeeType.GfsCode
                && a.SubBudgetClass == pv.SubBudgetClass
                && a.InstitutionCode == userPaystation.InstitutionCode)
               .FirstOrDefault();
            if (crCodes == null)
            {
                var response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + pv.SubBudgetClass + "' is missing. Please contact Administrator!";
                return Content(response);
            }
            var unappliedAccount = serviceManager.GetUnappliedAccount(
                   userPaystation.InstitutionCode,
                   pv.SubBudgetClass
                   );

            if (unappliedAccount == null)
            {
                var response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                return Content(response);
            }
            CurrencyRateView currencyRateView = db.CurrencyRateViews.Where(a => a.SubBudgetClass == pv.SubBudgetClass
                             && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();


            if (currencyRateView == null)
            {
                var response = "Currency Rate Setup is Incomplete";
                return Content(response);
            }

            try
            {
                var model = db.AccruedPaymentSummaries
                    .Where(a => a.PaymentVoucherId == pv.PaymentVoucherId)
                    .FirstOrDefault();

                if (model != null)
                {
                    var details = db.AccruedPaymentDetails
                        .Where(a => a.AccruedPaymentSummaryId
                         == model.AccruedPaymentSummaryId)
                         .ToList();
                    details.ForEach(d =>
                    {
                        db.AccruedPaymentDetails.Remove(d);
                    });
                    db.AccruedPaymentSummaries.Remove(model);
                    db.SaveChanges();
                }

                AccruedPaymentSummary apvs = new AccruedPaymentSummary
                {
                    AccruedPaymentSummaryNum = paymentVoucher.voucherDetails.Count(),
                    Book = pv.Book,
                    JournalTypeCode = pv.JournalTypeCode,
                    BudgetClass = pv.SubBudgetClass,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionId = userPaystation.InstitutionId,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    SubInstitutionId = userPaystation.InstitutionSubLevelId,
                    SubVoteCode = userPaystation.SubLevelCode,
                    SubVoteDesc = userPaystation.SubLevelDesc,
                    TotalAmount = (decimal)pv.OperationalAmount,
                    ApplyDate = pv.ApplyDate,
                    FinancialYear = (int)pv.FinancialYear,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity.GetUserName(),
                    PVNo = pv.PVNo,
                    PaymentVoucherId = pv.PaymentVoucherId,
                    OverallStatus = "Approved",
                };
                db.AccruedPaymentSummaries.Add(apvs);
                db.SaveChanges();

                foreach (var d in paymentVoucher.voucherDetails)
                {

                    var baseAmount = d.ExpenseAmount * currencyRateView.OperationalExchangeRate;

                    AccruedPaymentDetail apvd = new AccruedPaymentDetail
                    {
                        AccruedPaymentSummaryId = apvs.AccruedPaymentSummaryId,
                        OriginalAmount = d.ExpenseAmount,
                        OperationalAmount = d.ExpenseAmount,
                        BaseAmount = baseAmount,
                        BaseCurrency = currencyRateView.BaseCurrencyCode,
                        BaseCurrencyId = (int)currencyRateView.BasecurrencyId,
                        OperationalCurrency = currencyRateView.OperationalCurrencyCode,
                        OperationalCurrencyId = (int)currencyRateView.OperationalCurrencyId,
                        ExchangeRate = currencyRateView.OperationalExchangeRate,
                        DrGlAccount = d.ExpenditureLineItem,
                        CrGlAccount = crCodes.CrCoa,
                        FinancialYear = (int)pv.FinancialYear,
                        //FundingReference = d.FundingReference
                    };


                    db.AccruedPaymentDetails.Add(apvd);
                    pv.OverallStatus = "Confirmed";
                    pv.IsAccrualPayed = true;
                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Content("Sorry! Could not approve please contact system support!");
            }
            return Content("Success");
        }
        public JsonResult GetPayee(string search)
        {
            db.Database.CommandTimeout = 260;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            IQueryable<PayeeBankView> payeeQuery = db.PayeeBankViews
                 .Where(a => a.OverallStatus == "APPROVED")
                 .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                 .Where(a => a.VerificationStatus == "APPROVED")
                 .Where(a => a.PayeeOverallStatus == "ACTIVE")
                 .Where(a => a.Accountnumber.Contains(search) || a.AccountName.Contains(search))
                .Take(20);

            return Json(new
            {
                data = payeeQuery.ToList(),
            }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetFundBalance(
            DateTime? applyDate,
            string subBudgetClass,
            string JournalTypeCode = "UNKNOWN",
            string instCode = "UNKNOWN",
            string subWarrantCode = "UNKNOWN",
            bool IsAccrualVoucher = false
            )
        {
            db.Database.CommandTimeout = 1200;

            List<FundBalanceViewVM> tempBalanceList = new List<FundBalanceViewVM>();
            List<FundBalanceViewVM> balanceList = new List<FundBalanceViewVM>();
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                List<InstitutionSubLevel> allUserPaystations = serviceManager.GetAllUserPayStations(User.Identity.GetUserId());

                //var _InstitutionCode = instCode == "UNKNOWN" ? userPaystation.InstitutionCode : instCode;
                var _InstitutionCode = (instCode == "UNKNOWN" || (subBudgetClass.StartsWith("3") && subWarrantCode != "UNKNOWN")) ? userPaystation.InstitutionCode : instCode;
                tempBalanceList = fundBalanceServices.GetFundBalances(_InstitutionCode, subBudgetClass, applyDate == null ? DateTime.Now : (DateTime)applyDate, IsAccrualVoucher);

                var _JournalTypeCode = JournalTypeCode == "UNKNOWN" ? "PV" : JournalTypeCode;
                balanceList = tempBalanceList;
                List<string> _allSubLevelCodes = allUserPaystations.Select(a => a.SubLevelCode).ToList();
                var _SublevelCode = subWarrantCode == "UNKNOWN" ? userPaystation.SubLevelCode : subWarrantCode;
                if (subBudgetClass.StartsWith("3"))
                {
                    if (subWarrantCode == "UNKNOWN")
                    {
                        balanceList = tempBalanceList
                                     .Where(a => a.InstitutionCode == _InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       && a.SublevelCode == "0000")
                                     .ToList();
                    }
                    else //Return empty. No deposit payment for sub warrant
                    {
                        //balanceList = new List<FundBalanceViewVM>();
                        balanceList = tempBalanceList
                                     .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       && a.SublevelCode == subWarrantCode)
                                     .ToList();
                    }

                }
                else
                {
                    if (!IsAccrualVoucher)
                    {
                        if (subWarrantCode == "UNKNOWN")
                        {
                            balanceList = tempBalanceList
                                         .Where(a => a.InstitutionCode == _InstitutionCode
                                           && a.SubBudgetClass == subBudgetClass
                                           && _allSubLevelCodes.Contains(a.SublevelCode))
                                         .ToList();
                        }
                        else
                        {
                            balanceList = tempBalanceList
                                .Where(a => a.InstitutionCode == _InstitutionCode
                                  && a.SubBudgetClass == subBudgetClass
                                  && a.SublevelCode.Trim() == subWarrantCode.Trim())
                                 .ToList();
                        }
                    }
                }

                if (balanceList.Count > 0)
                    balanceList.OrderByDescending(a => a.SublevelCode).ToList();

                var response = Json(new { data = balanceList }, JsonRequestBehavior.AllowGet);
                response.MaxJsonLength = int.MaxValue;
                return response;
            }
            catch (Exception ex)
            {
                balanceList = new List<FundBalanceViewVM>();
                var response = Json(new { data = balanceList }, JsonRequestBehavior.AllowGet);
                response.MaxJsonLength = int.MaxValue;
                return response;
            }


        }

        public JsonResult GetFundBalanceClassic(
            DateTime? applyDate,
            string subBudgetClass,
            string JournalTypeCode = "UNKNOWN",
            string instCode = "UNKNOWN",
            string subWarrantCode = "UNKNOWN",
            bool IsAccrualVoucher = false
            )
        {
            db.Database.CommandTimeout = 1200;

            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>();
            List<FundBalance> tempBalanceList = new List<FundBalance>();
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                List<InstitutionSubLevel> allUserPaystations = serviceManager.GetAllUserPayStations(User.Identity.GetUserId());

                var _InstitutionCode = instCode == "UNKNOWN" ? userPaystation.InstitutionCode : instCode;
                BalanceResponse fundBalanceResponse =
                       GlService.GetFundBalance(
                           db, _InstitutionCode,
                           subBudgetClass,
                          applyDate == null ? DateTime.Now : (DateTime)applyDate, IsAccrualVoucher);

                if (fundBalanceResponse.overallStatus == "Error")
                {
                    return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
                }

                var _JournalTypeCode = JournalTypeCode == "UNKNOWN" ? "PV" : JournalTypeCode;

                List<string> _allSubLevelCodes = allUserPaystations.Select(a => a.SubLevelCode).ToList();
                var _SublevelCode = subWarrantCode == "UNKNOWN" ? userPaystation.SubLevelCode : subWarrantCode;
                if (subBudgetClass.StartsWith("3"))
                {
                    if (subWarrantCode == "UNKNOWN")
                    {
                        tempBalanceList = fundBalanceResponse.FundBalanceList
                                     .Where(a => a.InstitutionCode == _InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       && a.SublevelCode == "0000"
                                       // && a.SourceModule == "AP"
                                       //&& a.JournalTypeCode == _JournalTypeCode
                                       )
                                     .ToList();
                    }
                    else //Return empty. No deposit payment for sub warrant
                    {
                        fundBalanceList = new List<FundBalanceView>();
                    }

                }
                else //Normal non-deposit payment
                {
                    if (subWarrantCode == "UNKNOWN")
                    {
                        tempBalanceList = fundBalanceResponse.FundBalanceList
                                     .Where(a => a.InstitutionCode == _InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       && _allSubLevelCodes.Contains(a.SublevelCode)
                                     // && a.SourceModule == "AP"
                                     //  && a.JournalTypeCode == _JournalTypeCode
                                     ).ToList();
                    }
                    else
                    {
                        tempBalanceList = fundBalanceResponse.FundBalanceList
                     .Where(a => a.InstitutionCode == _InstitutionCode
                       && a.SubBudgetClass == subBudgetClass
                         && a.SublevelCode.Trim() == subWarrantCode.Trim()
                       // && a.SourceModule == "AP"
                       //  && a.JournalTypeCode == _JournalTypeCode
                       )
                     .ToList();
                    }
                }



            }
            catch (Exception ex)
            {
                return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
            }

            if (IsAccrualVoucher)
            {
                var fYear = serviceManager.GetFinancialYear((DateTime)applyDate);
                tempBalanceList = tempBalanceList
                    .Where(a => a.FinancialYear == fYear)
                    .ToList();
            }

            /**** Map Fund Balance Result to Match old format ****/
            foreach (var currentfundbalance in tempBalanceList)
            {
                FundBalanceView fundBalance = new FundBalanceView
                {
                    FundBalanceViewId = currentfundbalance.FundBalanceId,
                    BudgetOperationalAmount = 0,
                    BudgetBaseAmount = 0,
                    FundingRefNo = currentfundbalance.FundingRefNo,
                    FundingSource = "",
                    GlAccount = currentfundbalance.GlAccount,
                    GlAccountDesc = currentfundbalance.GlAccountDesc,
                    InstitutionCode = currentfundbalance.InstitutionCode,
                    SourceModule = "AP",
                    JournalTypeCode = "PV",
                    SubLevelCategory = "",
                    SublevelCode = currentfundbalance.SublevelCode,
                    SubBudgetClass = currentfundbalance.SubBudgetClass,
                    TrxType = "",
                    DrGfsCode = currentfundbalance.GlAccount,
                    AllocationAmount = currentfundbalance.Allocation,
                    ExpenditureToDate = 0,
                    FundBalance = currentfundbalance.Balance,
                    BudgetBalance = 0
                };

                fundBalanceList.Add(fundBalance);
            }

            //////
            return Json(new { data = fundBalanceList.OrderByDescending(a => a.SublevelCode).ToList() }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetFundBalanceByDate(
            DateTime applyDate,
            string subBudgetClass,
            string JournalTypeCode = "UNKNOWN",
            string instCode = "UNKNOWN",
            string subWarrantCode = "UNKNOWN"
            )
        {
            db.Database.CommandTimeout = 1200;

            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>();
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                List<InstitutionSubLevel> allUserPaystations = serviceManager.GetAllUserPayStations(User.Identity.GetUserId());

                var _InstitutionCode = instCode == "UNKNOWN" ? userPaystation.InstitutionCode : instCode;
                BalanceResponse fundBalanceResponse =
                    GlService.GetFundBalance(
                        db, _InstitutionCode,
                        subBudgetClass,
                        applyDate == null ? DateTime.Now : applyDate
                        );
                if (fundBalanceResponse.overallStatus == "Error")
                {
                    return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
                }

                var _JournalTypeCode = JournalTypeCode == "UNKNOWN" ? "PV" : JournalTypeCode;

                List<string> _allSubLevelCodes = allUserPaystations.Select(a => a.SubLevelCode).ToList();
                var _SublevelCode = subWarrantCode == "UNKNOWN" ? userPaystation.SubLevelCode : subWarrantCode;
                if (subBudgetClass.StartsWith("3"))
                {
                    if (subWarrantCode == "UNKNOWN")
                    {
                        fundBalanceList = fundBalanceResponse.FundBalanceViewList
                                     .Where(a => a.InstitutionCode == _InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       && a.SublevelCode == "0000"
                                       //&& _allSubLevelCodes.Contains(a.SublevelCode)
                                       && a.SourceModule == "AP"
                                       && a.JournalTypeCode == _JournalTypeCode)
                                     .ToList();
                    }
                    else //Return empty. No deposit payment for sub warrant
                    {
                        fundBalanceList = new List<FundBalanceView>();
                    }

                }
                else //Normal non-deposit payment
                {
                    if (subWarrantCode == "UNKNOWN")
                    {
                        fundBalanceList = fundBalanceResponse.FundBalanceViewList
                                     .Where(a => a.InstitutionCode == _InstitutionCode
                                       && a.SubBudgetClass == subBudgetClass
                                       // && _allSubLevelCodes.Contains(a.SublevelCode)
                                       && a.SourceModule == "AP"
                                       && a.JournalTypeCode == _JournalTypeCode)
                                     .ToList();
                    }
                    else
                    {
                        fundBalanceList = fundBalanceResponse.FundBalanceViewList
                     .Where(a => a.InstitutionCode == _InstitutionCode
                       && a.SubBudgetClass == subBudgetClass
                         && a.SublevelCode == subWarrantCode
                       && a.SourceModule == "AP"
                       && a.JournalTypeCode == _JournalTypeCode)
                     .ToList();
                    }
                }



            }
            catch (Exception ex)
            {
                return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAccrualPayed(string PVNo)
        {
            var data = db.PaymentVouchers
                .Where(a => a.SourceModuleReferenceNo == PVNo)
                .ToList();
            var sumList = data.Where(a => a.OverallStatus != "Cancelled").ToList();
            return Json(new
            {
                data,
                sum = sumList.Sum(a => a.OperationalAmount)
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAccrualFundBalance(
            int PaymentVoucherId
            )
        {
            db.Database.CommandTimeout = 1200;

            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>();
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                List<InstitutionSubLevel> allUserPaystations = serviceManager.GetAllUserPayStations(User.Identity.GetUserId());

                var pv = db.PaymentVouchers.Find(PaymentVoucherId);
                BalanceResponse fundBalanceResponse =
                    serviceManager.GetFundBalance(
                        userPaystation.InstitutionCode,
                        pv.SubBudgetClass,
                        DateTime.Now,
                        true);
                if (fundBalanceResponse.overallStatus == "Error")
                {
                    return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
                }
                var pvDetails = db.VoucherDetails
                                .Where(a => a.PaymentVoucherId == PaymentVoucherId)
                                .ToList();
                fundBalanceList = fundBalanceResponse.FundBalanceViewList
                   .Where(a => a.JournalTypeCode == "PV" && a.SourceModule == "AP")
                   .ToList();
            }
            catch (Exception ex)
            {
                return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
            }

            var response = Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
        }
        public JsonResult SearchFundBalance(string subBudgetClass, string keywords)
        {
            db.Database.CommandTimeout = 120;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>();
            try
            {
                fundBalanceList = db.FundBalanceViews
                .Where(a => a.InstitutionCode.Replace("|", "") == userPaystation.InstitutionCode
                     && a.SubBudgetClass == subBudgetClass
                     && a.SourceModule == "AP"
                     && a.FundBalance > 0
                     && a.JournalTypeCode == "PV")
                 .Where(a => a.GlAccountDesc.Contains(keywords)
                     || a.FundingRefNo.Contains(keywords)
                     || a.FundingSource.Contains(keywords)
                     || a.GlAccount.Contains(keywords))
                 .Take(5)
                 .ToList();
            }
            catch (Exception ex)
            {

            }

            return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, Authorize(Roles = "Voucher Entry")]
        //[ValidateAntiForgeryToken]
        public ActionResult EditVoucher(EditPaymentVoucherVm voucherData)
        {
            PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(voucherData.PaymentVoucherId);
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            var payerBank = db.InstitutionAccounts
             .Where(a => a.SubBudgetClass == paymentVoucher.SubBudgetClass
              && a.InstitutionCode == userPaystation.InstitutionCode
              && a.OverallStatus != "Cancelled"
              && a.IsTSA == false
             ).FirstOrDefault();
            if (payerBank == null)
            {
                response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '"
                    + paymentVoucher.SubBudgetClass
                    + "'. Please consult Administrator!";
                return Content(response);
            }
            var payeeType = db.PayeeTypes
                .Where(a => a.PayeeTypeCode == paymentVoucher.PayeeType
                  && a.Status != "Cancelled")
                .FirstOrDefault();

            if (payeeType == null)
            {
                response = "Vendor setup is incomplete. There is no payee type setup for '"
                    + paymentVoucher.PayeeType + "'. Please contact Administrator!";
                return Content(response);
            }
            var crCodes = db.JournalTypeViews
                .Where(a => a.CrGfsCode == payeeType.GfsCode
                   && a.SubBudgetClass == paymentVoucher.SubBudgetClass
                   && a.InstitutionCode == userPaystation.InstitutionCode)
                .FirstOrDefault();

            if (crCodes == null)
            {
                response = "Chart of Account setup is incomplete. COA with GFS '"
                    + payeeType.GfsCode + "' for subbudget class '"
                    + paymentVoucher.SubBudgetClass
                    + "' is missing. Please contact Administrator!";
                return Content(response);
            }

            var unappliedAccount = serviceManager.GetUnappliedAccount(
                      userPaystation.InstitutionCode,
                      paymentVoucher.SubBudgetClass
                      );

            if (unappliedAccount == null)
            {
                response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'"
                    + userPaystation.Institution.InstitutionName
                    + "'. Please consult Administrator!";
                return Content(response);
            }

            if (paymentVoucher.SourceModule == "Normal Voucher")
            {
                if (voucherData.InvoiceDate > DateTime.Now)
                {
                    // response = "Invalid Invoice date.";
                    //return Content(response);
                }
            }

            if (paymentVoucher.hasWithHolding)
            {
                var withHoldingCoa = db.JournalTypeViews
                    .Where(a => a.CrGfsCode == payeeType.WithheldGfsCode
                      && a.SubBudgetClass == paymentVoucher.SubBudgetClass
                      && a.InstitutionCode == userPaystation.InstitutionCode)
                    .FirstOrDefault();
                if (withHoldingCoa == null)
                {
                    response = "Withholding Generation Failed. Chart of Account setup is incomplete. Withholding COA with GFS '"
                        + payeeType.WithheldGfsCode
                        + "' for subbudget class '"
                        + paymentVoucher.SubBudgetClass
                        + "' is missing. Please contact Administrator!";
                    return Content(response);
                }
            }

            //if (paymentVoucher.ControlNumber != null)
            //{
            //    var controlNumExists = db.PaymentVouchers
            //        .Where(a => a.ControlNumber == voucherData.ControlNumber
            //          && a.OverallStatus != "Cancelled"
            //          && a.OverallStatus != "Unapplied"
            //          && a.PaymentVoucherId != paymentVoucher.PaymentVoucherId)
            //        .Count();
            //    if (controlNumExists > 0)
            //    {
            //        return Content("Duplicate Controll Number!");
            //    }
            //}

            try
            {
                // using (TransactionScope scope = new TransactionScope()) { }
                if (paymentVoucher != null)
                {
                    paymentVoucher.Narration = voucherData.Narration;
                    paymentVoucher.PaymentDesc = voucherData.PaymentDesc;
                    paymentVoucher.InvoiceNo = voucherData.InvoiceNo;
                    paymentVoucher.ControlNumber = voucherData.ControlNumber;
                    paymentVoucher.PaymentMethod = voucherData.PaymentMethod;

                    var payee = db.PayeeDetails.Find(paymentVoucher.PayeeDetailId);

                    if (payee.RequireControlNum == true && voucherData.ControlNumber == null)
                    {
                        return Content("This payee account[" + payee.Accountnumber + "] requires a control number");
                    }
                    if (voucherData.hasWithHolding)
                    {
                        // WITH HOLDING
                        paymentVoucher.ServiceAmount = voucherData.ServiceAmount;
                        paymentVoucher.VATOnService = voucherData.VATOnService;
                        paymentVoucher.GoodsAmount = voucherData.GoodsAmount;
                        paymentVoucher.VATOnGoods = voucherData.VATOnGoods;
                        paymentVoucher.OperationalWithHoldingAmount = voucherData.OperationalWithHoldingAmount;
                        paymentVoucher.BaseWithHoldingAmount = voucherData.BaseWithHoldingAmount;
                        paymentVoucher.hasWithHolding = voucherData.hasWithHolding;
                        paymentVoucher.OtherWithholdingAmount = voucherData.OtherWithholdingAmount;
                        paymentVoucher.OtherWithholdingPercent = voucherData.OtherWithholdingPercent;
                        paymentVoucher.hasWithHolding = true;
                    }
                    else
                    {
                        paymentVoucher.ServiceAmount = 0;
                        paymentVoucher.VATOnService = 0;
                        paymentVoucher.GoodsAmount = 0;
                        paymentVoucher.VATOnGoods = 0;
                        paymentVoucher.OperationalWithHoldingAmount = 0;
                        paymentVoucher.BaseWithHoldingAmount = 0;
                        paymentVoucher.hasWithHolding = false;
                        paymentVoucher.OtherWithholdingAmount = 0;
                        paymentVoucher.OtherWithholdingPercent = 0;
                    }

                    if (paymentVoucher.SourceModule == "Normal Voucher")
                    {
                        if (voucherData.ApplyDate.Year != 0001)
                        {
                            paymentVoucher.ApplyDate = voucherData.ApplyDate;
                        }
                        if (voucherData.InvoiceDate.Year != 0001)
                        {
                            paymentVoucher.InvoiceDate = voucherData.InvoiceDate;
                        }
                    }

                    if (paymentVoucher.IsAccrualVoucher == null)
                    {
                        paymentVoucher.IsAccrualVoucher = false;
                    }


                    var userInst = db.Institution.Find(userPaystation.InstitutionId);
                    var isEmbassy = userInst.InstitutionCategory.Trim() == "Embassy/Commission";
                    if ((!(bool)paymentVoucher.IsAccrualVoucher || paymentVoucher.IsAccrualVoucher == null) && !isEmbassy)
                    {
                        paymentVoucher.ApplyDate = DateTime.Now;
                        paymentVoucher.InvoiceDate = DateTime.Now;
                    }

                    paymentVoucher.MiscDeduction = voucherData.MiscDeduction;
                    paymentVoucher.MiscDeductionDescription = voucherData.MiscDeductionDescription;
                    paymentVoucher.MiscDeductionPayeeDetailsId = voucherData.MiscDeductionPayeeDetailsId;
                    paymentVoucher.MiscDeductionPayeeName = voucherData.MiscDeductionPayeeName;
                    db.SaveChanges();
                }
                else
                {
                    response = "Payment Voucher Not Found!";
                }

            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
            }

            return Content(response);
        }

        [HttpPost, Authorize(Roles = "Voucher Entry")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteVoucher(int PaymentVoucherId)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(PaymentVoucherId);
                    if (paymentVoucher != null)
                    {
                        try
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            paymentVoucher.CancelledAt = DateTime.Now;
                            paymentVoucher.CancelledBy = User.Identity.GetUserName();

                            if (paymentVoucher.hasWithHolding)
                            {
                                var wh = db.WithHoldingDetails.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                                if (wh != null)
                                {
                                    wh.OverallStatus = "Cancelled";
                                    wh.OverallStatusDesc = "Cancelled due to cancellation of PV:" + paymentVoucher.PVNo +
                                        "At: " + DateTime.Now + " By: " + User.Identity.GetUserName();
                                }
                            }

                            if (paymentVoucher.MiscDeduction != null
                                && paymentVoucher.MiscDeductionPayeeDetailsId != null)
                            {
                                var _miscVoucher = db.PaymentVouchers
                                    .Where(a => a.OtherSourceId == paymentVoucher.PaymentVoucherId
                                     && a.SourceModule == "MiscDeduction")
                                    .FirstOrDefault();
                                if (_miscVoucher != null)
                                {
                                    _miscVoucher.OverallStatus = "Cancelled";
                                    _miscVoucher.OverallStatusDesc = "Cancelled due to cancelleation";
                                    _miscVoucher.SourceModule = "_MiscDeduction";
                                    _miscVoucher.CancelledAt = DateTime.Now;
                                    _miscVoucher.CancelledBy = User.Identity.GetUserName();
                                }
                            }

                            db.SaveChanges();

                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);
                            if (response == "Success")
                            {
                                var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                                db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", parameters);

                                scope.Complete();
                            }

                        }
                        catch (Exception ex)
                        {
                            response = ex.InnerException.ToString();
                        }
                    }
                    else
                    {
                        response = "Payment Voucher Not Found!";
                    }
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                }
            }
            return Content(response);
        }



        [HttpPost, Authorize(Roles = "Voucher Entry")]
        //[ValidateAntiForgeryToken]
        public ActionResult ConfirmVoucher(int paymentVoucherId)
        {
            string response = "Success";

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                db.Database.CommandTimeout = 28000;
                PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(paymentVoucherId);

              
                if (paymentVoucher.MiscDeduction != null && paymentVoucher.MiscDeductionPayeeDetailsId == null)
                {
                    return Content("Please add misclellaneous deduction payee!");
                }

                if (paymentVoucher.PVNo.Contains(":"))
                {
                    paymentVoucher.PVNo = paymentVoucher.InstitutionCode + "V" + paymentVoucher.PaymentVoucherId;
                }

                var payee = db.PayeeDetails.Find(paymentVoucher.PayeeDetailId);
                if (payee != null)
                {
                    if (payee.RequireControlNum == true
                        && (paymentVoucher.ControlNumber == null || paymentVoucher.ControlNumber == ""))
                    {
                        return Content("This payee account [" + payee.Accountnumber + "] requires a control number");
                    }
                }

                if (paymentVoucher.SourceModule == "External")
                {
                    var result = PostTenPercent(paymentVoucherId);
                    if (result != "Success")
                    {
                        return Content(result);
                    }
                }

                try
                {

                    if (paymentVoucher.hasWithHolding)
                    {
                        ProcessResponse withHoldingStatus = serviceManager
                          .GenerateWithHolding(paymentVoucher.PaymentVoucherId);
                        if (withHoldingStatus.OverallStatus == "Error")
                        {
                            response = withHoldingStatus.OverallStatusDescription;
                            return Content(response);
                        }
                    }

                    if (paymentVoucher != null)
                    {

                        ComputeNetPayable(paymentVoucherId);
                        var ans = CreateDeduction(paymentVoucherId);
                        if (ans != "Success")
                        {
                            return Content(ans);
                        }

                        var voucherDetailsAmount = db.VoucherDetails
                            .Where(a => a.PaymentVoucherId == paymentVoucherId)
                            .Sum(a => a.OperationalAmount);
                        if (paymentVoucher.SourceModule != "Contract")
                        {
                            if (paymentVoucher.AdvancePayment != null)
                            {
                                voucherDetailsAmount = voucherDetailsAmount + paymentVoucher.AdvancePayment;
                            }
                            if (paymentVoucher.RetentionAmount != null)
                            {
                                voucherDetailsAmount = voucherDetailsAmount + paymentVoucher.RetentionAmount;
                            }
                            if (voucherDetailsAmount != paymentVoucher.OperationalAmount)
                            {
                                return Content("Total Voucher Amount: " + paymentVoucher.OperationalAmount
                                    + " Differs with Voucher Details Amount: " + voucherDetailsAmount +
                                    " Please cancell and create again!");
                            }
                        }
                        paymentVoucher.ConfirmedBy = User.Identity.Name;
                        paymentVoucher.ConfirmedAt = DateTime.Now;

                        db.SaveChanges();

                        InstitutionSubLevel userPaystation = serviceManager
                       .GetUserPayStation(User.Identity.GetUserId());

                        var institutionConfig = db.InstitutionConfigs
                        .Where(a => a.InstitutionId == userPaystation.InstitutionId
                          && a.ConfigName == "SkipExamination"
                          && a.OverallStatus != "CANCELLED")
                        .Any();

                        var userInst = db.Institution.Find(userPaystation.InstitutionId);
                        var isEmbassy = userInst.InstitutionCategory.Trim() == "Embassy/Commission";

                        if (isEmbassy)
                        {
                            paymentVoucher.PaymentMethod = "Cheque";
                        }

                        if (!institutionConfig)
                        {
                            paymentVoucher.OverallStatus = "Confirmed";

                            if (paymentVoucher.SourceModule == "Imprest")
                            {
                                var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                                if (imprest != null)
                                {
                                    imprest.VoucherStatus = "Confirmed";
                                }
                            }
                        }
                        else
                        {
                            paymentVoucher.ExaminedBy = "NA";
                            paymentVoucher.ExaminedAt = DateTime.Now;
                            paymentVoucher.OverallStatus = "Confirmed";
                            paymentVoucher.ExaminationStatus = "NA";

                            if (!paymentVoucher.hasWithHolding)
                            {
                                paymentVoucher.NetOperationalAmount = paymentVoucher.OperationalAmount;
                                paymentVoucher.NetBaseAmount = paymentVoucher.BaseAmount;
                            }
                            if (paymentVoucher.SourceModule == "Imprest")
                            {
                                var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                                if (imprest != null)
                                {
                                    imprest.VoucherStatus = "Examined";
                                }
                            }
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        response = "Payment Voucher Not Found!";
                    }

                    response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);
                    if (response == "Success")
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    //response = ex.Message.ToString();
                    //if (ex.InnerException != null)
                    //{
                    //    response = ex.InnerException.ToString();
                    //}

                    ErrorSignal.FromCurrentContext().Raise(ex);
                    Log.Information(ex + "{Name}!", "ErrorOnPaymentConfirmation");
                    response = "An error occurred while processing your request, please try again/ contact system support";
                }
            }
            return Content(response);
        }

        public string PostTenPercent(int id)
        {
            var response = "Success";
            try
            {
                var pv = db.PaymentVouchers.Find(id);

                var pvDetails = db.VoucherDetails
                    .Where(a => a.FundingReferenceNo == "INVALID" && a.PaymentVoucherId == id)
                    .Count();
                if (pvDetails > 0)
                {
                    return "Funding Reference is Required! Please update gl entries";
                }
                List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                var vourcherDetails = db.VoucherDetails
                    .Where(a => a.PaymentVoucherId == pv.PaymentVoucherId)
                    .ToList();

                foreach (var voucherDetail in vourcherDetails)
                {

                    TransactionLogVM transactionLogVM = new TransactionLogVM()
                    {
                        SourceModuleId = pv.PaymentVoucherId,
                        LegalNumber = pv.PVNo,
                        SourceModule = "Normal Voucher",
                        OverallStatus = pv.OverallStatus,
                        OverallStatusDesc = pv.OverallStatusDesc,
                        FundingRerenceNo = voucherDetail.FundingReferenceNo,
                        InstitutionCode = pv.InstitutionCode,
                        InstitutionName = pv.InstitutionName,
                        JournalTypeCode = pv.JournalTypeCode,
                        GlAccount = voucherDetail.DrGlAccount,
                        GlAccountDesc = voucherDetail.DrGlAccountDesc,
                        GfsCode = voucherDetail.GfsCode,
                        GfsCodeCategory = voucherDetail.GfsCodeCategory,
                        TransactionCategory = "Expenditure",
                        VoteDesc = voucherDetail.VoteDesc,
                        GeographicalLocationDesc = voucherDetail.GeographicalLocationDesc,
                        TrDesc = voucherDetail.TrDesc,
                        SubBudgetClass = pv.SubBudgetClass,
                        SubBudgetClassDesc = voucherDetail.SubBudgetClassDesc,
                        ProjectDesc = voucherDetail.ProjectDesc,
                        ServiceOutputDesc = voucherDetail.ServiceOutputDesc,
                        ActivityDesc = voucherDetail.ActivityDesc,
                        FundTypeDesc = voucherDetail.FundTypeDesc,
                        CofogDesc = voucherDetail.CofogDesc,
                        SubLevelCode = pv.SubLevelCode,
                        FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                        OperationalAmount = voucherDetail.OperationalAmount,
                        BaseAmount = voucherDetail.BaseAmount,
                        Currency = pv.OperationalCurrency,
                        CreatedAt = DateTime.Now,
                        CreatedBy = pv.CreatedBy,
                        ApplyDate = pv.ApplyDate,
                        PayeeCode = pv.PayeeCode,
                        PayeeName = pv.Payeename,
                        TransactionDesc = pv.PaymentDesc,
                        TR = voucherDetail.TR,
                        Facility = voucherDetail.Facility,
                        FacilityDesc = voucherDetail.FacilityDesc,
                        SourceModuleRefNo = pv.SourceModuleReferenceNo,
                        CostCentre = voucherDetail.CostCentre,
                        CostCentreDesc = voucherDetail.CostCentreDesc,
                        Level1Code = voucherDetail.Level1Code,
                        InstitutionLevel = voucherDetail.InstitutionLevel,
                        Level1Desc = voucherDetail.Level1Desc,
                        SubVote = voucherDetail.SubVote,
                        SubVoteDesc = voucherDetail.SubVoteDesc,
                        FundingSourceDesc = voucherDetail.FundingSourceDesc
                    };
                    transactionLogVMs.Add(transactionLogVM);
                }
                response = fundBalanceServices.PostTransaction(transactionLogVMs);
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            return response;
        }

        public void ComputeNetPayable(int Id)
        {
            try
            {
                var pv = db.PaymentVouchers.Find(Id);
                var NetOperationalAmount = pv.OperationalAmount;
                if (pv.hasWithHolding)
                {
                    NetOperationalAmount -= (pv.OperationalWithHoldingAmount ?? 0);
                }
                NetOperationalAmount -= (pv.LiquidatedDemageAmount ?? 0);
                NetOperationalAmount -= (pv.RetentionAmount ?? 0);
                NetOperationalAmount -= (pv.AdvancePayment ?? 0);
                NetOperationalAmount -= (pv.MiscDeduction ?? 0);
                pv.NetOperationalAmount = NetOperationalAmount;
                pv.NetBaseAmount = NetOperationalAmount;
                db.SaveChanges();
            }
            catch (Exception ex) { }
        }

        public string checkForValidAndNullParams(PaymentVoucher pv)
        {

            if (pv.PayeeType == null)
            {
                return "PayeeType cannot be null.\n Please contact system admin.!";
            }

            if (pv.PayeeBankAccount == null)
            {
                return "Payee bank account cannot be null.\n Please contact system admin.!";
            }

            if (pv.PayeeBIC == null)
            {
                return "PayeeBIC cannot be null.\nPlease contact system admin.!";
            }

            if (pv.PayeeCode == null)
            {
                return "PayeeCode cannot be null.\nPlease contact system admin.!";
            }

            if (pv.PVNo == null)
            {
                return "Sorry.! This voucher does not have PVNo.\nPlease cancell and re-create again.!";
            }

            if (pv.PayeeDetailId == null)
            {
                return "Sorry.! PayeeDetailsId is missing Please contact system admin.!";
            }
            return "Success";
        }
        [HttpGet, Authorize(Roles = "Voucher Examination")]
        public ActionResult ExamineVoucher()
        {
            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            return View();
        }


        [HttpPost, Authorize(Roles = "Voucher Examination")]
        //[ValidateAntiForgeryToken]
        public ActionResult ExamineVoucher(int paymentVoucherId)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";

            db.Database.CommandTimeout = 2600;
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(paymentVoucherId);
                    if (paymentVoucher == null)
                    {
                        response = "Payment Voucher Not Found!";
                        return Content(response);
                    }

                    //if(paymentVoucher.hasWithHolding)
                    //{
                    //    decimal withHoldingAmount = (decimal)paymentVoucher.GoodsAmount * (decimal)0.02 + (decimal) paymentVoucher.ServiceAmount * (decimal)0.05;
                    //    paymentVoucher.NetOperationalAmount = paymentVoucher.OperationalAmount - withHoldingAmount;
                    //    paymentVoucher.NetBaseAmount = paymentVoucher.OperationalAmount - withHoldingAmount;
                    //}


                    paymentVoucher.ExaminedBy = User.Identity.Name;
                    paymentVoucher.ExaminedAt = DateTime.Now;
                    paymentVoucher.OverallStatus = "Examined";
                    paymentVoucher.ExaminationStatus = "Examined";

                    if (!paymentVoucher.hasWithHolding)
                    {
                        paymentVoucher.NetOperationalAmount = paymentVoucher.OperationalAmount;
                        paymentVoucher.NetBaseAmount = paymentVoucher.BaseAmount;
                    }

                    if (paymentVoucher.SourceModule == "Imprest")
                    {
                        var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                        if (imprest != null)
                        {
                            imprest.VoucherStatus = "Examined";
                        }
                    }

                    db.SaveChanges();

                    response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);
                    if (response == "Success")
                    {

                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    Log.Information(ex + "{Name}!", "ErrorOnPaymentExamination");
                    response = ex.InnerException.ToString();
                }
            }

            return Content(response);
        }

        [HttpGet, Authorize(Roles = "Voucher Approval")]
        public ActionResult ApproveVoucher()
        {
            return View();
        }

        [HttpPost, Authorize(Roles = "Voucher Approval")]
        //[ValidateAntiForgeryToken]
        public ActionResult ApproveVoucher(int paymentVoucherId)
        {
            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());
            db.Database.CommandTimeout = 1200;
            string response = "Success";
          //  using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
           // {
                try
                {
                    PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(paymentVoucherId);
                var cuttOffStatus = serviceManager.GetCutOffStatus(paymentVoucher.ApplyDate, userPaystation.InstitutionCode);
                if (cuttOffStatus != "success")
                {
                    return Content(cuttOffStatus);
                }
                if (paymentVoucher != null)
                    {
                        string userId = User.Identity.GetUserId();
                        var approvalPaystion = db.UserPayStations
                            .Where(a => a.UserId == userId
                             && a.IsDefault == true)
                            .FirstOrDefault();
                        if (approvalPaystion == null)
                        {
                            response = "User paystation setup incomplete. Please consult System Administrator";
                            return Content(response);
                        }

                        var approvalLevel = db.InstitutionApprovalLevels
                            .Where(a => a.InstitutionApprovalLevelId == approvalPaystion.InstitutionApprovalLevelId
                             && a.OverallStatus != "Cancelled")
                            .FirstOrDefault();
                        if (approvalLevel == null)
                        {
                            response = "Approval level setup is incomplete. Please consult System Administrator";
                            return Content(response);
                        }

                        if (paymentVoucher.OperationalAmount > approvalLevel.MaxAmount)
                        {
                            response = "You are not authorized to approve payments of this amount!";
                            return Content(response);
                        }
                        paymentVoucher.ApprovedBy = User.Identity.Name;
                        paymentVoucher.ApprovedAt = DateTime.Now;
                        paymentVoucher.OverallStatus = "Approved";
                        paymentVoucher.ApprovalStatus = "Approved";
                        if (paymentVoucher.SourceModule.Contains("Accrual"))
                        {
                            if (paymentVoucher.IsAccrualPayed)
                            {
                                paymentVoucher.OverallStatus = "Approved";
                                paymentVoucher.ApprovalStatus = "Approved";
                            }
                            else
                            {
                                paymentVoucher.OverallStatus = "Approved - Waiting for Payment";
                                paymentVoucher.ApprovalStatus = "Approved - Waiting for Payment";
                            }
                        }
                        if (paymentVoucher.SourceModule == "Imprest")
                        {
                            var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                            if (imprest != null)
                            {
                                imprest.VoucherStatus = "Approved";
                            }
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        response = "Payment Voucher Not Found!";
                    }
                    response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);
                    if (response == "Success")
                    {
                        var parameters = new SqlParameter[] {
                             new SqlParameter("@LegalNumber", paymentVoucher.PVNo) };

                        db.Database.ExecuteSqlCommand("dbo.GlPostPv_p @LegalNumber", parameters);

                    //    scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    //response = ex.InnerException.ToString();
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    Log.Information(ex + "{Name}!", "ErrorOnPaymentApproval");
                }
       //     }
            return Content(response);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult RejectVoucher(RejectReasonVM rejectReasonVM)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            db.Database.CommandTimeout = 280000;
            //using (TransactionScope scope = new TransactionScope())
            //{
            try
            {
                PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(rejectReasonVM.Id);
                if (paymentVoucher != null)
                {
                    paymentVoucher.RejectedBy = User.Identity.Name;
                    paymentVoucher.RejectedAt = DateTime.Now;
                    paymentVoucher.RejectedReason = rejectReasonVM.Remark;


                    paymentVoucher.OverallStatusDesc = rejectReasonVM.Remark;

                    if (paymentVoucher.OverallStatus == "Pending"
                        || paymentVoucher.OverallStatus == "Rejected By Examiner"
                        || paymentVoucher.OverallStatus == "Approved - Waiting for Payment")
                    {
                        if (paymentVoucher.SourceModule == "Contract"
                              || paymentVoucher.SourceModule == "Purchase"
                              || paymentVoucher.SourceModule == "Accrual Purchase"
                              || paymentVoucher.SourceModule == "Accrual Contract"
                              || paymentVoucher.SourceModule == "Advance Payment")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            paymentVoucher.CancelledAt = DateTime.Now;
                            paymentVoucher.CancelledBy = User.Identity.GetUserName();
                            ReceivingSummary summary = db.ReceivingSummarys.Find(paymentVoucher.OtherSourceId);

                            if (summary.HasRetention)
                            {

                                if (summary.RetentionBy == "Accrual")
                                {
                                    RetentionPayment retentionPayment = db.RetentionPayments.Where(a => a.ReceivingSummaryId == summary.ReceivingSummaryId && a.Accrual == "Yes").FirstOrDefault();

                                    if (retentionPayment != null)
                                    {
                                        if (retentionPayment.OverallStatus == "Approved")
                                        {
                                            response = "Can not reject since its corresponding Accrual Retention of this voucher arleady approved";
                                            return Content(response);
                                        }
                                        else
                                        {
                                            retentionPayment.OverallStatus = "Incomplete";
                                            response = fundBalanceServices.CancelTransaction(retentionPayment.LegalNumber, retentionPayment.RetentionPaymentId, User.Identity.Name);
                                        }
                                    }
                                }
                                else
                                {
                                    FundTransferSummary fundTransferSummary = db.FundTransferSummaries.Where(a => a.OtherSourceId == paymentVoucher.PaymentVoucherId && a.SourceModule == "Retention Transfer").FirstOrDefault();
                                    if (fundTransferSummary != null)
                                    {
                                        if (fundTransferSummary.OverallStatus == "PROCESSED")
                                        {

                                            response = "Can not reject since its corresponding Fund Transfer of this voucher arleady processed";
                                            return Content(response);
                                        }
                                        else
                                        {
                                            fundTransferSummary.OverallStatus = "Cancelled";
                                            response = fundBalanceServices.CancelTransaction(fundTransferSummary.TransferRefNum, fundTransferSummary.FundTransferSummaryId, User.Identity.Name);

                                        }
                                    }
                                }


                            }

                            summary.OverallStatus = "RejectedByPO";
                            summary.RejectionReason = rejectReasonVM.Remark;
                            summary.Rejecter = "Payment Office";
                            summary.RejectedBy = User.Identity.Name;
                            summary.RejectedAt = DateTime.Now;
                            if (paymentVoucher.SourceModule == "Contract" || paymentVoucher.SourceModule == "Accrual Contract")
                            {
                                response = fundBalanceServices.UpdateTransaction(summary.ReceivingNumber, summary.ReceivingSummaryId, summary.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);
                            var parameters1 = new SqlParameter[] { new SqlParameter("@LegalNumber", summary.ReceivingNumber) };
                            db.Database.ExecuteSqlCommand("dbo.GlPostReversed_p @LegalNumber", parameters1);

                        }

                        if (paymentVoucher.SourceModule == "Retention")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var retention = db.RetentionPayments.Find(paymentVoucher.OtherSourceId);
                            if (retention != null)
                            {
                                retention.RejectionReason = rejectReasonVM.Remark;
                                retention.OverallStatus = "Rejected from Payment Voucher";
                                response = fundBalanceServices.UpdateTransaction(retention.LegalNumber, retention.RetentionPaymentId, retention.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                            var parameters1 = new SqlParameter[] { new SqlParameter("@LegalNumber", retention.LegalNumber) };
                            db.Database.ExecuteSqlCommand("dbo.GlPostReversed_p @LegalNumber", parameters1);
                        }
                        if (paymentVoucher.SourceModule == "Imprest")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                            if (imprest != null)
                            {
                                imprest.VoucherStatus = "Rejected from Payment Voucher";
                                imprest.OverallStatus = "Rejected";
                                imprest.Remark = rejectReasonVM.Remark;
                                response = fundBalanceServices.UpdateTransaction(imprest.ImprestNo, imprest.ImprestId, imprest.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);
                            //var param = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                            //db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", param);
                            var param = new SqlParameter[] { new SqlParameter("@LegalNumber", imprest.ImprestNo) };
                            db.Database.ExecuteSqlCommand("dbo.GlPostReversed_p @LegalNumber", param);
                        }

                        if (paymentVoucher.SourceModule == Libraries.Constants.SOURCE_MODULE_PREPAYMENT)
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var prepayment = db.PrePayments.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                            if (prepayment != null)
                            {
                                prepayment.OverallStatus = Libraries.Constants.CANCELLED_IN_PAYMENT_VOUCHER;
                                response = fundBalanceServices.UpdateTransaction(prepayment.PrePaymentNo, prepayment.PrePaymentId, prepayment.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);
                            var parameter = new SqlParameter[] { new SqlParameter("@PrePaymentNo", prepayment.PrePaymentNo) };
                            db.Database.ExecuteSqlCommand("dbo.GlPostReversed_p @PrePaymentNo", parameter);
                        }

                        if (paymentVoucher.SourceModule == "Loan")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";

                            var loan = db.Loans.Where(a => a.PaymentVoucherId == paymentVoucher.PaymentVoucherId).FirstOrDefault();
                            if (loan != null)
                            {
                                var getLoan = db.Loans.Find(loan.LoanId);
                                getLoan.OverAllStatus = Libraries.Constants.CANCELLED_IN_PAYMENT_VOUCHER;
                                response = fundBalanceServices.UpdateTransaction(getLoan.LoanNo, getLoan.LoanId, getLoan.OverAllStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                        }

                        if (paymentVoucher.SourceModule == "Unapplied")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var model = db.Unapplieds
                                .Where(a => a.EndToEndId == paymentVoucher.SourceModuleReferenceNo)
                                .FirstOrDefault();
                            if (model != null)
                            {
                                model.OverallStatus = "Cancelled";
                                model.CancelledAt = DateTime.Now;
                                model.CancelledBy = User.Identity.GetUserName();
                                response = fundBalanceServices.UpdateTransaction(model.EndToEndId, model.UnappliedId, model.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                        }

                        if (paymentVoucher.SourceModule == "AGTIF")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var model = db.LoanPaymentVouchers
                                .Where(a => a.PaymentVoucherId == paymentVoucher.OtherSourceId)
                                .FirstOrDefault();
                            if (model != null)
                            {
                                model.OverallStatus = "Rejected from Payment Voucher";
                                model.CancelledAt = DateTime.Now;
                                model.CancelledBy = User.Identity.GetUserName();
                                //response = fundBalanceServices.UpdateTransaction(model.loanleagalnumba, model.loanId, model.OverallStatus);
                            }
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                        }

                        if (paymentVoucher.SourceModule == "BulkPayment")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var model = db.PaymentBatches
                                .Where(a => a.BatchNo == paymentVoucher.SourceModuleReferenceNo)
                                .FirstOrDefault();
                            if (model != null)
                            {
                                model.OverallStatus = "Rejected in Payment Voucher";
                                model.CancelledAt = DateTime.Now;
                                model.CancelledBy = User.Identity.GetUserName();
                                model.PVNo = null;

                                var batchDetail = db.BulkPayments
                                    .Where(a => a.PaymentBatchID == model.PaymentBatchID)
                                    .ToList();

                                batchDetail.ForEach(item =>
                                {
                                    item.OverallStatus = "Rejected in Payment Voucher";
                                });
                            }
                            response = fundBalanceServices.UpdateTransaction(model.BatchNo, model.PaymentBatchID, model.OverallStatus);
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                        }

                        if (paymentVoucher.SourceModule == "PSAF")
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            var loanPV = db.LoanPaymentVouchers
                                .Find(int.Parse(paymentVoucher.SourceModuleReferenceNo));
                            loanPV.OverallStatus = "Rejected from Payment Voucher";
                            loanPV.RejectedReason = rejectReasonVM.Remark;
                            response = fundBalanceServices.CancelTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, User.Identity.Name);

                        }

                        if (paymentVoucher.SourceModule == Libraries.Constants.SOURCE_MODULE_PREPAYMENT)
                        {
                            var prepayment = db.PrePayments.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                            if (prepayment != null)
                            {
                                prepayment.OverallStatus = Libraries.Constants.CANCELLED_IN_PAYMENT_VOUCHER;
                                response = fundBalanceServices.UpdateTransaction(prepayment.PrePaymentNo, prepayment.PrePaymentId, prepayment.OverallStatus);
                            }
                        }
                        var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                        db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", parameters);
                    }
                    if (paymentVoucher.SourceModule == "External" && paymentVoucher.InstitutionCode == "00220000")
                    {
                        paymentVoucher.OverallStatus = "Cancelled";
                        ServiceManager.CancellTPPSVoucher(paymentVoucher.SourceModuleReferenceNo);
                    }
                    if (paymentVoucher.OverallStatus == "Confirmed"
                       || paymentVoucher.OverallStatus == "Rejected"
                       && paymentVoucher.ExaminationStatus != "NA")
                    {
                        paymentVoucher.OverallStatus = "Rejected By Examiner";

                        if (paymentVoucher.hasWithHolding)
                        {
                            var wd = db.WithHoldingDetails
                             .Where(a => a.PaymentVoucherId ==
                              paymentVoucher.PaymentVoucherId
                              && a.OverallStatus != "cancelled")
                             .FirstOrDefault();
                            if (wd != null)
                            {
                                db.WithHoldingDetails.Remove(wd);
                            }

                        }
                        response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);

                    }

                    if (paymentVoucher.OverallStatus == "Examined"
                        || paymentVoucher.OverallStatus == "Rejected in Voucher Generation"
                        || paymentVoucher.ExaminationStatus == "NA")
                    {
                        paymentVoucher.OverallStatus = "Rejected";
                        response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);
                    }

                    if (paymentVoucher.OverallStatus == "Approved")
                    {
                        if (paymentVoucher.ExaminedBy == "NA" && paymentVoucher.ExaminationStatus == "NA")
                        {
                            paymentVoucher.OverallStatus = "Rejected";
                        }
                        else
                        {
                            paymentVoucher.OverallStatus = "Rejected in Voucher Generation";
                        }

                        var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                        db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", parameters);

                        response = fundBalanceServices.UpdateTransaction(
                            paymentVoucher.PVNo,
                            paymentVoucher.PaymentVoucherId,
                            paymentVoucher.OverallStatus);
                    }

                    if (User.IsInRole("Voucher Examination"))
                    {
                        paymentVoucher.ExaminationStatus = "Rejected";
                        response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);

                    }
                    else if (User.IsInRole("Voucher Approval"))
                    {
                        paymentVoucher.ApprovalStatus = "Rejected";
                        response = fundBalanceServices.UpdateTransaction(paymentVoucher.PVNo, paymentVoucher.PaymentVoucherId, paymentVoucher.OverallStatus);

                    }
                    db.SaveChanges();

                    //if (paymentVoucher.SourceModule == "External")
                    //{
                    //    VoucherAPIServices.SendRejectionStatus(new {
                    //        receiver = paymentVoucher.InstitutionName,
                    //        status = paymentVoucher.OverallStatus,
                    //        statusDesc = paymentVoucher.OverallStatusDesc,
                    //        PVNo = paymentVoucher.PVNo
                    //    });
                    //}
                }
                else
                {
                    response = "Payment Voucher Not Found!";
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex + "{Name}!", "ErrorOnPaymentRejection");
                response = ex.InnerException.ToString();
            }
            return Content(response);
        }

        [HttpGet, Authorize(Roles = "Payment Generation")]
        public ActionResult GeneratePayment()
        {
            InstitutionSubLevel userPaystation = serviceManager
           .GetUserPayStation(User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && a.SubBudgetClass != null)
                .OrderBy(a => a.SubBudgetClass)
                .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            return View();
        }

        [HttpPost, Authorize(Roles = "Payment Generation")]
        //[ValidateAntiForgeryToken]
        public ActionResult GeneratePayment(List<int> paymentVoucherIds)
        {
            db.Database.CommandTimeout = 24000;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";

            //using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            //{
            try
            {
                List<PaymentVoucher> paymentVoucherList = db.PaymentVouchers
                    .Where(a => paymentVoucherIds.Contains(a.PaymentVoucherId)
                      && a.OverallStatus == "Approved")
                    .ToList();

                int firstPaymentVoucherId = paymentVoucherList[0].PaymentVoucherId;
                VoucherDetail voucherDetail = db.VoucherDetails
                    .Where(a => a.PaymentVoucherId == firstPaymentVoucherId)
                    .FirstOrDefault();

                foreach (PaymentVoucher paymentVoucher in paymentVoucherList)
                {
                    CreateDeduction(paymentVoucher.PaymentVoucherId);
                    ComputeNetPayable(paymentVoucher.PaymentVoucherId);
                    if (paymentVoucher.MiscDeductionPayeeDetailsId != null
                        && paymentVoucher.MiscDeductionPayeeName != null)
                    {
                        var miscResponse = CreateMiscDeducionVoucher(paymentVoucher.PVNo);
                        if (miscResponse != "Success")
                        {
                            return Content(miscResponse + ":PVNO " + paymentVoucher.PVNo);
                        }
                    }
                }

                decimal OperationalAmount = (decimal)paymentVoucherList.Sum(a => a.NetOperationalAmount);
                decimal BaseAmount = (decimal)paymentVoucherList.Sum(a => a.NetBaseAmount);

                var account = paymentVoucherList[0].PayerBankAccount;
                Account institutionAccount = db.Accounts
                    .Where(a => a.Status == "ACTIVE" && a.AccountNo == account)
                    .FirstOrDefault();

                var PayerBankBic = "";
                if (institutionAccount == null)
                {
                    PayerBankBic = "TANZTZTX";
                }
                else
                {
                    PayerBankBic = institutionAccount.BankBIC;
                }

                PaymentSummary paymentSummary = new PaymentSummary
                {
                    NumTrx = paymentVoucherList.Count(),
                    JournalTypeCode = "PD",
                    PayerBankAccount = paymentVoucherList[0].PayerBankAccount,
                    SubBudgetClass = paymentVoucherList[0].SubBudgetClass,
                    PayerBankName = paymentVoucherList[0].PayerBankName,
                    PayerBankBic = PayerBankBic,
                    PaymentMethod = paymentVoucherList[0].PaymentMethod,
                    CreatedBy = paymentVoucherList[0].ExaminedBy,
                    CreatedAt = DateTime.Now,
                    PaymentOfficeStatus = "Pending",
                    OverallStatus = "Pending",
                    Currency = paymentVoucherList[0].OperationalCurrency,
                    OperationalAmount = Math.Round(OperationalAmount, 2),
                    BaseAmount = Math.Round(BaseAmount, 2),
                    FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                    InstitutionId = userPaystation.InstitutionId,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    PaystationId = userPaystation.InstitutionSubLevelId,
                    DrGLAccount = voucherDetail.CrGlAccount,
                    CrGLAccount = paymentVoucherList[0].PayerCashAccount,
                    PayerAccountType = paymentVoucherList[0].PayerAccountType,
                    SubTsaBankAccount = paymentVoucherList[0].SubTsaBankAccount,
                    SubTsaCashAccount = paymentVoucherList[0].SubTsaCashAccount,
                    PaymentNo = institutionAccount.InstitutionCode + ":" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    GeneralLedgerStatus = "Pending"
                };
                db.PaymentSummaries.Add(paymentSummary);

                db.SaveChanges();
                int paymentSummaryId = paymentSummary.PaymentSummaryId;

                paymentSummary.PaymentNo = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, "D", paymentSummaryId);

                if (paymentSummary.PaymentNo.Contains("Error") || paymentSummary.PaymentNo == null)
                {
                    paymentSummary.OverallStatus = "Cancelled";
                    paymentSummary.OverallStatusDesc = "Cancelled due to null PaymentNo";
                    db.SaveChanges();
                    return Content("Payment Voucher Could not be generated. Please try again.! Error Code: 1122");
                }

                foreach (PaymentVoucher paymentVoucher in paymentVoucherList)
                {
                    paymentVoucher.OverallStatus = "Generated";
                    paymentVoucher.PaymentSummaryId = paymentSummaryId;
                    paymentVoucher.PaymentSummaryNo = paymentSummary.PaymentNo;

                    if (paymentVoucher.SourceModule == "Imprest")
                    {
                        var imprest = db.Imprests.Where(a => a.PVNo == paymentVoucher.PVNo).FirstOrDefault();
                        if (imprest != null)
                        {
                            imprest.VoucherStatus = "Generated";
                        }
                    }
                    if (paymentVoucher.SourceModule == "Contract" || paymentVoucher.SourceModule == "Accrual Contract")
                    {
                        manageRetentionServices.UpdateRetention(paymentVoucher);
                    }
                    db.SaveChanges();
                }
                var pvSummary = db.PaymentSummaries.Find(paymentSummary.PaymentSummaryId);
                if (pvSummary != null)
                {
                    var vouchers = db.PaymentVouchers.Where(a => a.PaymentSummaryId == pvSummary.PaymentSummaryId).Count();
                    if (vouchers == 0)
                    {
                        db.PaymentSummaries.Remove(pvSummary);
                    }
                }
                db.SaveChanges();

                //scope.Complete();
            }
            catch (Exception ex)
            {
                Log.Information(ex + "{Name}!", "ErrorOnPaymentGeneration");
                response = "An error occured while processing your request, please try again/ contact system support";
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            //}

            return Content(response);
        }



        private string CreateMiscDeductionVoucher(
            int voucherId,
            JournalTypeView journalTypeView
            )
        {
            try
            {
                //Fix Duplicates
                var _miscVoucher = db.PaymentVouchers
                     .Where(a => a.OtherSourceId == voucherId
                       && a.SourceModule == "MiscDeduction")
                     .FirstOrDefault();
                if (_miscVoucher != null)
                {
                    _miscVoucher.OverallStatus = "Cancelled";
                    _miscVoucher.OverallStatusDesc = "Cancelled due to rejection";
                    _miscVoucher.SourceModule = "_MiscDeduction";
                    _miscVoucher.CancelledAt = DateTime.Now;
                    _miscVoucher.CancelledBy = User.Identity.GetUserName();
                    db.SaveChanges();
                }


                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                var voucher = db.PaymentVouchers.Find(voucherId);
                var payeeDetail = db.PayeeDetails.Find(voucher.MiscDeductionPayeeDetailsId);
                var payee = db.Payees.Find(payeeDetail.PayeeId);

                var payerBank = db.InstitutionAccounts
                       .Where(a => a.SubBudgetClass == journalTypeView.SubBudgetClass
                         && a.InstitutionCode == userPaystation.InstitutionCode
                         && a.IsTSA == false
                         && a.OverallStatus != "Cancelled"
                       ).FirstOrDefault();

                if (payerBank == null)
                {
                    var response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + journalTypeView.SubBudgetClass + "'. Please consult Administrator!";
                    return response;
                }

                var payeeType = db.PayeeTypes
                    .Where(a => a.PayeeTypeCode == payeeDetail.PayeeType
                      && a.Status != "Cancelled")
                    .FirstOrDefault();

                if (payeeType == null)
                {
                    var response = "Vendor setup is incomplete. There is no payee type setup for '" + payeeDetail.PayeeType + "'. Please contact Administrator!";
                    return response;
                }

                var crCodes = db.JournalTypeViews
                    .Where(a => a.CrGfsCode == payeeType.GfsCode
                     && a.SubBudgetClass == journalTypeView.SubBudgetClass
                     && a.InstitutionCode == userPaystation.InstitutionCode)
                    .FirstOrDefault();
                if (crCodes == null)
                {
                    var response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + journalTypeView.SubBudgetClass + "' is missing. Please contact Administrator!";
                    return response;
                }

                var unappliedAccount = serviceManager.GetUnappliedAccount(
                           userPaystation.InstitutionCode,
                           voucher.SubBudgetClass
                           );

                if (unappliedAccount == null)
                {
                    var response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                    return response;
                }

                PaymentVoucher miscVoucher = new PaymentVoucher
                {
                    OtherSourceId = voucherId,
                    SourceModule = "MiscDeduction",
                    SourceModuleReferenceNo = "NA",
                    PayeeType = payeeDetail.PayeeType,
                    InvoiceNo = voucher.InvoiceNo,
                    InvoiceDate = voucher.InvoiceDate,
                    PayeeDetailId = payeeDetail.PayeeDetailId,
                    PayeeCode = payee.PayeeCode,
                    Payeename = payee.PayeeName,
                    PayeeBankAccount = payeeDetail.Accountnumber,
                    PayeeBankName = payeeDetail.BankName,
                    PayeeAccountName = payeeDetail.AccountName,
                    PayeeAddress = payee.Address1,
                    PayeeBIC = payeeDetail.BIC,
                    Narration = voucher.MiscDeductionDescription,
                    PaymentDesc = voucher.MiscDeductionDescription,
                    OperationalAmount = voucher.MiscDeduction,
                    BaseAmount = voucher.MiscDeduction,
                    BaseCurrency = "TZS",
                    OperationalCurrency = "TZS",
                    ExchangeRate = 1,
                    ApplyDate = voucher.ApplyDate,
                    PaymentMethod = voucher.PaymentMethod,
                    FinancialYear = serviceManager.GetFinancialYear(voucher.ApplyDate),
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    OverallStatus = "Waiting for MiscDeduction Update",
                    //OverallStatus = "Pending",
                    Book = "MAIN",
                    InstitutionId = userPaystation.InstitutionId,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    PaystationId = userPaystation.InstitutionSubLevelId,
                    SubLevelCategory = userPaystation.SubLevelCategory,
                    SubLevelCode = userPaystation.SubLevelCode,
                    SubLevelDesc = userPaystation.SubLevelDesc,
                    SubBudgetClass = journalTypeView.SubBudgetClass,
                    JournalTypeCode = "PV",
                    InstitutionAccountId = payerBank.InstitutionAccountId,
                    PayerBankAccount = payerBank.AccountNumber,
                    PayerBankName = payerBank.AccountName,
                    PayerBIC = payerBank.BIC,
                    PayerCashAccount = payerBank.GlAccount,
                    PayableGlAccount = crCodes.CrCoa,
                    UnappliedAccount = unappliedAccount.AccountNumber,
                    PayerAccountType = payerBank.AccountType,
                    IsAccrualPayed = false,

                    //Sub TSA
                    SubTsaBankAccount = payerBank.SubTSAAccountNumber,
                    SubTsaCashAccount = payerBank.SubTSAGlAccount,
                };

                db.PaymentVouchers.Add(miscVoucher);
                db.SaveChanges();

                miscVoucher.PVNo = serviceManager
               .GetLegalNumber(userPaystation.InstitutionCode, "V", miscVoucher.PaymentVoucherId);


                if (miscVoucher.PVNo.Contains("Error") || miscVoucher.PVNo == null)
                {
                    miscVoucher.OverallStatus = "Cancelled";
                    miscVoucher.OverallStatusDesc = "Cancelled due to null PVNo";
                    miscVoucher.CancelledAt = DateTime.Now;
                    miscVoucher.CancelledBy = "System";
                    return "Voucher Confirmation failed Please try again. Error code: 1122";
                }

                VoucherDetail voucherDetail = new VoucherDetail
                {
                    PaymentVoucherId = miscVoucher.PaymentVoucherId,
                    JournalTypeCode = "PV",
                    DrGlAccount = journalTypeView.DrCoa,
                    DrGlAccountDesc = journalTypeView.DrCoaDesc,
                    CrGlAccount = crCodes.CrCoa,
                    CrGlAccountDesc = crCodes.CrCoaDesc,
                    FundingReferenceNo = voucher.PVNo,
                    OperationalAmount = miscVoucher.OperationalAmount,
                    BaseAmount = miscVoucher.OperationalAmount,
                };
                db.VoucherDetails.Add(voucherDetail);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            return "Success";
        }
        [HttpGet, Authorize(Roles = "Payment Approval")]
        public ActionResult ApprovePayment()
        {
            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());
            //ViewBag.UserRole = User.IsInRole("PaymentSlip Submission") ? "PaymentSlip Submission" : "Payment Approval";
            ViewBag.UserRole = "Payment Approval";
            var institutionConfig = db.InstitutionConfigs
                .Where(a => a.InstitutionId == userPaystation.InstitutionId
                    && a.ConfigName == "RequirePaymentAttachment"
                    // && a.ConfigFlag == true
                    && a.OverallStatus.ToUpper() != "CANCELLED")
                   .ToList()
                   .Count();

            ViewBag.IsAttachmentExempted = institutionConfig > 0;

            ViewBag.reportUrlName = ReportManager.GetReportUrl(db, "IFMISTZ");
            return View();
        }


        [HttpGet, Authorize(Roles = "PaymentSlip Submission")]
        public ActionResult PaymentSubmission()
        {
            InstitutionSubLevel userPaystation = serviceManager
                .GetUserPayStation(User.Identity.GetUserId());
            ViewBag.UserRole = "PaymentSlip Submission";
            var institutionConfig = db.InstitutionConfigs
                .Where(a => a.InstitutionId == userPaystation.InstitutionId
                    && a.ConfigName == "RequirePaymentAttachment"
                    // && a.ConfigFlag == true
                    && a.OverallStatus.ToUpper() != "CANCELLED")
                   .ToList()
                   .Count();

            ViewBag.IsAttachmentExempted = institutionConfig > 0;
            ViewBag.reportUrlName = ReportManager.GetReportUrl(db, "IFMISTZ");
            return View();
        }
        [HttpPost, Authorize(Roles = "Payment Approval")]
        //[ValidateAntiForgeryToken]
        public ActionResult ApprovePayment(int paymentSummaryId)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            db.Database.CommandTimeout = 2600;
            try
            {
                PaymentSummary paymentSummary = db.PaymentSummaries.Find(paymentSummaryId);
                if (paymentSummary == null)
                {
                    response = "Payment Summary Not Found!";
                    return Content(response);
                }
                paymentSummary.ApprovedBy = User.Identity.Name;
                paymentSummary.ApprovedAt = DateTime.Now;
                paymentSummary.OverallStatus = "Approved";
                paymentSummary.ApprovalStatus = "Approved";
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
            }
            return Content(response);
        }

        public ActionResult SendToPO(int paymentSummaryId)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            try
            {
                PaymentSummary paymentSummary = db.PaymentSummaries.Find(paymentSummaryId);
                if (paymentSummary == null)
                {
                    response = "Payment Summary Not Found!";
                    return Content(response);
                }
                paymentSummary.OverallStatus = "Sent To PO";
                paymentSummary.ApprovalStatus = "Sent To PO";
                paymentSummary.SentToPOAt = DateTime.Now;
                paymentSummary.SentToPOBy = User.Identity.GetUserName();

                if (paymentSummary.PaymentSlipFilePath == null)
                {
                    paymentSummary.PaymentSlipFilePath = "NA";
                }

                db.SaveChanges();

            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
            }

            return Content(response);
        }
        [HttpPost, AuditIgnore]
        //[ValidateAntiForgeryToken]
        public ActionResult AttachSlip(AttachSlipVm attachSlipVm)
        {
            var response = "Success";
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            if (attachSlipVm.file != null && attachSlipVm.file.ContentLength > 0)
                try
                {
                    PaymentSummary paymentSummary = db.PaymentSummaries.Find(attachSlipVm.paymentSummaryId);
                    if (paymentSummary == null)
                    {
                        response = "Invalid Payment Summary Reference. Please contact System Administrator!";
                        return Content(response);
                    }
                    string fileName = paymentSummary.PaymentNo + DateTime.Now.ToString("_yyyyMMddHHmmss") + ".pdf";
                    string path = "~/Content/uploads";
                    if (!Directory.Exists(Server.MapPath(path)))
                    {
                        Directory.CreateDirectory(Server.MapPath(path));
                    }
                    string paymentSlipFilePath = Path.Combine(Server.MapPath(path), fileName);
                    paymentSummary.PaymentSlipFilePath = fileName;
                    paymentSummary.OverallStatus = "Slip Attached";
                    paymentSummary.FileAttachedAt = DateTime.Now;
                    paymentSummary.FileAttachedBy = User.Identity.GetUserName();
                    attachSlipVm.file.SaveAs(paymentSlipFilePath);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                }
            else
            {
                response = "You have not specified a file.";
            }
            return Content(response);
        }

        public JsonResult GetPaymentFile(string overallStatus = "Pending")
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<PaymentFile> list;
            if (overallStatus == "PaySlipAttached")
            {
                list = db.PaymentFiles.AsNoTracking()
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                          && a.OverallStatus == "Approved"
                          && a.PaymentSlipFilePath != null)
                    .OrderByDescending(a => a.PaymentFileId)
                    .ToList();
            }
            else
            {
                list = db.PaymentFiles.AsNoTracking()
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                           && (a.PaymentSlipFilePath == null
                            && a.OverallStatus != "Cancelled"
                           || a.PaymentSlipFilePath == ""))
                           .OrderByDescending(a => a.PaymentFileId)
                    .ToList();
            }


            return Json(new { data = list.Where(a => a.OverallStatus != "Cancelled").ToList() }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPaymentSummary(string overallStatus = "Pending")
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<PaymentSummary> list = new List<PaymentSummary> { };
            //List<PaymentSummary> list2;
			db.Database.CommandTimeout = 1200;
            if (overallStatus == "PaySlipAttached")
            {
                list = db.PaymentSummaries.AsNoTracking()
                    .Where(a => a.OverallStatus != "Rejected"
                      && a.OverallStatus != "Cancelled"
                      && a.OverallStatus != "PROCESSED"
                      && a.OverallStatus != "ChequeApproved"
                    )
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                         && a.OverallStatus == "Approved"
                         && a.PaymentSlipFilePath != null)
                    .OrderByDescending(a => a.PaymentSummaryId)
                    .ToList();
            }
            else
            {
                list = db.PaymentSummaries.AsNoTracking()
                  .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                  .Where(a => 
                      a.OverallStatus != "Rejected"
                    && a.OverallStatus != "Cancelled"
                    && a.OverallStatus != "PROCESSED"
                    && a.OverallStatus != "ChequeApproved")
                  .Where(a => string.IsNullOrEmpty(a.PaymentSlipFilePath) ||
                          (a.OverallStatus == "Rejected in Payment Office" ||
                          a.OverallStatus == "Slip Attached")
                         )
                  .OrderByDescending(a => a.PaymentSummaryId)
                  .ToList();
                //foreach (var item in list2)
                //{
                //    if (item.PaymentNo.Contains(":"))
                //    {
                //        item.PaymentNo = item.InstitutionCode + "D" + item.PaymentSummaryId;
                //        db.SaveChanges();
                //    }
                //    var vouchers = db.PaymentVouchers.Where(a => a.PaymentSummaryId == item.PaymentSummaryId).Count();
                //    if (vouchers > 0)
                //    {
                //        list.Add(item);
                //    }
                //}
            }
            //var data = _FixPaymentSummaryDeduction(list);
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AdvancedEdit(int paymentId)
        {
            InstitutionSubLevel userPaystation = serviceManager
              .GetUserPayStation(User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                  && a.SubBudgetClass != null)
                .OrderBy(a => a.SubBudgetClass)
                .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            ViewBag.PayeeTypesList = db.PayeeTypes.ToList();

            var paymentVoucher = db.PaymentVouchers.Find(paymentId);
            ViewBag.paymentVoucher = paymentVoucher;
            List<VoucherDetail> voucherDetails = paymentVoucher.VoucherDetails.ToList();
            ViewBag.VoucherDetails = voucherDetails;
            ViewBag.paymentId = paymentId;
            ViewBag.subBudgetClass = subBudgetClassList
                .Where(a => a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                .First();

            return View();
        }

        public JsonResult GetVoucherDetails(int id)
        {
            var list = db.VoucherDetails.AsNoTracking()
                .Where(a => a.PaymentVoucherId == id);
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Voucher Entry")]
        //[ValidateAntiForgeryToken]
        public ActionResult UpdateVoucher(PaymentVoucherVM paymentVoucher, int voucherId)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            string response = "Success";
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    var payerBank = db.InstitutionAccounts
                        .Where(a => a.SubBudgetClass == paymentVoucher.SubBudgetClass
                          && a.InstitutionCode == userPaystation.InstitutionCode
                          && a.IsTSA == false
                          && a.OverallStatus != "Cancelled"
                        ).FirstOrDefault();
                    if (payerBank == null)
                    {
                        response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentVoucher.SubBudgetClass + "'. Please consult Administrator!";
                        return Content(response);
                    }
                    var payeeType = db.PayeeTypes
                        .Where(a => a.PayeeTypeCode == paymentVoucher.PayeeType
                          && a.Status != "Cancelled")
                        .FirstOrDefault();

                    if (payeeType == null)
                    {
                        response = "Vendor setup is incomplete. There is no payee type setup for '" + paymentVoucher.PayeeType + "'. Please contact Administrator!";
                        return Content(response);
                    }
                    // var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == paymentVoucher.SubBudgetClass && a.SublevelCode == userPaystation.SubLevelCode && a.SubLevelCategory == userPaystation.SubLevelCategory).FirstOrDefault();
                    var crCodes = db.JournalTypeViews
                        .Where(a => a.CrGfsCode == payeeType.GfsCode
                         && a.SubBudgetClass == paymentVoucher.SubBudgetClass
                         && a.InstitutionCode == userPaystation.InstitutionCode)
                        .FirstOrDefault();
                    if (crCodes == null)
                    {
                        response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentVoucher.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return Content(response);
                    }
                    var unappliedAccount = serviceManager.GetUnappliedAccount(
                                       userPaystation.InstitutionCode,
                                       paymentVoucher.SubBudgetClass
                                       );

                    if (unappliedAccount == null)
                    {
                        response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'"
                            + userPaystation.Institution.InstitutionName
                            + "'. Please consult Administrator!";
                        return Content(response);
                    }

                    if (paymentVoucher.InvoiceDate > DateTime.Now)
                    {
                        //response = "Invalid Apply Date.";
                        // return Content(response);
                    }

                    if (paymentVoucher.hasWithHolding)
                    {
                        var withHoldingCoa = db.JournalTypeViews
                            .Where(a => a.CrGfsCode == payeeType.WithheldGfsCode
                              && a.SubBudgetClass == paymentVoucher.SubBudgetClass
                              && a.InstitutionCode == userPaystation.InstitutionCode)
                            .FirstOrDefault();

                        if (withHoldingCoa == null)
                        {
                            response = "Withholding Generation Failed. Chart of Account setup is incomplete. Withholding COA with GFS '" + payeeType.WithheldGfsCode + "' for subbudget class '" + paymentVoucher.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }
                    }

                    CurrencyRateView currencyRateView = db.CurrencyRateViews
                        .Where(a => a.SubBudgetClass == paymentVoucher.SubBudgetClass
                              && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();


                    if (serviceManager.GetFinancialYear(paymentVoucher.ApplyDate) == -1)
                    {
                        return Content("The given apply date does not exist in the current Financial Year");
                    }

                    PaymentVoucher voucher = db.PaymentVouchers.Find(voucherId);

                    if (paymentVoucher.PayeeName == null)
                    {
                        voucher.PayeeType = paymentVoucher.PayeeType;
                        voucher.InvoiceNo = paymentVoucher.InvoiceNo;
                        voucher.InvoiceDate = paymentVoucher.InvoiceDate;
                        voucher.Narration = paymentVoucher.Comments;
                        voucher.ControlNumber = paymentVoucher.ControlNumber;
                        voucher.PaymentDesc = paymentVoucher.PaymentDescription;
                        voucher.OperationalAmount = paymentVoucher.OperationalAmount;
                        voucher.BaseAmount = paymentVoucher.BaseAmount;
                        voucher.BaseCurrency = paymentVoucher.BaseCurrencyCode;
                        voucher.OperationalCurrency = paymentVoucher.OperationalCurrencyCode;
                        voucher.ExchangeRate = paymentVoucher.ExchangeRate;
                        voucher.PaymentMethod = paymentVoucher.PaymentMethod;
                        voucher.SubBudgetClass = paymentVoucher.SubBudgetClass;
                    }
                    else
                    {
                        voucher.PayeeType = paymentVoucher.PayeeType;
                        voucher.InvoiceNo = paymentVoucher.InvoiceNo;
                        voucher.InvoiceDate = paymentVoucher.InvoiceDate;
                        voucher.PayeeDetailId = paymentVoucher.PayeeDetailId;
                        voucher.PayeeCode = paymentVoucher.PayeeCode;
                        voucher.Payeename = paymentVoucher.PayeeName;
                        voucher.PayeeBankAccount = paymentVoucher.BankAccountNo;
                        voucher.PayeeBankName = paymentVoucher.BankName;
                        voucher.PayeeAccountName = paymentVoucher.PayeeAccountName;
                        voucher.PayeeAddress = paymentVoucher.Address;
                        voucher.PayeeBIC = paymentVoucher.PayeeBIC;
                        voucher.Narration = paymentVoucher.Comments;
                        voucher.ControlNumber = paymentVoucher.ControlNumber;
                        voucher.PaymentDesc = paymentVoucher.PaymentDescription;
                        voucher.OperationalAmount = paymentVoucher.OperationalAmount;
                        voucher.BaseAmount = paymentVoucher.BaseAmount;
                        voucher.BaseCurrency = paymentVoucher.BaseCurrencyCode;
                        voucher.OperationalCurrency = paymentVoucher.OperationalCurrencyCode;
                        voucher.ExchangeRate = paymentVoucher.ExchangeRate;
                        voucher.PaymentMethod = paymentVoucher.PaymentMethod;
                        voucher.InstitutionId = userPaystation.InstitutionId;
                        voucher.InstitutionCode = userPaystation.InstitutionCode;
                        voucher.InstitutionName = userPaystation.Institution.InstitutionName;
                        voucher.PaystationId = userPaystation.InstitutionSubLevelId;
                        voucher.SubLevelCategory = userPaystation.SubLevelCategory;
                        voucher.SubLevelCode = userPaystation.SubLevelCode;
                        voucher.SubLevelDesc = userPaystation.SubLevelDesc;
                        voucher.SubBudgetClass = paymentVoucher.SubBudgetClass;
                        voucher.InstitutionAccountId = payerBank.InstitutionAccountId;
                        voucher.PayerBankAccount = payerBank.AccountNumber;
                        voucher.PayerBankName = payerBank.AccountName;
                        voucher.PayerBIC = payerBank.BIC;
                        voucher.PayerCashAccount = payerBank.GlAccount;
                        voucher.PayableGlAccount = crCodes.CrCoa;
                        voucher.UnappliedAccount = unappliedAccount.AccountNumber;
                        voucher.PayerAccountType = payerBank.AccountType;
                    }

                    voucher.ServiceAmount = paymentVoucher.ServiceAmount;
                    voucher.VATOnService = paymentVoucher.VATOnService;
                    voucher.GoodsAmount = paymentVoucher.GoodsAmount;
                    voucher.VATOnGoods = paymentVoucher.VATOnGoods;
                    voucher.OperationalWithHoldingAmount = paymentVoucher.OperationalWithHoldingAmount;
                    voucher.BaseWithHoldingAmount = paymentVoucher.BaseWithHoldingAmount;
                    voucher.hasWithHolding = paymentVoucher.hasWithHolding;
                    voucher.OtherWithholdingAmount = paymentVoucher.OtherWithholdingAmount;
                    voucher.OtherWithholdingPercent = paymentVoucher.OtherWithholdingPercent;

                    if (!voucher.hasWithHolding)
                    {
                        voucher.ServiceAmount = 0;
                        voucher.VATOnService = 0;
                        voucher.GoodsAmount = 0;
                        voucher.VATOnGoods = 0;
                        voucher.OperationalWithHoldingAmount = 0;
                        voucher.BaseWithHoldingAmount = 0;
                        voucher.OtherWithholdingAmount = 0;
                        voucher.OtherWithholdingPercent = 0;
                        voucher.hasWithHolding = false;
                    }
                    if (voucher.ApplyDate != null)
                    {
                        voucher.ApplyDate = paymentVoucher.ApplyDate;
                    }
                    //Sub TSA
                    voucher.SubTsaBankAccount = payerBank.SubTSAAccountNumber;
                    voucher.SubTsaCashAccount = payerBank.SubTSAGlAccount;

                    // St Payment
                    voucher.StPaymentFlag = paymentVoucher.IsStPayment;
                    voucher.ParentInstitutionCode = paymentVoucher.ParentInstitutionCode;
                    voucher.ParentInstitutionName = paymentVoucher.ParentInstitutionName;
                    voucher.SubWarrantCode = paymentVoucher.SubWarrantCode;
                    voucher.SubWarrantDescription = paymentVoucher.SubWarrantDescription;

                    voucher.MiscDeduction = paymentVoucher.MiscDeduction;
                    voucher.MiscDeductionDescription = paymentVoucher.MiscDeductionDescription;
                    voucher.MiscDeductionPayeeDetailsId = paymentVoucher.MiscDeductionPayeeDetailsId;

                    var userInst = db.Institution.Find(userPaystation.InstitutionId);
                    var isEmbassy = userInst.InstitutionCategory.Trim() == "Embassy/Commission";
                    if ((!(bool)voucher.IsAccrualVoucher || voucher.IsAccrualVoucher == null) && !isEmbassy
                        && voucher.SourceModule != "External")
                    {
                        voucher.ApplyDate = DateTime.Now;
                        voucher.InvoiceDate = DateTime.Now;
                    }

                    db.SaveChanges();


                    if (paymentVoucher.voucherDetails.Count() > 0)
                    {
                        List<VoucherDetail> vl = db.VoucherDetails
                                           .Where(a => a.PaymentVoucherId == voucherId)
                                           .ToList();

                        foreach (var item in vl)
                        {
                            db.VoucherDetails.Remove(db.VoucherDetails.Find(item.VoucherDetailId));
                        }

                        List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();
                        foreach (VoucherDetailVm voucherDetailVm in paymentVoucher.voucherDetails)
                        {
                            COA coa = db.COAs.Where(a => a.GlAccount == voucherDetailVm.ExpenditureLineItem && a.Status != "Cancelled").FirstOrDefault();
                            VoucherDetail voucherDetail = new VoucherDetail
                            {
                                PaymentVoucherId = voucherId,
                                JournalTypeCode = "PV",
                                DrGlAccount = voucherDetailVm.ExpenditureLineItem,
                                DrGlAccountDesc = voucherDetailVm.ItemDescription,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = voucherDetailVm.FundingReference,
                                OperationalAmount = voucherDetailVm.ExpenseAmount,
                                BaseAmount = voucherDetailVm.BaseAmountDetail,
                                GfsCode = coa.GfsCode,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                VoteDesc = coa.VoteDesc,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
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
                                TR = coa.TR,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                SourceModuleRefNo = voucher.PVNo,
                                FundingSourceDesc = coa.FundingSourceDesc
                            };

                            voucherDetailList.Add(voucherDetail);
                        }
                        db.VoucherDetails.AddRange(voucherDetailList);
                        db.SaveChanges();
                    }


                    //Edit transaction
                    List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                    var vourcherDetails = db.VoucherDetails.Where(a => a.PaymentVoucherId == voucher.PaymentVoucherId).ToList();

                    foreach (var voucherDetail in vourcherDetails)
                    {
                        TransactionLogVM transactionLogVM = new TransactionLogVM()
                        {
                            SourceModuleId = voucher.PaymentVoucherId,
                            OverallStatus = voucher.OverallStatus,
                            OverallStatusDesc = voucher.OverallStatusDesc,
                            LegalNumber = voucher.PVNo,
                            SourceModule = "Normal Voucher",
                            FundingRerenceNo = voucherDetail.FundingReferenceNo,
                            InstitutionCode = voucher.InstitutionCode,
                            InstitutionName = voucher.InstitutionName,
                            JournalTypeCode = voucher.JournalTypeCode,
                            GlAccount = voucherDetail.DrGlAccount,
                            GlAccountDesc = voucherDetail.DrGlAccountDesc,
                            GfsCode = voucherDetail.GfsCode,
                            GfsCodeCategory = voucherDetail.GfsCodeCategory,
                            TransactionCategory = "Expenditure",
                            VoteDesc = voucherDetail.VoteDesc,
                            GeographicalLocationDesc = voucherDetail.GeographicalLocationDesc,
                            TrDesc = voucherDetail.TrDesc,
                            SubBudgetClass = voucher.SubBudgetClass,
                            SubBudgetClassDesc = voucherDetail.SubBudgetClassDesc,
                            ProjectDesc = voucherDetail.ProjectDesc,
                            ServiceOutputDesc = voucherDetail.ServiceOutputDesc,
                            ActivityDesc = voucherDetail.ActivityDesc,
                            FundTypeDesc = voucherDetail.FundTypeDesc,
                            CofogDesc = voucherDetail.CofogDesc,
                            SubLevelCode = voucher.SubLevelCode,
                            FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                            OperationalAmount = voucherDetail.OperationalAmount,
                            BaseAmount = voucherDetail.BaseAmount,
                            Currency = voucher.OperationalCurrency,
                            CreatedAt = DateTime.Now,
                            CreatedBy = voucher.CreatedBy,
                            ApplyDate = voucher.ApplyDate,
                            PayeeCode = voucher.PayeeCode,
                            PayeeName = voucher.Payeename,
                            TransactionDesc = voucher.PaymentDesc,
                            Facility = voucherDetail.Facility,
                            FacilityDesc = voucherDetail.FacilityDesc,
                            CostCentre = voucherDetail.CostCentre,
                            CostCentreDesc = voucherDetail.CostCentreDesc,
                            Level1Code = userPaystation.Institution.Level1Code,
                            InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                            Level1Desc = voucherDetail.Level1Desc,
                            TR = voucherDetail.TR,
                            SubVote = voucherDetail.SubVote,
                            SubVoteDesc = voucherDetail.SubVoteDesc,
                            SourceModuleRefNo = voucher.PVNo,
                            FundingSourceDesc = voucherDetail.FundingSourceDesc
                        };
                        transactionLogVMs.Add(transactionLogVM);
                    }

                    if (voucher.SourceModule == "Ten Percent")
                    {
                        scope.Complete();
                    }
                    else
                    {
                        response = fundBalanceServices.EditTransaction(transactionLogVMs);
                        if (response == "Success")
                        {
                            scope.Complete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.Message;
                    if (response == null)
                    {
                        response = ex.Message;
                    }
                }
            }
            return Content(response);
        }

        public ActionResult UnGeneratePayment(int PaymentSummaryId)
        {
            string response = "Success";
            try
            {
                db.Database.CommandTimeout = 120000;
                PaymentSummary ps = db.PaymentSummaries.Find(PaymentSummaryId);
                ps.OverallStatus = "Cancelled";
                ps.OverallStatusDesc = "Un Generated By " + User.Identity.Name + " At " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");

                List<PaymentVoucher> vouchers = db.PaymentVouchers
                    .Where(a => a.PaymentSummaryId == PaymentSummaryId)
                    .ToList();

                foreach (var v in vouchers)
                {
                    v.OverallStatus = "Approved";
                    if (v.SourceModule == "Contract" || v.SourceModule == "Accrual Contract")
                    {
                        try
                        {
                            manageRetentionServices.CancellRetention(v);
                        }
                        catch (Exception ex) { }
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Content("Sorry! Could't ungenerate please contact system support!");
            }
            return Content(response);
        }

        public ActionResult ReverseWithholding(string PVNo)
        {
            try
            {
                PaymentVoucher pv = db.PaymentVouchers
                    .Where(a => a.PVNo == PVNo)
                    .FirstOrDefault();
                pv.OverallStatus = "Cancelled";

                var wd = db.WithHoldingDetails
                    .Where(a => a.PaidVoucherNo == PVNo)
                    .ToList();

                foreach (var w in wd)
                {
                    w.OverallStatus = "Pending";
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Content("Sorry! Could't reverse please contact system support!");
            }

            return Content("Success");
        }

        public JsonResult GetParentInstitutions()
        {
            DefaultUserPayStationVM userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            var data = db.InstitutionSubWarrantHolders
                 .Where(a => a.StInstitutionCode == userPaystation.InstitutionCode)
                 .DistinctBy(a => a.ParentInstitutionId)
                 .ToList();
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSubWarrants(string institutionCode)
        {
            DefaultUserPayStationVM userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            var data = db.InstitutionSubWarrantHolders
                 .Where(a => a.ParentInstitutionCode == institutionCode
                    && a.StInstitutionCode == userPaystation.InstitutionCode)
                 .ToList();
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public string CreateMiscDeducionVoucher(string PVNo)
        {
            var response = "";
            try
            {

                var paymentVoucher = db.PaymentVouchers.Where(a => a.PVNo == PVNo).FirstOrDefault();
                if (paymentVoucher == null)
                {
                    return "Voucher does not exist";
                }

                PaymentVoucherDeductionType dType = db.PaymentVoucherDeductionTypes
                 .Where(a => a.DeductionTypeName == "Misc Deduction")
                 .FirstOrDefault();

                if (dType == null)
                {
                    return "Setup for 'DeductionTypeName: Misc Deduction' is Missing";
                }

                if (dType != null)
                {
                    JournalTypeView journalTypeView =
                          db.JournalTypeViews
                          .Where(a => a.JournalTypeCode == "PV"
                            && a.InstitutionCode == paymentVoucher.InstitutionCode
                            && a.DrGfsCode == dType.DeductionGfsCode
                            && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                          .FirstOrDefault();

                    if (journalTypeView == null)
                    {
                        return "COA setup is Missing for SubbudgetClass="
                        + paymentVoucher.SubBudgetClass +
                        " DeduductionGFSCode =" + dType.DeductionGfsCode +
                        " InstititionCode =" + paymentVoucher.InstitutionCode +
                        " and JournalTypeCode='PV'";
                    }
                    response = CreateMiscDeductionVoucher(paymentVoucher.PaymentVoucherId, journalTypeView);
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return response;
        }

        public ActionResult FixMissingMiscDeducionVoucher(string PVNo)
        {
            var response = "";
            try
            {

                var paymentVoucher = db.PaymentVouchers.Where(a => a.PVNo == PVNo).FirstOrDefault();
                if (paymentVoucher == null)
                {
                    return Content("Voucher does not exist");
                }

                if (paymentVoucher.MiscDeductionPayeeDetailsId == null || paymentVoucher.MiscDeductionPayeeName == null)
                {

                    return Content("Misc DeductionPayee is Missing");
                }
                PaymentVoucherDeductionType dType = db.PaymentVoucherDeductionTypes
                 .Where(a => a.DeductionTypeName == "Misc Deduction")
                 .FirstOrDefault();

                if (dType == null)
                {
                    return Content("Setup for 'DeductionTypeName: Misc Deduction' is Missing");
                }

                if (dType != null)
                {
                    JournalTypeView journalTypeView =
                          db.JournalTypeViews
                          .Where(a => a.JournalTypeCode == "GJ"
                            && a.InstitutionCode == paymentVoucher.InstitutionCode
                            && a.DrGfsCode == dType.DeductionGfsCode
                            && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                          .FirstOrDefault();

                    if (journalTypeView == null)
                    {
                        return Content("COA setup is Missing for SubbudgetClass = " + paymentVoucher.SubBudgetClass +
                        " DeduductionGFSCode =" + dType.DeductionGfsCode +
                        " InstititionCode =" + paymentVoucher.InstitutionCode +
                        " and JournalTypeCode='PV'");
                    }
                    response = CreateMiscDeductionVoucher(paymentVoucher.PaymentVoucherId, journalTypeView, "Pending");
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Content(response);
        }
        private string CreateMiscDeductionVoucher(int voucherId, JournalTypeView jv, string status = "UNKNOWN")
        {
            try
            {
                var _miscVoucher = db.PaymentVouchers
                     .Where(a => a.OtherSourceId == voucherId
                       && a.SourceModule == "MiscDeduction")
                     .FirstOrDefault();
                if (_miscVoucher != null)
                {
                    _miscVoucher.OverallStatus = "Cancelled";
                    _miscVoucher.OverallStatusDesc = "Cancelled due to rejection";
                    _miscVoucher.SourceModule = "_MiscDeduction";
                    _miscVoucher.CancelledAt = DateTime.Now;
                    _miscVoucher.CancelledBy = User.Identity.GetUserName();
                    db.SaveChanges();
                }

                var voucher = db.PaymentVouchers.Find(voucherId);
                var payeeDetail = db.PayeeDetails.Find(voucher.MiscDeductionPayeeDetailsId);
                var payee = db.Payees.Find(payeeDetail.PayeeId);

                PaymentVoucher miscVoucher = new PaymentVoucher
                {
                    OtherSourceId = voucherId,
                    SourceModule = "MiscDeduction",
                    SourceModuleReferenceNo = voucher.PVNo,
                    PayeeType = payeeDetail.PayeeType,
                    InvoiceNo = voucher.InvoiceNo,
                    InvoiceDate = voucher.InvoiceDate,
                    PayeeDetailId = payeeDetail.PayeeDetailId,
                    PayeeCode = payee.PayeeCode,
                    Payeename = payee.PayeeName,
                    PayeeBankAccount = payeeDetail.Accountnumber,
                    PayeeBankName = payeeDetail.BankName,
                    PayeeAccountName = payeeDetail.AccountName,
                    PayeeAddress = payee.Address1,
                    PayeeBIC = payeeDetail.BIC,
                    Narration = voucher.MiscDeductionDescription,
                    PaymentDesc = voucher.MiscDeductionDescription,
                    OperationalAmount = voucher.MiscDeduction,
                    BaseAmount = voucher.MiscDeduction,
                    BaseCurrency = "TZS",
                    OperationalCurrency = "TZS",
                    ExchangeRate = 1,
                    ApplyDate = voucher.ApplyDate,
                    PaymentMethod = voucher.PaymentMethod,
                    FinancialYear = serviceManager.GetFinancialYear(voucher.ApplyDate),
                    CreatedBy = voucher.ConfirmedBy,
                    CreatedAt = voucher.ConfirmedAt ?? DateTime.Now,
                    OverallStatus = status == "UNKNOWN" ? "Waiting for MiscDeduction Update" : status,
                    Book = "MAIN",
                    InstitutionId = voucher.InstitutionId,
                    InstitutionCode = voucher.InstitutionCode,
                    InstitutionName = voucher.InstitutionName,
                    PaystationId = voucher.PaystationId,
                    SubLevelCategory = voucher.SubLevelCategory,
                    SubLevelCode = voucher.SubLevelCode,
                    SubLevelDesc = voucher.SubLevelDesc,
                    SubBudgetClass = jv.SubBudgetClass,
                    JournalTypeCode = "PV",
                    InstitutionAccountId = voucher.InstitutionAccountId,
                    PayerBankAccount = voucher.PayerBankAccount,
                    PayerBankName = voucher.PayerBankName,
                    PayerBIC = voucher.PayerBIC,
                    PayerCashAccount = voucher.PayerCashAccount,
                    PayableGlAccount = voucher.PayableGlAccount,
                    UnappliedAccount = voucher.UnappliedAccount,
                    PayerAccountType = voucher.PayerAccountType,
                    IsAccrualPayed = false,
                    PVNo = voucher.InstitutionCode + ":" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    //Sub TSA
                    SubTsaBankAccount = voucher.SubTsaBankAccount,
                    SubTsaCashAccount = voucher.SubTsaCashAccount,
                };

                db.PaymentVouchers.Add(miscVoucher);
                db.SaveChanges();

                miscVoucher.PVNo = serviceManager.GetLegalNumber(voucher.InstitutionCode, "V", miscVoucher.PaymentVoucherId);


                if (miscVoucher.PVNo.Contains("Error") || miscVoucher.PVNo == null)
                {
                    miscVoucher.OverallStatus = "Cancelled";
                    miscVoucher.OverallStatusDesc = "Cancelled due to null PVNo";
                    miscVoucher.CancelledAt = DateTime.Now;
                    miscVoucher.CancelledBy = "System";
                    return "Voucher Confirmation failed Please try again. Error code: 1122";
                }

                var vd = db.VoucherDetails.Where(a => a.PaymentVoucherId == voucher.PaymentVoucherId).FirstOrDefault();
                if (vd != null)
                {
                    VoucherDetail voucherDetail = new VoucherDetail
                    {
                        PaymentVoucherId = miscVoucher.PaymentVoucherId,
                        JournalTypeCode = "PV",
                        DrGlAccount = jv.DrCoa,
                        DrGlAccountDesc = jv.DrCoaDesc,
                        CrGlAccount = vd.CrGlAccount,
                        CrGlAccountDesc = vd.CrGlAccountDesc,
                        FundingReferenceNo = voucher.PVNo,
                        OperationalAmount = miscVoucher.OperationalAmount,
                        BaseAmount = miscVoucher.OperationalAmount,
                    };
                    db.VoucherDetails.Add(voucherDetail);
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            return "Success";
        }
        public ActionResult FixMissingDeduction(string PvNo)
        {
            try
            {
                var modal = db.PaymentVouchers.Where(a => a.PVNo == PvNo).FirstOrDefault();
                if (modal != null)
                {
                    CreateDeduction(modal.PaymentVoucherId);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return Content(ex.InnerException.Message.ToString());
                }
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }

        public ActionResult FixPaymentSummaryDeduction(string PaymentNo)
        {
            try
            {
                var pvs = db.PaymentVouchers.Where(a => a.PaymentSummaryNo == PaymentNo).ToList();
                foreach (var pv in pvs)
                {
                    var voucherDetailsAmount = db.VoucherDetails
                        .Where(a => a.PaymentVoucherId == pv.PaymentVoucherId)
                        .Sum(a => a.OperationalAmount);
                    if (voucherDetailsAmount != pv.OperationalAmount)
                    {
                        return Content("PVNo: " + pv.PVNo + " OperationalAmount: " + pv.OperationalAmount +
                            " Differs with Voucher Details Amount: " + voucherDetailsAmount);
                    }
                    CreateDeduction(pv.PaymentVoucherId);
                }
                var summary = db.PaymentSummaries.Where(a => a.PaymentNo == PaymentNo).FirstOrDefault();
                if (summary != null)
                {
                    summary.OperationalAmount = pvs.Sum(a => a.NetOperationalAmount);
                    summary.BaseAmount = pvs.Sum(a => a.NetBaseAmount);
                    summary.NumTrx = pvs.Count();
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return Content(ex.InnerException.Message.ToString());
                }
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }

        public List<PaymentSummary> _FixPaymentSummaryDeduction(List<PaymentSummary> list)
        {
            List<PaymentSummary> _list = new List<PaymentSummary> { };
            list.ForEach(l =>
            {
                var pvs = db.PaymentVouchers.Where(a => a.PaymentSummaryNo == l.PaymentNo).ToList();
                foreach (var pv in pvs)
                {
                    CreateDeduction(pv.PaymentVoucherId);
                    ComputeNetPayable(pv.PaymentVoucherId);
                }
                var summary = db.PaymentSummaries.Where(a => a.PaymentNo == l.PaymentNo).FirstOrDefault();
                if (summary != null)
                {
                    summary.OperationalAmount = pvs.Sum(a => a.NetOperationalAmount);
                    summary.BaseAmount = pvs.Sum(a => a.NetBaseAmount);
                    summary.NumTrx = pvs.Count();
                    db.SaveChanges();
                }
                _list.Add(summary);
            });
            return _list;
        }

        public string CreateDeduction(int paymentVoucherId)
        {
            PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(paymentVoucherId);
            var deductions = db.PaymentVoucherDeductions.Where(a => a.PVNo == paymentVoucher.PVNo).ToList();
            var response = "Success";
            if (deductions.Any())
            {
                db.PaymentVoucherDeductions.RemoveRange(deductions);
                db.SaveChanges();
            }

            if (paymentVoucher != null)
            {
                if (paymentVoucher.RetentionAmount != null)
                {
                    response = SaveDeduction(paymentVoucher, "Retention", (decimal)paymentVoucher.RetentionAmount);
                    if (response != "Success")
                    {
                        return response;
                    }
                }

                if (paymentVoucher.hasWithHolding && paymentVoucher.OperationalWithHoldingAmount != null)
                {
                    response = SaveDeduction(paymentVoucher, "WithHoldingTax", (decimal)paymentVoucher.OperationalWithHoldingAmount);
                    if (response != "Success")
                    {
                        return response;
                    }
                }

                if (paymentVoucher.LiquidatedDemageAmount != null)
                {
                    response = SaveDeduction(paymentVoucher, "Liquidated Damage", (decimal)paymentVoucher.LiquidatedDemageAmount);
                    if (response != "Success")
                    {
                        return response;
                    }
                }

                if (paymentVoucher.AdvancePayment != null)
                {
                    response = SaveDeduction(paymentVoucher, "Advance Payment", (decimal)paymentVoucher.AdvancePayment);
                    if (response != "Success")
                    {
                        return response;
                    }
                }

                if (paymentVoucher.MiscDeductionPayeeName != null
                    && paymentVoucher.MiscDeductionPayeeDetailsId != null
                    && paymentVoucher.MiscDeductionPayeeDetailsId != 0)
                {
                    response = SaveDeduction(paymentVoucher, "Misc Deduction", (decimal)paymentVoucher.MiscDeduction);
                    if (response != "Success")
                    {
                        return response;
                    }
                }
            }
            return response;
        }

        public string SaveDeduction(PaymentVoucher paymentVoucher, string dedType, decimal amount)
        {

            PaymentVoucherDeductionType dType = db.PaymentVoucherDeductionTypes
                .Where(a => a.DeductionTypeName == dedType)
                .FirstOrDefault();
            if (dType != null)
            {
                JournalTypeView journalTypeView =
                      db.JournalTypeViews
                      .Where(a =>
                         a.InstitutionCode == paymentVoucher.InstitutionCode
                        && a.DrGfsCode == dType.DeductionGfsCode
                        && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                      .FirstOrDefault();

                if (journalTypeView != null)
                {
                    PaymentVoucherDeduction pvDeduction = new PaymentVoucherDeduction
                    {
                        PVNo = paymentVoucher.PVNo,
                        PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                        PaymentDeductionTypeId = dType.PaymentVoucherDeductionTypeId,
                        COA = journalTypeView == null ? "" : journalTypeView.DrCoa,
                        OperationalAmount = amount,
                        BaseAmount = amount,
                        InstitutionCode = paymentVoucher.InstitutionCode,
                        CreatedAt = DateTime.Now,
                        Status = "Active"
                    };
                    db.PaymentVoucherDeductions.Add(pvDeduction);
                    db.SaveChanges();
                }
                else
                {
                    return dedType + " COA for gfs " + dType.DeductionGfsCode + " and sub budget class " + paymentVoucher.SubBudgetClass + " is missing";
                }
            }
            return "Success";
        }

        public void FixDeductionDuplicates(int PvId, decimal amount)
        {
            var pvDeduction = db.PaymentVoucherDeductions
            .Where(a => a.PaymentVoucherId == PvId
              && a.OperationalAmount == amount)
            .FirstOrDefault();

            if (pvDeduction != null)
            {
                db.PaymentVoucherDeductions
                    .Remove(
                     db.PaymentVoucherDeductions
                    .Find(pvDeduction.PaymentVoucherDeductionId));
                db.SaveChanges();
            }

        }

        public JsonResult ValidateControlNumber(string ctrlNo)
        {
            var response = new GePGResponseVm();
            var gepgIp = Properties.Settings.Default.LiveGepgControlNumValidationIp;
            var gepgUrl = Properties.Settings.Default.LiveGepgControlNumValidationUrl;
            string ospSysId = "MUSE";
            string billReqId = DateTime.Now.ToString("yyyyMMddHHmmss");
            try
            {
                string spCode = "SP" + ctrlNo.Substring(2, 3);

                //if (Properties.Settings.Default.HostingEnvironment == "Live")
                //{
                //    gepgIp = Properties.Settings.Default.LiveGepgControlNumValidationIp;
                //    gepgUrl = Properties.Settings.Default.LiveGepgControlNumValidationUrl;
                //}
                //else
                //{
                //    gepgIp = Properties.Settings.Default.TestGepgControlNumValidationIp;
                //    gepgUrl = Properties.Settings.Default.TestGepgControlNumValidationUrl;
                //}

                var data = new XDocument(
                  new XElement("gepgBillAccQryReq",
                   new XElement("BillReqId", billReqId),
                   new XElement("SpCode", spCode),
                   new XElement("OspSysId", ospSysId),
                   new XElement("BillCtrNum", ctrlNo)
                ));

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                StringWriter sw = new StringWriter();
                using (XmlWriter xw = XmlWriter.Create(sw, settings))
                {
                    data.Save(xw);
                }

                string certPass = Properties.Settings.Default.PfxPassword;
                string certStorePath = Properties.Settings.Default.PfxPath + "museprivate.pfx";
                var hashSignature = DigitalSignature.GenerateSignature(sw.ToString(), certPass, certStorePath);
                var signedData = "<Gepg>" + sw.ToString() + "<gepgSignature>" + hashSignature + "</gepgSignature></Gepg>";

                Log.Information(signedData + "{Name}!", "ValidateControlNoRequest");

                HttpWebResponse httpResponse = serviceManager.SendToGepgCtrlValidate(signedData, gepgUrl, gepgIp, spCode);

                if (httpResponse == null)
                {
                    var error = "Error on getting response from remote server. Contact system support";
                    Log.Information("EmptyResponseFromGepg" + "{Name}!", "EmptyResponseFromGepg");
                    return Json(new { error }, JsonRequestBehavior.AllowGet);
                }

                StreamReader sr = new StreamReader(httpResponse.GetResponseStream());
                var xmlString = @sr.ReadToEnd().Trim().ToString();


                Log.Information(xmlString + "{Name}!", "ValidateControlNoResponseFromGepg");

                XDocument xDoc = XDocument.Parse(xmlString, LoadOptions.None);

                response = serializeResponse(xDoc);
            }
            catch (Exception ex)
            {
                var error = "Exception on getting control number. Contact system support";
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Json(new { error }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }

        public class GePGResponseVm
        {
            public string BillPayOpt { get; set; }
            public decimal BillAmt { get; set; }
            public string TrxSts { get; set; }
            public string BillStsCode { get; set; }
            public string Message { get; set; }
            public string xmlResponse { get; set; }
            public string jsonResponse { get; set; }
        }

        public GePGResponseVm serializeResponse(XDocument xDoc)
        {
            var response = new GePGResponseVm();
            response.xmlResponse = xDoc.ToString();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xDoc.ToString());
            response.jsonResponse = JsonConvert.SerializeXmlNode(doc);

            response.xmlResponse = xDoc.ToString();
            if (xDoc.Descendants("BillGrpHdr").Any())
            {
                var r = (from x in xDoc.Descendants("BillGrpHdr")
                         select new
                         { TrxSts = (string)x.Element("TrxSts") })
                        .SingleOrDefault();

                response.TrxSts = r.TrxSts;
            }
            if (xDoc.Descendants("BillTrxDtl").Any())
            {
                var r = (from x in xDoc.Descendants("BillTrxDtl")
                         select new
                         {
                             BillStsCode = (string)x.Element("BillStsCode"),
                         }).SingleOrDefault();
                response.BillStsCode = r.BillStsCode;
                if (r.BillStsCode == "7101")
                {
                    response.Message = "Valid";
                    var r2 = (from x in xDoc.Descendants("BillTrxDtl")
                              select new
                              {
                                  BillPayOpt = (string)x.Element("BillPayOpt") ?? "",
                                  BillAmt = (decimal)x.Element("BillAmt"),
                              })
                          .SingleOrDefault();
                    response.BillPayOpt = r2.BillPayOpt;
                    response.BillAmt = r2.BillAmt;
                }
                else
                {
                    response.Message = "Invalid";
                }
            }
            return response;
        }


        [HttpGet, Authorize(Roles = "Voucher Entry")]
        public ActionResult EditVoucherGlEntries(int id)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var voucher = db.PaymentVouchers.Find(id);
            ViewBag.PaymentVoucherId = id;
            ViewBag.Total = voucher.OperationalAmount;
            ViewBag.Currency = voucher.OperationalCurrency;
            ViewBag.SBC = voucher.SubBudgetClass;
            ViewBag.SourceModule = voucher.SourceModule;
            ViewBag.SubBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                 && a.SubBudgetClass != null
                 && a.SubBudgetClass != "303")
                .OrderBy(a => a.SubBudgetClass)
                .ToList();
            return View();
        }

        public ActionResult CreateGlItems(PaymentVoucherVM paymentVoucher)
        {
            db.Database.CommandTimeout = 1200;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var voucher = db.PaymentVouchers.Find(paymentVoucher.PaymentVoucherId);

            var payeeType = db.PayeeTypes
                .Where(a => a.PayeeTypeCode == voucher.PayeeType
                  && a.Status != "Cancelled")
                .FirstOrDefault();

            if (payeeType == null)
            {
                var response = "Vendor setup is incomplete. There is no payee type setup for '" + voucher.PayeeType + "'. Please contact Administrator!";
                return Content(response);
            }

            var crCodes = db.JournalTypeViews
                .Where(a => a.CrGfsCode == payeeType.GfsCode
                 && a.SubBudgetClass == voucher.SubBudgetClass
                 && a.InstitutionCode == userPaystation.InstitutionCode)
                .FirstOrDefault();
            if (crCodes == null)
            {
                var response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + voucher.SubBudgetClass + "' is missing. Please contact Administrator!";
                return Content(response);
            }
            try
            {
                var existingDetails = db.VoucherDetails.Where(a => a.PaymentVoucherId == voucher.PaymentVoucherId).ToList();
                if (existingDetails.Count() > 0)
                {
                    db.VoucherDetails.RemoveRange(existingDetails);
                    db.SaveChanges();
                }

                List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                foreach (VoucherDetailVm voucherDetailVm in paymentVoucher.voucherDetails)
                {
                    COA coa = db.COAs.Where(a => a.GlAccount == voucherDetailVm.ExpenditureLineItem && a.Status != "Cancelled").FirstOrDefault();
                    VoucherDetail voucherDetail = new VoucherDetail
                    {
                        PaymentVoucherId = voucher.PaymentVoucherId,
                        JournalTypeCode = "PV",
                        DrGlAccount = voucherDetailVm.ExpenditureLineItem,
                        DrGlAccountDesc = voucherDetailVm.ItemDescription,
                        CrGlAccount = crCodes.CrCoa,
                        CrGlAccountDesc = crCodes.CrCoaDesc,
                        FundingReferenceNo = voucherDetailVm.FundingReference,
                        OperationalAmount = voucherDetailVm.ExpenseAmount,
                        BaseAmount = voucherDetailVm.BaseAmountDetail,
                        GfsCode = coa.GfsCode,
                        GfsCodeCategory = coa.GfsCodeCategory,
                        VoteDesc = coa.VoteDesc,
                        GeographicalLocationDesc = coa.GeographicalLocationDesc,
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
                        Level1Desc = coa.Level1Desc ?? "MISSING",
                        TR = coa.TR,
                        SubVote = coa.SubVote,
                        SubVoteDesc = coa.SubVoteDesc,
                        SourceModuleRefNo = voucher.PVNo,
                        FundingSourceDesc = coa.FundingSourceDesc
                    };
                    voucherDetailList.Add(voucherDetail);
                }
                db.VoucherDetails.AddRange(voucherDetailList);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                return Content("Operation failed please contact system support!");
            }
            return Content("Success");
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
