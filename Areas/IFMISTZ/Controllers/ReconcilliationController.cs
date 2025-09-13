using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Transactions;
using System.Web.Mvc;
using static IFMIS.Libraries.ServiceManager;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class ReconcilliationController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();

        [HttpGet, Authorize(Roles = "Reconciliation Entry")]
        public ActionResult ReconcilliationStatus(string month, int? year)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.Accounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNo).ToList();
            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            db.Database.CommandTimeout = 1200;
            List<ReconciliationStatusVw> status = new List<ReconciliationStatusVw>();
            using (var t = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
                }))
            {
                status = (from a in db.ReconciliationStatusVws 
                          where userAccount.Contains(a.BankAccount)
                          select a).ToList();
            }

            List<ReconciliationStatusVM> data = new List<ReconciliationStatusVM>();
            if (month == null && year == null)
            {
                month = DateTime.Now.ToString("MM");
                year = Convert.ToInt32(DateTime.Now.ToString("yyyy"));
                var endDate = ServiceManager.GetEndDateByMonthId(month, year);
                var checkdate = year + "/" + month + "/" + endDate;
                foreach (var item in status)
                {
                    var vm = new ReconciliationStatusVM
                    {
                        ID = item.ID,
                        BankAccount = item.BankAccount,
                        AccountName = item.AccountName,
                        AutoMatched = item.AutoMatched,
                        ManualMatched = item.ManualMatched,
                        GLOutostanding = item.GLOutostanding,
                        BankOutostanding = item.BankOutostanding,
                        CheckDate = item.CheckDate

                    };
                    data.Add(vm);
                }
                data = data.Where(p => p.CheckDate == Convert.ToDateTime(checkdate)).ToList();
            }
            else
            {

                var endDate = ServiceManager.GetEndDateByMonthId(month, year);

                var checkdate = year + "/" + month + "/" + endDate;

                foreach (var item in status)
                {
                    var vm = new ReconciliationStatusVM
                    {
                        ID = item.ID,
                        BankAccount = item.BankAccount,
                        AccountName = item.AccountName,
                        AutoMatched = item.AutoMatched,
                        ManualMatched = item.ManualMatched,
                        GLOutostanding = item.GLOutostanding,
                        BankOutostanding = item.BankOutostanding,
                        CheckDate = item.CheckDate

                    };
                    data.Add(vm);
                }

                data = data.Where(p => p.CheckDate == Convert.ToDateTime(checkdate)).ToList();
            }
            return View(data);
        }


        public ActionResult MatchedList(string BankAcct, DateTime checkDate)
        {
            if (checkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var matchedList = db.MatchedVws
                .Where(a => a.BankAccount == BankAcct
                && a.CheckDate == checkDate)
                .ToList();
       
            return PartialView("_MatchedList", matchedList);
        }

        public ActionResult MatchedListDt(string BankAcct, DateTime checkDate)
        {
            if (checkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var matchedList = db.MatchedVws
                .Where(a => a.BankAccount == BankAcct && a.CheckDate == checkDate)
                .Take(50)
                .ToList();
            ViewBag.checkDate = checkDate;
            ViewBag.BankAcct = BankAcct;
            return View(matchedList);
        }

        public JsonResult GetMatchedListDt(string BankAcct, DateTime checkDate, string search)
        {
            var matchedList = db.MatchedVws
                .Where(a => a.BankAccount == BankAcct && a.CheckDate == checkDate)
                .Where(a => a.MatchNo.Contains(search) || a.LegalNumber.Contains(search) || a.RefBank.Contains(search) || a.RelatedRefBank.Contains(search))
                .Take(20)
                .ToList();
            return Json(new { data = matchedList }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GLOutStandingList(string BankAcct, DateTime checkDate)
        {
            List<GLOutoStandingVw> GloutostandingList = null;
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, BankAcct, checkDate);
            }
            if (reconcilliationResponse.overallStatus == "Error")
            {
                //Handle error 
            }
            GloutostandingList = reconcilliationResponse.GLOutoStandingVwList;
            return PartialView("_GLOutStandingList", GloutostandingList);
        }

        public ActionResult BankOutStandingList(string BankAcct, DateTime checkDate)
        {

            List<BankOutoStandingVw> BankStandingList = null;
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponse = ReconcilliationService.GetBankOutStanding(db, BankAcct, checkDate);
            }
            if (reconcilliationResponse.overallStatus == "Error")
            {
                //Handle error 
            }
            BankStandingList = reconcilliationResponse.BankOutStandingVwList;
            return PartialView("_BankOutStandingList", BankStandingList);
        }


        public ActionResult ManualMatchingList2(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "" && BankAcct == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (checkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var manualMatchingList = (from a in db.ManualMatchedViews
                                      where a.BankAccount == BankAcct
                                      where a.CheckDate == checkDate
                                      select a
                                     ).ToList();

            return PartialView("_ManualMatchedList2", manualMatchingList);
        }


        public ActionResult ManualMatchingList(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "" && BankAcct == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (checkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var manualMatchingList = (from a in db.GeneralLedgers
                                      join b in db.BankStatementDetails
                                      on a.MatchNo equals b.MatchNo
                                      where a.BankAccountNumber == BankAcct
                                      where a.CheckDate == checkDate
                                      where a.ReconciliationType == "Manual"
                                      where b.ReconciliationType == "Manual"
                                      where a.ReconciliationStatus == "Matched"
                                      where b.ReconciliationStatus == "Matched"
                                      join c in db.BankStatementSummarys
                                     on b.BankStatementSummaryId equals c.BankStatementSummaryId
                                      where c.BankAccountNumber == BankAcct
                                      select new { a, b, c } into d
                                      select new ManualMatchedVM
                                      {
                                          TransactionType = d.a.TransactionType,
                                          BankAccount = d.a.BankAccountNumber,
                                          AccountNameGL = d.c.BankAccountName,
                                          TransactionDate = d.a.ApplyDate,
                                          LegalNumber = d.a.LegalNumber,
                                          DocumentNo = d.a.DocumentNo,
                                          DescriptionGL = d.a.TransactionDesc,
                                          OperationalAmountGL = d.a.OperationalAmount,
                                          MatchNo = d.a.MatchNo,
                                          BankDate = d.c.StatementDate,
                                          RefBank = d.b.TransactionRef,
                                          RelatedRefBank = d.b.RelatedRef,
                                          AmountBank = d.b.TransactionAmount,
                                          BankMatchedNo = d.b.MatchNo,
                                      }).ToList();

            return PartialView("_ManualMatchedList", manualMatchingList);
        }

        [HttpGet, Authorize(Roles = "Reconciliation Entry")]
        public ActionResult SearchReconciliation()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            SearchReconcilliationVM vm = new SearchReconcilliationVM();
            var AccountList = ServiceManager
                .GetAccountListrec(db, institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            return View(vm);
        }

        //public ActionResult GetTransactionList(string AccountNumber, DateTime chkDate)
        //{
        //    string month;
        //    int year;
        //    string currentMonth;
        //    string response;
        //    PostReconcilliationVM vm = new PostReconcilliationVM();
        //    try
        //    {
        //    //currentMonth = DateTime.Now.ToString("MM");
        //    if (chkDate == null)
        //    {
        //        response = "Please Renconcilled Date is Required!";
        //        return Json(response, JsonRequestBehavior.AllowGet);
        //    }

        //    if (AccountNumber == "")
        //    {
        //        response = "Please Account Number is Required!";
        //        return Json(response, JsonRequestBehavior.AllowGet);
        //    }

        //    month = chkDate.ToString("MM");
        //    year = chkDate.Year;
        //    var endDate = ServiceManager.GetEndDateByMonthId(month, year);
        //    DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
        //        DateTime checkdateCb = checkdate.AddHours(23).AddMinutes(59).AddSeconds(59);


        //        //var xxx = checkdate.AddHours(23);
        //        vm.CheckDate = checkdate;
        //    vm.BankAccount = AccountNumber;

        //    //Mwandiko mpya 15/06/2020 by Samwel SKL//
        //    //------GL OutStanding--------//
        //    db.Database.CommandTimeout = 12000;
        //    List<GLOutoStandingVw> GLOutStandingVwsList = null;
        //    ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
        //    if (AccountNumber != "" && checkdate != null)
        //    {
        //            reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, AccountNumber, checkdate);
        //    }
        //    if (reconcilliationResponse.overallStatus == "Error")
        //    {
        //        //Handle error 
        //    }
        //    GLOutStandingVwsList = reconcilliationResponse.GLOutoStandingVwList;

        //    //Total Gl
        //    if (GLOutStandingVwsList != null){
        //        vm.TotalOutStandingGL = GLOutStandingVwsList
        //           .Sum(a => a.OperationalAmountGL);
        //    }else{
        //        vm.TotalOutStandingGL = 0;
        //    }

        //    //Payment GL
        //    if (GLOutStandingVwsList != null)
        //    {
        //        var totalpaymentGLOutoStanding = GLOutStandingVwsList
        //            .Where(a => a.TransactionType == "CR")
        //            .OrderByDescending(a => a.ID).ToList();
        //        vm.TotalOutStandingPaymentGL = -1 * (totalpaymentGLOutoStanding
        //            .Sum(a => a.OperationalAmountGL));
        //    }
        //    else
        //    {
        //        vm.TotalOutStandingPaymentGL = 0;
        //    }
        //    //GL Receipt
        //    if (GLOutStandingVwsList != null)
        //    {
        //        var totalReceiptGLOutoStanding2 = GLOutStandingVwsList
        //            .Where(a => a.TransactionType == "DR")
        //            .OrderByDescending(a => a.ID).ToList();
        //        vm.TotalOutStandingReceiptGL = totalReceiptGLOutoStanding2
        //            .Sum(a => a.OperationalAmountGL);
        //    }
        //    else
        //    {
        //        vm.TotalOutStandingReceiptGL = 0;
        //    }
        //    //---End GL Outstanding------//

        //    //Bank OutStanding
        //    db.Database.CommandTimeout = 1200;
        //    List<BankOutoStandingVw> BankOutoStandingList = null;
        //    ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
        //    if (AccountNumber != "" && checkdate != null)
        //    {
        //        reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, AccountNumber, checkdate);
        //    }
        //    if (reconcilliationResponseBank.overallStatus == "Error")
        //    {
        //        //Handle error 
        //    }
        //    BankOutoStandingList = reconcilliationResponseBank.BankOutStandingVwList;
        //    //Total Bank
        //    if (BankOutoStandingList != null)
        //    {
        //        vm.TotalOutStandingBank = BankOutoStandingList
        //           .Sum(a => a.Amount);
        //    }
        //    else
        //    {
        //        vm.TotalOutStandingBank = 0;
        //    }

        //    //Payment bank
        //    if (BankOutoStandingList != null)
        //    {
        //        var totalPaymentBankOutStanding = BankOutoStandingList
        //            .Where(a => a.TransactionType == "DR")
        //            .OrderByDescending(a => a.ID).ToList();
        //        vm.TotalOutStandingPaymentBank = -1 * (totalPaymentBankOutStanding
        //            .Sum(a => a.Amount));
        //    }
        //    else
        //    {
        //        vm.TotalOutStandingPaymentBank = 0;
        //    }

        //    //Receipt bank
        //    if (BankOutoStandingList != null)
        //    {
        //        var totalReceiptBankOutStanding = BankOutoStandingList
        //            .Where(a => a.TransactionType == "CR")
        //            .OrderByDescending(a => a.ID).ToList();
        //        vm.TotalOutStandingReceiptBank = totalReceiptBankOutStanding
        //            .Sum(a => a.Amount);
        //    }
        //    else
        //    {
        //        vm.TotalOutStandingReceiptBank = 0;
        //    }
        //    //--End Bank OutStanding --//

        //    //ReceiptBankPaGL
        //    var totalReceiptBankPaGL = vm.TotalOutStandingReceiptBank + vm.TotalOutStandingPaymentGL;
        //    if (totalReceiptBankPaGL != null)
        //    {
        //        vm.ReceiptBankPaGL = totalReceiptBankPaGL;
        //    }
        //    else
        //    {
        //        vm.ReceiptBankPaGL = 0;
        //    }

        //    //PaBankReceiptGL
        //    var totalPaBankReceiptGL = vm.TotalOutStandingPaymentBank + vm.TotalOutStandingReceiptGL;
        //    if (totalPaBankReceiptGL != null)
        //    {
        //        vm.PaBankReceiptGL = totalPaBankReceiptGL;
        //    }
        //    else
        //    {
        //        vm.PaBankReceiptGL = 0;
        //    }

        //    //Mwandiko mpya 15/06/2020
        //    List<BankStatementSummary> BankStatementSummaryList = null;
        //    BankStatementSummaryList = db.BankStatementSummarys
        //        .Where(a => a.BankAccountNumber == AccountNumber
        //        //&& a.OverallStatus=="Approved"
        //         && a.StatementDate <= checkdate)
        //        .ToList();

        //    DateTime minDate;
        //    if (BankStatementSummaryList.Count() != 0) { 
        //     minDate = BankStatementSummaryList
        //        .Select(a => a.StatementDate)
        //        .Min();
        //    }else
        //    {
        //        minDate = checkdate;
        //    }

        //    DateTime maxDate;
        //    if (BankStatementSummaryList.Count() != 0)
        //    {
        //        maxDate = BankStatementSummaryList
        //        .Select(a => a.StatementDate)
        //        .Max();
        //    }
        //    else
        //    {
        //        maxDate = checkdate;
        //    }

        //    //Opening balance as per Bank Statement
        //    var OpenBal = BankStatementSummaryList
        //                    .Where(a => a.StatementDate == minDate)
        //                    .OrderByDescending(a => a.BankStatementSummaryId)
        //                    .FirstOrDefault();

        //    if (OpenBal != null)
        //    {
        //        vm.OpeningBalanceBank = (decimal)OpenBal.OpeningBalance;
        //    }
        //    else
        //    {
        //        vm.OpeningBalanceBank = 0;
        //    }

        //    //closing Balance as per Bank Statement
        //    var ClosingBal = BankStatementSummaryList
        //                    .Where(a => a.StatementDate == maxDate)
        //                    .OrderByDescending(a => a.BankStatementSummaryId)
        //                    .FirstOrDefault();

        //    if (ClosingBal != null)
        //    {
        //        vm.ClosingBalanceBank = (decimal)ClosingBal.ClosingBalance;
        //    }
        //    else
        //    {
        //        vm.ClosingBalanceBank = 0;
        //    }

        //    //Closing balance 
        //    List<GeneralLedger> GeneralLedgersList  = new List<GeneralLedger>();
        //    using (var t = new TransactionScope(TransactionScopeOption.Required,
        //        new TransactionOptions
        //        {
        //            IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
        //        }))
        //    {
        //        GeneralLedgersList = db.GeneralLedgers
        //       .Where(a => a.BankAccountNumber == AccountNumber
        //        && a.CheckDate <= checkdate)
        //       .ToList();
        //    }

        //    var MaxApplyDate = GeneralLedgersList
        //        .Select(a => a.ApplyDate)
        //        .Max();

        //    string applydate1 = Convert.ToDateTime(MaxApplyDate).ToString("MM/dd/yyyy");
        //    DateTime applydate2 = Convert.ToDateTime(applydate1);
        //    DateTime applydate3 = applydate2.AddHours(23).AddMinutes(59).AddSeconds(59);

        //    //closing balance 
        //    var closingcashBookBalance = GeneralLedgersList
        //                   .Where(a => a.ApplyDate <= applydate3
        //                   && a.ReconciliationStatus != "Memorandum").
        //                   OrderByDescending(a => a.GeneralLedgerId)
        //                   .ToList();
        //    if (closingcashBookBalance != null)
        //    {
        //        vm.ClosingBalanceGL = (decimal)closingcashBookBalance
        //            .Sum(a => a.OperationalNetChange);
        //    }
        //    else
        //    {
        //        vm.ClosingBalanceGL = 0;
        //    }

        //    //Adjusted Balance
        //    var totalAdjustedBalance = vm.ClosingBalanceBank - (vm.TotalOutStandingReceiptBank + vm.TotalOutStandingPaymentGL) + (vm.TotalOutStandingPaymentBank + vm.TotalOutStandingReceiptGL);
        //    if (totalAdjustedBalance != null)
        //    {
        //        vm.TotalAdjustedBalance = (decimal)totalAdjustedBalance;
        //    }
        //    else
        //    {
        //        vm.TotalAdjustedBalance = 0;
        //    }

        //    }catch (Exception ex)
        //    {
        //        response = ex.Message.ToString();
        //    }

        //    return PartialView("_PostReconcilliation", vm);
        //}

        public ActionResult GetTransactionList(string AccountNumber, DateTime chkDate)
        {
            string month;
            int year;
            string currentMonth;
            string response;
            PostReconcilliationVM vm = new PostReconcilliationVM();
            try
            {
                //currentMonth = DateTime.Now.ToString("MM");
                if (chkDate == null)
                {
                    response = "Please Renconcilled Date is Required!";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                if (AccountNumber == "")
                {
                    response = "Please Account Number is Required!";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                month = chkDate.ToString("MM");
                year = chkDate.Year;
                var endDate = ServiceManager.GetEndDateByMonthId(month, year);
                DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
                DateTime checkdateCb = checkdate.AddHours(23).AddMinutes(59).AddSeconds(59);


                //var xxx = checkdate.AddHours(23);
                vm.CheckDate = checkdate;
                vm.BankAccount = AccountNumber;

                //Mwandiko mpya 15/06/2020 by Samwel SKL//
                //------GL OutStanding--------//
                db.Database.CommandTimeout = 12000;
                List<GLOutoStandingVw> GLOutStandingVwsList = null;
                ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
                if (AccountNumber != "" && checkdate != null)
                {
                    reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, AccountNumber, checkdate);
                }
                if (reconcilliationResponse.overallStatus == "Error")
                {
                    //Handle error 
                }
                GLOutStandingVwsList = reconcilliationResponse.GLOutoStandingVwList;

                //Total Gl
                if (GLOutStandingVwsList != null)
                {
                    vm.TotalOutStandingGL = GLOutStandingVwsList
                       .Sum(a => a.OperationalAmountGL);
                }
                else
                {
                    vm.TotalOutStandingGL = 0;
                }

                //Payment GL
                if (GLOutStandingVwsList != null)
                {
                    var totalpaymentGLOutoStanding = GLOutStandingVwsList
                        .Where(a => a.TransactionType == "CR")
                        .OrderByDescending(a => a.ID).ToList();
                    vm.TotalOutStandingPaymentGL = -1 * (totalpaymentGLOutoStanding
                        .Sum(a => a.OperationalAmountGL));
                }
                else
                {
                    vm.TotalOutStandingPaymentGL = 0;
                }
                //GL Receipt
                if (GLOutStandingVwsList != null)
                {
                    var totalReceiptGLOutoStanding2 = GLOutStandingVwsList
                        .Where(a => a.TransactionType == "DR")
                        .OrderByDescending(a => a.ID).ToList();
                    vm.TotalOutStandingReceiptGL = totalReceiptGLOutoStanding2
                        .Sum(a => a.OperationalAmountGL);
                }
                else
                {
                    vm.TotalOutStandingReceiptGL = 0;
                }
                //---End GL Outstanding------//

                //Bank OutStanding
                db.Database.CommandTimeout = 1200;
                List<BankOutoStandingVw> BankOutoStandingList = null;
                ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
                if (AccountNumber != "" && checkdate != null)
                {
                    reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, AccountNumber, checkdate);
                }
                if (reconcilliationResponseBank.overallStatus == "Error")
                {
                    //Handle error 
                }
                BankOutoStandingList = reconcilliationResponseBank.BankOutStandingVwList;
                //Total Bank
                if (BankOutoStandingList != null)
                {
                    vm.TotalOutStandingBank = BankOutoStandingList
                       .Sum(a => a.Amount);
                }
                else
                {
                    vm.TotalOutStandingBank = 0;
                }

                //Payment bank
                if (BankOutoStandingList != null)
                {
                    var totalPaymentBankOutStanding = BankOutoStandingList
                        .Where(a => a.TransactionType == "DR")
                        .OrderByDescending(a => a.ID).ToList();
                    vm.TotalOutStandingPaymentBank = -1 * (totalPaymentBankOutStanding
                        .Sum(a => a.Amount));
                }
                else
                {
                    vm.TotalOutStandingPaymentBank = 0;
                }

                //Receipt bank
                if (BankOutoStandingList != null)
                {
                    var totalReceiptBankOutStanding = BankOutoStandingList
                        .Where(a => a.TransactionType == "CR")
                        .OrderByDescending(a => a.ID).ToList();
                    vm.TotalOutStandingReceiptBank = totalReceiptBankOutStanding
                        .Sum(a => a.Amount);
                }
                else
                {
                    vm.TotalOutStandingReceiptBank = 0;
                }
                //--End Bank OutStanding --//

                //ReceiptBankPaGL
                var totalReceiptBankPaGL = vm.TotalOutStandingReceiptBank + vm.TotalOutStandingPaymentGL;
                if (totalReceiptBankPaGL != null)
                {
                    vm.ReceiptBankPaGL = totalReceiptBankPaGL;
                }
                else
                {
                    vm.ReceiptBankPaGL = 0;
                }

                //PaBankReceiptGL
                var totalPaBankReceiptGL = vm.TotalOutStandingPaymentBank + vm.TotalOutStandingReceiptGL;
                if (totalPaBankReceiptGL != null)
                {
                    vm.PaBankReceiptGL = totalPaBankReceiptGL;
                }
                else
                {
                    vm.PaBankReceiptGL = 0;
                }

                //Mwandiko mpya 15/06/2020
                List<BankStatementSummary> BankStatementSummaryList = null;
                BankStatementSummaryList = db.BankStatementSummarys
                    .Where(a => a.BankAccountNumber == AccountNumber
                     //&& a.OverallStatus=="Approved"
                     && a.StatementDate <= checkdate)
                    .ToList();

                DateTime minDate;
                if (BankStatementSummaryList.Count() != 0)
                {
                    minDate = BankStatementSummaryList
                       .Select(a => a.StatementDate)
                       .Min();
                }
                else
                {
                    minDate = checkdate;
                }

                DateTime maxDate;
                if (BankStatementSummaryList.Count() != 0)
                {
                    maxDate = BankStatementSummaryList
                    .Select(a => a.StatementDate)
                    .Max();
                }
                else
                {
                    maxDate = checkdate;
                }

                //Opening balance as per Bank Statement
                var OpenBal = BankStatementSummaryList
                                .Where(a => a.StatementDate == minDate)
                                .OrderByDescending(a => a.BankStatementSummaryId)
                                .FirstOrDefault();

                if (OpenBal != null)
                {
                    vm.OpeningBalanceBank = (decimal)OpenBal.OpeningBalance;
                }
                else
                {
                    vm.OpeningBalanceBank = 0;
                }

                //closing Balance as per Bank Statement
                var ClosingBal = BankStatementSummaryList
                                .Where(a => a.StatementDate == maxDate)
                                .OrderByDescending(a => a.BankStatementSummaryId)
                                .FirstOrDefault();

                if (ClosingBal != null)
                {
                    vm.ClosingBalanceBank = (decimal)ClosingBal.ClosingBalance;
                }
                else
                {
                    vm.ClosingBalanceBank = 0;
                }

                //Closing balance 
                List<GeneralLedger> GeneralLedgersList = new List<GeneralLedger>();
                using (var t = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions
                    {
                        IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
                    }))
                {
                    GeneralLedgersList = db.GeneralLedgers
                   .Where(a => a.BankAccountNumber == AccountNumber
                    && a.CheckDate <= checkdate
                    && a.ReconciliationStatus != "Memorandum")
                   .ToList();
                }

                //var MaxApplyDate = GeneralLedgersList
                //    .Select(a => a.ApplyDate)
                //    .Max();

                //string applydate1 = Convert.ToDateTime(MaxApplyDate).ToString("MM/dd/yyyy");
                //DateTime applydate2 = Convert.ToDateTime(applydate1);
                //DateTime applydate3 = applydate2.AddHours(23).AddMinutes(59).AddSeconds(59);

                //closing balance 
                //var closingcashBookBalance = GeneralLedgersList
                //               .Where(a => a.ApplyDate <= applydate3
                //               && a.ReconciliationStatus != "Memorandum").
                //               OrderByDescending(a => a.GeneralLedgerId)
                //               .ToList();
                if (GeneralLedgersList != null)
                {
                    vm.ClosingBalanceGL = (decimal)GeneralLedgersList
                        .Sum(a => a.OperationalNetChange);
                }
                else
                {
                    vm.ClosingBalanceGL = 0;
                }

                //Adjusted Balance
                var totalAdjustedBalance = vm.ClosingBalanceBank - (vm.TotalOutStandingReceiptBank + vm.TotalOutStandingPaymentGL) + (vm.TotalOutStandingPaymentBank + vm.TotalOutStandingReceiptGL);
                if (totalAdjustedBalance != null)
                {
                    vm.TotalAdjustedBalance = (decimal)totalAdjustedBalance;
                }
                else
                {
                    vm.TotalAdjustedBalance = 0;
                }

            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            return PartialView("_PostReconcilliation", vm);
        }


        [HttpPost, Authorize(Roles = "Reconciliation Entry")]
        public JsonResult PostReconcilliation(PostReconcilliationVM postReconcilliationVM)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string response = "";
            db.Database.CommandTimeout = 1200;
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var accname = db.InstitutionAccounts
                    .Where(a => a.AccountNumber == postReconcilliationVM.BankAccount)
                    .Select(a => a.AccountName)
                    .FirstOrDefault();

                    int financialyear = ServiceManager
                        .GetFinancialYear(db, (DateTime)postReconcilliationVM.CheckDate);
                    int finacialperiod = ServiceManager
                        .GetFinancialPeriod((DateTime)postReconcilliationVM.CheckDate);

                    var exstinginpost = db.ReconciliationPosteds
                        .Where(a => a.AccountNumber == postReconcilliationVM.BankAccount
                        && a.CheckDate == postReconcilliationVM.CheckDate
                        && a.OverallStatus != "Rejected").Count();

                    int currentmonth = Convert.ToInt32(DateTime.Now.ToString("MM"));
                    DateTime x = Convert.ToDateTime(postReconcilliationVM.CheckDate.ToString());
                    string xy = x.ToString("dd/MM/yyyy");
                    //int seleckedMonthchk = Convert.ToInt32((xy).ToString().Substring(0, 2));
                    //month = item2.ToString("MM");///xy.Substring(0, 2);
                    int seleckedMonthchk = Convert.ToInt32(postReconcilliationVM.CheckDate.ToString("MM"));
                    //if (currentmonth >= seleckedMonthchk)
                    //{

                    if (exstinginpost != 0)
                    {
                        response = "Fail";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }

                    ReconciliationPosted post = new ReconciliationPosted()
                    {
                        ReconciliationNo = Convert.ToInt32(financialyear.ToString() + finacialperiod.ToString()),
                        InstitutionId = userPaystation.InstitutionId,
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionName = userPaystation.Institution.InstitutionName,
                        PaystationId = userPaystation.InstitutionSubLevelId,
                        ReconciliationDate = DateTime.Now,
                        AccountNumber = postReconcilliationVM.BankAccount,
                        AccountName = accname,
                        FinancialPeriod = finacialperiod,
                        FinancialYear = financialyear,
                        ConfirmBy = User.Identity.Name,
                        ConfirmAt = DateTime.Now,
                        OverallStatus = "Confirmed",
                        CurrencyId = 1,
                        TotalReconciled = postReconcilliationVM.TotalReconciled,
                        ClosingBalanceGL = postReconcilliationVM.ClosingBalanceGL,
                        OpeningBalanceBank = postReconcilliationVM.OpeningBalanceBank,
                        ClosingBalanceBank = postReconcilliationVM.ClosingBalanceBank,
                        OutStaReceiptBank = postReconcilliationVM.TotalOutStandingReceiptBank,
                        OutStaPaymentBank = postReconcilliationVM.TotalOutStandingPaymentBank,
                        OutStaReceiptGL = postReconcilliationVM.TotalOutStandingReceiptGL,
                        OutStaPaymentGL = postReconcilliationVM.TotalOutStandingPaymentGL,
                        AdjustedBalance = postReconcilliationVM.TotalAdjustedBalance,
                        CheckDate = postReconcilliationVM.CheckDate,
                    };

                    db.ReconciliationPosteds.Add(post);
                    db.SaveChanges();


                    int id = post.ReconciliationPostedId;

                    var postNumber = id.ToString().PadLeft(6, '0');
                    var matchedGL = (from a in db.GeneralLedgers
                                     where a.ReconciliationStatus == "Matched"
                                     where a.CheckDate == postReconcilliationVM.CheckDate
                                     where a.BankAccountNumber == postReconcilliationVM.BankAccount
                                     select a).ToList();

                    var matchedBank = (from a in db.BankStatementDetails
                                       join b in db.BankStatementSummarys
                                       on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                       where b.CheckDate == postReconcilliationVM.CheckDate
                                       where b.BankAccountNumber == postReconcilliationVM.BankAccount
                                       where a.ReconciliationStatus == "Matched"
                                       select a).ToList();


                    foreach (var item in matchedGL)
                    {
                        item.ReconciliationStatus = "Reconcilled";
                        item.ReconciliationNo = Convert.ToInt32(postNumber);
                    }

                    foreach (var item in matchedBank)
                    {
                        item.ReconciliationStatus = "Reconcilled";
                        item.ReconciliationNo = Convert.ToInt32(postNumber);
                    }
                    db.SaveChanges();
                    transaction.Commit();
                    response = "Success";
                }
                catch (Exception ex)
                {
                    response = ex.Message.ToString();
                    transaction.Rollback();
                }
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }



        [HttpGet, Authorize(Roles = "Reconciliation Entry")]
        public ActionResult ConfirmationList()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            var userAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNumber).ToList();

            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


            List<ReconciliationPosted> confirmationList = new List<ReconciliationPosted>();

            confirmationList = db.ReconciliationPosteds
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && ( a.OverallStatus == "Confirmed" || a.OverallStatus == "NULL" || a.OverallStatus == "Rejected")
                ).ToList();


            return View(confirmationList);
        }

        [HttpGet, Authorize(Roles = "Reconciliation Approval")]
        public ActionResult ApprovalList()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            var userAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNumber).ToList();

            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            List<ReconciliationPosted> approvalList = new List<ReconciliationPosted>();

            approvalList =  db.ReconciliationPosteds
                .Where(a=> a.InstitutionCode == userPaystation.InstitutionCode 
                &&  a.OverallStatus == "Confirmed" 
                ).ToList();

            return View(approvalList);
        }

        [HttpGet, Authorize(Roles = "Reconciliation Approval")]
        public ActionResult ApprovalDetails(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager
                .GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            var userAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNumber)
                .ToList();

            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var approvalList = (from a in db.ReconciliationPosteds
                                where a.ReconciliationPostedId == id
                                where a.InstitutionCode == userPaystation.InstitutionCode
                                where a.OverallStatus == "Confirmed"
                                select a).FirstOrDefault();

            return View(approvalList);
        }

        [HttpPost, Authorize(Roles = "Reconciliation Approval")]
        public ActionResult ApproveReconciliation(int id)
        {

            string response = "";
            try
            {
                var approvallist = db.ReconciliationPosteds
                    .Where(a => a.ReconciliationPostedId == id)
                    .FirstOrDefault();

                approvallist.OverallStatus = "Approved";
                approvallist.ApprovedBy = User.Identity.Name;
                approvallist.ApprovedAt = DateTime.Now;


                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {

                response = "DbException";
                response = ex.ToString();
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, Authorize(Roles = "Reconciliation Approval")]
        public ActionResult RejectReconciliation(int ids, string reason)
        {
            string response = "";
            db.Database.CommandTimeout = 1200;
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    ReconciliationPosted posted = db.ReconciliationPosteds
                        .Where(a => a.ReconciliationPostedId == ids)
                        .FirstOrDefault();

                posted.OverallStatus = "Rejected";
                posted.RejectedReason = reason;
                posted.RejectedBy = User.Identity.GetUserName();
                posted.RejectedAt = DateTime.Now;

                var reconcilledGlList = (from a in db.GeneralLedgers
                                 where a.ReconciliationStatus == "Reconcilled"
                                 where a.BankAccountNumber== posted.AccountNumber
                                 where a.ReconciliationNo == posted.ReconciliationPostedId
                                 select a).ToList();


                var reconcilledBankList = (from a in db.BankStatementDetails
                                   join b in db.BankStatementSummarys
                                   on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                   where b.BankAccountNumber == posted.AccountNumber
                                   where a.ReconciliationStatus == "Reconcilled"
                                   where a.ReconciliationNo == posted.ReconciliationPostedId
                                   select a).ToList();


                foreach (var item in reconcilledGlList)
                {
                    item.ReconciliationStatus = "Matched";
                }

                foreach (var item in reconcilledBankList)
                {
                    item.ReconciliationStatus = "Matched";
                    
                }
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";
                }
                catch (Exception ex)
                {
                    response = ex.Message.ToString();
                    trans.Rollback();
                }
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [HttpGet, Authorize(Roles = "Reconciliation Approval")]
        public ActionResult ApprovedReconcilliation()
        {

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            var userAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNumber)
                .ToList();

            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var approvedList = (from a in db.ReconciliationPosteds
                                where userAccount.Contains(a.AccountNumber)
                                where a.OverallStatus == "Approved"
                                select a).ToList();


            return View(approvedList);

        }
        public ActionResult OutReceiptBankList(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            List<BankOutoStandingVw> BankOutStandingList = null;
            ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, BankAcct, checkDate);
            }
            if (reconcilliationResponseBank.overallStatus == "Error")
            {
                //Handle error 
            }

            BankOutStandingList = reconcilliationResponseBank
                .BankOutStandingVwList
                .Where(a => a.TransactionType == "CR")
                .ToList();

            return PartialView("_BankOutStandingList", BankOutStandingList);
        }

        public ActionResult OutPaymentBankList(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
     
            List<BankOutoStandingVw> BankStandingList = null;
            ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, BankAcct, checkDate);
            }
            if (reconcilliationResponseBank.overallStatus == "Error")
            {
                //Handle error 
            }
            BankStandingList = reconcilliationResponseBank
                .BankOutStandingVwList
                .Where(a => a.TransactionType == "DR")
                .ToList();
            return PartialView("_BankOutStandingList", BankStandingList);
        }


        public ActionResult OutReceiptGLList(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            List<GLOutoStandingVw> GLStandingList = null;
            ReconcilliationResponse reconcilliationResponseGL = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponseGL = ReconcilliationService.GetOutStandingGl(db, BankAcct, checkDate);
            }
            if (reconcilliationResponseGL.overallStatus == "Error")
            {
                //Handle error 
            }
            GLStandingList = reconcilliationResponseGL
                .GLOutoStandingVwList
                .Where(a => a.TransactionType == "DR")
                .ToList();
            return PartialView("_GLOutStandingList", GLStandingList);

           
        }

        public ActionResult OutPaymentGLList(string BankAcct, DateTime checkDate)
        {
            if (BankAcct == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            List<GLOutoStandingVw> GLStandingList = null;
            ReconcilliationResponse reconcilliationResponseGL = new ReconcilliationResponse();
            if (BankAcct != "" && checkDate != null)
            {
                reconcilliationResponseGL = ReconcilliationService.GetOutStandingGl(db, BankAcct, checkDate);
            }
            if (reconcilliationResponseGL.overallStatus == "Error")
            {
                //Handle error 
            }
            GLStandingList = reconcilliationResponseGL
                .GLOutoStandingVwList
                .Where(a => a.TransactionType == "CR")
                .ToList();
            return PartialView("_GLOutStandingList", GLStandingList);

        }


        public ActionResult SearchReconciliationOutStanding()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;

            OutStandingVM vm = new OutStandingVM();
       
            var AccountList = ServiceManager
              .GetAccountListrec(db, institutionCode)
              .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
              .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            return View(vm);
        }
        public ActionResult OutStandingList2(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            month = chkDate.Substring(3, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode).Select(a => a.AccountNumber).ToList();


            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Database.CommandTimeout = 1200;
            OutStandingVM vm = new OutStandingVM();
            vm.BankOutoStandingList = (from a in db.BankOutoStandingVws
                                       where userAccount.Contains(a.BankAccount)
                                       where a.CheckDate == checkdate
                                       where a.BankAccount == AccountNumber
                                       select a).ToList();

            vm.GLOutoStandingList = (from a in db.GLOutoStandingVws
                                     where userAccount.Contains(a.BankAccountGL)
                                     where a.CheckDate == checkdate
                                     where a.BankAccount == AccountNumber
                                     select a).ToList();

            return PartialView("_OutStandingList", vm);
        }

        public ActionResult OutStandingLis2t(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            month = chkDate.Substring(3, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode).Select(a => a.AccountNumber).ToList();


            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Database.CommandTimeout = 300;
            OutStandingVM vm = new OutStandingVM();
            vm.BankOutoStandingList = (from a in db.BankOutoStandingVws
                                       where userAccount.Contains(a.BankAccount)
                                       where a.CheckDate == checkdate
                                       where a.BankAccount == AccountNumber
                                       select a).ToList();

            vm.GLOutoStandingList = (from a in db.GLOutoStandingVws
                                     where userAccount.Contains(a.BankAccountGL)
                                     where a.CheckDate == checkdate
                                     where a.BankAccount == AccountNumber
                                     select a).ToList();

            return PartialView("_OutStandingList", vm);
        }


        public ActionResult OutStandingList(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNumber)
                .ToList();


            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Database.CommandTimeout = 1200; 
            OutStandingVM vm = new OutStandingVM();
            List<BankOutoStandingVw> BankOutoStandingList = new List<BankOutoStandingVw>();
            ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
            if (AccountNumber != "" && checkdate != null)
            {
                reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, AccountNumber, checkdate);
            }
            if (reconcilliationResponseBank.overallStatus == "Error")
            {
                //Handle error 
            }

            var bankoutstandingList = reconcilliationResponseBank.BankOutStandingVwList;
            if (bankoutstandingList == null)
            {
                vm.BankOutoStandingList = BankOutoStandingList;

            }
            else
            {

                vm.BankOutoStandingList = reconcilliationResponseBank
               .BankOutStandingVwList
               .Where(a => a.ReconciliationStatus == "Pending")
               .ToList();
            }

            List<GLOutoStandingVw> GLOutoStandingList = new List<GLOutoStandingVw>();
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
            if (AccountNumber != "" && checkdate != null)
            {
                reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, AccountNumber, checkdate);
            }
            if (reconcilliationResponse.overallStatus == "Error")
            {
                //Handle error 
            }

            var gloutostandingList = reconcilliationResponse.GLOutoStandingVwList;
            if (gloutostandingList == null)
            {
                vm.GLOutoStandingList = GLOutoStandingList;

            }
            else
            {
                vm.GLOutoStandingList = reconcilliationResponse
                    .GLOutoStandingVwList
                    .Where(a => a.ReconciliationStatus == "Pending")
                    .ToList();
            }

            ViewBag.AccountNumber = AccountNumber;
            ViewBag.chkDate = chkDate;
            return View(vm);
        }


        public ActionResult OutStandingListNew(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            ViewBag.AccountNumber = AccountNumber;
            ViewBag.chkDate = chkDate;
            return View();
        }




        public JsonResult GetOutStandingListGl(string AccountNumber, string chkDate)
        {
            string month;
            int year;
            List<GLOutoStandingVw> GLOutoStandingList = new List<GLOutoStandingVw>();
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
            reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, AccountNumber, checkdate);
         
            if (reconcilliationResponse.overallStatus == "Error" || reconcilliationResponse.GLOutoStandingVwList==null)
            {
                return Json(new { data = GLOutoStandingList }, JsonRequestBehavior.AllowGet);
            }


            var response = Json(new { data = reconcilliationResponse.GLOutoStandingVwList.Where(a => a.ReconciliationStatus == "Pending").ToList()}, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;

            //var data = reconcilliationResponse.GLOutoStandingVwList.Where(a => a.ReconciliationStatus == "Pending").ToList();
            //return Json(new { data },JsonRequestBehavior.AllowGet );
        }


        public JsonResult GetOutStandingListBank(string AccountNumber, string chkDate)
        {
            string month;
            int year;
            List<BankOutoStandingVw> BankOutoStandingList = new List<BankOutoStandingVw>();
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();
            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
            reconcilliationResponse = ReconcilliationService.GetBankOutStanding(db, AccountNumber, checkdate);

            if (reconcilliationResponse.overallStatus == "Error" || reconcilliationResponse.BankOutStandingVwList == null)
            {
                return Json(new { data = BankOutoStandingList }, JsonRequestBehavior.AllowGet);
            }

            //var response = Json(new { data = reconcilliationResponse.BankOutStandingVwList.Where(a => a.ReconciliationStatus == "Pending").ToList() }, JsonRequestBehavior.AllowGet);
            //response.MaxJsonLength = int.MaxValue;
            //return response;

            var data = reconcilliationResponse.BankOutStandingVwList.Where(a => a.ReconciliationStatus == "Pending").ToList();
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GlBankMatching(int[] cashIds, int[] bankIds)
        {
            string response = "";
            DateTime? matchedDate;
            DateTime? CheckDateGl;
            DateTime? CheckDateBank;

            db.Database.CommandTimeout = 1200;
            using (var trans = db.Database.BeginTransaction())
            {
            try
            {
               
            db.Database.CommandTimeout = 1200;
            var outStandingGl = (from a in db.GeneralLedgers
                            where cashIds.Contains(a.GeneralLedgerId)
                            where a.ReconciliationStatus == "Pending"
                            select a
                        ).ToList();

            int id = outStandingGl.Select(a => a.GeneralLedgerId).FirstOrDefault();

            string matchNumber = id.ToString().PadLeft(10, '0');

            var outStandingBank = (from a in db.BankStatementDetails
                                   where bankIds.Contains(a.BankStatementDetailId)
                                   where a.ReconciliationStatus == "Pending"
                                   select a
                                  ).ToList();

           var bankStandingList = (from a in db.BankStatementDetails
                                    join b in db.BankStatementSummarys
                                    on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                    where bankIds.Contains(a.BankStatementDetailId)
                                    && a.ReconciliationStatus == "Pending"
                                    select b ).ToList();

            CheckDateGl = outStandingGl.Select(a => a.CheckDate).Max();
            CheckDateBank = bankStandingList.Select(a => a.CheckDate).Max();

           matchedDate = CheckDateGl;
           if(CheckDateBank > CheckDateGl)
            {
              matchedDate = CheckDateBank;
            }

            foreach (var item in outStandingGl)
            {
                item.ReconciliationStatus = "Matched";
                item.DateReconciled = DateTime.Now;
                item.ReconciliationType = "Manual";
                item.ClearedType = "GLBank";
                item.ReconciledBy = User.Identity.Name;
                item.MatchNo = matchNumber;
                item.MatchedDate = matchedDate;
            }

            foreach (var item in outStandingBank)
            {
                item.ReconciliationStatus = "Matched";
                item.DateReconciled = DateTime.Now;
                item.ReconciliationType = "Manual";
                item.ClearedType = "GLBank";
                item.ReconciledBy = User.Identity.Name;
                item.MatchNo = matchNumber;
                item.MatchedDate = matchedDate;
              }

            db.SaveChanges();
            trans.Commit();
            response = "Success";
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
                trans.Rollback();
            }
        }  
        return Json(response, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SearchBanktoBank()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;

            BanktoBankVM vm = new BanktoBankVM();
            var AccountList = ServiceManager
                .GetAccountListrec(db, institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            return View(vm);
        }
        public ActionResult BanktoBankOutStanding(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            month = chkDate.Substring(3, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode).Select(a => a.AccountNumber).ToList();


            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Database.CommandTimeout = 300;
            BanktoBankVM vm = new BanktoBankVM();

            vm.BankOutoStandingList = (from a in db.BankOutoStandingVws
                                       where userAccount.Contains(a.BankAccount)
                                       where a.CheckDate == checkdate
                                       where a.BankAccount == AccountNumber
                                       select a).ToList();

            ViewBag.DR = (from a in db.BankOutoStandingVws
                          where userAccount.Contains(a.BankAccount)
                          where a.CheckDate <= checkdate
                          where a.BankAccount == AccountNumber
                          where a.TransactionType == "DR"
                          select a).ToList();

            ViewBag.CR = (from a in db.BankOutoStandingVws
                          where userAccount.Contains(a.BankAccount)
                          where a.CheckDate <= checkdate
                          where a.BankAccount == AccountNumber
                          where a.TransactionType == "CR"
                          select a).ToList();

            return PartialView("_BanktoBankList", vm);
        }


        public ActionResult BanktoBankList(string AccountNumber, string chkDate)
        {
            string month;
            int year;


            if (chkDate == "" && chkDate == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (AccountNumber == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode).Select(a => a.AccountNumber).ToList();


            if (userAccount == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            db.Database.CommandTimeout = 1200;
            BanktoBankVM vm = new BanktoBankVM();


            db.Database.CommandTimeout = 1200;
            List<BankOutoStandingVw> BankOutoStandingList = null;
            ReconcilliationResponse reconcilliationResponseBank = new ReconcilliationResponse();
            if (AccountNumber != "" && checkdate != null)
            {
                reconcilliationResponseBank = ReconcilliationService.GetBankOutStanding(db, AccountNumber, checkdate);
            }
            if (reconcilliationResponseBank.overallStatus == "Error")
            {
                //Handle error 
            }
            BankOutoStandingList = reconcilliationResponseBank.BankOutStandingVwList;

            ViewBag.DR = BankOutoStandingList.Where(a => a.TransactionType == "DR"
                                           && a.ReconciliationStatus == "Pending"
                                           ).ToList();

            ViewBag.CR = BankOutoStandingList.Where(a => a.TransactionType == "CR"
                                        && a.ReconciliationStatus == "Pending"
                                        ).ToList();

            return View(vm);
        }


        public JsonResult BanktoBankMatching(int[] bankIds)
        {
            string response = "";
            DateTime? matchedDate;
            db.Database.CommandTimeout = 1200;
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {

                    var outStandingBank = (from a in db.BankStatementDetails
                                           where bankIds.Contains(a.BankStatementDetailId)
                                           where a.ReconciliationStatus == "Pending"
                                           select a
                                           ).ToList();

                    int id = outStandingBank.Select(a => a.BankStatementDetailId).FirstOrDefault();
                    string matchNumber = id.ToString().PadLeft(10, '0');

                    var bankStandingList = (from a in db.BankStatementDetails
                                            join b in db.BankStatementSummarys
                                            on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                            where bankIds.Contains(a.BankStatementDetailId)
                                            && a.ReconciliationStatus == "Pending"
                                            select b).ToList();

                    var CheckedList = bankStandingList
                      .Select(a => a.CheckDate)
                      .Distinct()
                      .ToList();

                    matchedDate = CheckedList.Max();

                    foreach (var item in outStandingBank)
                    {
                        item.ReconciliationStatus = "Matched";
                        item.DateReconciled = DateTime.Now;
                        item.ReconciliationType = "Manual";
                        item.ClearedType = "BankBank";
                        item.ReconciledBy = User.Identity.Name;
                        item.MatchNo = matchNumber;
                        item.MatchedDate = matchedDate;
                    }
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";
                }

                catch (Exception ex)
                {
                    response = ex.Message.ToString();
                    trans.Rollback();
                }
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SearchGLtoGL()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;

            GLtoGLVM vm = new GLtoGLVM();
            var AccountList = ServiceManager
              .GetAccountListrec(db, institutionCode)
              .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
              .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            return View(vm);
        }
    
        public ActionResult GLtoGLList(string AccountNumber, string chkDate)
        {
            string month;
            int year;

            if (chkDate == "" && chkDate == null){ return new HttpStatusCodeResult(HttpStatusCode.BadRequest);}

            if (AccountNumber == ""){ return new HttpStatusCodeResult(HttpStatusCode.BadRequest);}

            month = chkDate.Substring(0, 2);
            year = Convert.ToInt32(chkDate.Substring(6, 4));
            var endDate = ServiceManager.GetEndDateByMonthId(month, year);
            DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var userAccount = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode).Select(a => a.AccountNumber).ToList();

            if (userAccount == null){ return new HttpStatusCodeResult(HttpStatusCode.BadRequest);}

            db.Database.CommandTimeout = 1200;
            List<GLOutoStandingVw> OutStandingList = new List<GLOutoStandingVw>();
            ReconcilliationResponse reconcilliationResponse = new ReconcilliationResponse();

            if (AccountNumber != "" && checkdate != null)
            {
                reconcilliationResponse = ReconcilliationService.GetOutStandingGl(db, AccountNumber, checkdate);
            }

            if (reconcilliationResponse.overallStatus == "Error")
            {
                //Handle error 
            }

            OutStandingList = reconcilliationResponse.GLOutoStandingVwList;

            ViewBag.GLOutoStandingListDR = OutStandingList
                                           .Where(a=>a.TransactionType=="DR" 
                                           && a.ReconciliationStatus=="Pending"
                                           ).ToList();

            ViewBag.GLOutoStandingListCR = OutStandingList
                                        .Where(a => a.TransactionType == "CR"
                                        && a.ReconciliationStatus == "Pending"
                                        ).ToList();

            return View();
        }


        public JsonResult GLtoGLMatching(int[] ledgerIds)
        {
            string response = "";
            DateTime? matchedDate;

            db.Database.CommandTimeout = 1200;
            using (var transaction = db.Database.BeginTransaction())
            {
           try
            {
                var outStandingGL = (from a in db.GeneralLedgers
                                     where ledgerIds.Contains(a.GeneralLedgerId)
                                     where a.ReconciliationStatus == "Pending"
                                     select a
                                 ).ToList();

                int id = outStandingGL.Select(a => a.GeneralLedgerId).FirstOrDefault();
                string matchNumber = id.ToString().PadLeft(10, '0');

               var  CheckedList =outStandingGL
                        .Select(a => a.CheckDate)
                        .Distinct()
                        .ToList();

               matchedDate = CheckedList.Max();

                foreach (var item in outStandingGL)
                {
                    item.ReconciliationStatus = "Matched";
                    item.DateReconciled = DateTime.Now;
                    item.ReconciliationType = "Manual";
                    item.ClearedType = "GLGL";
                    item.ReconciledBy = User.Identity.Name;
                    item.MatchNo = matchNumber;
                    item.MatchedDate = matchedDate;
                }
                db.SaveChanges();
                transaction.Commit();
                response = "Success";
            }

            catch (Exception ex)
            {
                response = ex.Message.ToString();
                transaction.Rollback();
            }
        }
        return Json(response, JsonRequestBehavior.AllowGet);
    }

        public DateTime? GetCheckDateList(string accountNumber)
        {
            string month;
            int year;

            var checkDatepost = db.ReconciliationPosteds
                .Where(a => a.AccountNumber == accountNumber 
                && a.OverallStatus != "Rejected")
                .OrderByDescending(a => a.ReconciliationPostedId)
                .Select(a => a.CheckDate)
                .Max();

            var datechk = checkDatepost;
            if (checkDatepost == null)
            {
                var x = DateTime.Now.ToString("MM/dd/yyyy");
                month = x.Substring(0, 2);
                year = Convert.ToInt32(x.Substring(6, 4));
                var endDate = ServiceManager.GetEndDateByMonthId(month, year);
                DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
                datechk = checkdate;
                return datechk;
            }
            else
            {
                DateTime y = Convert.ToDateTime(checkDatepost);
                string yz = y.AddMonths(1).ToString("MM/dd/yyyy");
                month = yz.Substring(0, 2);
                year = Convert.ToInt32(yz.Substring(6, 4));
                var endDate = ServiceManager.GetEndDateByMonthId(month, year);
                DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);
                datechk = checkdate;
                return datechk;
            }
        }


        [HttpGet]
        public ActionResult SearchMatched()
        {
      
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            SearchMatchedVM vm = new SearchMatchedVM();
            var AccountList = ServiceManager
                .GetAccountListrec(db, institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            
            return View(vm);
        }


        public ActionResult SearchMatched(SearchMatchedVM vm)
        {
            string type = vm.ClearType.ToString();
            var param = vm.MatchNumber;
            var manualMatchedVMs = new List<ManualMatchdVM>();

            if (type == "BankBank")
            {
                var banktobankList = (from a in db.BankStatementDetails
                                      join b in db.BankStatementSummarys
                                      on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                      where b.BankAccountNumber == vm.AccountNumber
                                      where a.ClearedType == "BankBank"
                                     && (a.MatchNo.Contains(param) 
                                      || a.TransactionRef.ToString().Contains(param) 
                                      || a.TransactionAmount.ToString().Contains(param) 
                                      || a.RelatedRef.ToString().Contains(param)
                                      ) select a).ToList();

                foreach (var item in banktobankList)
                {
                    var manualMatchedVM = new ManualMatchdVM
                    {
                        LegalNumber = item.TransactionRef,
                        Description = item.Description,
                        TransactionType = item.TransactionType,
                        Amount = item.TransactionAmount,
                        MatchNo = item.MatchNo,
                        ReconciledBy=item.ReconciledBy,
                        ClearedType=item.ClearedType,
                        ReconciliationType=item.ReconciliationType
                    };
                    manualMatchedVMs.Add(manualMatchedVM);
                }
                return PartialView("_UnMatchedList", manualMatchedVMs);

            } 
            else if (type == "GLBank"){

                var generalLedgerList = db.GeneralLedgers
                        .Where(a => a.ClearedType == "GLBank" 
                        && a.BankAccountNumber==vm.AccountNumber
                        && (a.MatchNo.Contains(param) 
                        || a.LegalNumber.ToString().Contains(param) 
                        || a.OperationalAmount.ToString().Contains(param)) 
                        //&& a.ReconciliationStatus == "Matched"
                    ).ToList();

                foreach (var item in generalLedgerList)
                {
                    var manualMatchedVM = new ManualMatchdVM
                    {
                        LegalNumber = item.LegalNumber,
                        Description = item.TransactionDesc,
                        TransactionType = item.TransactionType,
                        Amount = item.OperationalAmount,
                        MatchNo = item.MatchNo,
                        ReconciledBy = item.ReconciledBy,
                        ClearedType = item.ClearedType,
                        ReconciliationType = item.ReconciliationType
                    };
                    manualMatchedVMs.Add(manualMatchedVM);
                }

                db.Database.CommandTimeout = 1200;
          
                var banktobankList = (from a in db.BankStatementDetails
                                      join b in db.BankStatementSummarys
                                      on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                      where b.BankAccountNumber == vm.AccountNumber
                                      where a.ClearedType == "GLBank"
                                     && (a.MatchNo.Contains(param)
                                      || a.TransactionRef.ToString().Contains(param)
                                      || a.TransactionAmount.ToString().Contains(param)
                                      || a.RelatedRef.ToString().Contains(param)
                                      )
                                      select a).ToList();

                foreach (var item2 in banktobankList)
                    {
                        var manualMatchedVM = new ManualMatchdVM
                        {
                            LegalNumber = item2.TransactionRef,
                            Description = item2.Description,
                            TransactionType = item2.TransactionType,
                            Amount = item2.TransactionAmount,
                            MatchNo = item2.MatchNo,
                            ReconciledBy = item2.ReconciledBy,
                            ClearedType = item2.ClearedType,
                            ReconciliationType = item2.ReconciliationType
                        };
                        manualMatchedVMs.Add(manualMatchedVM);
                    }

                  return PartialView("_UnMatchedList", manualMatchedVMs);
            }
            else
            {
                db.Database.CommandTimeout = 1200;
                var generalLedgerList = db.GeneralLedgers
                       .Where(a => a.ClearedType == "GLGL" 
                       && a.BankAccountNumber == vm.AccountNumber
                       && (a.MatchNo.Contains(param) || a.LegalNumber.ToString().Contains(param) || a.OperationalAmount.ToString().Contains(param))
                      //&& a.ReconciliationStatus == "Matched"
                   ).ToList();

                foreach (var item in generalLedgerList)
                {
                    var manualMatchedVM = new ManualMatchdVM
                    {
                        LegalNumber = item.LegalNumber,
                        Description = item.TransactionDesc,
                        TransactionType = item.TransactionType,
                        Amount = item.OperationalAmount,
                        MatchNo = item.MatchNo,
                        ReconciledBy = item.ReconciledBy,
                        ClearedType = item.ClearedType,
                        ReconciliationType = item.ReconciliationType
                    };
                    manualMatchedVMs.Add(manualMatchedVM);
                }
                return PartialView("_UnMatchedList", manualMatchedVMs);
            }
        }

        public JsonResult Unmatching(string[] bankIds)
        {
            string response = "";
            db.Database.CommandTimeout = 1200;
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.CommandTimeout = 1200;
                    var outStandingGl = (from a in db.GeneralLedgers
                                 where bankIds.Contains(a.MatchNo.ToString())
                                 //where a.ReconciliationStatus == "Matched"
                                 select a
                               ).ToList();


                    var outStandingBank = (from a in db.BankStatementDetails
                                           where bankIds.Contains(a.MatchNo.ToString())
                                           //where a.ReconciliationStatus == "Matched"
                                           select a
                                          ).ToList();

                    foreach (var item in outStandingGl)
                    {
                        item.ReconciliationStatus = "Pending";
                        item.DateReconciled = DateTime.Now;
                        item.ReconciliationType = " ";
                        item.ClearedType = "";
                        item.ReconciledBy = "";
                       // item.MatchedDate = "";
                        item.MatchNo = "";
                    }

                    foreach (var item in outStandingBank)
                    {
                        item.ReconciliationStatus = "Pending";
                        item.DateReconciled = DateTime.Now;
                        item.ReconciliationType = " ";
                        item.ClearedType = "";
                        item.ReconciledBy = "";
                        //item.MatchedDate = '';
                        item.MatchNo = "";
                    }
                    db.SaveChanges();
                    transaction.Commit();
                    response = "Success";

                }catch (Exception ex)
                {
                    response = ex.Message.ToString();
                    transaction.Rollback();
                }
            }

        return Json(response, JsonRequestBehavior.AllowGet);
      }

        [HttpGet, Authorize()]
        public ActionResult ReverseApprovedReconciliation()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = ServiceManager
                .GetAccountListrec(db, institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            return View(vm);
        }


        [HttpPost, Authorize()]
        public ActionResult ReverseApprovedReconciliation(string account, DateTime statementDate)
        {
            string response = null;
            string month;
            string tarehe;
            int year;
            month = statementDate.ToString("MM");
            year = statementDate.Year;
            tarehe = statementDate.ToString("dd");
            DateTime EndDateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);
            db.Database.CommandTimeout = 1200;
            try
            {
                ProcessResponse Status = new ProcessResponse();
                Status.OverallStatus = "Pending";
                var parameters = new SqlParameter[] {
                     new SqlParameter("@AccountNumber", account),
                     new SqlParameter("@CheckDate",EndDateFormated)
                };
                var statusRec = db.Database.SqlQuery<ReconcillationStatusVM>("dbo.ReverseApprovedReconciliation_sp @AccountNumber,@CheckDate", parameters).FirstOrDefault();
                Status.OverallStatus = statusRec.OverallStatus;

                if(Status.OverallStatus=="Success"){

                    response = "Success";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Authorize()]
        public ActionResult TransactionStatus()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            SearchTransactionVM vm = new SearchTransactionVM ();
            var AccountList = ServiceManager
                .GetAccountListrec(db, institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            return View(vm);
        }

        public ActionResult TransactionStatus(SearchTransactionVM vm)
        {
            string accountNumber = vm.AccountNumber.ToString();
            var parameter = vm.SearchParameter;
            var transactionStatusVms = new List<TransactionStatusVM>();
            db.Database.CommandTimeout = 1200;

            var generalLedgerList = db.GeneralLedgers
                .Where(a => a.BankAccountNumber== accountNumber 
                && a.ReconciliationStatus != "Memorandum"
                && (a.MatchNo.Contains(parameter) 
                || a.LegalNumber.ToString().Contains(parameter) 
                || a.OperationalAmount.ToString().Contains(parameter))
                ).ToList();


                foreach (var item in generalLedgerList)
                {
                    var manualMatchedVM = new TransactionStatusVM
                    {
                        LegalNumber = item.LegalNumber,
                        Description = item.TransactionDesc,
                        TransactionType = item.TransactionType,
                        Amount = item.OperationalAmount,
                        ReconciliationStatus=item.ReconciliationStatus,
                        MatchNo = item.MatchNo,
                        ClearedType = item.ClearedType,
                        ReconciliationType=item.ReconciliationType,
                        ReconciledBy=item.ReconciledBy,
                        TransactionDate=item.ApplyDate,
                        source="Cash Book"
                    };
                transactionStatusVms.Add(manualMatchedVM);
                }


            var banktobankList = (from a in db.BankStatementDetails
                                  join b in db.BankStatementSummarys
                                  on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                  where b.BankAccountNumber == vm.AccountNumber
                                 && (a.MatchNo.Contains(parameter)
                                 || a.TransactionRef.ToString().Contains(parameter)
                                 || a.RelatedRef.ToString().Contains(parameter)
                                 || a.TransactionAmount.ToString().Contains(parameter)
                                 || a.RelatedRef.ToString().Contains(parameter)
                                  )
                                  select new { a, b } into c
                                  select new TransactionStatusVM
                                  {
                                      LegalNumber = c.a.TransactionRef,
                                      Description = c.a.Description,
                                      TransactionType = c.a.TransactionType,
                                      Amount = c.a.TransactionAmount,
                                      ReconciliationStatus = c.a.ReconciliationStatus,
                                      MatchNo = c.a.MatchNo,
                                      ClearedType = c.a.ClearedType,
                                      ReconciliationType = c.a.ReconciliationType,
                                      ReconciledBy = c.a.ReconciledBy,
                                      TransactionDate = c.b.StatementDate,

                                  }).ToList();

            foreach (var item2 in banktobankList)
                {
                    var manualMatchedVM = new TransactionStatusVM
                    {
                        LegalNumber = item2.LegalNumber,
                        Description = item2.Description,
                        TransactionType = item2.TransactionType,
                        Amount = item2.Amount,
                        ReconciliationStatus = item2.ReconciliationStatus,
                        MatchNo = item2.MatchNo,
                        ClearedType = item2.ClearedType,
                        ReconciliationType = item2.ReconciliationType,
                        ReconciledBy = item2.ReconciledBy,
                        TransactionDate = item2.TransactionDate,
                        source = "Bank"

                    };
                transactionStatusVms.Add(manualMatchedVM);
                }

            return PartialView("_TransactionStatusList", transactionStatusVms);

        }

        //public ActionResult AllExport(SearchTransactionVM vm)
        //{

        //    string accountNumber = vm.AccountNumber.ToString();
        //    var parameter = vm.SearchParameter;
        //    var transactionStatusVms = new List<TransactionStatusVM>();
        //    db.Database.CommandTimeout = 1200;

        //    var generalLedgerList = db.GeneralLedgers
        //        .Where(a => a.BankAccountNumber == accountNumber
        //        && a.ReconciliationStatus != "Memorandum"
        //        && (a.MatchNo.Contains(parameter)
        //        || a.LegalNumber.ToString().Contains(parameter)
        //        || a.OperationalAmount.ToString().Contains(parameter))
        //        ).ToList();


        //    foreach (var item in generalLedgerList)
        //    {
        //        var manualMatchedVM = new TransactionStatusVM
        //        {
        //            LegalNumber = item.LegalNumber,
        //            Description = item.TransactionDesc,
        //            TransactionType = item.TransactionType,
        //            Amount = item.OperationalAmount,
        //            ReconciliationStatus = item.ReconciliationStatus,
        //            MatchNo = item.MatchNo,
        //            ClearedType = item.ClearedType,
        //            ReconciliationType = item.ReconciliationType,
        //            ReconciledBy = item.ReconciledBy,
        //            TransactionDate = item.ApplyDate,
        //            source = "Cash Book"
        //        };
        //        transactionStatusVms.Add(manualMatchedVM);
        //    }


        //    var banktobankList = (from a in db.BankStatementDetails
        //                          join b in db.BankStatementSummarys
        //                          on a.BankStatementSummaryId equals b.BankStatementSummaryId
        //                          where b.BankAccountNumber == vm.AccountNumber
        //                         && (a.MatchNo.Contains(parameter)
        //                         || a.TransactionRef.ToString().Contains(parameter)
        //                         || a.RelatedRef.ToString().Contains(parameter)
        //                         || a.TransactionAmount.ToString().Contains(parameter)
        //                         || a.RelatedRef.ToString().Contains(parameter)
        //                          )
        //                          select new { a, b } into c
        //                          select new TransactionStatusVM
        //                          {
        //                              LegalNumber = c.a.TransactionRef,
        //                              Description = c.a.Description,
        //                              TransactionType = c.a.TransactionType,
        //                              Amount = c.a.TransactionAmount,
        //                              ReconciliationStatus = c.a.ReconciliationStatus,
        //                              MatchNo = c.a.MatchNo,
        //                              ClearedType = c.a.ClearedType,
        //                              ReconciliationType = c.a.ReconciliationType,
        //                              ReconciledBy = c.a.ReconciledBy,
        //                              TransactionDate = c.b.StatementDate,

        //                          }).ToList();

        //    foreach (var item2 in banktobankList)
        //    {
        //        var manualMatchedVM = new TransactionStatusVM
        //        {
        //            LegalNumber = item2.LegalNumber,
        //            Description = item2.Description,
        //            TransactionType = item2.TransactionType,
        //            Amount = item2.Amount,
        //            ReconciliationStatus = item2.ReconciliationStatus,
        //            MatchNo = item2.MatchNo,
        //            ClearedType = item2.ClearedType,
        //            ReconciliationType = item2.ReconciliationType,
        //            ReconciledBy = item2.ReconciledBy,
        //            TransactionDate = item2.TransactionDate,
        //            source = "Bank"

        //        };
        //        transactionStatusVms.Add(manualMatchedVM);
        //    }
        //    //return PartialView("_TransactionStatusList", transactionStatusVms);

        //    string heading = "Transactions LIST";
        //    string[] columns = { "TransactionDate", "LegalNumber", "Description", "TransactionType", "Amount", "ReconciliationStatus", "MatchNo", "ClearedType", "ReconciliationType", "ReconciledBy", "source" };
        //    string fileName = "transactionList" + ".xlsx";
        //    byte[] fileContent = ExcelExportHelper.ExportExcel(transactionStatusVms, heading, true, columns);
        //    return File(fileContent, ExcelExportHelper.ExcelContentType, fileName);
        //}

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
