using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using IFMIS.Services;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class TransactionAdjustmentController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();
        private readonly IFundBalanceServices fundBalanceServices;
        readonly IServiceManager serviceManager;
        public TransactionAdjustmentController()
        {

        }

        public TransactionAdjustmentController(
            IFundBalanceServices fundBalanceServices,
            IServiceManager serviceManager
            )
        {
            this.fundBalanceServices = fundBalanceServices;
            this.serviceManager = serviceManager;
        }

        [HttpGet, Authorize(Roles = "Transaction Adjustment Entry")]
        public ActionResult PendingReversal()
        {
            return View();
        }

        [HttpGet, Authorize(Roles = "Transaction Adjustment Entry")]
        public ActionResult ReverseTransaction()
        {
            InstitutionSubLevel userPaystation = serviceManager
             .GetUserPayStation(User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                  && a.SubBudgetClass != null)
                .OrderBy(a => a.SubBudgetClass)
                .ToList();
            ViewBag.SubWarrant = db.InstitutionSubWarrantHolders
                .Where(a => a.StInstitutionCode == userPaystation.InstitutionCode)
                .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            return View();
        }

        [HttpPost]
        public ActionResult ReverseTransaction(TrxReversalVm trxReversalVm)
        {
            db.Database.CommandTimeout = 1200;
            string response = "Success";
            try
            {
                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

                List<GeneralLedger> gl = db.GeneralLedgers
                .Where(a => a.LegalNumber == trxReversalVm.LegalNumber
                   && a.InstitutionCode == userPaystation.InstitutionCode
                  // && (a.ReversalFlag == false || a.ReversalFlag == null)
                ).ToList();

                if (gl.Count() == 0)
                {
                    return Content("We could not find this Legal Number ("+ trxReversalVm.LegalNumber + ") in the General Ledger.");
                }

                foreach (var GL in gl)
                {
                    GeneralLedger _model = db.GeneralLedgers.Find(GL.GeneralLedgerId);
                    _model.ReversalFlag = true;
                    db.SaveChanges();
                }

                decimal OperationalAmount = 0;
                decimal BaseAmount = 0;
                foreach (var item in gl)
                {

                    if (item.TransactionType == "DR")
                    {
                        OperationalAmount += item.OperationalAmount != null ? (Decimal)item.OperationalAmount : 0;
                        BaseAmount += item.BaseAmount != null ? (Decimal)item.BaseAmount : 0;
                    }
                }

                int financialYear = 0;
                if (trxReversalVm.ApplyDate >= new DateTime(trxReversalVm.ApplyDate.Year, 7, 1)
                   && trxReversalVm.ApplyDate <= new DateTime(trxReversalVm.ApplyDate.Year, 12, 31)
                   )
                {
                    financialYear = trxReversalVm.ApplyDate.Year + 1;
                }
                else
                {
                    financialYear = trxReversalVm.ApplyDate.Year;
                }

                //if (ServiceManager.GetFinancialYear(db, trxReversalVm.ApplyDate)
                //    < ServiceManager.GetFinancialYear(db, DateTime.Now))
                //{
                //    var modal = db.RestrictionFinancialYears
                //        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                //        && a.OverallStatus == "Active").FirstOrDefault();
                //    if (modal != null)
                //    {
                //        if (modal.FinancialYear != ServiceManager.GetFinancialYear(db, trxReversalVm.ApplyDate))
                //        {
                //            return Content("Invalid Apply date!");
                //        }
                //    }
                //    else
                //    {
                //        return Content("Invalid Apply date!");
                //    }
                //}

                TransactionAdjustmentSummary model = new TransactionAdjustmentSummary
                {
                    TrxAdjustmentCategory = trxReversalVm.AdjustType,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    FinancialYear = financialYear,
                    LegalNumber = trxReversalVm.LegalNumber.Replace(trxReversalVm.ToString().ToCharArray()[8], 'R'),
                    DocumentNum = trxReversalVm.LegalNumber,
                    JournalTypeCode = trxReversalVm.JournalCode,
                    ApplyDate = trxReversalVm.ApplyDate,
                    SubBudgetClass = "102",
                    CurrencyCode = "TZS",
                    OperationalAmount = OperationalAmount,
                    BaseAmount = BaseAmount,
                    AdjustmentReason = trxReversalVm.Reason,
                    AdjustmentReasonDesc = trxReversalVm.Description,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    ApprovalStatus = "Pending",
                    OverallStatus = "Pending",
                    GlStatus = "Pending"            
                };

                db.TransactionAdjustmentSummaries.Add(model);
                db.SaveChanges();

                foreach (var item in gl)
                {
                    TransactionAdjustementDetail trxDetail = new TransactionAdjustementDetail
                    {
                        TrxAdjustmentSummaryId = model.TrxAdjustmentSummaryId,
                        GlAccount = item.GlAccountCode,
                        TransactionType = item.TransactionType == "DR" ? "CR" : "DR",
                        OperationalAmount = (decimal)item.OperationalAmount,
                        BaseAmount = (decimal)item.OperationalAmount,
                        FundingRefNo = item.FundingRef,
                        SubLevelCode = userPaystation.SubLevelCode
                    };
                    db.TransactionAdjustementDetails.Add(trxDetail);
                    db.SaveChanges();
                }


                //Post transaction 
                List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                string voucherSubLevelCode = userPaystation.SubLevelCode;
                string voucherInstitutionCode = userPaystation.InstitutionCode;
                string voucherInstitutionName = userPaystation.Institution.InstitutionName;

                foreach (var voucherDetail in gl)
                {
                    COA coa = db.COAs.Where(a => a.GlAccount == voucherDetail.GlAccountCode && a.Status != "Cancelled")
                        .FirstOrDefault();
                    TransactionLogVM transactionLogVM = new TransactionLogVM()
                    {
                        SourceModuleId = model.TrxAdjustmentSummaryId,
                        LegalNumber = model.LegalNumber,
                        SourceModule = "Journal Adjustment",
                        OverallStatus = model.OverallStatus,
                        OverallStatusDesc = model.OverallStatus,
                        FundingRerenceNo = voucherDetail.FundingRef,
                        InstitutionCode = voucherInstitutionCode,
                        InstitutionName = voucherInstitutionName,
                        JournalTypeCode = trxReversalVm.JournalCode,
                        GlAccount = voucherDetail.GlAccountCode,
                        GlAccountDesc = coa.GlAccountDesc,
                        GfsCode = coa.GfsCode,
                        GfsCodeCategory = coa.GfsCodeCategory,
                        TransactionCategory  = voucherDetail.TransactionType == "CR" ? "Expenditure" : "Revenue",
                        VoteDesc = coa.VoteDesc,
                        GeographicalLocationDesc = coa.GeographicalLocationDesc,
                        TrDesc = coa.TrDesc,
                        SubBudgetClass = coa.SubBudgetClass,
                        SubBudgetClassDesc = coa.subBudgetClassDesc,
                        ProjectDesc = coa.ProjectDesc,
                        ServiceOutputDesc = coa.ServiceOutputDesc,
                        ActivityDesc = coa.ActivityDesc,
                        FundTypeDesc = coa.FundTypeDesc,
                        CofogDesc = coa.CofogDesc,
                        SubLevelCode = voucherSubLevelCode,
                        FinancialYear = serviceManager.GetFinancialYear(trxReversalVm.ApplyDate),
                        OperationalAmount = voucherDetail.OperationalAmount,
                        BaseAmount = voucherDetail.BaseAmount,
                        Currency = model.CurrencyCode,
                        CreatedAt = DateTime.Now,
                        CreatedBy = model.CreatedBy,
                        ApplyDate = model.ApplyDate,
                        PayeeCode = voucherDetail.PayeeID,
                        PayeeName = voucherDetail.PayeeName,
                        TransactionDesc = voucherDetail.TransactionDesc,
                        TR = coa.TR,
                        Facility = coa.Facility,
                        FacilityDesc = coa.FacilityDesc,
                        //SourceModuleRefNo = paymentVoucherSummary.SourceModuleReferenceNo,
                        CostCentre = coa.CostCentre,
                        CostCentreDesc = coa.CostCentreDesc,
                        Level1Code = userPaystation.Institution.Level1Code,
                        InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                        Level1Desc = coa.Level1Desc,
                        SubVote = coa.SubVote,
                        SubVoteDesc = coa.SubVoteDesc,
                        FundingSourceDesc = coa.FundingSourceDesc,
                    };
                    transactionLogVMs.Add(transactionLogVM);
                }
                response = fundBalanceServices.PostTransaction(transactionLogVMs);

            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
            }

            return Content(response);
        }

        [HttpPost]
        public ActionResult JournalAdjustment(TrxReversalVm trxReversalVm)
        {
            string response = "Success";
            db.Database.CommandTimeout = 1200;
            try
            {

                InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
             
                int financialYear = 0;
                if (trxReversalVm.ApplyDate >= new DateTime(trxReversalVm.ApplyDate.Year, 7, 1)
                    && trxReversalVm.ApplyDate <= new DateTime(trxReversalVm.ApplyDate.Year, 12, 31)
                    )
                {
                    financialYear = trxReversalVm.ApplyDate.Year + 1;
                }
                else
                {
                    financialYear = trxReversalVm.ApplyDate.Year;
                }

                //if (ServiceManager.GetFinancialYear(db, trxReversalVm.ApplyDate)
                //    < ServiceManager.GetFinancialYear(db, DateTime.Now))
                //{
                //    var modal = db.RestrictionFinancialYears
                //        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                //        && a.OverallStatus == "Active").FirstOrDefault();
                //    if (modal != null)
                //    {
                //        if (modal.FinancialYear != ServiceManager.GetFinancialYear(db, trxReversalVm.ApplyDate))
                //        {
                //            return Content("Invalid Apply date!");
                //        }
                //    }
                //    else
                //    {
                //        return Content("Invalid Apply date!");
                //    }
                //}

                string voucherSubLevelCode = userPaystation.SubLevelCode;
                string voucherInstitutionCode = userPaystation.InstitutionCode;
                string voucherInstitutionName = userPaystation.Institution.InstitutionName;
                if (trxReversalVm.JournalCode == "SW")
                {
                    voucherInstitutionCode = trxReversalVm.GlItemsList[0].ParentInstitutionCode;
                    voucherInstitutionName = trxReversalVm.GlItemsList[0].ParentInstitutionName;
                }

                TransactionAdjustmentSummary model = new TransactionAdjustmentSummary
                {
                    TrxAdjustmentCategory = trxReversalVm.AdjustType,
                    InstitutionCode = userPaystation.InstitutionCode,
                    InstitutionName = userPaystation.Institution.InstitutionName,
                    FinancialYear = financialYear,
                    DocumentNum = trxReversalVm.DocumentNumber,
                    JournalTypeCode = trxReversalVm.JournalCode,
                    ApplyDate = trxReversalVm.ApplyDate,
                    SubBudgetClass = "102",
                    CurrencyCode = "TZ",
                    OperationalAmount = trxReversalVm.AmountToReceive,
                    BaseAmount = trxReversalVm.AmountToReceive,
                    AdjustmentReason = trxReversalVm.Reason,
                    AdjustmentReasonDesc = trxReversalVm.Description,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    ApprovalStatus = "Pending",
                    OverallStatus = "Pending",
                    GlStatus = "Pending",
                };

                db.TransactionAdjustmentSummaries.Add(model);
                db.SaveChanges();
         
                model.LegalNumber = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, trxReversalVm.JournalCode, model.TrxAdjustmentSummaryId);
                db.SaveChanges();
     
                foreach (var item in trxReversalVm.GlItemsList)
                {
                    TransactionAdjustementDetail trxDetail = new TransactionAdjustementDetail
                    {
                        TrxAdjustmentSummaryId = model.TrxAdjustmentSummaryId,
                        TransactionType = item.TrxType,
                        GlAccount = item.GLAccount,
                        OperationalAmount = item.Amount,
                        BaseAmount = item.Amount,
                        FundingRefNo = item.FundingRefNo,
                        SubLevelCode = voucherSubLevelCode
                    };
                    if (trxReversalVm.JournalCode == "SW")
                    {
                        trxDetail.SubLevelCode = item.SubLevelCode;
                    }
                    db.TransactionAdjustementDetails.Add(trxDetail);
                    db.SaveChanges();
                }

                //Post transaction 
                List<TransactionLogVM> transactionLogVMs = new List<TransactionLogVM>();
                var vourcherDetails = db.TransactionAdjustementDetails
                 .Where(a => a.TrxAdjustmentSummaryId == model.TrxAdjustmentSummaryId)
                 .ToList();


                foreach (var voucherDetail in vourcherDetails)
                {
                    COA coa = db.COAs.Where(a => a.GlAccount == voucherDetail.GlAccount && a.Status != "Cancelled")
                        .FirstOrDefault();
                    TransactionLogVM transactionLogVM = new TransactionLogVM()
                    {
                        SourceModuleId = model.TrxAdjustmentSummaryId,
                        LegalNumber = model.LegalNumber,
                        SourceModule = "Journal Adjustment",
                        OverallStatus = model.OverallStatus,
                        OverallStatusDesc = model.OverallStatus,
                        FundingRerenceNo = voucherDetail.FundingRefNo,
                        InstitutionCode = voucherInstitutionCode,
                        InstitutionName = voucherInstitutionName,
                        JournalTypeCode = trxReversalVm.JournalCode,
                        GlAccount = voucherDetail.GlAccount,
                        GlAccountDesc = coa.GlAccountDesc,
                        GfsCode = coa.GfsCode,
                        GfsCodeCategory = coa.GfsCodeCategory,
                        TransactionCategory = voucherDetail.TransactionType == "CR"? "Expenditure":"Revenue",
                        VoteDesc = coa.VoteDesc,
                        GeographicalLocationDesc = coa.GeographicalLocationDesc,
                        TrDesc = coa.TrDesc,
                        SubBudgetClass = coa.SubBudgetClass,
                        SubBudgetClassDesc = coa.subBudgetClassDesc,
                        ProjectDesc = coa.ProjectDesc,
                        ServiceOutputDesc = coa.ServiceOutputDesc,
                        ActivityDesc = coa.ActivityDesc,
                        FundTypeDesc = coa.FundTypeDesc,
                        CofogDesc = coa.CofogDesc,
                        SubLevelCode = voucherDetail.SubLevelCode,
                        FinancialYear = serviceManager.GetFinancialYear(trxReversalVm.ApplyDate),
                        OperationalAmount = voucherDetail.OperationalAmount,
                        BaseAmount = voucherDetail.BaseAmount,
                        Currency = model.CurrencyCode,
                        CreatedAt = DateTime.Now,
                        CreatedBy = model.CreatedBy,
                        ApplyDate = model.ApplyDate,
                        //PayeeCode = voucherDetail.PayeeID,
                        //PayeeName = voucherDetail.PayeeName,
                        //TransactionDesc = voucherDetail.TransactionDesc,
                        TR = coa.TR,
                        Facility = coa.Facility,
                        FacilityDesc = coa.FacilityDesc,
                        //SourceModuleRefNo = paymentVoucherSummary.SourceModuleReferenceNo,
                        CostCentre = coa.CostCentre,
                        CostCentreDesc = coa.CostCentreDesc,
                        Level1Code = userPaystation.Institution.Level1Code,
                        InstitutionLevel = userPaystation.Institution.InstitutionLevel,
                        Level1Desc = coa.Level1Desc,
                        SubVote = coa.SubVote,
                        SubVoteDesc = coa.SubVoteDesc,
                        FundingSourceDesc = coa.FundingSourceDesc,
                    };
                    transactionLogVMs.Add(transactionLogVM);
                }
                response = fundBalanceServices.PostTransaction(transactionLogVMs);
            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
            }

            return Content(response);
        }

        public JsonResult GetJournalType()
        {
            List<JournalTypeSummary> journalTypesList = db.JournalTypeSummaries
                .Where(a => a.IsGL == true)
                .ToList();
            return Json(new { data = journalTypesList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPayement(string journalTypeCode, string search)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            List<GeneralLedger> list = db.GeneralLedgers
                .Where(a => a.JournalTypeCode == journalTypeCode
                   && a.InstitutionCode == userPaystation.InstitutionCode
                 //  && (a.ReversalFlag == false || a.ReversalFlag == null)
                 )
                .Where(a => a.LegalNumber.Contains(search))
                .Take(20)
                .ToList();
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CheckTransactionReverseStatus(string LegalNumber)
        {
            db.Database.CommandTimeout = 120;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<TransactionReverseStatusView> list = new List<TransactionReverseStatusView> { };
            //List<TransactionReverseStatusView> list = db.TransactionReverseStatusViews
            //    .Where(a => a.LegalNumber == LegalNumber
            //       && a.InstitutionCode == userPaystation.InstitutionCode)
            //    .ToList();
            //if (list.Count() > 1)
            //{
            //    list = list.Take(1).ToList();
            //}
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetFundBalance(string subBudgetClass, string search = "a")
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<FundBalanceView> fundBalanceList = new List<FundBalanceView>() { };
            try
            {
                fundBalanceList = db.FundBalanceViews
                .Where(a => a.JournalTypeCode == "GJ" && a.InstitutionCode == userPaystation.InstitutionCode
                 && a.SubBudgetClass == subBudgetClass)
                .Where(a => a.GlAccount.Contains(search)
                  || a.GlAccountDesc.Contains(search)
                  || a.FundingSource.Contains(search))
                .Take(20)
                .ToList();
            }
            catch (Exception ex)
            {

            }
            return Json(new { data = fundBalanceList }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetFundBalance1(string subBudgetClass, string JournalCode, string subWarrant)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
           // List<FundBalanceView> fundBalanceList = new List<FundBalanceView>() { };
            BalanceResponse fundBalanceResponse = new BalanceResponse();
            try
            {
              
                if (JournalCode == "AJ")
                {
                    fundBalanceResponse =
                                  serviceManager.GetFundBalanceAJ(userPaystation.InstitutionCode);
                }
                else if (JournalCode == "SW")
                {
                    fundBalanceResponse = serviceManager.GetFundBalanceSW(userPaystation.InstitutionCode, subBudgetClass);
                }
                else
                {
                    fundBalanceResponse = serviceManager.GetFundBalanceGJ(userPaystation.InstitutionCode, subBudgetClass);
                }

                if (fundBalanceResponse.overallStatus == "Error")
                {
                    return Json(new { data = fundBalanceResponse.FundBalanceViewList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {

            }
            var data = fundBalanceResponse.FundBalanceViewList;
            if (subWarrant != null && subWarrant != "")
            {
                data = data.Where(a => a.SublevelCode == subWarrant).ToList();
            }
            var response = Json(new { data }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
        }

        [HttpGet, Authorize(Roles = "Transaction Adjustment Approval")]
        public ActionResult ApproveTransationAdjustment()
        {
            return View();
        }

        public ActionResult ApproveTrxAdjustmentSummary(int TrxAdjustmentSummaryId, string Status)
        {
            string response = "Success";
            db.Database.CommandTimeout = 1200;

            try
            {
                TransactionAdjustmentSummary model = db
                    .TransactionAdjustmentSummaries
                    .Find(TrxAdjustmentSummaryId);

                var currenstStatus = model.OverallStatus;

                model.ApprovalStatus = Status == "Approved" ? Status : null;
                model.OverallStatus = Status;
                model.ApprovedAt = DateTime.Now;
                model.ApprovedBy = User.Identity.GetUserName();

                db.SaveChanges();

                response = fundBalanceServices.UpdateTransaction(model.LegalNumber, model.TrxAdjustmentSummaryId, model.OverallStatus);
                if (response != "Success")
                {
                    model.OverallStatus = currenstStatus;
                    db.SaveChanges();
                    return Content(response);
                }

                if (Status == "Approved")
                {
                    var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", model.JournalTypeCode) };
                    db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Content(response);
        }

        public ActionResult RejectTrxAdjustmentSummary(int TrxAdjustmentSummaryId, string RejectionReason)
        {
            string response = "Success";
            db.Database.CommandTimeout = 1200;
 
                try
                {
                    TransactionAdjustmentSummary model = db
                        .TransactionAdjustmentSummaries
                        .Find(TrxAdjustmentSummaryId);
                var currentStatus = model.OverallStatus;
                    model.ApprovalStatus = "Rejected";
                    model.OverallStatus = "Rejected";
                    model.RejectionReason = RejectionReason;
                    db.SaveChanges();

                    response = fundBalanceServices.UpdateTransaction(model.LegalNumber, model.TrxAdjustmentSummaryId, model.OverallStatus);
                    if (response != "Success")
                    {
                     model.OverallStatus = currentStatus;
                     db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    response = ex.InnerException.ToString();
                }
            return Content(response);
        }

        public JsonResult GetTransactionAdjustmentSummary(string Status = "Pending")
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<TransactionAdjustmentSummary> list = new List<TransactionAdjustmentSummary>();
            if (Status == "Pending")
            {
                list = db.TransactionAdjustmentSummaries
                    .Where(a => (a.OverallStatus == Status || a.OverallStatus == "Rejected")
                     && a.InstitutionCode == userPaystation.InstitutionCode)
                    .ToList();
            }
            else
            {
                list = db.TransactionAdjustmentSummaries
                               .Where(a => a.OverallStatus == Status
                                && a.InstitutionCode == userPaystation.InstitutionCode)
                               .ToList();
            }
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetTransactionAdjustmentDetails(int TrxId)
        {
            List<TransactionAdjustementDetail> list = db.TransactionAdjustementDetails
                .Where(a => a.TrxAdjustmentSummaryId == TrxId)
                .ToList();
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult TransactionAdjustmentTrackerList()
        {
            return View();
        }


        public JsonResult GetTransactionAdjustmentTracker(PaymentTrackerVm paymentTracker)
        {

            string search = paymentTracker.keywords == null ? "0" : paymentTracker.keywords;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<TransactionAdjustmentSummary> paymentVoucherList = new List<TransactionAdjustmentSummary>();
            if (paymentTracker.OverrallStatus == "All")
            {
                if (paymentTracker.FromDate.Year == 0001
                    || paymentTracker.ToDate.Year == 0001)
                {
                    paymentVoucherList = db.TransactionAdjustmentSummaries
                        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                          && a.OverallStatus != "Cancelled")
                        .Where(b => b.TrxAdjustmentCategory.Contains(search)
                              || b.LegalNumber.Contains(search)
                              || b.RejectionReason.Contains(search)
                              || b.AdjustmentReasonDesc.Contains(paymentTracker.keywords))
                         .OrderByDescending(a => a.TrxAdjustmentSummaryId)
                         .ToList();
                }
                else
                {
                    paymentVoucherList = db.TransactionAdjustmentSummaries
                        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                           && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(paymentTracker.FromDate)
                           && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(paymentTracker.ToDate)))
                             .Where(b => b.TrxAdjustmentCategory.Contains(search)
                              || b.LegalNumber.Contains(search)
                              || b.RejectionReason.Contains(search)
                              || b.AdjustmentReasonDesc.Contains(paymentTracker.keywords))
                         .OrderByDescending(a => a.TrxAdjustmentSummaryId)
                         .ToList();
                }
            }
            else
            {
                if (paymentTracker.FromDate.Year == 0001
                   || paymentTracker.ToDate.Year == 0001)
                {
                    paymentVoucherList = db.TransactionAdjustmentSummaries
                        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                         && a.OverallStatus == paymentTracker.OverrallStatus)
                          .Where(b => b.TrxAdjustmentCategory.Contains(search)
                              || b.LegalNumber.Contains(search)
                              || b.RejectionReason.Contains(search)
                              || b.AdjustmentReasonDesc.Contains(paymentTracker.keywords))
                         .OrderByDescending(a => a.TrxAdjustmentSummaryId)
                         .ToList();
                }
                else
                {
                    paymentVoucherList = db.TransactionAdjustmentSummaries
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                       && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(paymentTracker.FromDate)
                       && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(paymentTracker.ToDate))
                     && a.OverallStatus == paymentTracker.OverrallStatus)
                    .Where(b => b.TrxAdjustmentCategory.Contains(search)
                              || b.LegalNumber.Contains(search)
                              || b.RejectionReason.Contains(search)
                              || b.AdjustmentReasonDesc.Contains(paymentTracker.keywords))
                         .OrderByDescending(a => a.TrxAdjustmentSummaryId)
                         .ToList();
                }

            }
            return Json(new { data = paymentVoucherList }, JsonRequestBehavior.AllowGet);
        }
    }
}