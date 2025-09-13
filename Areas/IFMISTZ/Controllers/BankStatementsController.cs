using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using IFMIS.Services;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class BankStatementsController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();
        // GET: IFMISTZ/BankStatements
        readonly IServiceManager serviceManager;
        public BankStatementsController()
        {

        }

        public BankStatementsController(
            IServiceManager serviceManager
            )
        {
            this.serviceManager = serviceManager;
        }

        [HttpGet, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult UploadStatement()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            var bankList = serviceManager.GetBankList().Select(a => new { a.BankId, a.BankName }).Distinct();
            vm.bankNameList = new SelectList(bankList, "BankName", "BankName");
            return View(vm);

        }


        [HttpPost, Authorize(Roles = "Bank Statement Entry")]
        public JsonResult UploadStatement(UploadBankStatementVM bankStatementVM)
        {
            //string response = "";
            string response = "Success";
            string fileExtension = "";
            string month;
            int year;
            string existingStatementDate = "";
            string LastvalueDate = "";
            decimal commulativeBalance = 0;

            try
            {
                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);

                if (!(fileExtension == ".xlsx"))
                {
                    response = "Invalid file format, file format should be MS Excel 2007 !";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                var bankstatementList = new List<UploadBankStatementVM>();
                using (var package = new ExcelPackage(bankStatementVM.FileName.InputStream))
                {
                    var currentSheet = package.Workbook.Worksheets;
                    var workSheet = currentSheet.First();
                    var noOfRows = workSheet.Dimension.End.Row;
                    var noOfCols = workSheet.Dimension.End.Column;

                    for (int j = 2; j <= noOfRows; j++)
                    {
                        if (j == noOfRows)
                        {
                            LastvalueDate = LastvalueDate + " ";
                        }

                        string accountNum = "";
                        if (workSheet.Cells[j, 1].Value != null)
                        {
                            accountNum = workSheet.Cells[j, 1].Value.ToString();

                            if (accountNum != bankStatementVM.BankAccountNumber)
                            {
                                response = "The imported Account Number Do not Match with Selected Account";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }
                        }
                        else
                        {
                            response = "The file your want to import does not contain Acount Number";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        //string transactionRef = "";
                        //if (workSheet.Cells[j, 2].Value != null)
                        //    transactionRef = workSheet.Cells[j, 2].Value.ToString();

                        string transactionRef = "";
                        if (workSheet.Cells[j, 2].Value != null)
                        {
                            transactionRef = workSheet.Cells[j, 2].Value.ToString();

                        }
                        else
                        {
                            response = "Transaction Reference is Required";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string relatedRef = "";
                        if (workSheet.Cells[j, 3].Value != null)
                            relatedRef = workSheet.Cells[j, 3].Value.ToString();

                        string description = "";
                        if (workSheet.Cells[j, 4].Value != null)
                            description = workSheet.Cells[j, 4].Value.ToString();

                        string DueDateString = "";
                        if (workSheet.Cells[j, 5].Value != null)
                            DueDateString = workSheet.Cells[j, 5].Value.ToString();
                        DateTime valueDate = DateTime.FromOADate(int.Parse(DueDateString));
                        LastvalueDate = valueDate + " row " + j.ToString();
                        decimal amount = 0;
                        if (workSheet.Cells[j, 6].Value != null)
                            amount = Convert.ToDecimal(workSheet.Cells[j, 6].Value.ToString());


                        if (workSheet.Cells[j, 7].Value != null)
                            commulativeBalance = Convert.ToDecimal(workSheet.Cells[j, 7].Value.ToString());

                      var bankstatementvm = new UploadBankStatementVM()
                        {
                            BankAccountNumber = accountNum,
                            TransactionRef = transactionRef,
                            RelatedRef = relatedRef,
                            Description = description,
                            ValueDate = valueDate,
                            Amount = amount,
                            CommulativeBalance = commulativeBalance,
                        };
                        bankstatementList.Add(bankstatementvm);
                    }
                }

                decimal openingBal = bankstatementList.Select(a => a.CommulativeBalance).FirstOrDefault();
                var DateList = bankstatementList.OrderBy(a => a.ValueDate).Select(a => a.ValueDate).Distinct().ToList();
                decimal lastBalance = openingBal;
                foreach (var date in DateList)
                {
                    string tarehe = date.ToString("MM/dd/yyyy");//"11/27/2020 00:00:00";
                    string mwaka = tarehe.Substring(6, 4);
                    string siku = tarehe.Substring(3, 2);
                    string mwezi = tarehe.Substring(0, 2);

                    var bankStatements = new List<BankStatementDetail>();
                    var bankSummary = new BankStatementSummary();
                    DateTime StatementDate = new DateTime(int.Parse(mwaka), int.Parse(mwezi), int.Parse(siku));

                    month = StatementDate.ToString("MM");
                    year = StatementDate.Year;
                    var endDate = serviceManager.GetEndDateByMonthId(month, year);
                    DateTime checkdate = Convert.ToDateTime(year + "/" + month + "/" + endDate);

                    var Statementdetails = (from a in bankstatementList
                                            where a.ValueDate == date
                                            select a).ToList();


                    decimal openingBalance = lastBalance, closingBalance = 0;
                    //openingBalance = Statementdetails[0].CommulativeBalance - (decimal)Statementdetails[0].Amount;
                    //closingBalance = Statementdetails[Statementdetails.Count - 1].CommulativeBalance;

                    //openingBalance = openingBal;
                    //decimal amount = Statementdetails.Sum(a => a.Amount);
                    closingBalance = openingBalance + (Statementdetails.Sum(a => a.Amount));

                    var accountNumber = Statementdetails[0].BankAccountNumber;
                    Account account = db.Accounts.Where(a => a.AccountNo == accountNumber).FirstOrDefault();

                    var accname = account.AccountName;
                    var bankbic = account.BankBIC;
                    int currencyid = account.CurrencyId;

                    var bankname = db.Banks.Where(a => a.BIC == bankbic).Select(a => a.BankName).FirstOrDefault();
                    var CurrencyCode = db.Currencies.Where(a => a.CurrencyId == currencyid).Select(a => a.CurrencyCode).FirstOrDefault();

                    var BankCount = (from a in db.BankStatementDetails
                                     join b in db.BankStatementSummarys
                                     on a.BankStatementSummaryId equals b.BankStatementSummaryId
                                     where (b.StatementDate >= StatementDate && b.StatementDate <= StatementDate)
                                     where b.BankAccountNumber == accountNumber
                                     select a).Count();

                    if (BankCount == 0)
                    {
                        var bankstateSummary = new BankStatementSummary()
                        {
                            BankAccountName = accname,
                            BankName = bankname,
                            BankBic = bankbic,
                            CurrencyId = currencyid,
                            CurrencyCode = CurrencyCode,
                            BankAccountNumber = Statementdetails[0].BankAccountNumber,
                            StatementDate = StatementDate, /*Convert.ToDateTime(statDate),*/
                            CheckDate = checkdate,
                            OpeningBalance = openingBalance,
                            ClosingBalance = closingBalance,
                            CreatedDateTime = DateTime.Now,
                            IncomingMessageId = 0,
                            OverallStatus = "Pending",
                            Sources = "Uploaded",
                            UploadedBy = User.Identity.Name,
                            UploadedAt = DateTime.Now
                           
                        };

                        bankstateSummary.BankStatementInternalReference = bankstateSummary.GetBankStatementInternalReference;

                        db.BankStatementSummarys.Add(bankstateSummary);
                        int id = bankstateSummary.BankStatementSummaryId;

                        foreach (var item in Statementdetails)
                        {
                            string transactionType;
                            decimal amount;

                            if (item.Amount > 0)
                            {
                                transactionType = "CR";
                            }
                            else
                            {
                                transactionType = "DR";
                            }

                            if (item.Amount > 0)
                            {
                                amount = item.Amount;
                            }
                            else
                            {
                                amount = -1 * (item.Amount);
                            }
                            var bankStatementDetail = new BankStatementDetail()
                            {
                                BankStatementSummaryId = id,
                                TransactionRef = item.TransactionRef,
                                RelatedRef = item.RelatedRef,
                                Description = item.Description,
                                TransactionAmount = amount,
                                ReconciliationStatus = "Uploaded",
                                TransactionType = transactionType,
                            };
                            bankStatements.Add(bankStatementDetail);
                        }
                        db.BankStatementDetails.AddRange(bankStatements);
                        db.SaveChanges();

                        //var parameters = new SqlParameter[] { new SqlParameter("@BankAccount", accountNumber) };
                        //db.Database.ExecuteSqlCommand("ReconciledMatchedTransaction_p @BankAccount", parameters);

                        // response = "Success";
                        lastBalance = closingBalance;
                    }
                    else
                    {
                        existingStatementDate += StatementDate + ", ";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Statement Date: " + LastvalueDate + " " + ex.InnerException.ToString();
            }

            if (response == "Success")
            {
                response = "Uploaded Successfully";
            }

            if (existingStatementDate != "" && response == "Uploaded Successfully")
            {
                response += "!  But Statement Date " + existingStatementDate + " already exist";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult BankstatementList()
        {
            return View();
        }

        [HttpGet, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult getBankstatement()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var userAccount = db.Accounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNo).ToList();

            List<BankStatementSummary> BankStatementSummaryList = null;
            BankStatementSummaryList = (from a in db.BankStatementSummarys
                                        where a.Sources == "Uploaded"
                                        where a.OverallStatus == "Pending"
                                        where userAccount.Contains(a.BankAccountNumber)
                                        select a)
                                        .OrderBy(a => a.BankAccountNumber)
                                        .OrderBy(a => a.StatementDate)
                                        .ToList();


            var response = Json(new { data = BankStatementSummaryList.Where(a => a.OverallStatus == "Pending" || a.OverallStatus == "Rejected").ToList() }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;

            //return Json(new { data = BankStatementSummaryList.Where(a => a.OverallStatus == "Pending" || a.OverallStatus == "Rejected").ToList() }, JsonRequestBehavior.AllowGet);

        }

        [HttpGet, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult GetStatementDetails(int summaryid)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<BankStatementDetail> BankStatementDetailList = null;
            BankStatementDetailList = (from a in db.BankStatementDetails
                                       where a.BankStatementSummaryId == summaryid
                                       select a)
                                        .OrderBy(a => a.BankStatementDetailId)
                                        .ToList();

            var response = Json(new { data2 = BankStatementDetailList.ToList() }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;

            //return Json(new { data2 = BankStatementDetailList.ToList() }, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost, Authorize(Roles = "Bank Statement Entry")]
        //public ActionResult Confirmation(BankStatementVM statement)
        //{
        //    string response = null;
        //    InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
        //    try
        //    {
        //        BankStatementSummary BankStatementSummaryList = db.BankStatementSummarys
        //            .Where(a => a.BankStatementSummaryId == statement.BankStatementSummaryId)
        //            .FirstOrDefault();
        //        BankStatementSummaryList.OverallStatus = "Confirmed";
        //        BankStatementSummaryList.ConfirmedBy = User.Identity.Name;
        //        BankStatementSummaryList.ConfirmedAt = DateTime.Now;
        //        BankStatementSummaryList.ConfirmRemark = statement.ConfirmRemark;

        //        List<BankStatementDetail> BankStatementDetailList = null;
        //        BankStatementDetailList = db.BankStatementDetails
        //            .Where(a => a.BankStatementSummaryId == statement.BankStatementSummaryId)
        //            .ToList();

        //        if (BankStatementDetailList != null)
        //        {
        //            foreach (var item in BankStatementDetailList)
        //            {
        //                item.ReconciliationStatus = "Confirmed";
        //            }
        //        }

        //        db.SaveChanges();
        //        response = "Success";
        //    }
        //    catch (Exception ex)
        //    {
        //        response = ex.Message.ToString();
        //    }
        //    return Json(response, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult Confirmation(int BankStatementSummaryId, string remarks)
        {
            string response = null;
            decimal openingBalance = 0;
            decimal bankStatementClosingBalance = 0;
            decimal totalReceiptBalance = 0;
            decimal totalPaymentBalance = 0;
            decimal calculatedCloseBalance = 0;
            DateTime PreStatementdate;
            DateTime PreviousStatementdate;
            decimal PreviousClosingBalance = 0;
            string month;
            string tarehe;
            int year;

            db.Database.CommandTimeout = 1200;
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    BankStatementSummary SelectedBankStatementSummary = db.BankStatementSummarys
                    .Where(a => a.BankStatementSummaryId == BankStatementSummaryId)
                    .FirstOrDefault();

                    if (SelectedBankStatementSummary == null)
                    {
                        response = "The Statement Date does not exist";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                    List<BankStatementDetail> SelectedBankStatementDetailList = db.BankStatementDetails
                        .Where(a => a.BankStatementSummaryId == BankStatementSummaryId)
                        .ToList();
                    //Previous closing Balance
                    //PreviousStatementdate = BankStatementSummaryList.StatementDate.AddDays(-1);
                    PreStatementdate = SelectedBankStatementSummary.StatementDate;
                    month = PreStatementdate.ToString("MM");
                    year = PreStatementdate.Year;
                    tarehe = PreStatementdate.ToString("dd");
                    DateTime PreStatementdateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

                    List<BankStatementSummary> PrevBankStatementSummaryList = db.BankStatementSummarys
                       .Where(a => a.BankAccountNumber == SelectedBankStatementSummary.BankAccountNumber
                       && a.StatementDate < PreStatementdateFormated
                       && (a.OverallStatus == "Pending" || a.OverallStatus == "Confirmed" || a.OverallStatus == "Approved")
                       ).ToList();


                    if (PrevBankStatementSummaryList.Count() > 0)
                    {
                        var lastStatementDate = PrevBankStatementSummaryList
                        .Where(a => a.OverallStatus == "Confirmed" || a.OverallStatus == "Approved")
                        .Select(a => a.StatementDate)
                        .Max();

                        if (lastStatementDate != null)
                        {
                            PreviousStatementdate = lastStatementDate;
                        }
                        else
                        {
                            response = "You must confirm the previous statements before this one";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        PreviousStatementdate = SelectedBankStatementSummary.StatementDate;
                    }

                    //PreviousStatementdate = BankStatementSummaryList.StatementDate;
                    month = PreviousStatementdate.ToString("MM");
                    year = PreviousStatementdate.Year;
                    tarehe = PreviousStatementdate.ToString("dd");
                    DateTime PreviousStatementdateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

                    openingBalance = (decimal)SelectedBankStatementSummary.OpeningBalance;
                    if (PreviousStatementdateFormated != PreStatementdateFormated)
                    {
                        BankStatementSummary PreviousBankStatementSummary = db.BankStatementSummarys
                        .Where(a => a.BankAccountNumber == SelectedBankStatementSummary.BankAccountNumber
                        && a.StatementDate == PreviousStatementdateFormated
                        && (a.OverallStatus == "Confirmed" || a.OverallStatus == "Approved")
                        ).FirstOrDefault();

                        if (PreviousBankStatementSummary != null)
                        {
                            PreviousClosingBalance = (decimal)PreviousBankStatementSummary.ClosingBalance;
                        }
                        else
                        {
                            response = "Missing previous statement";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                        //openingBalance = (decimal)BankStatementSummaryList.OpeningBalance;
                        if (PreviousClosingBalance != openingBalance)
                        {
                            response = "The Previous Closing Balance is not equal to Opening Balance";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                    }

                    bankStatementClosingBalance = (decimal)SelectedBankStatementSummary.ClosingBalance;
                    totalReceiptBalance = (decimal)(SelectedBankStatementDetailList
                        .Where(a => a.TransactionType == "CR")
                        .Sum(a => a.TransactionAmount));

                    totalPaymentBalance = (decimal)(SelectedBankStatementDetailList
                      .Where(a => a.TransactionType == "DR")
                      .Sum(a => a.TransactionAmount));

                    calculatedCloseBalance = (openingBalance + totalReceiptBalance) - totalPaymentBalance;

                    if (calculatedCloseBalance != bankStatementClosingBalance)
                    {
                        response = "Opening Balance + Total Receipt Balance - Total Payment Balance  is not equal to Closing Balance  !";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }

                    SelectedBankStatementSummary.OverallStatus = "Confirmed";
                    SelectedBankStatementSummary.ConfirmedBy = User.Identity.Name;
                    SelectedBankStatementSummary.ConfirmedAt = DateTime.Now;
                    SelectedBankStatementSummary.ConfirmRemark = remarks;
                    if (SelectedBankStatementDetailList != null)
                    {
                        foreach (var item in SelectedBankStatementDetailList)
                        {
                            item.ReconciliationStatus = "Confirmed";
                        }
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
                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult DeleteStatement(int id)
        {
            string response = null;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            try
            {
                BankStatementSummary BankStatementSummaryList = db.BankStatementSummarys
                    .Where(a => a.BankStatementSummaryId == id)
                    .FirstOrDefault();

                List<BankStatementDetail> BankStatementDetailList = null;
                BankStatementDetailList = db.BankStatementDetails
                    .Where(a => a.BankStatementSummaryId == id)
                    .ToList();

                db.BankStatementSummarys.Remove(BankStatementSummaryList);
                db.BankStatementDetails.RemoveRange(BankStatementDetailList);
                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [HttpGet, Authorize(Roles = "Bank Statement Approval")]
        public ActionResult StatementApproval()
        {
            return View();
        }

        [HttpGet, Authorize(Roles = "Bank Statement Approval")]
        public ActionResult getStatementApproval()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var userAccount = db.Accounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNo).ToList();

            List<BankStatementSummary> BankStatementSummaryList = null;

            BankStatementSummaryList = (from a in db.BankStatementSummarys
                                        where userAccount.Contains(a.BankAccountNumber)
                                        where a.Sources == "Uploaded"
                                        where a.OverallStatus == "Confirmed"
                                        select a)
                                        .OrderBy(a => a.BankAccountNumber)
                                        .OrderBy(a => a.StatementDate)
                                        //.Take(5)
                                        .ToList();

            var response = Json(new { data = BankStatementSummaryList }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
            //return Json(new { data = BankStatementSummaryList }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Bank Statement Approval")]
        public ActionResult StatementApproval(int BankStatementSummaryId, string remarks)
        {
            string response = null;
            string accountNumber = null;

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            try
            {

                db.Database.CommandTimeout = 1200;
                BankStatementSummary BankStatementSummaryList = db.BankStatementSummarys
                    .Where(a => a.BankStatementSummaryId == BankStatementSummaryId)
                    .FirstOrDefault();
                BankStatementSummaryList.OverallStatus = "Approved";
                BankStatementSummaryList.ApprovedBy = User.Identity.Name;
                BankStatementSummaryList.ApprovedAt = DateTime.Now;
                BankStatementSummaryList.ApprovalRemark = remarks;

                List<BankStatementDetail> BankStatementDetailList = null;
                BankStatementDetailList = db.BankStatementDetails
                    .Where(a => a.BankStatementSummaryId == BankStatementSummaryId)
                    .ToList();

                if (BankStatementDetailList != null)
                {
                    foreach (var item in BankStatementDetailList)
                    {
                        item.ReconciliationStatus = "Pending";
                    }
                }
                accountNumber = BankStatementSummaryList.BankAccountNumber;
                db.SaveChanges();

                var parameters = new SqlParameter[] { new SqlParameter("@BankAccount", accountNumber) };
                db.Database.ExecuteSqlCommand("ReconciledMatchedTransactionStp @BankAccount", parameters);
                response = "Success";

            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Authorize(Roles = "Bank Statement Approval")]
        public ActionResult StatementRejection(BankStatementVM statement)
        {
            string response = null;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            try
            {
                BankStatementSummary BankStatementSummaryList = db.BankStatementSummarys
                    .Where(a => a.BankStatementSummaryId == statement.BankStatementSummaryId)
                    .FirstOrDefault();
                BankStatementSummaryList.OverallStatus = "Rejected";
                BankStatementSummaryList.RejectedBy = User.Identity.Name;
                BankStatementSummaryList.RejectedAt = DateTime.Now;
                BankStatementSummaryList.RejectedRemark = statement.RejectedRemark;

                List<BankStatementDetail> BankStatementDetailList = null;
                BankStatementDetailList = db.BankStatementDetails
                    .Where(a => a.BankStatementSummaryId == statement.BankStatementSummaryId)
                    .ToList();

                if (BankStatementDetailList != null)
                {
                    foreach (var item in BankStatementDetailList)
                    {
                        item.ReconciliationStatus = "Rejected";
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


        [HttpGet, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult RemoveStatement()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");


            return View(vm);
        }


        [HttpPost, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult RemoveStatement(string account, string removeType, DateTime statementDate, DateTime statementDateFrom, DateTime statementDateTo)
        {
            string response = null;
            string[] AuditNumber;
            int[] SummaryId;
            string month;
            string tarehe;
            int year;

            db.Database.CommandTimeout = 1200;
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {

                    if (removeType == "Full")
                    {

                        month = statementDateFrom.ToString("MM");
                        year = statementDateFrom.Year;
                        tarehe = statementDateFrom.ToString("dd");
                        DateTime statementDateFormFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

                        month = statementDateTo.ToString("MM");
                        year = statementDateTo.Year;
                        tarehe = statementDateTo.ToString("dd");
                        DateTime statementDateToFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

                        db.Database.CommandTimeout = 1200;
                        List<BankStatementSummary> BankStatementSummaryList = null;
                        BankStatementSummaryList = db.BankStatementSummarys
                            .Where(a => a.BankAccountNumber == account
                            && (a.StatementDate >= statementDateFormFormated && a.StatementDate <= statementDateToFormated))
                            .ToList();

                        if (BankStatementSummaryList == null || BankStatementSummaryList.Count() == 0)
                        {
                            response = "The Statement Date Between " + statementDateFrom + " and " + statementDateTo + " of Account number " + account + " does not exist";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        SummaryId = BankStatementSummaryList.Select(a => a.BankStatementSummaryId).ToArray();

                        List<BankStatementDetail> BankStatementDetailList = null;
                        BankStatementDetailList = (from b in db.BankStatementDetails
                                                   where SummaryId.Contains(b.BankStatementSummaryId)
                                                   select b).ToList();

                        if (BankStatementDetailList == null || BankStatementDetailList.Count() == 0)
                        {
                            db.BankStatementSummarys.RemoveRange(BankStatementSummaryList);
                            db.SaveChanges();
                            transaction.Commit();
                            response = "Success";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        AuditNumber = BankStatementDetailList.Select(a => a.MatchNo).ToArray();

                        db.Database.CommandTimeout = 1200;
                        var generalLedgerList = (from a in db.GeneralLedgers
                                                 where AuditNumber.Contains(a.MatchNo.ToString())
                                                 where a.BankAccountNumber == account
                                                 select a
                                               ).ToList();

                        foreach (var item in generalLedgerList)
                        {
                            item.ReconciliationStatus = "Pending";
                            item.DateReconciled = DateTime.Now;
                            item.ReconciliationType = " ";
                            item.ClearedType = "";
                            item.ReconciledBy = User.Identity.Name;
                            item.MatchNo = "";
                        }
                        db.BankStatementSummarys.RemoveRange(BankStatementSummaryList);
                        db.BankStatementDetails.RemoveRange(BankStatementDetailList);

                        RemoveStatementLog removeStatementLog = new RemoveStatementLog
                        {
                            AccountNumber = account,
                            StatementDateStart = statementDateFormFormated,
                            StatementDateEnd = statementDateToFormated,
                            RemovedBy = User.Identity.Name,
                            RemovedAt = DateTime.Now,
                            RemoveType = removeType,
                            OverallStatus = "Removed Successfully"

                        };

                        db.RemoveStatementLogs.Add(removeStatementLog);

                    }
                    else
                    {
                        month = statementDate.ToString("MM");
                        year = statementDate.Year;
                        tarehe = statementDate.ToString("dd");
                        DateTime statementDateFommatted = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

                        db.Database.CommandTimeout = 1200;
                        BankStatementSummary BankStatementSummaryList = null;
                        BankStatementSummaryList = db.BankStatementSummarys
                           .Where(a => a.StatementDate == statementDateFommatted && a.BankAccountNumber == account)
                           .FirstOrDefault();

                        if (BankStatementSummaryList == null)
                        {
                            response = "The Statement Date " + statementDateFommatted + " of Account number " + account + " does not exist";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        List<BankStatementDetail> BankStatementDetailList = null;
                        BankStatementDetailList = db.BankStatementDetails
                            .Where(a => a.BankStatementSummaryId == BankStatementSummaryList.BankStatementSummaryId)
                            .ToList();

                        if (BankStatementDetailList == null || BankStatementDetailList.Count() == 0)
                        {
                            db.BankStatementSummarys.Remove(BankStatementSummaryList);
                            db.SaveChanges();
                            transaction.Commit();
                            response = "Success";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        AuditNumber = BankStatementDetailList.Select(a => a.MatchNo).ToArray();

                        db.Database.CommandTimeout = 1200;
                        var generalLedgerList = (from a in db.GeneralLedgers
                                                 where AuditNumber.Contains(a.MatchNo.ToString())
                                                 where a.BankAccountNumber == account
                                                 select a
                                             ).ToList();

                        foreach (var item in generalLedgerList)
                        {
                            item.ReconciliationStatus = "Pending";
                            item.DateReconciled = DateTime.Now;
                            item.ReconciliationType = " ";
                            item.ClearedType = "";
                            item.ReconciledBy = User.Identity.Name;
                            item.MatchNo = "";
                        }
                        db.BankStatementSummarys.Remove(BankStatementSummaryList);
                        db.BankStatementDetails.RemoveRange(BankStatementDetailList);


                        RemoveStatementLog removeStatementLog = new RemoveStatementLog
                        {
                            AccountNumber = account,
                            StatementDateStart = statementDateFommatted,
                            StatementDateEnd = statementDateFommatted,
                            RemovedBy = User.Identity.Name,
                            RemovedAt = DateTime.Now,
                            RemoveType = removeType,
                            OverallStatus = "Removed Successfully"
                        };

                        db.RemoveStatementLogs.Add(removeStatementLog);
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
                return Json(response, JsonRequestBehavior.AllowGet);
            }

        }


        [HttpGet, Authorize()]
        public ActionResult BankStatementAnalysis()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            return View(vm);
        }


        [HttpPost, Authorize(Roles = "Bank Statement Entry")]
        public ActionResult BankStatementAnalysis(string account, DateTime startDate, DateTime EndDate)
        {
            string response = null;
            string month;
            string tarehe;
            int year;

            month = startDate.ToString("MM");
            year = startDate.Year;
            tarehe = startDate.ToString("dd");
            DateTime startDateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

            month = EndDate.ToString("MM");
            year = EndDate.Year;
            tarehe = EndDate.ToString("dd");
            DateTime EndDateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);


            db.Database.CommandTimeout = 1200;
            try
            {
                 var parameters = new SqlParameter[] { 
                    new SqlParameter("@AccountNum", account),
                    new SqlParameter("@StartDate",startDateFormated),
                    new SqlParameter("@EndDate",EndDateFormated) 
                 };

                db.Database.ExecuteSqlCommand("dbo.AnalyseBankStatementBalance_p @AccountNum, @StartDate, @EndDate", parameters);
               response = "Success";
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Authorize()]
        public ActionResult BankStatementAnalysisList()
        {
            return View();
        }

        public ActionResult GetBankStatementAnalysisList()
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var userAccount = db.Accounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode)
                .Select(a => a.AccountNo).ToList();

            List<BankStatementBalanceReview> BankStatementBalanceReviewList = null;

            BankStatementBalanceReviewList = (from a in db.BankStatementBalanceReviews
                                              where userAccount.Contains(a.BankAccountNumber)
                                              where a.OverallStatus != "VALID"
                                              select a)
                                              .OrderBy(a => a.BankAccountNumber)
                                              .OrderBy(a => a.StatementDate)
                                              .ToList();

            var response = Json(new { data = BankStatementBalanceReviewList }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
      
        }

        [HttpGet, Authorize()]
        public ActionResult RequestBankStatement()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            return View(vm);
        }

        //public ActionResult RequestBankStatement(string account, DateTime statementDate)
        //{
        //    string response = "";
        //    string month;
        //    string tarehe;
        //    int year;
        //    string receiverUrl;
        //    string clientCertStorePath;
        //    string clientCertPass;
        //    string receiverBic;


        //    string accountbic = db.Accounts
        //        .Where(a => a.AccountNo == account)
        //        .Select(a => a.BankBIC)
        //        .FirstOrDefault();


        //    month = statementDate.ToString("MM");
        //    year = statementDate.Year;
        //    tarehe = statementDate.ToString("dd");
        //    DateTime statementDateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

        //    db.Database.CommandTimeout = 1200;
        //    try
        //    {
        //        if (accountbic == "TANZTZTX")
        //        {
        //            List<BankStatementSummary> BankStatementSummaryList = null;
        //            BankStatementSummaryList = db.BankStatementSummarys
        //                .Where(a => a.BankAccountNumber == account
        //                && (a.StatementDate == statementDateFormated))
        //                .ToList();

        //            if (BankStatementSummaryList.Count() > 0)
        //            {
        //                response = "The Statement Date " + statementDate + " of Account number " + account + "  exist";
        //                return Content(response);
        //            }

        //            string statementDateString = Convert.ToDateTime(statementDate).ToString("yyyyMMdd");
        //            string Url = "http://10.1.67.145:8090/esb/RequestStatements?stdate=" + statementDateString + "&&acc=" + account;
        //            string responseStatus = GetUrl(Url);

        //            if (responseStatus == null)
        //            {
        //                response = "Error on getting response from remote server. Contact system support";
        //                return Content(response);
        //            }
        //            response = responseStatus;
        //        }

        //        else if (accountbic == "CORUTZTZ")
        //        {

        //            var apiClient = db.ApiClients
        //            .Where(a => a.ClientId == accountbic
        //            && a.MessageType == "Request")
        //            .FirstOrDefault();

        //            if (apiClient == null)
        //            {
        //                response = "Api client is not Found!";
        //                return Json(response, JsonRequestBehavior.AllowGet);
        //            }

        //            clientCertStorePath = apiClient.ClientPublicKey;
        //            clientCertPass = apiClient.ClientPassword;
        //            receiverUrl = apiClient.ClientUrl;
        //            receiverBic = apiClient.ClientId;

        //            receiverUrl = apiClient.ClientUrl;

        //            string requiredDate = year + "-" + month + "-" + tarehe;
        //            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        //            string MsgID = "STM" + timestamp;
        //            string RequestId = "REQ" + timestamp;

        //            string comercialXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
        //                         "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_statement_request.xsd\">" +
        //                         "<Header>" +
        //                            "<Sender>MOFPTZTZ</Sender>" +
        //                            "<Receiver>" + receiverBic + "</Receiver>" +
        //                            "<MsgId>" + MsgID + "</MsgId>" +
        //                            "<PaymentType>P113</PaymentType>" +
        //                            "<MessageType>REQUEST</MessageType>" +
        //                          "</Header>" +
        //                          "<RequestSummary>" +
        //                            "<RequestId>" + RequestId + "</RequestId>" +
        //                            "<CreDtTm>" + requiredDate + "T00:00:00</CreDtTm>" +
        //                            "<AcctNum>" + account + "</AcctNum>" +
        //                          "</RequestSummary>" +
        //                        "</Document>";

        //            XDocument xmlData = XDocument.Parse(comercialXml);
        //            var schemaPath = "";

        //            schemaPath = db.SystemConfigs
        //                .Where(a => a.ConfigName == "XMLSchemaPath")
        //                .Select(a => a.ConfigValue)
        //                .FirstOrDefault();

        //            schemaPath = schemaPath + "schema_statement_request.xsd";

        //            var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
        //            if (!isSchemaValid.ValidationStatus)
        //            {
        //                response = isSchemaValid.ValidationDesc;
        //                return Content(response);
        //            }

        //            string certPass = "";
        //            string certStorePath = "";

        //            certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
        //            certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;

        //            var hashSignature = DigitalSignature.GenerateSignature(comercialXml, certPass, certStorePath);
        //            var signedData = comercialXml + "|" + hashSignature;

        //            //log request
        //            Log.Information(signedData + "{Name}!", "OutgoingMessages");

        //            HttpWebResponse httpResponse = ServiceManager.SendToCommercialBank(signedData, receiverUrl);

        //            if (httpResponse == null)
        //            {
        //                response = "Error on getting response from remote server. Contact system support";
        //                return Content(response);
        //            }

        //            if (httpResponse.StatusCode == HttpStatusCode.OK)
        //            {
        //                StreamReader sr = new StreamReader(httpResponse.GetResponseStream());
        //                var xmlString = @sr.ReadToEnd().Trim().ToString();
        //                //log response
        //                Log.Information(xmlString + "{Name}!", "IncomingResponses");
        //                var dataArray = xmlString.Split('|');
        //                var dataPart = dataArray[0];
        //                var dataSignature = dataArray[1];

        //                XDocument xDocResponse = XDocument.Parse(dataPart, LoadOptions.None);
        //                //// Start of temporary check to simulate issues
        //                //if (Properties.Settings.Default.HostingEnvironment == "Live")
        //                //{
        //                //    xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
        //                //       new XElement("Document",
        //                //          new XElement("Header",
        //                //             new XElement("Sender", "CORUTZTZ"),
        //                //             new XElement("Receiver", "MOFPTZTZ"),
        //                //             new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
        //                //             new XElement("PaymentType", "P500"),
        //                //             new XElement("MessageType", "RESPONSE")),
        //                //          new XElement("ResponseSummary",
        //                //             new XElement("OrgMsgId", MsgID),
        //                //             new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
        //                //          new XElement("ResponseDetails",
        //                //             new XElement("PaymentRef", "NA"),
        //                //             new XElement("RespStatus", "ACCEPTED"),
        //                //             new XElement("Description", "Accepted Successfully")))));

        //                //    XmlWriterSettings settings = new XmlWriterSettings();
        //                //    settings.OmitXmlDeclaration = true;
        //                //    StringWriter sw = new StringWriter();
        //                //    using (XmlWriter xw = XmlWriter.Create(sw, settings))
        //                //    // or to write to a file...
        //                //    //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
        //                //    {
        //                //        xDocResponse.Save(xw);
        //                //    }
        //                //}
        //                // End of temporary check to simulate NMB issues

        //                //check schema
        //                schemaPath = db.SystemConfigs
        //                    .Where(a => a.ConfigName == "XMLSchemaPath")
        //                    .Select(a => a.ConfigValue)
        //                    .FirstOrDefault();

        //                schemaPath = schemaPath + "schema_block_response.xsd";
        //                isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
        //                if (!isSchemaValid.ValidationStatus)
        //                {
        //                    return Content("File submission failed," + isSchemaValid.ValidationDesc);
        //                }

        //                //validate signature
        //                var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
        //                if (!isSignatureValid)
        //                {
        //                    return Content("File submission failed, response signature is invalid");
        //                }


        //                var responseDetails = (from u in xDocResponse.Descendants("ResponseDetails")
        //                                       select new
        //                                       {
        //                                           PaymentRef = (string)u.Element("PaymentRef"),
        //                                           RespStatus = (string)u.Element("RespStatus"),
        //                                           Description = (string)u.Element("Description")

        //                                       }).FirstOrDefault();


        //                response = responseDetails.Description;

        //                return Content(response);
        //            }

        //            return Content(response);


        //        }else{

        //         var apiClient = db.ApiClients
        //         .Where(a => a.ClientId == accountbic
        //         && a.MessageType == "Request")
        //         .FirstOrDefault();

        //            if (apiClient == null)
        //            {
        //                response = "Api client is not Found!";
        //                return Json(response, JsonRequestBehavior.AllowGet);
        //            }

        //            clientCertStorePath = apiClient.ClientPublicKey;
        //            clientCertPass = apiClient.ClientPassword;
        //            receiverUrl = apiClient.ClientUrl;
        //            receiverBic = apiClient.ClientId;

        //            receiverUrl = apiClient.ClientUrl;

        //            string requiredDate = year + "-" + month + "-" + tarehe;
        //            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        //            string MsgID = "STM" + timestamp;
        //            string RequestId = "REQ" + timestamp;

        //            string comercialXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
        //                         "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_statement_request.xsd\">" +
        //                         "<Header>" +
        //                            "<Sender>MOFPTZTZ</Sender>" +
        //                            "<Receiver>" + receiverBic + "</Receiver>" +
        //                            "<MsgId>" + MsgID + "</MsgId>" +
        //                            "<PaymentType>P113</PaymentType>" +
        //                            "<MessageType>REQUEST</MessageType>" +
        //                          "</Header>" +
        //                          "<RequestSummary>" +
        //                            "<RequestId>" + RequestId + "</RequestId>" +
        //                            "<CreDtTm>" + requiredDate + "T00:00:00</CreDtTm>" +
        //                            "<AcctNum>" + account + "</AcctNum>" +
        //                          "</RequestSummary>" +
        //                        "</Document>";

        //            XDocument xmlData = XDocument.Parse(comercialXml);
        //            var schemaPath = "";

        //            schemaPath = db.SystemConfigs
        //                .Where(a => a.ConfigName == "XMLSchemaPath")
        //                .Select(a => a.ConfigValue)
        //                .FirstOrDefault();

        //            schemaPath = schemaPath + "schema_statement_request.xsd";

        //            var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
        //            if (!isSchemaValid.ValidationStatus)
        //            {
        //                response = isSchemaValid.ValidationDesc;
        //                return Content(response);
        //            }

        //            string certPass = "";
        //            string certStorePath = "";

        //            certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
        //            certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;

        //            var hashSignature = DigitalSignature.GenerateSignature(comercialXml, certPass, certStorePath);
        //            var signedData = comercialXml + "|" + hashSignature;

        //            //log request
        //            Log.Information(signedData + "{Name}!", "OutgoingMessages");

        //            HttpWebResponse httpResponse = ServiceManager.SendToCommercialBank(signedData, receiverUrl);

        //            if (httpResponse == null)
        //            {
        //                response = "Error on getting response from remote server. Contact system support";
        //                return Content(response);
        //            }

        //            if (httpResponse.StatusCode == HttpStatusCode.OK)
        //            {
        //                StreamReader sr = new StreamReader(httpResponse.GetResponseStream());
        //                var xmlString = @sr.ReadToEnd().Trim().ToString();
        //                //log response
        //                Log.Information(xmlString + "{Name}!", "IncomingResponses");
        //                var dataArray = xmlString.Split('|');
        //                var dataPart = dataArray[0];
        //                var dataSignature = dataArray[1];

        //                XDocument xDocResponse = XDocument.Parse(dataPart, LoadOptions.None);
        //                // Start of temporary check to simulate issues
        //                //if (Properties.Settings.Default.HostingEnvironment == "Live")
        //                //{
        //                //    xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
        //                //       new XElement("Document",
        //                //          new XElement("Header",
        //                //             new XElement("Sender", receiverBic),
        //                //             new XElement("Receiver", "MOFPTZTZ"),
        //                //             new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
        //                //             new XElement("PaymentType", "P500"),
        //                //             new XElement("MessageType", "RESPONSE")),
        //                //          new XElement("ResponseSummary",
        //                //             new XElement("OrgMsgId", MsgID),
        //                //             new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
        //                //          new XElement("ResponseDetails",
        //                //             new XElement("PaymentRef", "NA"),
        //                //             new XElement("RespStatus", "ACCEPTED"),
        //                //             new XElement("Description", "Accepted Successfully")))));

        //                //    XmlWriterSettings settings = new XmlWriterSettings();
        //                //    settings.OmitXmlDeclaration = true;
        //                //    StringWriter sw = new StringWriter();
        //                //    using (XmlWriter xw = XmlWriter.Create(sw, settings))
        //                //    // or to write to a file...
        //                //    //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
        //                //    {
        //                //        xDocResponse.Save(xw);
        //                //    }
        //                //}
        //                // End of temporary check to simulate NMB issues

        //                //check schema
        //                schemaPath = db.SystemConfigs
        //                    .Where(a => a.ConfigName == "XMLSchemaPath")
        //                    .Select(a => a.ConfigValue)
        //                    .FirstOrDefault();

        //                schemaPath = schemaPath + "schema_block_response.xsd";
        //                isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
        //                if (!isSchemaValid.ValidationStatus)
        //                {
        //                    return Content("File submission failed," + isSchemaValid.ValidationDesc);
        //                }

        //                //validate signature
        //                var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
        //                if (!isSignatureValid)
        //                {
        //                    return Content("File submission failed, response signature is invalid");
        //                }

        //                var responseDetails = (from u in xDocResponse.Descendants("ResponseDetails")
        //                                       select new
        //                                       {
        //                                           PaymentRef = (string)u.Element("PaymentRef"),
        //                                           RespStatus = (string)u.Element("RespStatus"),
        //                                           Description = (string)u.Element("Description")

        //                                       }).FirstOrDefault();


        //                response = responseDetails.Description;

        //                return Content(response);
        //            }

        //            return Content(response);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorSignal.FromCurrentContext().Raise(ex);
        //        response = "Internal Server Error. " + ex.Message.ToString();
        //    }

        //    return Content(response);
        //}


        public ActionResult RequestBankStatement(string account, DateTime statementDate)
        {
            string response = "";

            if (statementDate.Year == DateTime.Now.Year && statementDate.Month == DateTime.Now.Month
            && statementDate.Day == DateTime.Now.Day)
            {
                response = "You can not request the Bank statement for today";
                return Content(response);
            }


            string status =RequestStatement(account, statementDate);

            response = status;

            return Content(response);
        }

       private string RequestStatement(string account, DateTime statementDate)
        {
            string response = "";
            string month;
            string tarehe;
            int year;
            string receiverUrl;
            string clientCertStorePath;
            string clientCertPass;
            string receiverBic;
            DateTime dateNow;


            string accountbic = db.Accounts
                .Where(a => a.AccountNo == account)
                .Select(a => a.BankBIC)
                .FirstOrDefault();


            month = statementDate.ToString("MM");
            year = statementDate.Year;
            tarehe = statementDate.ToString("dd");
            DateTime statementDateFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

            db.Database.CommandTimeout = 1200;
            try
            {

                //if (statementDate.ToString("MM/dd/yyyy") = DateTime.Now.ToString("MM/dd/yyyy"))
                //{
                //    response = "Invalid Request";
                //    return response;
                //}

                List<BankStatementSummary> BankStatementSummaryList = null;
                BankStatementSummaryList = db.BankStatementSummarys
                    .Where(a => a.BankAccountNumber == account
                    && (a.StatementDate == statementDateFormated))
                    .ToList();

                if (BankStatementSummaryList.Count() > 0)
                {
                    response = "The Statement Date " + statementDate + " of Account number " + account + "  exist";
                    return response;
                }

                if (accountbic == "TANZTZTX")
                {
                    //List<BankStatementSummary> BankStatementSummaryList = null;
                    //BankStatementSummaryList = db.BankStatementSummarys
                    //    .Where(a => a.BankAccountNumber == account
                    //    && (a.StatementDate == statementDateFormated))
                    //    .ToList();

                    //if (BankStatementSummaryList.Count() > 0)
                    //{
                    //    response = "The Statement Date " + statementDate + " of Account number " + account + "  exist";
                    //    return response;
                    //}

                    string statementDateString = Convert.ToDateTime(statementDate).ToString("yyyyMMdd");
                    string Url = "http://10.1.67.145:8090/esb/RequestStatements?stdate=" + statementDateString + "&&acc=" + account;
                    string responseStatus = GetUrl(Url);

                    if (responseStatus == null)
                    {
                        response = "Error on getting response from remote server. Contact system support";
                        return response;
                    }
                    response = responseStatus;
                }

                else if (accountbic == "CORUTZTZ")
                {

                    var apiClient = db.ApiClients
                    .Where(a => a.ClientId == accountbic
                    && a.MessageType == "Request")
                    .FirstOrDefault();

                    if (apiClient == null)
                    {
                        response = "Api client is not Found!";
                        return response;
                    }

                    clientCertStorePath = apiClient.ClientPublicKey;
                    clientCertPass = apiClient.ClientPassword;
                    receiverUrl = apiClient.ClientUrl;
                    receiverBic = apiClient.ClientId;

                    receiverUrl = apiClient.ClientUrl;

                    string requiredDate = year + "-" + month + "-" + tarehe;
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string MsgID = "STM" + timestamp;
                    string RequestId = "REQ" + timestamp;

                    string comercialXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
                                 "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_statement_request.xsd\">" +
                                 "<Header>" +
                                    "<Sender>MOFPTZTZ</Sender>" +
                                    "<Receiver>" + receiverBic + "</Receiver>" +
                                    "<MsgId>" + MsgID + "</MsgId>" +
                                    "<PaymentType>P113</PaymentType>" +
                                    "<MessageType>REQUEST</MessageType>" +
                                  "</Header>" +
                                  "<RequestSummary>" +
                                    "<RequestId>" + RequestId + "</RequestId>" +
                                    "<CreDtTm>" + requiredDate + "T00:00:00</CreDtTm>" +
                                    "<AcctNum>" + account + "</AcctNum>" +
                                  "</RequestSummary>" +
                                "</Document>";

                    XDocument xmlData = XDocument.Parse(comercialXml);
                    var schemaPath = "";

                    schemaPath = db.SystemConfigs
                        .Where(a => a.ConfigName == "XMLSchemaPath")
                        .Select(a => a.ConfigValue)
                        .FirstOrDefault();

                    schemaPath = schemaPath + "schema_statement_request.xsd";

                    var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
                    if (!isSchemaValid.ValidationStatus)
                    {
                        response = isSchemaValid.ValidationDesc;
                        return response;
                    }

                    string certPass = "";
                    string certStorePath = "";

                    certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
                    certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;

                    var hashSignature = DigitalSignature.GenerateSignature(comercialXml, certPass, certStorePath);
                    var signedData = comercialXml + "|" + hashSignature;

                    //log request
                    Log.Information(signedData + "{Name}!", "OutgoingMessages");

                    HttpWebResponse httpResponse = serviceManager.SendToCommercialBank(signedData, receiverUrl);

                    if (httpResponse == null)
                    {
                        response = "Error on getting response from remote server. Contact system support";
                        return response;
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
                        //// Start of temporary check to simulate issues
                        //if (Properties.Settings.Default.HostingEnvironment == "Live")
                        //{
                        //    xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                        //       new XElement("Document",
                        //          new XElement("Header",
                        //             new XElement("Sender", "CORUTZTZ"),
                        //             new XElement("Receiver", "MOFPTZTZ"),
                        //             new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                        //             new XElement("PaymentType", "P500"),
                        //             new XElement("MessageType", "RESPONSE")),
                        //          new XElement("ResponseSummary",
                        //             new XElement("OrgMsgId", MsgID),
                        //             new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                        //          new XElement("ResponseDetails",
                        //             new XElement("PaymentRef", "NA"),
                        //             new XElement("RespStatus", "ACCEPTED"),
                        //             new XElement("Description", "Accepted Successfully")))));

                        //    XmlWriterSettings settings = new XmlWriterSettings();
                        //    settings.OmitXmlDeclaration = true;
                        //    StringWriter sw = new StringWriter();
                        //    using (XmlWriter xw = XmlWriter.Create(sw, settings))
                        //    // or to write to a file...
                        //    //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
                        //    {
                        //        xDocResponse.Save(xw);
                        //    }
                        //}
                        // End of temporary check to simulate NMB issues

                        //check schema
                       /* schemaPath = db.SystemConfigs
                            .Where(a => a.ConfigName == "XMLSchemaPath")
                            .Select(a => a.ConfigValue)
                            .FirstOrDefault();

                        schemaPath = schemaPath + "schema_block_response.xsd";
                        isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
                        if (!isSchemaValid.ValidationStatus)
                        {
                            response = "File submission failed," + isSchemaValid.ValidationDesc;
                            return response;
                        }

                        //validate signature
                        var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
                        if (!isSignatureValid)
                        {
                            response = "File submission failed, response signature is invalid";
                            return response;
                        }
                       */

                        var responseDetails = (from u in xDocResponse.Descendants("ResponseDetails")
                                               select new
                                               {
                                                   PaymentRef = (string)u.Element("PaymentRef"),
                                                   RespStatus = (string)u.Element("RespStatus"),
                                                   Description = (string)u.Element("Description")

                                               }).FirstOrDefault();


                        response = responseDetails.Description;

                        return response;
                    }

                    return response;

                }
                else if (accountbic == "NMIBTZTZ")
                {

                    var apiClient = db.ApiClients
                    .Where(a => a.ClientId == accountbic)
                    .FirstOrDefault();

                    if (apiClient == null)
                    {
                        response = "Api client is not Found!";
                        return response;
                    }

                    clientCertStorePath = apiClient.ClientPublicKey;
                    clientCertPass = apiClient.ClientPassword;
                    receiverUrl = apiClient.ClientUrl;
                    receiverBic = apiClient.ClientId;

                    receiverUrl = apiClient.ClientUrl;

                    string requiredDate = year + "-" + month + "-" + tarehe;
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string MsgID = "STM" + timestamp;
                    string RequestId = "REQ" + timestamp;

                    string comercialXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
                                 "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_statement_request.xsd\">" +
                                 "<Header>" +
                                    "<Sender>MOFPTZTZ</Sender>" +
                                    "<Receiver>" + receiverBic + "</Receiver>" +
                                    "<MsgId>" + MsgID + "</MsgId>" +
                                    "<PaymentType>P113</PaymentType>" +
                                    "<MessageType>REQUEST</MessageType>" +
                                  "</Header>" +
                                  "<RequestSummary>" +
                                    "<RequestId>" + RequestId + "</RequestId>" +
                                    "<CreDtTm>" + requiredDate + "T00:00:00</CreDtTm>" +
                                    "<AcctNum>" + account + "</AcctNum>" +
                                  "</RequestSummary>" +
                                "</Document>";

                    XDocument xmlData = XDocument.Parse(comercialXml);
                    var schemaPath = "";

                    schemaPath = db.SystemConfigs
                        .Where(a => a.ConfigName == "XMLSchemaPath")
                        .Select(a => a.ConfigValue)
                        .FirstOrDefault();

                    schemaPath = schemaPath + "schema_statement_request.xsd";

                    var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
                    if (!isSchemaValid.ValidationStatus)
                    {
                        response = isSchemaValid.ValidationDesc;
                        return response;
                    }

                    string certPass = "";
                    string certStorePath = "";

                    certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
                    certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;

                    var hashSignature = DigitalSignature.GenerateSignature(comercialXml, certPass, certStorePath);
                    var signedData = comercialXml + "|" + hashSignature;

                    //log request
                    Log.Information(signedData + "{Name}!", "OutgoingMessages");

                    HttpWebResponse httpResponse = serviceManager.SendToCommercialBank(signedData, receiverUrl);

                    if (httpResponse == null)
                    {
                        response = "Error on getting response from remote server. Contact system support";
                        return response;
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
                        //// Start of temporary check to simulate issues
                        //if (Properties.Settings.Default.HostingEnvironment == "Live")
                        //{
                        //    xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                        //       new XElement("Document",
                        //          new XElement("Header",
                        //             new XElement("Sender", "CORUTZTZ"),
                        //             new XElement("Receiver", "MOFPTZTZ"),
                        //             new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                        //             new XElement("PaymentType", "P500"),
                        //             new XElement("MessageType", "RESPONSE")),
                        //          new XElement("ResponseSummary",
                        //             new XElement("OrgMsgId", MsgID),
                        //             new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                        //          new XElement("ResponseDetails",
                        //             new XElement("PaymentRef", "NA"),
                        //             new XElement("RespStatus", "ACCEPTED"),
                        //             new XElement("Description", "Accepted Successfully")))));

                        //    XmlWriterSettings settings = new XmlWriterSettings();
                        //    settings.OmitXmlDeclaration = true;
                        //    StringWriter sw = new StringWriter();
                        //    using (XmlWriter xw = XmlWriter.Create(sw, settings))
                        //    // or to write to a file...
                        //    //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
                        //    {
                        //        xDocResponse.Save(xw);
                        //    }
                        //}
                        // End of temporary check to simulate NMB issues

                        //check schema
                        /* schemaPath = db.SystemConfigs
                             .Where(a => a.ConfigName == "XMLSchemaPath")
                             .Select(a => a.ConfigValue)
                             .FirstOrDefault();

                         schemaPath = schemaPath + "schema_block_response.xsd";
                         isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
                         if (!isSchemaValid.ValidationStatus)
                         {
                             response = "File submission failed," + isSchemaValid.ValidationDesc;
                             return response;
                         }

                         //validate signature
                         var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
                         if (!isSignatureValid)
                         {
                             response = "File submission failed, response signature is invalid";
                             return response;
                         }
                        */

                        var responseDetails = (from u in xDocResponse.Descendants("ResponseDetails")
                                               select new
                                               {
                                                   PaymentRef = (string)u.Element("PaymentRef"),
                                                   RespStatus = (string)u.Element("RespStatus"),
                                                   Description = (string)u.Element("Description")

                                               }).FirstOrDefault();


                        response = responseDetails.Description;

                        return response;
                    }

                    return response;

                }
                else

                {

                    var apiClient = db.ApiClients
                    .Where(a => a.ClientId == accountbic
                    && a.MessageType == "Request")
                    .FirstOrDefault();

                    if (apiClient == null)
                    {
                        response = "Api client is not Found!";
                        return response;
                    }

                    clientCertStorePath = apiClient.ClientPublicKey;
                    clientCertPass = apiClient.ClientPassword;
                    receiverUrl = apiClient.ClientUrl;
                    receiverBic = apiClient.ClientId;

                    receiverUrl = apiClient.ClientUrl;

                    string requiredDate = year + "-" + month + "-" + tarehe;
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string MsgID = "STM" + timestamp;
                    string RequestId = "REQ" + timestamp;

                    string comercialXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" +
                                 "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_statement_request.xsd\">" +
                                 "<Header>" +
                                    "<Sender>MOFPTZTZ</Sender>" +
                                    "<Receiver>" + receiverBic + "</Receiver>" +
                                    "<MsgId>" + MsgID + "</MsgId>" +
                                    "<PaymentType>P113</PaymentType>" +
                                    "<MessageType>REQUEST</MessageType>" +
                                  "</Header>" +
                                  "<RequestSummary>" +
                                    "<RequestId>" + RequestId + "</RequestId>" +
                                    "<CreDtTm>" + requiredDate + "T00:00:00</CreDtTm>" +
                                    "<AcctNum>" + account + "</AcctNum>" +
                                  "</RequestSummary>" +
                                "</Document>";

                    XDocument xmlData = XDocument.Parse(comercialXml);
                    var schemaPath = "";

                    schemaPath = db.SystemConfigs
                        .Where(a => a.ConfigName == "XMLSchemaPath")
                        .Select(a => a.ConfigValue)
                        .FirstOrDefault();

                    schemaPath = schemaPath + "schema_statement_request.xsd";

                    var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
                    if (!isSchemaValid.ValidationStatus)
                    {
                        response = isSchemaValid.ValidationDesc;
                        return response;
                    }

                    string certPass = "";
                    string certStorePath = "";

                    certPass = Properties.Settings.Default.MofpPrivatePfxPasswd;
                    certStorePath = Properties.Settings.Default.MofpPrivatePfxPath;

                    var hashSignature = DigitalSignature.GenerateSignature(comercialXml, certPass, certStorePath);
                    var signedData = comercialXml + "|" + hashSignature;

                    //log request
                    Log.Information(signedData + "{Name}!", "OutgoingMessages");

                    HttpWebResponse httpResponse = serviceManager.SendToCommercialBank(signedData, receiverUrl);

                    if (httpResponse == null)
                    {
                        response = "Error on getting response from remote server. Contact system support";
                        return response;
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
                        // Start of temporary check to simulate issues
                        //if (Properties.Settings.Default.HostingEnvironment == "Live")
                        //{
                        //    xDocResponse = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                        //       new XElement("Document",
                        //          new XElement("Header",
                        //             new XElement("Sender", receiverBic),
                        //             new XElement("Receiver", "MOFPTZTZ"),
                        //             new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                        //             new XElement("PaymentType", "P500"),
                        //             new XElement("MessageType", "RESPONSE")),
                        //          new XElement("ResponseSummary",
                        //             new XElement("OrgMsgId", MsgID),
                        //             new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                        //          new XElement("ResponseDetails",
                        //             new XElement("PaymentRef", "NA"),
                        //             new XElement("RespStatus", "ACCEPTED"),
                        //             new XElement("Description", "Accepted Successfully")))));

                        //    XmlWriterSettings settings = new XmlWriterSettings();
                        //    settings.OmitXmlDeclaration = true;
                        //    StringWriter sw = new StringWriter();
                        //    using (XmlWriter xw = XmlWriter.Create(sw, settings))
                        //    // or to write to a file...
                        //    //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
                        //    {
                        //        xDocResponse.Save(xw);
                        //    }
                        //}
                        // End of temporary check to simulate NMB issues

                        //check schema
                        schemaPath = db.SystemConfigs
                            .Where(a => a.ConfigName == "XMLSchemaPath")
                            .Select(a => a.ConfigValue)
                            .FirstOrDefault();

                        schemaPath = schemaPath + "schema_block_response.xsd";
                        isSchemaValid = XMLTools.ValidateXml(xDocResponse, schemaPath);
                        if (!isSchemaValid.ValidationStatus)
                        {
                            response = "File submission failed," + isSchemaValid.ValidationDesc;
                            return response;
                        }

                        //validate signature
                        var isSignatureValid = DigitalSignature.VerifySignature(clientCertStorePath, clientCertPass, dataPart, dataSignature);
                        if (!isSignatureValid)
                        {
                            response = "File submission failed, response signature is invalid";
                            return response;
                         
                        }

                        var responseDetails = (from u in xDocResponse.Descendants("ResponseDetails")
                                               select new
                                               {
                                                   PaymentRef = (string)u.Element("PaymentRef"),
                                                   RespStatus = (string)u.Element("RespStatus"),
                                                   Description = (string)u.Element("Description")

                                               }).FirstOrDefault();


                        response = responseDetails.Description;

                        return response;
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Internal Server Error. " + ex.Message.ToString();
            }

            return response;
        }


        private string GetUrl(string destinationUrl)
        {
            string responseStatus;

            WebRequest request = WebRequest.Create(destinationUrl);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Credentials = CredentialCache.DefaultCredentials;

            WebResponse response = request.GetResponse();

            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                // Display the content.  
              responseStatus= responseFromServer;
            }

            // Close the response.  
            response.Close();
            return responseStatus.ToString();
        }


        [HttpGet]
        public ActionResult BankStatementBalanceList()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            ViewBag.accountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");
            return View();
        }

        public JsonResult GetBankStatementBalanceList(string accountNumber,DateTime statementDateFrom, DateTime statementDateTo)
        {
         
            int[] statementSummaryId;
            string month;
            string tarehe;
            int year;
            decimal totalReceiptBalance = 0;
            decimal totalPaymentBalance = 0;

            month = statementDateFrom.ToString("MM");
            year = statementDateFrom.Year;
            tarehe = statementDateFrom.ToString("dd");
            DateTime statementDateFormFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

            month = statementDateTo.ToString("MM");
            year = statementDateTo.Year;
            tarehe = statementDateTo.ToString("dd");
            DateTime statementDateToFormated = Convert.ToDateTime(year + "/" + month + "/" + tarehe);

            db.Database.CommandTimeout = 1200;
            List<BankStatementSummary> BankStatementSummaryList = null;
            BankStatementSummaryList = db.BankStatementSummarys
                .Where(a => a.BankAccountNumber == accountNumber
                && (a.StatementDate >= statementDateFormFormated && a.StatementDate <= statementDateToFormated))
                .ToList();

            statementSummaryId = BankStatementSummaryList
                .Select(a => a.BankStatementSummaryId)
                .ToArray();

            var BankStatementSummaryVMList = new List<BankStatementSummaryVM>();
            foreach (var summaryId in statementSummaryId)
            {
                List<BankStatementDetail> BankStatementDetailList = null;
                BankStatementDetailList = (from b in db.BankStatementDetails
                                           where(b.BankStatementSummaryId== summaryId)
                                           select b).ToList();

                totalReceiptBalance = (decimal)BankStatementDetailList
                    .Where(a => a.TransactionType == "CR")
                    .Sum(a => a.TransactionAmount);

                totalPaymentBalance = (decimal)BankStatementDetailList
                    .Where(a => a.TransactionType == "DR")
                    .Sum(a => a.TransactionAmount);

                BankStatementSummary StatementSummaryLis = new BankStatementSummary();
                StatementSummaryLis =  db.BankStatementSummarys
                    .Where (a => a.BankStatementSummaryId == summaryId)
                    .FirstOrDefault();

                var bankstatementvm = new BankStatementSummaryVM()
                {
                    AccountNumber = StatementSummaryLis.BankAccountNumber,
                    AccountName = StatementSummaryLis.BankAccountName,
                    StatementDate = StatementSummaryLis.StatementDateFormatted,
                    OpeningBalance = StatementSummaryLis.OpeningBalance,
                    ClosingBalance = StatementSummaryLis.ClosingBalance,
                    CreatedDateTime = StatementSummaryLis.CreatedDateTime,
                    OverallStatus = StatementSummaryLis.OverallStatus,
                    Sources = StatementSummaryLis.Sources,
                    TotalCredit = totalReceiptBalance,
                    TotalDebit = totalPaymentBalance,
                };
              
                BankStatementSummaryVMList.Add(bankstatementvm);
            }

            var response = Json(new { data = BankStatementSummaryVMList.OrderBy(a => a.StatementDate) }, JsonRequestBehavior.AllowGet);
            response.MaxJsonLength = int.MaxValue;
            return response;
            //return Json(new { data = BankStatementSummaryVMList.OrderBy(a=>a.StatementDate) }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet, Authorize()]
        public ActionResult RequestAllBankStatement()
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionCode = userPaystation.InstitutionCode;
            UploadBankStatementVM vm = new UploadBankStatementVM();
            var AccountList = serviceManager
                .GetAccountListrec(institutionCode)
                .Select(a => new { a.AccountNo, a.AccountName, a.AccountNoAccountName })
                .Distinct();
            vm.AccountNumberNameList = new SelectList(AccountList, "AccountNo", "AccountNoAccountName");

            return View(vm);
        }

        public ActionResult RequestAllBankStatement(string account,DateTime startDate,DateTime EndDate)
        {
          
            string response = "";
            try
            {
                var today = DateTime.Today;
                var yestarday = today.AddDays(-1);
                DateTime EndDated = yestarday;

                DateTime startDated = new DateTime(2020, 01, 01);

                BankStatementAnalysis(account, startDated, EndDated);

                List<BankStatementBalanceReview> bankStatementStatusList = db.BankStatementBalanceReviews
                                         .Where(a => a.OverallStatus == "MISSING" && a.BankAccountNumber== account)
                                         .OrderBy(a => a.StatementDate)
                                         .ToList();

                if (bankStatementStatusList.Count == 0)
                {
                    response = "No Missing Bank Statement for Account Number " + account;
                    return Json(response,JsonRequestBehavior.AllowGet);
                }

                foreach (BankStatementBalanceReview statementList in bankStatementStatusList)
                {
                    response += statementList.StatementDate.ToString("yyyy-MM-dd") + " ";
                    response += statementList.BankAccountNumber + " ";
                    response += RequestStatement(statementList.BankAccountNumber, statementList.StatementDate) + " ";
                }

               // BankStatementAnalysis(account, startDate, EndDate);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Internal Server Error. " + ex.Message.ToString();
            }

            return Content(response);
        }

    }
}