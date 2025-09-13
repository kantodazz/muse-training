using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class BackLogController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();
        private delegate DateTime ToDateTime(Int64 value);
        public ActionResult BackLogList()
        {
            return View();
        }

        [HttpGet]
        public ActionResult BackLogApprove()
        {
            return View();
        }
        public ActionResult BackLogEntry()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

            ViewBag.accountsList = db.InstitutionAccounts
                .Where(a => a.InstitutionId == userPaystation.InstitutionId
                  && a.OverallStatus != "Cancelled")
                .ToList();

            var subBudgetClassList = db.CurrencyRateViews
                 .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                   && a.SubBudgetClass != null)
                 .OrderBy(a => a.SubBudgetClass)
                 .ToList();
            ViewBag.subBudgetClassList = subBudgetClassList;
            ViewBag.FinancialYearList = db.FinancialYears.ToList();
            return View();
        }

        public ActionResult BackLogCreate(BackLogFormVm vm)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

            InstitutionAccount institutionAccount =
                db.InstitutionAccounts.Find(vm.InstitutionAccountId);

            if (institutionAccount == null)
            {
                return Content("Institution account could not be found..!!");
            }

            var extension = Path.GetExtension(vm.file.FileName);
            if (extension != ".xlsx" && extension != ".xls")
            {
                return Content("Expected .xlsx or .xls format");
            }

            string path = Server.MapPath("~/Uploads/BackLog/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = path + Path.GetFileName(vm.file.FileName);
            vm.file.SaveAs(filePath);

            var resp = JService.ExcelToJson<BackLogVm>(filePath);
            if (resp.Error != string.Empty)
            {
                return Content(resp.Error);
            }

            List<BackLogVm> list = resp.Response;
            if (list.Sum(a => a.Amount) == 0)
            {
                return Content("Empty Excel Was Uploaded.");
            }


            foreach (BackLogVm backLogVm in list)
            {
                //Check for Payee
                //Payee payee = db.Payees
                //       .Where(a => a.ApprovalStatus != "Cancelled"
                //         && a.PayeeCode == backLogVm.PayeeCode)
                //       .FirstOrDefault();
                //if (payee == null)
                //{
                //    return Content("PayeeCode = " + backLogVm.PayeeCode+" Does not exist..!!");
                //}

                var payeeType = db.PayeeTypes
                   .Where(a => a.PayeeTypeCode == backLogVm.PayeeType.Trim()
                     && a.Status != "Cancelled")
                   .FirstOrDefault();

                if (payeeType == null)
                {
                    string response = "Vendor setup is incomplete. There is no payee type setup for '" + backLogVm.PayeeType + "'. Please contact Administrator!";
                    return Content(response);
                }

                var crCodes = db.JournalTypeViews
                .Where(a => a.CrGfsCode == payeeType.GfsCode
                 && a.SubBudgetClass == vm.SubBudgetClass
                 && a.InstitutionCode == userPaystation.InstitutionCode)
                .FirstOrDefault();
                if (crCodes == null)
                {
                    string response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + vm.SubBudgetClass + "' is missing. Please contact Administrator!";
                    return Content(response);
                }

                var coa = db.COAs
                    .Where(a => a.GlAccount == backLogVm.GlAccount.Trim())
                    .Count();

                if (coa == 0)
                {
                    return Content("GL Account " + backLogVm.GlAccount.Trim() + "Doesnot Exist..!!");
                }
            }

            //Check for Fund Balance
            db.Database.CommandTimeout = 120;
            //List<FundBalanceView> fv = db.FundBalanceViews
            //    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
            //        && a.SubBudgetClass == vm.SubBudgetClass
            //        && a.FinancialYear == vm.FinancialYear)
            //    .ToList();

            //if (fv.Sum(a=>a.FundBalance) < list.Sum(a => a.Amount))
            //{
            //    return Content("Insufficient Fund Balance FundBalance="
            //        + fv.Sum(a => a.FundBalance) +" Batch Amount ="+ list.Sum(a => a.Amount));
            //}

            var payerBank = db.InstitutionAccounts
                   .Where(a => a.SubBudgetClass == vm.SubBudgetClass
                     && a.InstitutionCode == userPaystation.InstitutionCode
                     && a.IsTSA == false
                     && a.OverallStatus != "Cancelled"
                   ).FirstOrDefault();

            if (payerBank == null)
            {
                string response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + vm.SubBudgetClass + "'. Please consult Administrator!";
                return Content(response);
            }


            var unappliedAccount = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && a.AccountType.ToUpper() == "UNAPPLIED"
                && a.IsTSA == false
                && a.OverallStatus != "Cancelled"
                ).FirstOrDefault();

            if (unappliedAccount == null)
            {
                string response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                return Content(response);
            }

            BackLogTransactionSummary backLogTransactionSummary;
            try
            {
                backLogTransactionSummary = new BackLogTransactionSummary
                {
                    PayerBankAccount = institutionAccount.AccountNumber,
                    PayerBankName = institutionAccount.BankName,
                    ExcelFilePath = vm.file.FileName,
                    NumTrx = list.Count(),
                    OperationalAmount = list.Sum(a => a.Amount),
                    BaseAmount = list.Sum(a => a.Amount),
                    OverallStatus = "Pending",
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity.GetUserName()
                };

                db.BackLogTransactionSummaries.Add(backLogTransactionSummary);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }

            foreach (BackLogVm backLogVm in list)
            {
                try
                {
                    ToDateTime toDateTime = delegate (Int64 date)
                    {
                        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddMilliseconds(date)
                        .ToLocalTime();
                    };

                    BackLogTransaction backLogTransaction = new BackLogTransaction
                    {
                        SourceModule = "BackLog",
                        FundingReferenceNo = backLogVm.FundingReferenceNo,
                        JournalTypeCode = "BL",
                        InvoiceNo = backLogVm.InvoiceNo,
                        InvoiceDate = toDateTime(backLogVm.InvoiceDate),
                        Narration = backLogVm.Narration,
                        PayeeType = backLogVm.PayeeType,
                        PayeeCode = backLogVm.PayeeCode,
                        Payeename = backLogVm.PayeeName,
                        PayeeBankAccount = backLogVm.PayeeBankAccount,
                        PayeeBankName = backLogVm.PayeeBankName,
                        PayeeAccountName = backLogVm.PayeeAccountName,
                        PayerBankAccount = institutionAccount.AccountNumber,
                        PayerBankName = institutionAccount.BankName,
                        OperationalAmount = backLogVm.Amount,
                        BaseAmount = backLogVm.Amount,
                        BaseCurrency = institutionAccount.Currency,
                        OperationalCurrency = vm.operationalCurrencyCode,
                        ExchangeRate = vm.exchangeRate,
                        ApplyDate = toDateTime(backLogVm.ApplyDate),
                        ChequeNo = backLogVm.ChequeNo,
                        SubBudgetClass = vm.SubBudgetClass,
                        GlAccount = backLogVm.GlAccount.Trim(),
                        PaymentMethod = "Cheque",
                        FinancialYear = vm.FinancialYear,
                        CreatedBy = User.Identity.GetUserName(),
                        CreatedAt = DateTime.Now,
                        OverallStatus = "Pending",
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionName = userPaystation.Institution.InstitutionName,
                        BackLogTransactionSummaryId = backLogTransactionSummary.BackLogTransactionSummaryId
                    };

                    db.BackLogTransactions.Add(backLogTransaction);
                    db.SaveChanges();

                    var blt = db.BackLogTransactions
                   .Find(backLogTransaction.BackLogTransactionId);

                    blt.PVNo = ServiceManager
                    .GetLegalNumber(db, userPaystation.InstitutionCode, "B", blt.BackLogTransactionId);

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Content(ex.Message.ToString());
                }
            }

            return Content("Success");
        }

        public ActionResult BackLogCancel()
        {
            return Content("Success");
        }

        public ActionResult BackLogConfirm()
        {
            return Content("Success");
        }

        [HttpPost]
        public ActionResult BackLogApprove(int Id)
        {
            try
            {
                db.Database.CommandTimeout = 1200;
                BackLogTransactionSummary backLogTransactionSummary =
                    db.BackLogTransactionSummaries.Find(Id);
                if (backLogTransactionSummary == null)
                {
                    return Content("Invalid Backlog Transaction");
                }

                backLogTransactionSummary.OverallStatus = "Approved";
                backLogTransactionSummary.ApprovedAt = DateTime.Now;
                backLogTransactionSummary.ApprovedBy = User.Identity.GetUserName();
                db.SaveChanges();

                List<BackLogTransaction> list = db.BackLogTransactions
                    .Where(a => a.BackLogTransactionSummaryId == Id
                    && a.OverallStatus != "Cancelled")
                    .ToList();

                foreach (var item in list)
                {
                    var blt = db.BackLogTransactions.Find(item.BackLogTransactionId);
                    blt.OverallStatus = "Approved";
                    blt.JournalTypeCode = "BLP";
                    blt.ApprovedAt = DateTime.Now;
                    blt.ApprovedBy = User.Identity.GetUserName();

                    PaymentVoucher pv = db.PaymentVouchers
                        .Where(a => a.PaymentSummaryNo == item.ChequeNo
                         && a.SourceModule == "BackLog")
                        .FirstOrDefault();

                    pv.OverallStatus = "BackLog-Approved";
                    pv.ApprovedAt = DateTime.Now;
                    pv.ApprovedBy = User.Identity.GetUserName();
                    db.SaveChanges();
                }

                var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", "BLP") };
                db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);
            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult BackLogConfirm(int Id)
        {
            try
            {
                db.Database.CommandTimeout = 1200;
                BackLogTransactionSummary backLogTransactionSummary =
                    db.BackLogTransactionSummaries.Find(Id);
                if (backLogTransactionSummary == null)
                {
                    return Content("Invalid Backlog Transaction");
                }

                backLogTransactionSummary.OverallStatus = "Confirmed";
                backLogTransactionSummary.ConfirmedAt = DateTime.Now;
                backLogTransactionSummary.ConfirmedBy = User.Identity.GetUserName();
                db.SaveChanges();

                List<BackLogTransaction> list = db.BackLogTransactions
                    .Where(a => a.BackLogTransactionSummaryId == Id
                    && a.OverallStatus != "Cancelled")
                    .ToList();


                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

                foreach (var item in list)
                {
                    var blt = db.BackLogTransactions.Find(item.BackLogTransactionId);
                    blt.OverallStatus = "Confirmed";
                    blt.ConfirmedAt = DateTime.Now;
                    blt.ConfirmedBy = User.Identity.GetUserName();


                    //Payee payee =
                    //    db.Payees
                    //    .Where(a => a.ApprovalStatus != "Cancelled"
                    //     && a.PayeeCode == blt.PayeeCode)
                    //    .FirstOrDefault();
                    //if (payee == null)
                    //{
                    //    backLogTransactionSummary.OverallStatus = "Pending";
                    //    db.SaveChanges();
                    //    return Content("Invalid Payee with PayeeCode = " + blt.PayeeCode);
                    //}
                    var payerBank = db.InstitutionAccounts
                    .Where(a => a.SubBudgetClass == blt.SubBudgetClass
                      && a.InstitutionCode == userPaystation.InstitutionCode
                      && a.IsTSA == false
                      && a.OverallStatus != "Cancelled"
                    ).FirstOrDefault();
                    if (payerBank == null)
                    {
                        string response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + blt.SubBudgetClass + "'. Please consult Administrator!";
                        return Content(response);
                    }

                    var payeeType = db.PayeeTypes
                        .Where(a => a.PayeeTypeCode == blt.PayeeType
                          && a.Status != "Cancelled")
                        .FirstOrDefault();

                    if (payeeType == null)
                    {
                        string response = "Vendor setup is incomplete. There is no payee type setup for '" + blt.PayeeType + "'. Please contact Administrator!";
                        return Content(response);
                    }

                    var crCodes = db.JournalTypeViews
                    .Where(a => a.CrGfsCode == payeeType.GfsCode
                      && a.SubBudgetClass == blt.SubBudgetClass
                      && a.InstitutionCode == userPaystation.InstitutionCode)
                    .FirstOrDefault();

                    if (crCodes == null)
                    {
                        string response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + blt.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return Content(response);
                    }

                    var unappliedAccount = db.InstitutionAccounts
                        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                          && a.AccountType.ToUpper() == "UNAPPLIED"
                          && a.IsTSA == false
                          && a.OverallStatus != "Cancelled"
                        ).FirstOrDefault();

                    if (unappliedAccount == null)
                    {
                        string response = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + userPaystation.Institution.InstitutionName + "'. Please consult Administrator!";
                        return Content(response);
                    }

                    PaymentVoucher paymentVoucher = new PaymentVoucher
                    {
                        SourceModule = "BackLog",
                        SourceModuleReferenceNo = "NA",
                        PayeeType = blt.PayeeType,
                        InvoiceNo = blt.InvoiceNo,
                        InvoiceDate = blt.InvoiceDate,
                        //PayeeCode = payee.PayeeCode,
                        Payeename = blt.Payeename,
                        ChequeNo = blt.ChequeNo,
                        PayeeBankAccount = blt.PayeeBankAccount,
                        PayeeBankName = blt.PayeeBankName,
                        PayeeAccountName = blt.PayeeAccountName,
                        Narration = blt.Narration,
                        OperationalAmount = blt.OperationalAmount,
                        BaseAmount = blt.BaseAmount,
                        ExchangeRate = blt.ExchangeRate,
                        ApplyDate = blt.ApplyDate,
                        PaymentMethod = blt.PaymentMethod,
                        FinancialYear = item.FinancialYear,
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now,
                        OverallStatus = "BackLog-Verified",
                        Book = "MAIN",
                        PaymentSummaryNo = blt.ChequeNo,
                        InstitutionId = userPaystation.InstitutionId,
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionName = userPaystation.Institution.InstitutionName,
                        PaystationId = userPaystation.InstitutionSubLevelId,
                        SubLevelCategory = userPaystation.SubLevelCategory,
                        SubLevelCode = userPaystation.SubLevelCode,
                        SubLevelDesc = userPaystation.SubLevelDesc,
                        SubBudgetClass = blt.SubBudgetClass,
                        JournalTypeCode = "BL",
                        InstitutionAccountId = payerBank.InstitutionAccountId,
                        PayerBankAccount = payerBank.AccountNumber,
                        PayerBankName = payerBank.AccountName,
                        PayerBIC = payerBank.BIC,
                        PayerCashAccount = payerBank.GlAccount.Trim(),
                        PayableGlAccount = crCodes.CrCoa,
                        UnappliedAccount = unappliedAccount.AccountNumber,
                        PayerAccountType = payerBank.AccountType,
                        PVNo = item.PVNo
                    };
                    db.PaymentVouchers.Add(paymentVoucher);
                    db.SaveChanges();

                    VoucherDetail voucherDetail = new VoucherDetail
                    {
                        PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                        JournalTypeCode = "BL",
                        DrGlAccount = blt.GlAccount.Trim(),
                        CrGlAccount = crCodes.CrCoa,
                        CrGlAccountDesc = crCodes.CrCoaDesc,
                        FundingReferenceNo = blt.FundingReferenceNo,
                        OperationalAmount = blt.OperationalAmount,
                        BaseAmount = blt.BaseAmount
                    };

                    db.VoucherDetails.Add(voucherDetail);
                    db.SaveChanges();
                }
                var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", "BL") };
                db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);

            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }

        [HttpPost]
        public ActionResult BackLogReject(int Id)
        {
            try
            {
                BackLogTransactionSummary backLogTransactionSummary =
                    db.BackLogTransactionSummaries.Find(Id);
                if (backLogTransactionSummary == null)
                {
                    return Content("Invalid Backlog Transaction");
                }

                backLogTransactionSummary.OverallStatus = "Rejected";
                db.SaveChanges();

                List<BackLogTransaction> list = db.BackLogTransactions
                    .Where(a => a.BackLogTransactionSummaryId == Id
                    && a.OverallStatus != "Cancelled")
                    .ToList();

                foreach (var item in list)
                {
                    var blt = db.BackLogTransactions.Find(item.BackLogTransactionId);
                    blt.OverallStatus = "Rejected";

                    PaymentVoucher pv = db.PaymentVouchers
                     .Where(a => a.PaymentSummaryNo == item.ChequeNo
                      && a.SourceModule == "BackLog")
                     .FirstOrDefault();
                    VoucherDetail vch = db.VoucherDetails
                        .Where(a => a.PaymentVoucherId == pv.PaymentVoucherId)
                        .FirstOrDefault();

                    db.VoucherDetails
                        .Remove(db.VoucherDetails
                        .Find(vch.VoucherDetailId)
                     );

                    db.PaymentVouchers
                        .Remove(db.PaymentVouchers
                        .Find(pv.PaymentVoucherId)
                        );
                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }
        [HttpPost]
        public ActionResult BackLogCancel(int Id)
        {
            try
            {
                BackLogTransactionSummary backLogTransactionSummary =
                    db.BackLogTransactionSummaries.Find(Id);
                if (backLogTransactionSummary == null)
                {
                    return Content("Invalid Backlog Transaction");
                }

                backLogTransactionSummary.OverallStatus = "Cancelled";
                db.SaveChanges();

                List<BackLogTransaction> list = db.BackLogTransactions
                    .Where(a => a.BackLogTransactionSummaryId == Id
                    && a.OverallStatus != "Cancelled")
                    .ToList();

                foreach (var item in list)
                {
                    var blt = db.BackLogTransactions.Find(item.BackLogTransactionId);
                    blt.OverallStatus = "Cancelled";
                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }
        [HttpPost]
        public ActionResult BackLogCancelItem(int Id)
        {
            try
            {
                BackLogTransaction backLogTransaction =
                    db.BackLogTransactions.Find(Id);
                if (backLogTransaction == null)
                {
                    return Content("Invalid Item");
                }

                backLogTransaction.OverallStatus = "Cancelled";
                PaymentVoucher pv = db.PaymentVouchers
                    .Where(a => a.PaymentSummaryNo == backLogTransaction.ChequeNo
                     && a.SourceModule == "BackLog")
                    .FirstOrDefault();
                if (pv != null)
                {
                    pv.OverallStatus = "BackLog-Cancelled";
                    pv.ApprovedAt = DateTime.Now;
                    pv.ApprovedBy = User.Identity.GetUserName();
                }

                db.SaveChanges();

                BackLogTransactionSummary backLogTransactionSummary =
                    db.BackLogTransactionSummaries
                    .Find(backLogTransaction.BackLogTransactionSummaryId);

                List<BackLogTransaction> list =
                    db.BackLogTransactions
                    .Where(a => a.OverallStatus != "Cancelled"
                     && a.BackLogTransactionSummaryId == backLogTransaction.BackLogTransactionSummaryId)
                    .ToList();

                backLogTransactionSummary.NumTrx = list.Count();
                backLogTransactionSummary.OperationalAmount = list.Sum(a => a.OperationalAmount);

                db.SaveChanges();

            }
            catch (Exception ex)
            {
                return Content(ex.Message.ToString());
            }
            return Content("Success");
        }
        public JsonResult BackLogGet(string status = "Pending")
        {
            List<BackLogTransactionSummary> list = new List<BackLogTransactionSummary>();
            if (status == "Pending")
            {
                list = db.BackLogTransactionSummaries
                .Where(a => a.OverallStatus == "Pending"
                  || a.OverallStatus == "Rejected")
                .OrderByDescending(a => a.BackLogTransactionSummaryId)
                .ToList();
            }
            else
            {
                list = db.BackLogTransactionSummaries
                    .Where(a => a.OverallStatus == "Confirmed")
                      .OrderByDescending(a => a.BackLogTransactionSummaryId)
                     .ToList();
            }

            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult BackLogTransactionGet(int Id)
        {
            var list = db.BackLogTransactions
                .Where(a => a.OverallStatus != "Cancelled"
                && a.BackLogTransactionSummaryId == Id)
                .ToList();
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BackLogTestAPI()
        {
            string path = "F:\\PROGRAMMING\\MY_PROJECTS\\services\\test.xlsx";
            var list = JService.ExcelToJson<BackLogVm>(path);
            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

    }
}