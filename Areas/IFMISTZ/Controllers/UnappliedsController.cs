using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using IFMIS.Services;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Transactions;
using System.Data.SqlClient;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class UnappliedsController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();
        private readonly IFundBalanceServices fundBalanceServices;
        private readonly IServiceManager serviceManager;

        public UnappliedsController()
        {

        }

        public UnappliedsController(
            IFundBalanceServices fundBalanceServices,
            IServiceManager serviceManager
            )
        {
            this.fundBalanceServices = fundBalanceServices;
            this.serviceManager = serviceManager;
        }
        // GET: IFMISTZ/Unapplieds
        public ActionResult UnappliedList()
        {
            //InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            //var unapplied = (from a in db.Unapplieds
            //                 where a.OverallStatus=="Pending"
            //                 group a by new { a.InstitutionCode,a.BenName,a.InstitutionName ,a.BenAcct,a.OverallStatus,a.BankingStatusDesc,a.EndToEndId,a.TrxAmount} into b
            //                 select new UnappliedVM
            //                 {
            //                     //UnappliedId=b.Key.UnappliedId,
            //                     BenName = b.Key.InstitutionCode,
            //                     InstitutionCode=b.Key.InstitutionCode,
            //                     InstitutionName=b.Key.InstitutionName,
            //                     BenAcct=b.Key.BenAcct,
            //                     TrxAmount=b.Sum(c=>c.TrxAmount),
            //                     OverallStatus=b.Key.OverallStatus,
            //                     BankingStatusDesc=b.Key.BankingStatusDesc,
            //                     EndToEndId=b.Key.EndToEndId
            //                 }).ToList();

            //return View(unapplied);

            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            return View(db.Unapplieds.Where(a => (a.OverallStatus == "Edited" || a.OverallStatus == "Rejected") && a.InstitutionCode == userPayStation.InstitutionCode).ToList());

        }

        // GET: IFMISTZ/Unapplieds/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Unapplied unapplied = await db.Unapplieds.FindAsync(id);
            if (unapplied == null)
            {
                return HttpNotFound();
            }
            return View(unapplied);
        }

        // GET: IFMISTZ/Unapplieds/Create
        public ActionResult CreateUnapplied(int? id)
        {
            var createUnappliedVM = new CreateUnappliedVM();
            ViewBag.Banks = db.Banks.ToList();
            return View();
        }



        // POST: IFMISTZ/Unapplieds/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, Authorize(Roles = "Unapplied Confirmation")]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUnapplied(CreateUnappliedVM createUnappliedVM)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
                    var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                    var institutionId = 0;
                    if (userPayStation != null)
                    {
                        institutionId = userPayStation.InstitutionId;
                    }

                    Unapplied unapplied = db.Unapplieds.Find(createUnappliedVM.UnappliedId);
                    unapplied.NewBenName = createUnappliedVM.NewBenName;
                    unapplied.NewBankAccount = createUnappliedVM.NewBankAccount;
                    unapplied.NewBankName = createUnappliedVM.NewBankName;
                    unapplied.NewBIC = createUnappliedVM.NewBenBic;
                    unapplied.OverallStatus = "Edited";

                    await db.SaveChangesAsync();
                    return RedirectToAction("UnappliedList");
                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }


            }

            return View(createUnappliedVM);
        }

        // GET: IFMISTZ/Unapplieds/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Unapplied unapplied = await db.Unapplieds.FindAsync(id);
            if (unapplied == null)
            {
                return HttpNotFound();
            }
            return View(unapplied);
        }

        // POST: IFMISTZ/Unapplieds/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "UnappliedId,VendorCode,BenName,BenAcct,UnappliedAccount,TrxAmount,EndToEndId,BenBic,BankName,PaymentDesc,PaymentDate,BankingStatus,BankingStatusDesc,InstitutionCode,InstitutionName,TransferId,OverallStatus,ConfirmedAt,ConfirmedBy,ExpenditureType,IsOpened,PaymentRef,CashBookStatus,FinancialYear,ClearedBy,ClearedAt,OldBankingStatusDesc,InstitutionId,UnappliedRef,RelatedRef,BankRef,BankRelatedRef,OverallStatusDescription,CreatedAt,UnappliedPayStationId,FundingSourceAcct,UnappliedAt")] Unapplied unapplied)
        {
            if (ModelState.IsValid)
            {
                db.Entry(unapplied).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(unapplied);
        }

        // GET: IFMISTZ/Unapplieds/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Unapplied unapplied = await db.Unapplieds.FindAsync(id);
            if (unapplied == null)
            {
                return HttpNotFound();
            }
            return View(unapplied);
        }

        // POST: IFMISTZ/Unapplieds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Unapplied unapplied = await db.Unapplieds.FindAsync(id);
            db.Unapplieds.Remove(unapplied);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost, Authorize(Roles = "Unapplied Confirmation")]
        public ActionResult ConfirmUnapplied(/*string[] ids*/ int id)
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }
            string response = "";
            ProcessResponse processResponse = new ProcessResponse();
            processResponse.OverallStatus = "Pending";
            processResponse.ReturnId = 0;

            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    Unapplied unapplieds = db.Unapplieds.Find(id);

                    //if (unapplieds == null)
                    //{
                    //    processResponse.OverallStatus = "Error";
                    //    processResponse.OverallStatusDescription = "Unapplied Transaction not found";
                    //    response = "Unapplied Transaction not found";
                    //    return Content(response);
                    //}

                    var unappliedAccount = db.InstitutionAccounts
                    .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                    && a.AccountType.ToUpper() == "UNAPPLIED"
                    && a.IsTSA == false
                    && a.OverallStatus != "Cancelled"
                    ).FirstOrDefault();

                    if (unapplieds.UnappliedAccount == null)
                    {
                        response = "There is no unapplied account for this unapplied, Please consult Administrator!";
                        return Content(response);
                    }

                    CurrencyRateView currencyRateView = db.CurrencyRateViews
                        .Where(a => a.InstitutionId == userPaystation.InstitutionId
                                && (a.SubBudgetClass == "303" || a.SubBudgetClass == "345")
                                ).FirstOrDefault();

                    if (currencyRateView == null)
                    {
                        response = "Currency Rate Setup is Incomplete";
                        return Content(response);
                    }

                    var payerBank = db.InstitutionAccounts
                    .Where(a => a.SubBudgetClass == "303"
                      && a.InstitutionCode == userPaystation.InstitutionCode
                       && a.AccountType.ToUpper() == "UNAPPLIED"
                       && a.AccountNumber == unapplieds.UnappliedAccount
                       && a.UnnappliedAccountNumber != null
                      && a.IsTSA == false
                      && a.OverallStatus != "Cancelled"
                    ).FirstOrDefault();

                    if (payerBank == null)
                    {
                        response = "Institution Bank Account Setup is Incomplete. There is no expenditure account for sub budget class '" + 303 + "'. Please consult Administrator!";
                        return Content(response);
                    }

                    if (unapplieds.NewBIC == null)
                    {
                        response = "Payee BIC is missing";
                        return Content(response);
                    }

                    if (unapplieds.NewBankAccount == null)
                    {
                        response = "Payee Account is null";
                        return Content(response);
                    }

                    if (unapplieds.BulkPaymentStatus == null)
                    {

                        var payee = db.Payees
                            .Where(a => a.PayeeCode == unapplieds.VendorCode
                            && a.OverallStatus == "ACTIVE")
                            .FirstOrDefault();

                        var payeeDetails = db.PayeeDetails
                            .Where(a => a.PayeeId == payee.PayeeId
                            && a.Accountnumber == unapplieds.NewBankAccount
                            && a.IsActive == true)
                            .FirstOrDefault();

                        var payeeType = db.PayeeTypes
                            .Where(a => a.PayeeTypeCode.ToUpper() == payeeDetails.PayeeType.ToUpper()).FirstOrDefault();

                        if (payeeType == null)
                        {
                            processResponse.OverallStatus = "Error";
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + payeeDetails.PayeeType + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        db.Database.CommandTimeout = 1200;
                        var crCodes = db.JournalTypeViews
                            .Where(a => a.CrGfsCode == payeeType.GfsCode
                            && a.SubBudgetClass == "303"
                            && a.InstitutionCode == unapplieds.InstitutionCode).FirstOrDefault();

                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + 303 + "' is missing. Please contact Administrator!";
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + 303 + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        var baseAmount = unapplieds.TrxAmount * currencyRateView.OperationalExchangeRate;

                        var voucher = new PaymentVoucher
                        {

                            PayeeDetailId = payeeDetails.PayeeDetailId,
                            SourceModule = "Unapplied",
                            SourceModuleReferenceNo = unapplieds.EndToEndId,
                            JournalTypeCode = "PV",
                            //Narration = unapplieds.PaymentDesc,
                            //PaymentDesc = unapplieds.BankingStatusDesc,
                            PaymentDesc = "Unapplied Payment",
                            PayeeCode = unapplieds.VendorCode,
                            Payeename = unapplieds.BenName,
                            PayeeBankAccount = unapplieds.NewBankAccount,
                            //PayeeBankAccount = payeeDetails.Accountnumber,
                            PayeeAccountName = unapplieds.NewBenName,
                            //PayeeAccountName = payeeDetails.AccountName,
                            PayeeBankName = unapplieds.NewBankName,
                            PayeeAddress = payee.Address1,
                            //PayeeBIC = unapplieds.NewBIC,
                            PayeeBIC = unapplieds.NewBankAccount is null ? unapplieds.BenBic : unapplieds.NewBIC,
                            PayeeType = payeeDetails.PayeeType,
                            PayerBankAccount = payerBank.AccountNumber,
                            PayerBankName = payerBank.AccountName,
                            PayerBIC = payerBank.BIC,
                            PayerCashAccount = payerBank.GlAccount,
                            PayerAccountType = "Expenditure",
                            OperationalAmount = unapplieds.TrxAmount,
                            BaseAmount = unapplieds.BaseAmount,
                            ExchangeRate = unapplieds.ExchangeRate,
                            ApplyDate = DateTime.Now,
                            SubBudgetClass = "303",
                            PaymentMethod = "EFT",
                            FinancialYear = serviceManager.GetFinancialYear(unapplieds.UnappliedAt),
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now,
                            OverallStatus = "Pending",
                            Book = "Main",
                            InstitutionId = unapplieds.InstitutionId,
                            InstitutionCode = unapplieds.InstitutionCode,
                            InstitutionName = unapplieds.InstitutionName,
                            PaystationId = unapplieds.UnappliedPayStationId,
                            SubLevelCategory = userPaystation.SubLevelCategory,
                            SubLevelCode = userPayStation.SubLevelCode,
                            SubLevelDesc = userPayStation.SubLevelDesc,
                            ReversalFlag = false,
                            GeneralLedgerStatus = "Pending",
                            QueueId = 0,
                            OverallStatusDesc = "Pending",
                            PayableGlAccount = crCodes.CrCoa,
                            UnappliedAccount = unapplieds.UnappliedAccount,
                            InstitutionAccountId = payerBank.InstitutionAccountId,
                            OtherSourceId = id,
                            //Sub TSA
                            SubTsaBankAccount = payerBank.SubTSAAccountNumber,
                            SubTsaCashAccount = payerBank.SubTSAGlAccount,
                            BaseCurrency = "TZS",
                            OperationalCurrency = "TZS"
                        };

                        if (voucher == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Error saving payment voucher";
                            response = "Error saving payment voucher";
                            //return processResponse;
                            return Content(response);
                        }
                        db.PaymentVouchers.Add(voucher);

                        //db.SaveChanges();
                        List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                        List<Unapplied> unappliedDetailList = db.Unapplieds.Where(a => a.UnappliedId == unapplieds.UnappliedId).ToList();


                        foreach (Unapplied unappliedDetail in unappliedDetailList)
                        {
                            VoucherDetail voucherDetail = new VoucherDetail
                            {
                                PaymentVoucherId = voucher.PaymentVoucherId,
                                JournalTypeCode = "PV",
                                DrGlAccount = payerBank.ReceivingGlAccount,
                                DrGlAccountDesc = payerBank.ReceivingGlAccountDesc,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = unappliedDetail.EndToEndId,
                                OperationalAmount = unappliedDetail.TrxAmount,
                                //BaseAmount = unappliedDetail.TrxAmount * currencyRateView.OperationalExchangeRate,
                                BaseAmount = unappliedDetail.BaseAmount
                            };

                            voucherDetailList.Add(voucherDetail);
                        }
                        db.VoucherDetails.AddRange(voucherDetailList);

                        db.SaveChanges();

                        string PVNo = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, "V", voucher.PaymentVoucherId);
                        voucher.PVNo = PVNo;
                        //unapplieds.PaymentVoucherId = voucher.PaymentVoucherId;

                        unapplieds.OverallStatus = "Confirmed";
                        unapplieds.ConfirmedBy = User.Identity.Name;
                        unapplieds.ConfirmedAt = DateTime.Now;
                        ///unapplieds.UnappliedRef = voucher.PVNo;

                        db.SaveChanges();
                        trans.Commit();
                        response = "Success";
                    }
                    else
                    {

                        var payee = db.BulkPayments
                            .Where(a => a.BeneficiaryCode == unapplieds.VendorCode)
                            .FirstOrDefault();

                        var bulkpayee = db.Payees
                           .Where(a => a.PayeeName == "Bulky Payment"
                            && a.OverallStatus == "Cancelled")
                           .FirstOrDefault();

                        var payeeDetails = db.PayeeDetails
                         .Where(a => a.PayeeId == bulkpayee.PayeeId
                         && a.OverallStatus == "Cancelled"
                        ).FirstOrDefault();

                        var payeeType = db.PayeeTypes
                            .Where(a => a.PayeeTypeCode.ToUpper() == "Employee")
                            .FirstOrDefault();

                        if (payeeType == null)
                        {
                            processResponse.OverallStatus = "Error";
                            response = "Vendor setup is incomplete. There is no payee type setup for '" + "Employee" + "'. Please contact Administrator!";
                            return Content(response);
                        }

                        var crCodes = db.JournalTypeViews
                            .Where(a => a.CrGfsCode == payeeType.GfsCode
                            && a.SubBudgetClass == "303"
                            && a.InstitutionCode == unapplieds.InstitutionCode)
                            .FirstOrDefault();

                        if (crCodes == null)
                        {
                            response = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + 303 + "' is missing. Please contact Administrator!";
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + 303 + "' is missing. Please contact Administrator!";
                            return Content(response);
                        }

                        //var baseAmount = unapplieds.TrxAmount * currencyRateView.OperationalExchangeRate;
                        var voucher = new PaymentVoucher
                        {
                            PayeeDetailId = payeeDetails.PayeeDetailId,
                            SourceModule = "Unapplied",
                            SourceModuleReferenceNo = unapplieds.EndToEndId,
                            JournalTypeCode = "PV",
                            Narration = unapplieds.PaymentDesc,
                            PaymentDesc = "Unapplied Payment",
                            PayeeCode = unapplieds.VendorCode,
                            Payeename = unapplieds.BenName,
                            PayeeBankAccount = unapplieds.NewBankAccount,
                            PayeeAccountName = unapplieds.NewBenName,
                            PayeeBankName = unapplieds.NewBankName,
                            PayeeAddress = "Addresss",
                            //PayeeBIC = unapplieds.NewBIC,
                            PayeeBIC = unapplieds.NewBankAccount is null ? unapplieds.BenBic : unapplieds.NewBIC,
                            PayeeType = "Employee",
                            PayerBankAccount = payerBank.AccountNumber,
                            PayerBankName = payerBank.AccountName,
                            PayerBIC = payerBank.BIC,
                            PayerCashAccount = payerBank.GlAccount,
                            PayerAccountType = "Expenditure",
                            OperationalAmount = unapplieds.TrxAmount,
                            //BaseAmount = baseAmount,
                            //ExchangeRate = currencyRateView.OperationalExchangeRate,
                            BaseAmount = unapplieds.BaseAmount,
                            ExchangeRate = unapplieds.ExchangeRate,
                            ApplyDate = DateTime.Now,
                            SubBudgetClass = "303",
                            PaymentMethod = "EFT",
                            FinancialYear = serviceManager.GetFinancialYear(unapplieds.UnappliedAt),
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now,
                            OverallStatus = "Pending",
                            Book = "Main",
                            InstitutionId = unapplieds.InstitutionId,
                            InstitutionCode = unapplieds.InstitutionCode,
                            InstitutionName = unapplieds.InstitutionName,
                            PaystationId = unapplieds.UnappliedPayStationId,
                            SubLevelCategory = userPaystation.SubLevelCategory,
                            SubLevelCode = userPayStation.SubLevelCode,
                            SubLevelDesc = userPayStation.SubLevelDesc,
                            ReversalFlag = false,
                            GeneralLedgerStatus = "Pending",
                            QueueId = 0,
                            OverallStatusDesc = "Pending",
                            PayableGlAccount = crCodes.CrCoa,
                            UnappliedAccount = unapplieds.UnappliedAccount,
                            InstitutionAccountId = payerBank.InstitutionAccountId,
                            OtherSourceId = id,
                            //Sub TSA
                            SubTsaBankAccount = payerBank.SubTSAAccountNumber,
                            SubTsaCashAccount = payerBank.SubTSAGlAccount,
                            BaseCurrency = "TZS",
                            OperationalCurrency = "TZS"
                        };

                        if (voucher == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Error saving payment voucher";
                            response = "Error saving payment voucher";
                            return Content(response);
                        }
                        db.PaymentVouchers.Add(voucher);

                        List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();
                        List<Unapplied> unappliedDetailList = db.Unapplieds.Where(a => a.UnappliedId == unapplieds.UnappliedId).ToList();
                        foreach (Unapplied unappliedDetail in unappliedDetailList)
                        {
                            VoucherDetail voucherDetail = new VoucherDetail
                            {
                                PaymentVoucherId = voucher.PaymentVoucherId,
                                JournalTypeCode = "PV",
                                DrGlAccount = payerBank.ReceivingGlAccount,
                                DrGlAccountDesc = payerBank.ReceivingGlAccountDesc,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = unappliedDetail.EndToEndId,
                                OperationalAmount = unappliedDetail.TrxAmount,
                                //BaseAmount = unappliedDetail.TrxAmount * currencyRateView.OperationalExchangeRate,
                                BaseAmount = unappliedDetail.BaseAmount,

                            };

                            voucherDetailList.Add(voucherDetail);
                        }
                        db.VoucherDetails.AddRange(voucherDetailList);

                        db.SaveChanges();

                        string PVNo = serviceManager.GetLegalNumber(userPaystation.InstitutionCode, "V", voucher.PaymentVoucherId);
                        voucher.PVNo = PVNo;
                        unapplieds.OverallStatus = "Confirmed";
                        unapplieds.ConfirmedBy = User.Identity.Name;
                        unapplieds.ConfirmedAt = DateTime.Now;

                        db.SaveChanges();
                        trans.Commit();
                        response = "Success";

                        ///
                    }
                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    response = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UnappliedReceiptDetails(string receiptRef)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            ViewBag.Unapplied = db.Unapplieds.Where(a => a.UnappliedRef == receiptRef).FirstOrDefault();
            var details = db.Unapplieds.Where(a => a.UnappliedRef == receiptRef && a.InstitutionCode == userPaystation.InstitutionCode).ToList();

            return View(details);
        }
     
        public ActionResult UnappliedTrackerList()
        {
            return View();
        }


        public JsonResult GetUnapplied(SearchImprestVM unappliedTracker)
        {

            string search = unappliedTracker.Keywords == null ? "0" : unappliedTracker.Keywords;
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            List<Unapplied> imprestList = new List<Unapplied>();
            if (unappliedTracker.OverallStatus == "All")
            {
                if (unappliedTracker.start_date.Year == 0001
                    || unappliedTracker.end_date.Year == 0001)
                {
                    imprestList = db.Unapplieds.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && a.OverallStatus == unappliedTracker.OverallStatus
                ).Where(b => b.BenName.Contains(search)
                || b.BenAcct.Contains(search)
                || b.VendorCode.Contains(search)
                || b.BenName.Contains(unappliedTracker.Keywords)
                )
                .OrderByDescending(a => a.UnappliedId)
                .ToList();
                }

                else
                {
                    imprestList = db.Unapplieds.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                   && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(unappliedTracker.start_date)
                                              && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(unappliedTracker.end_date))
                                   && a.OverallStatus == unappliedTracker.OverallStatus
                                   ).Where(b => b.BenName.Contains(search)
                                   || b.BenAcct.Contains(search)
                                   || b.VendorCode.Contains(search)
                                   || b.BenName.Contains(unappliedTracker.Keywords)
                                   )
                                   .OrderByDescending(a => a.UnappliedId)
                                   .ToList();
                }

            }
            else
            {

                if (unappliedTracker.start_date.Year == 0001
                           || unappliedTracker.end_date.Year == 0001)
                {
                    imprestList = db.Unapplieds.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                && a.OverallStatus == unappliedTracker.OverallStatus
                ).Where(b => b.BenName.Contains(search)
                || b.BenAcct.Contains(search)
                || b.VendorCode.Contains(search)
                || b.BenName.Contains(unappliedTracker.Keywords)
                )
                .OrderByDescending(a => a.UnappliedId)
                .ToList();
                }
                else
                {

                    imprestList = db.Unapplieds.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                                  && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(unappliedTracker.start_date)
                                                             && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(unappliedTracker.end_date))
                                                  && a.OverallStatus == unappliedTracker.OverallStatus
                                                  ).Where(b => b.BenName.Contains(search)
                                                  || b.BenAcct.Contains(search)
                                                  || b.VendorCode.Contains(search)
                                                  || b.BenName.Contains(unappliedTracker.Keywords)
                                                  )
                                                  .OrderByDescending(a => a.UnappliedId)
                                                  .ToList();
                }
            }
            return Json(new { data = imprestList }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UnappliedTracker()
        {
            var statue = serviceManager.GetStatue();
            var statueSelectList = serviceManager.GetSelectListItems(statue);
            var tracker = new TrackerVM
            {
                OverallStatue = new SelectList(statueSelectList, "Value", "Text")
            };

            return View(tracker);
        }

        public ActionResult GetUnappliedInformation(TrackerVM trackerVM)
        {
            IQueryable<Unapplied> unapplied;

            //var institutionId = 0;
            string institutionCode = "";
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            if (userPayStation != null)
            {
                institutionCode = userPayStation.InstitutionCode;
            }

            unapplied = db.Unapplieds.Where(a => a.InstitutionCode == institutionCode);

            if (trackerVM.SearchKeyword != null)
            {
                unapplied = unapplied.Where(a => a.UnappliedRef.Contains(trackerVM.SearchKeyword) || a.BenName.Contains(trackerVM.SearchKeyword));
            }

            switch (trackerVM.OverallStatus)
            {
                case "Pending":
                    unapplied = unapplied.Where(a => a.OverallStatus == "Pending" && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "Confirmed":
                    unapplied = unapplied.Where(a => a.OverallStatus == "Confirmed" && (DbFunctions.TruncateTime(a.ConfirmedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.ConfirmedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "Approved":
                    unapplied = unapplied.Where(a => a.OverallStatus == "Approved" && (DbFunctions.TruncateTime(a.ApprovedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.ApprovedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "All":
                    unapplied = unapplied.Where(a => DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate));
                    break;
                default:
                    break;
            }

            return PartialView("_Unapplieds", unapplied);
        }

        [HttpPost, Authorize(Roles = "Unapplied Confirmation")]
        public ActionResult ClearUnapplied(string[] ids)
        {

            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }
            string response = "";

            using (var trans = db.Database.BeginTransaction())
            {
                try
                {

                    var unapplieds = (from p in db.Unapplieds
                                      where ids.Contains(p.UnappliedId.ToString())
                                      where p.InstitutionCode == userPaystation.InstitutionCode
                                      select p).ToList();

                    foreach (var item in unapplieds)
                    {
                        item.OverallStatus = "Cleared";
                        item.ClearedBy = User.Identity.Name;
                        item.ClearedAt = DateTime.Now;
                        //item.UnappliedRef = receipt.ReferenceNo;
                    }

                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";
                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    response = "DbException";
                    trans.Rollback();
                }
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PayeeDetails(int payeeDetailId)
        {
            var details = db.PayeeDetails.Where(a => a.PayeeDetailId == payeeDetailId).ToList();
            return View(details);
        }

        public ActionResult Payee(string payeecode)
        {
            //Unapplied unapplieds = db.Unapplieds.Find(id);
            var payee = db.Payees.Where(a => a.PayeeCode == payeecode).ToList();
            //var payee = db.Payees.Where(a => a.PayeeCode == unapplieds.VendorCode).FirstOrDefault();
            return View(payee);
        }

        //public JsonResult GetPendingUnapplieds(string term)
        //{


        //    var userPayStation = ServiceManager.GetDefaultUserPayStation(db, User.Identity.GetUserId());
        //    List<Select2DTOString> refs = new List<Select2DTOString>();
        //    if (userPayStation != null)
        //    {
        //        var institutionAccounts = db.InstitutionAccounts.Where(a => a.InstitutionId == userPayStation.InstitutionId).Select(a => a.AccountNumber).ToArray();

        //        var models = db.Unapplieds.Where(m =>m.VendorCode.ToString().Contains(term) || m.BenName.ToString().Contains(term) || m.EndToEndId.ToString().Contains(term)).Distinct().ToList();

        //        foreach (var item in models)
        //        {
        //            refs.Add(new Select2DTOString(item.VendorCode, item.VendorCode));
        //        }
        //    }

        //    return Json(new { refs }, JsonRequestBehavior.AllowGet);
        //}

        //public JsonResult GetPendingUnapplied(string id)
        //{
        //    var refNo = (from u in db.DummySalaries
        //                 where u.DocumentNo == id
        //                 select new
        //                 {
        //                     id = u.DocumentNo,
        //                     text = u.DocumentNo
        //                 }).FirstOrDefault();

        //    return Json(new
        //    {
        //        id = refNo.id,
        //        text = refNo.text
        //    }, JsonRequestBehavior.AllowGet);
        //}

        public JsonResult SearchUnapplied(string term)
        {
            var userPayStation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());

            List<Select2DTO> unappliedList = new List<Select2DTO>();

            try
            {

                var unapplieds = (from a in db.Unapplieds.Where(a => (a.VendorCode.Contains(term) || a.BenName.Contains(term) || a.EndToEndId.Contains(term))
                                  && a.InstitutionId == userPayStation.InstitutionId
                                  && (a.OverallStatus == "Approved" || a.OverallStatus == "Cancelled"))
                                  select new
                                  {
                                      UnappliedId = a.UnappliedId,
                                      VendorCode = a.VendorCode,
                                      VendorName = a.VendorCode + " - " + a.BenName + " - " + a.EndToEndId
                                  }).ToList();

                foreach (var item in unapplieds)
                {
                    unappliedList.Add(new Select2DTO((int)item.UnappliedId, item.VendorName));
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return Json(new { unappliedList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnappliedDetails(int? id)
        {
            string response = "";
            var unapplied = db.Unapplieds.Find(id);
            var payee = db.Payees.Where(a => a.PayeeCode == unapplied.VendorCode && a.OverallStatus == "ACTIVE").FirstOrDefault();
            //if (payee == null)
            //{
            //    response = "Error: This Payee Is not ACTIVE!";
            //    return Json(new
            //    {
            //        response
            //    }, JsonRequestBehavior.AllowGet);
            //}
            var payeeDetails = db.PayeeDetails.Where(a => a.PayeeId == payee.PayeeId && a.IsActive == true).ToList();

            var accounts = (from u in payeeDetails
                            select new
                            {
                                u.Accountnumber,
                                AccountnumberAccountName = u.Accountnumber + " " + u.AccountName
                            }).ToList();

            //var newBenName = unapplied.BenName;
            //var newBankName = unapplied.BankName;
            //var newBenBic = unapplied.BenBic;

            //if (payeeDetails.Count == 1)
            //{
            //    newBenName = payeeDetails[0].AccountName;
            //    newBankName = payeeDetails[0].BankName;
            //    newBenBic = payeeDetails[0].BIC;
            //}

            if (unapplied != null)
            {
                return Json(new
                {
                    unapplied.BenName,
                    //NewBenName = newBenName,
                    unapplied.VendorCode,
                    unapplied.EndToEndId,
                    BankAccount = unapplied.BenAcct,
                    unapplied.BankName,
                    //NewBankName = newBankName,
                    unapplied.BenBic,
                    //NewBenBic = newBenBic,
                    unapplied.TrxAmount,
                    unapplied.UnappliedAccount,
                    unapplied.PaymentDesc,
                    unapplied.BankingStatusDesc,
                    Accounts = accounts,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {

            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAccountDetails(string accountNumber)
        {
            var payeeDetail = db.PayeeDetails
                .Where(a => a.Accountnumber == accountNumber
                && a.IsActive == true)
                .FirstOrDefault();

            if (payeeDetail != null)
            {
                return Json(new
                {
                    NewBenName = payeeDetail.AccountName,
                    NewBankName = payeeDetail.BankName,
                    NewBenBic = payeeDetail.BIC,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {

            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost, Authorize(Roles = "Unapplied Confirmation")]
        public ActionResult CancelUnapplied(int? id)
        {
            string response = "Success";
            try
            {
                var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
                Unapplied unapplied = db.Unapplieds.Find(id);
                if (unapplied != null)
                {

                    unapplied.OverallStatus = "Cancelled";
                    unapplied.CancelledAt = DateTime.Now;
                    unapplied.CancelledBy = User.Identity.Name;
                    db.SaveChanges();
                }
                else
                {
                    response = "Payee Not Found!";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            return Content(response);
        }

        [HttpGet]
        public ActionResult WithdrawUnapplied(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Unapplied unapplied = db.Unapplieds.Find(id);

            if (unapplied == null)
            {
                return HttpNotFound();
            }

            var UnappliedWithdrawVM = new UnappliedWithdrawVM
            {
                UnappliedId = unapplied.UnappliedId,
                Remarks = unapplied.Remarks,
            };
            return PartialView("_WithdrawRemarks", UnappliedWithdrawVM);

        }

        [HttpPost]
        public ActionResult WithdrawUnapplied(UnappliedWithdrawVM unappliedWithdrawVM)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }
            string response = "Success";
            try
            {
                Unapplied unapplied = db.Unapplieds.Find(unappliedWithdrawVM.UnappliedId);
                var FinancialYear = ServiceManager.GetFinancialYear(db, (DateTime)unapplied.UnappliedAt).ToString();
                var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "303" && a.InstitutionCode == unapplied.InstitutionCode).FirstOrDefault();
                //var cashAccountFrom = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.GlAccount).FirstOrDefault();
                //var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
                if (unapplied.NewBankAccount == null)
                {
                    response = "Payee Account is null";
                    return Content(response);
                }

                if (unapplied.NewBIC == null)
                {
                    response = "Payee BIC is missing";
                    return Content(response);
                }

                //var parameter = new SqlParameter[] {
                //new SqlParameter("@InstitutionCode", unapplied.InstitutionCode),
                //new SqlParameter("@InstitutionAccount", unapplied.UnappliedAccount),
                //new SqlParameter("@SubBudgetClass", "303"),
                //new SqlParameter("@FinancialYear", FinancialYear),};

                //var cashbookBalance = db.Database.SqlQuery<GeneralLedgerListVM>("GetCashAccountBalanceP @InstitutionCode, @InstitutionAccount, @SubBudgetClass , @FinancialYear", parameter).FirstOrDefault();
                //if (unapplied.TrxAmount > cashbookBalance.OperationalAmount)
                //{
                //    response = "Sorry,Transfer exceed Cashbook Balance!";
                //    return Content(response);
                //}

                if (unapplied != null)
                {
                    unapplied.WithdrawnBy = User.Identity.Name;
                    unapplied.WithdrawnAt = DateTime.Now;
                    unapplied.OverallStatus = "Withdrawn";
                    unapplied.Remarks = unappliedWithdrawVM.Remarks;

                    var transfer = new FundTransferSummary
                    {
                        TransferType = "Within",
                        TransferCategory = "Internal",
                        FundingRefNo = unapplied.EndToEndId,
                        InstitutionCodeFrom = unapplied.InstitutionCode,
                        InstitutionCodeTo = unapplied.InstitutionCode,
                        InstitutionNameFrom = unapplied.InstitutionName,
                        InstitutionNameTo = unapplied.InstitutionName,
                        InstitutionIdFrom = unapplied.InstitutionId,
                        InstitutionIdTo = unapplied.InstitutionId,
                        SubBudgetClassFrom = "303",
                        SubBudgetClassTo = "301",
                        BankAccountFrom = unapplied.UnappliedAccount,
                        BankAccountTo = unapplied.UnappliedAccount,
                        CashAccountFrom = institutionAccount.GlAccount,
                        CashAccountTo = institutionAccount.GlAccount,
                        TotalBaseAmount = unapplied.TrxAmount,
                        TransferDescription = "Unapplied Withdraw,End to End Id" + "-" + " " + unapplied.EndToEndId,
                        SourceModule = "Unapplied",
                        JournalTypeCode = "FTI",
                        OverallStatus = "Pending",
                        ApprovalStatus = "Pending",
                        FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                        CreatedAt = DateTime.Now,
                        CreatedBy = User.Identity.Name

                    };
                    db.SaveChanges();
                }
                else
                {
                    response = "unapplie Not Found!";
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = ex.Message.ToString();
            }

            return Content(response);
        }
        [HttpGet]
        public ActionResult UnappliedWithdrawalList()
        {
            var userPayStation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            var withdrawList = db.Unapplieds.Where(a => (a.OverallStatus == "Withdrawn" || a.OverallStatus == "Withdraw Rejected") && a.InstitutionCode == userPayStation.InstitutionCode).ToList();
            return View(withdrawList);
        }
        [HttpPost]
        public ActionResult ApproveWithdrawal(int? id)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var transferType = "";
            var transferCategory = "";
            var journalTypeCode = "";

            Unapplied unapplied = db.Unapplieds.Find(id);
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }

            var institutionDetails = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode
                                            && a.OverallStatus == "Active").FirstOrDefault();

            string response = "Success";
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {

                    var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "303" && a.InstitutionCode == unapplied.InstitutionCode).FirstOrDefault();
                    var cashAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.GlAccount).FirstOrDefault();
                    var bankAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.AccountNumber).FirstOrDefault();
                    //var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
                    CurrencyRateView currencyRateView = db.CurrencyRateViews.Where(a => a.SubBudgetClass == "303"
                                   && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();

                    if (currencyRateView == null)
                    {
                        response = "Currency Rate Setup is Incomplete";
                        return Content(response);
                    }
                    if (unapplied.NewBankAccount == null)
                    {
                        response = "Payee Account is missing";
                        return Content(response);
                    }

                    if (unapplied.NewBIC == null)
                    {
                        response = "Payee BIC is missing";
                        return Content(response);
                    }
                    if (unapplied != null)
                    {
                        unapplied.WithdrawnBy = User.Identity.Name;
                        unapplied.WithdrawnAt = DateTime.Now;
                        unapplied.OverallStatus = "Withdrawal Approved";
                        if (unapplied.UnappliedAccount == bankAccountTo)
                        {
                            transferType = "Within";
                            transferCategory = "Internal";
                            journalTypeCode = "FTI";
                        }
                        else
                        {
                            transferType = "Between";
                            transferCategory = "Bank";
                            journalTypeCode = "FTF";
                        }

                        var referenceNo = db.FundTransferSummaries.Where(a => a.TransferRefNum == unapplied.EndToEndId && a.OverallStatus == "Cancelled").FirstOrDefault();

                        var bic1 = db.Accounts.Where(a => a.AccountNo == bankAccountTo).FirstOrDefault();
                        var bictTo = db.Banks.Find(bic1.BankId);
                        var bic2 = db.Accounts.Where(a => a.AccountNo == unapplied.UnappliedAccount).FirstOrDefault();
                        var bicFrom = db.Banks.Find(bic2.BankId);

                        var transferSummary = new FundTransferSummary
                        {
                            TransferType = transferType,
                            TransferCategory = transferCategory,
                            TransferDescription = "Unapplied Withdrawal",
                            FundingRefNo = unapplied.EndToEndId,
                            TransferRefNum = unapplied.EndToEndId,
                            InstitutionCodeFrom = unapplied.InstitutionCode,
                            InstitutionCodeTo = unapplied.InstitutionCode,
                            InstitutionNameFrom = unapplied.InstitutionName,
                            InstitutionNameTo = unapplied.InstitutionName,
                            InstitutionIdFrom = unapplied.InstitutionId,
                            InstitutionIdTo = unapplied.InstitutionId,
                            SubBudgetClassFrom = "303",
                            SubBudgetClassTo = "301",
                            BankAccountFrom = unapplied.UnappliedAccount,
                            BankAccountTo = bankAccountTo,
                            CashAccountFrom = institutionAccount.GlAccount,
                            CashAccountTo = cashAccountTo,
                            TotalBaseAmount = currencyRateView.OperationalExchangeRate * (unapplied.TrxAmount),
                            TotalOperationalAmount = unapplied.TrxAmount,
                            SourceModule = "Unapplied",
                            JournalTypeCode = journalTypeCode,
                            OverallStatus = "Pending",
                            ApprovalStatus = "Pending",
                            JournalTypeId = 21,
                            OperationalCurrency = currencyRateView.OperationalCurrencyCode,
                            OperationalExchangeRate = currencyRateView.OperationalExchangeRate,
                            FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            RegionCode = "TZDO",
                            BankAccountBicFrom = bictTo.BIC,
                            BankAccountBicTo = bicFrom.BIC

                        };
                        db.FundTransferSummaries.Add(transferSummary);
                        db.SaveChanges();

                        int fundTransferSummaryId = transferSummary.FundTransferSummaryId;
                        List<FundTransferDetail> fundTransferDetailList = new List<FundTransferDetail>();

                        List<Unapplied> unappliedDetailList = db.Unapplieds.Where(a => a.UnappliedId == unapplied.UnappliedId).ToList();

                        var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();

                        foreach (Unapplied unappliedDetail in unappliedDetailList)
                        {
                            COA coa = db.COAs.Where(a => a.GlAccount == institutionAccount.ReceivingGlAccount.Replace("-", "|") && a.Status == "ACTIVE").FirstOrDefault();
                            FundTransferDetail transferDetail = new FundTransferDetail
                            {
                                FundTransferSummaryId = fundTransferSummaryId,
                                DrGlAccount = institutionAccount.ReceivingGlAccount.Replace("-", "|"), //from
                                CrGlAccount = receivingAccount.Replace("-", "|"),//To
                                BaseAmount = currencyRateView.OperationalExchangeRate * unappliedDetail.TrxAmount,
                                OperationalAmount = unappliedDetail.TrxAmount,
                                FundingReferenceNo = unappliedDetail.EndToEndId,
                                GfsCode = coa.GfsCode,
                                VoteDesc = coa.VoteDesc,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TrDesc = coa.TrDesc,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutput,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                InstitutionLevel = coa.InstitutionLevel,
                                Level1Code = coa.Level1Code,
                                Level1Desc = coa.Level1Desc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                PayeeCode = coa.SubVote,
                                PayeeName = coa.SubVoteDesc,
                                TR = coa.TR,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                Facility = coa.Facility,
                                FundingSourceDesc = coa.FundingSourceDesc,
                                TransactionDesc = transferSummary.TransferDescription,
                                SourceModule = "Unapplied",
                                SourceModuleRefNo = transferSummary.TransferRefNum,
                                FacilityDesc = coa.FacilityDesc
                            };

                            fundTransferDetailList.Add(transferDetail);
                        }
                        db.FundTransferDetails.AddRange(fundTransferDetailList);
                        db.SaveChanges();

                        var transactionLogs = new List<TransactionLogVM>();
                        var transactionLogs2 = new List<TransactionLogVM>();
                        foreach (var item in fundTransferDetailList)
                        {
                            var coa1 = db.COAs.Where(a => a.GlAccount == item.DrGlAccount && a.InstitutionCode == userPaystation.InstitutionCode && a.Status == "ACTIVE").FirstOrDefault();

                            var transactionLog = new TransactionLogVM
                            {
                                SourceModuleId = unapplied.UnappliedId,
                                LegalNumber = transferSummary.TransferRefNum,
                                FundingRerenceNo = unapplied.EndToEndId,
                                InstitutionCode = unapplied.InstitutionCode,
                                InstitutionName = unapplied.InstitutionName,
                                JournalTypeCode = "FTI",
                                GlAccount = item.DrGlAccount,
                                GlAccountDesc = "NA",
                                GfsCode = coa1.GfsCode, //Coa
                                GfsCodeCategory = coa1.GfsCodeCategory, //Coa
                                TransactionCategory = "Expenditure",
                                VoteDesc = coa1.VoteDesc, //Coa
                                GeographicalLocationDesc = coa1.GeographicalLocationDesc, //Coa
                                TrDesc = coa1.TrDesc, //Coa
                                SubBudgetClass = coa1.SubBudgetClass,
                                SubBudgetClassDesc = coa1.subBudgetClassDesc,
                                ProjectDesc = coa1.ProjectDesc, //Coa
                                ServiceOutputDesc = coa1.ServiceOutputDesc,//Coa
                                ActivityDesc = coa1.ActivityDesc, //Coa
                                FundTypeDesc = coa1.FundTypeDesc, //Coa
                                CofogDesc = coa1.CofogDesc, //From Coa
                                SubLevelCode = coa1.SubVote, //Coa
                                FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                                OperationalAmount = unapplied.TrxAmount,
                                BaseAmount = unapplied.TrxAmount * currencyRateView.OperationalExchangeRate,
                                Currency = "TZS",
                                CreatedAt = DateTime.Now,
                                CreatedBy = User.Identity.Name,
                                ApplyDate = unapplied.UnappliedAt,
                                PayeeCode = unapplied.VendorCode, //From my form
                                PayeeName = unapplied.BenName, //From my form
                                TransactionDesc = unapplied.BankingStatusDesc,
                                SourceModule = "Unapplied",
                                SourceModuleRefNo = unapplied.EndToEndId,
                                // OverallStatus = unapplied.OverallStatus,
                                OverallStatus = "Approved",
                                OverallStatusDesc = unapplied.OverallStatus,
                                FundingSourceDesc = "NA",
                                InstitutionLevel = institutionDetails.InstitutionLevel,
                                Level1Code = institutionDetails.Level1Code,
                                Level1Desc = institutionDetails.Level1Desc,
                                TR = coa1.TR,
                                CostCentre = coa1.CostCentre,
                                CostCentreDesc = coa1.CostCentreDesc,
                                SubVote = coa1.SubVote,
                                SubVoteDesc = coa1.SubVoteDesc,
                                Facility = coa1.Facility,
                                FacilityDesc = coa1.Facility,
                            };

                            transactionLogs.Add(transactionLog);

                            var coa2 = db.COAs.Where(a => a.GlAccount == item.CrGlAccount && a.InstitutionCode == userPaystation.InstitutionCode && a.Status == "ACTIVE").FirstOrDefault();

                            var transactionLog2 = new TransactionLogVM
                            {
                                SourceModuleId = unapplied.UnappliedId,
                                LegalNumber = transferSummary.TransferRefNum,
                                FundingRerenceNo = transferSummary.TransferRefNum,
                                InstitutionCode = unapplied.InstitutionCode,
                                InstitutionName = unapplied.InstitutionName,
                                JournalTypeCode = "FTI",
                                GlAccount = item.CrGlAccount,
                                GlAccountDesc = coa2.GlAccountDesc,
                                GfsCode = coa2.GfsCode, //Coa
                                GfsCodeCategory = coa2.GfsCodeCategory, //Coa
                                TransactionCategory = "Revenue",
                                VoteDesc = coa2.VoteDesc, //Coa
                                GeographicalLocationDesc = coa2.GeographicalLocationDesc, //Coa
                                TrDesc = coa2.TrDesc, //Coa
                                SubBudgetClass = coa2.SubBudgetClass,
                                SubBudgetClassDesc = coa2.subBudgetClassDesc,
                                ProjectDesc = coa2.ProjectDesc, //Coa
                                ServiceOutputDesc = coa2.ServiceOutputDesc,//Coa
                                ActivityDesc = coa2.ActivityDesc, //Coa
                                FundTypeDesc = coa1.FundTypeDesc, //Coa
                                CofogDesc = coa2.CofogDesc, //From Coa
                                SubLevelCode = coa2.Level1Code, //Coa
                                FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                                OperationalAmount = unapplied.TrxAmount,
                                BaseAmount = unapplied.TrxAmount * currencyRateView.OperationalExchangeRate,
                                Currency = "TZS",
                                CreatedAt = DateTime.Now,
                                CreatedBy = User.Identity.Name,
                                ApplyDate = unapplied.UnappliedAt,
                                PayeeCode = unapplied.VendorCode, //From my form
                                PayeeName = unapplied.BenName, //From my form
                                TransactionDesc = unapplied.BankingStatusDesc,
                                SourceModule = "Unapplied",
                                SourceModuleRefNo = unapplied.EndToEndId,
                                //OverallStatus = unapplied.OverallStatus,
                                OverallStatus = "Approved",
                                OverallStatusDesc = unapplied.OverallStatus,
                                FundingSourceDesc = "NA",
                                InstitutionLevel = institutionDetails.InstitutionLevel,
                                Level1Code = institutionDetails.Level1Code,
                                Level1Desc = institutionDetails.Level1Desc,
                                TR = coa2.TR,
                                CostCentre = coa2.CostCentre,
                                CostCentreDesc = coa2.CostCentreDesc,
                                SubVote = coa1.SubVote,
                                SubVoteDesc = coa2.SubVoteDesc,
                                Facility = coa2.Facility,
                                FacilityDesc = coa2.Facility,
                            };

                            transactionLogs2.Add(transactionLog2);
                        }
                        response = fundBalanceServices.PostTransaction(transactionLogs);
                        response = fundBalanceServices.PostTransaction(transactionLogs2);
                        if (response == "Success")
                        {
                            //trans.Commit();
                            scope.Complete();
                        }
                        else
                        {
                            //trans.Rollback();
                        }
                    }
                }

                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    response = ex.Message.ToString();
                }
            }

            return Content(response);
        }
        //[HttpPost]
        //public ActionResult ApproveWithdrawalMinistries(int? id)
        //{
        //    InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
        //    var financialYear = ServiceManager.GetFinancialYear(db, DateTime.Now);
        //    var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

        //    var transferType = "";
        //    var transferCategory = "";
        //    var journalTypeCode = "";

        //    Unapplied unapplied = db.Unapplieds.Find(id);
        //    var institutionId = 0;
        //    if (userPayStation != null)
        //    {
        //        institutionId = userPayStation.InstitutionId;
        //    }
        //    string response = "Success";
        //    try
        //    {

        //        var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "303" && a.InstitutionCode == unapplied.InstitutionCode).FirstOrDefault();
        //        var cashAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.GlAccount).FirstOrDefault();
        //        var bankAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.AccountNumber).FirstOrDefault();
        //        //var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
        //        CurrencyRateView currencyRateView = db.CurrencyRateViews.Where(a => a.SubBudgetClass == "303"
        //                       && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();

        //        if (currencyRateView == null)
        //        {
        //            response = "Currency Rate Setup is Incomplete";
        //            return Content(response);
        //        }

        //        if (unapplied.NewBankAccount == null)
        //        {
        //            response = "Payee Account is missing";
        //            return Content(response);
        //        }

        //        if (unapplied.NewBIC == null)
        //        {
        //            response = "Payee BIC is missing";
        //            return Content(response);
        //        }

        //        if (unapplied != null)
        //        {
        //            unapplied.WithdrawnBy = User.Identity.Name;
        //            unapplied.WithdrawnAt = DateTime.Now;
        //            unapplied.OverallStatus = "Withdrawal Approved";
        //            if (unapplied.UnappliedAccount == bankAccountTo)
        //            {
        //                transferType = "Within";
        //                transferCategory = "Internal";
        //                journalTypeCode = "FTI";
        //            }
        //            else
        //            {
        //                transferType = "Between";
        //                transferCategory = "Bank";
        //                journalTypeCode = "FTF";
        //            }

        //            var transferSummary = new FundTransferSummary
        //            {

        //                TransferType = transferType,
        //                TransferCategory = transferCategory,
        //                TransferDescription = "Unapplied Withdraw",
        //                FundingRefNo = unapplied.EndToEndId,
        //                TransferRefNum = unapplied.EndToEndId,
        //                InstitutionCodeFrom = unapplied.InstitutionCode,
        //                InstitutionCodeTo = unapplied.InstitutionCode,
        //                InstitutionNameFrom = unapplied.InstitutionName,
        //                InstitutionNameTo = unapplied.InstitutionName,
        //                InstitutionIdFrom = unapplied.InstitutionId,
        //                InstitutionIdTo = unapplied.InstitutionId,
        //                SubBudgetClassFrom = "303",
        //                SubBudgetClassTo = "101",
        //                BankAccountFrom = unapplied.UnappliedAccount,
        //                BankAccountTo = "9921180001",
        //                CashAccountFrom = institutionAccount.GlAccount,
        //                CashAccountTo = cashAccountTo,
        //                TotalBaseAmount = currencyRateView.OperationalExchangeRate * (unapplied.TrxAmount),
        //                TotalOperationalAmount = unapplied.TrxAmount,
        //                SourceModule = "Unapplied",
        //                JournalTypeCode = journalTypeCode,
        //                OverallStatus = "Pending",
        //                ApprovalStatus = "Pending",
        //                JournalTypeId = 21,
        //                OperationalCurrency = currencyRateView.OperationalCurrencyCode,
        //                OperationalExchangeRate = currencyRateView.OperationalExchangeRate,
        //                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
        //                CreatedAt = DateTime.Now,
        //                CreatedBy = User.Identity.Name
        //            };
        //            db.FundTransferSummaries.Add(transferSummary);
        //            db.SaveChanges();

        //            int fundTransferSummaryId = transferSummary.FundTransferSummaryId;
        //            List<FundTransferDetail> fundTransferDetailList = new List<FundTransferDetail>();

        //            List<Unapplied> unappliedDetailList = db.Unapplieds.Where(a => a.UnappliedId == unapplied.UnappliedId).ToList();

        //            var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
        //            foreach (Unapplied unappliedDetail in unappliedDetailList)
        //            {
        //                FundTransferDetail transferDetail = new FundTransferDetail
        //                {
        //                    FundTransferSummaryId = fundTransferSummaryId,
        //                    DrGlAccount = institutionAccount.ReceivingGlAccount.Replace("-", "|"),
        //                    CrGlAccount = receivingAccount.Replace("-", "|"),
        //                    BaseAmount = currencyRateView.OperationalExchangeRate * unappliedDetail.TrxAmount,
        //                    OperationalAmount = unappliedDetail.TrxAmount,
        //                    FundingReferenceNo = unappliedDetail.EndToEndId
        //                };

        //                fundTransferDetailList.Add(transferDetail);
        //            }
        //            db.FundTransferDetails.AddRange(fundTransferDetailList);

        //            db.SaveChanges();
        //        }
        //        else
        //        {
        //            response = "unapplie Not Found!";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorSignal.FromCurrentContext().Raise(ex);
        //        response = ex.Message.ToString();
        //    }

        //    return Content(response);
        //}


        [HttpPost]
        public ActionResult ApproveWithdrawalMinistries(int? id)
        {
            InstitutionSubLevel userPaystation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
            var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
            var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());

            var transferType = "";
            var transferCategory = "";
            var journalTypeCode = "";

            Unapplied unapplied = db.Unapplieds.Find(id);
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }
            string response = "Success";
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {

                    var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "303" && a.InstitutionCode == unapplied.InstitutionCode).FirstOrDefault();
                    var cashAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.GlAccount).FirstOrDefault();
                    var bankAccountTo = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.AccountNumber).FirstOrDefault();
                    //var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "301" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
                    CurrencyRateView currencyRateView = db.CurrencyRateViews.Where(a => a.SubBudgetClass == "303"
                                   && a.InstitutionId == userPaystation.InstitutionId).FirstOrDefault();

                    if (currencyRateView == null)
                    {
                        response = "Currency Rate Setup is Incomplete";
                        return Content(response);
                    }
                    var institutionDetails = db.Institution.Where(a => a.InstitutionCode == userPayStation.InstitutionCode
                                                   && a.OverallStatus == "Active").FirstOrDefault();
                    if (unapplied.NewBankAccount == null)
                    {
                        response = "Payee Account is missing";
                        return Content(response);
                    }

                    if (unapplied.NewBIC == null)
                    {
                        response = "Payee BIC is missing";
                        return Content(response);
                    }

                    if (unapplied != null)
                    {
                        unapplied.WithdrawnBy = User.Identity.Name;
                        unapplied.WithdrawnAt = DateTime.Now;
                        unapplied.OverallStatus = "Withdrawal Approved";
                        if (unapplied.UnappliedAccount == bankAccountTo)
                        {
                            transferType = "Within";
                            transferCategory = "Internal";
                            journalTypeCode = "FTI";
                        }
                        else
                        {
                            transferType = "Between";
                            transferCategory = "Bank";
                            journalTypeCode = "FTF";
                        }

                        var pvNo = db.PaymentVouchers.Where(a => a.PVNo == unapplied.EndToEndId).FirstOrDefault();
                        var payerAccount = pvNo.PayeeBankAccount;

                        var bic2 = db.Accounts.Where(a => a.AccountNo == unapplied.UnappliedAccount).FirstOrDefault();
                        var bicFrom = db.Banks.Find(bic2.BankId);

                        var bic1 = db.Accounts.Where(a => a.AccountNo == pvNo.PayerBankAccount).FirstOrDefault();
                        var bictTo = db.Banks.Find(bic1.BankId);

                        var transferSummary = new FundTransferSummary
                        {

                            TransferType = transferType,
                            TransferCategory = transferCategory,
                            TransferDescription = "Unapplied Withdraw",
                            FundingRefNo = unapplied.EndToEndId,
                            TransferRefNum = unapplied.EndToEndId,
                            InstitutionCodeFrom = unapplied.InstitutionCode,
                            InstitutionCodeTo = unapplied.InstitutionCode,
                            InstitutionNameFrom = unapplied.InstitutionName,
                            InstitutionNameTo = unapplied.InstitutionName,
                            InstitutionIdFrom = unapplied.InstitutionId,
                            InstitutionIdTo = unapplied.InstitutionId,
                            //SubBudgetClassFrom = "303",
                            //SubBudgetClassTo = "101",
                            SubBudgetClassFrom = "303",
                            SubBudgetClassTo = pvNo.SubBudgetClass,
                            BankAccountFrom = unapplied.UnappliedAccount,
                            //BankAccountTo = "9921180001",
                            BankAccountTo = pvNo.PayerBankAccount,
                            //CashAccountFrom = institutionAccount.GlAccount,
                            //CashAccountTo = cashAccountTo,
                            CashAccountFrom = institutionAccount.GlAccount,
                            CashAccountTo = pvNo.PayerCashAccount,
                            TotalBaseAmount = currencyRateView.OperationalExchangeRate * (unapplied.TrxAmount),
                            TotalOperationalAmount = unapplied.TrxAmount,
                            SourceModule = "Unapplied",
                            JournalTypeCode = journalTypeCode,
                            OverallStatus = "Pending",
                            ApprovalStatus = "Pending",
                            JournalTypeId = 21,
                            OperationalCurrency = currencyRateView.OperationalCurrencyCode,
                            OperationalExchangeRate = currencyRateView.OperationalExchangeRate,
                            FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            RegionCode = "TZDO",
                            BankAccountBicFrom = bictTo.BIC,
                            BankAccountBicTo = bicFrom.BIC
                        };
                        db.FundTransferSummaries.Add(transferSummary);
                        db.SaveChanges();

                        int fundTransferSummaryId = transferSummary.FundTransferSummaryId;
                        List<FundTransferDetail> fundTransferDetailList = new List<FundTransferDetail>();

                        List<Unapplied> unappliedDetailList = db.Unapplieds.Where(a => a.UnappliedId == unapplied.UnappliedId).ToList();

                        //var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == "101" && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
                        var receivingAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == pvNo.SubBudgetClass && a.InstitutionCode == unapplied.InstitutionCode).Select(a => a.ReceivingGlAccount).FirstOrDefault();
                        foreach (Unapplied unappliedDetail in unappliedDetailList)
                        {
                            var coa = db.COAs.Where(a => a.GlAccount == institutionAccount.ReceivingGlAccount).FirstOrDefault();
                            FundTransferDetail transferDetail = new FundTransferDetail
                            {
                                FundTransferSummaryId = fundTransferSummaryId,
                                DrGlAccount = institutionAccount.ReceivingGlAccount.Replace("-", "|"),
                                CrGlAccount = receivingAccount.Replace("-", "|"),
                                BaseAmount = currencyRateView.OperationalExchangeRate * unappliedDetail.TrxAmount,
                                OperationalAmount = unappliedDetail.TrxAmount,
                                FundingReferenceNo = unappliedDetail.EndToEndId,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                TR = coa.TR,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                GfsCode = coa.GfsCode,
                                GfsCodeCategory = coa.GfsCodeCategory,
                                GeographicalLocationDesc = coa.GeographicalLocationDesc,
                                TrDesc = coa.TrDesc,
                                ProjectDesc = coa.ProjectDesc,
                                ServiceOutputDesc = coa.ServiceOutputDesc,
                                ActivityDesc = coa.ActivityDesc,
                                FundTypeDesc = coa.FundTypeDesc,
                                CofogDesc = coa.CofogDesc,
                                VoteDesc = coa.VoteDesc,
                            };

                            fundTransferDetailList.Add(transferDetail);
                        }
                        db.FundTransferDetails.AddRange(fundTransferDetailList);

                        db.SaveChanges();

                        var transactionLogs = new List<TransactionLogVM>();
                        foreach (var item in fundTransferDetailList)
                        {
                            var coa = db.COAs.Where(a => a.GlAccount == item.DrGlAccount).FirstOrDefault();
                            var transactionLog = new TransactionLogVM
                            {
                                SourceModuleId = unapplied.UnappliedId,
                                LegalNumber = unapplied.EndToEndId,
                                //FundingRerenceNo = dummyPayment.DocumentNo,
                                FundingRerenceNo = item.FundingReferenceNo,
                                InstitutionCode = unapplied.InstitutionCode,
                                InstitutionName = unapplied.InstitutionName,
                                JournalTypeCode = journalTypeCode,
                                GlAccount = item.DrGlAccount,
                                GlAccountDesc = "NA",
                                GfsCode = coa.GfsCode, //Coa
                                GfsCodeCategory = coa.GfsCodeCategory, //Coa
                                TransactionCategory = "Expenditure",
                                VoteDesc = coa.VoteDesc, //Coa
                                GeographicalLocationDesc = coa.GeographicalLocationDesc, //Coa
                                TrDesc = coa.TrDesc, //Coa
                                SubBudgetClass = pvNo.SubBudgetClass,
                                SubBudgetClassDesc = coa.subBudgetClassDesc,
                                ProjectDesc = coa.ProjectDesc, //Coa
                                ServiceOutputDesc = coa.ServiceOutputDesc,//Coa
                                ActivityDesc = coa.ActivityDesc, //Coa
                                FundTypeDesc = coa.FundTypeDesc, //Coa
                                CofogDesc = coa.CofogDesc, //From Coa
                                //SubLevelCode = userPayStation.SubLevelCode, //Coa
                                SubLevelCode = coa.SubVote, //Coa
                                FinancialYear = serviceManager.GetFinancialYear(DateTime.Now),
                                OperationalAmount = item.OperationalAmount,
                                BaseAmount = (item.OperationalAmount * currencyRateView.OperationalExchangeRate),
                                Currency = "TZS",
                                CreatedAt = DateTime.Now,
                                CreatedBy = unapplied.ConfirmedBy,
                                ApplyDate = unapplied.UnappliedAt,
                                PayeeCode = unapplied.VendorCode, //From my form
                                PayeeName = unapplied.BenName, //From my form
                                TransactionDesc = unapplied.BankingStatusDesc,
                                //OverallStatus = unapplied.OverallStatus,
                                OverallStatus = "Approved",
                                OverallStatusDesc = unapplied.BankingStatusDesc,
                                CostCentre = coa.CostCentre,
                                CostCentreDesc = coa.CostCentreDesc,
                                SubVote = coa.SubVote,
                                SubVoteDesc = coa.SubVoteDesc,
                                TR = coa.TR,
                                Facility = coa.Facility,
                                FacilityDesc = coa.FacilityDesc,
                                InstitutionLevel = institutionDetails.InstitutionLevel,
                                Level1Code = institutionDetails.Level1Code,
                                Level1Desc = institutionDetails.Level1Desc,
                                SourceModuleRefNo = unapplied.EndToEndId,
                                FundingSourceDesc = "NA",
                                SourceModule = "Fund Transfer",

                            };

                            transactionLogs.Add(transactionLog);
                        }

                        response = fundBalanceServices.PostTransaction(transactionLogs);
                        if (response == "Success")
                        {
                            scope.Complete();
                        }
                        response = "Success";
                    }
                    else
                    {
                        response = "unapplie Not Found!";
                    }
                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    response = ex.Message.ToString();
                }
            }
            return Content(response);
        }


        public ActionResult PreviousYearUnappliedList()
        {
            var unapplied = db.Unapplieds.Where(a => a.OverallStatus == "Waiting Confirmation" && a.isPreviousYearUnapplied == true).ToList();
            return View(unapplied);
        }


        public ActionResult UploadUnapplied()
        {
            UploadNgomePayeeVM vm = new UploadNgomePayeeVM();
            return View(vm);
        }

        [HttpPost]
        public ActionResult UploadUnapplied(UploadNgomePayeeVM uploadNgomePayeeVM)
        {
            var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            string response = "";
            string fileExtension = "";
            string filename = "";
            string fileLocation = "";
            System.Data.DataTable dt = new System.Data.DataTable();
            DataSet ds = new DataSet();

            try
            {

                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);

                if (!(fileExtension == ".xls" || fileExtension == ".xlsx"))
                {
                    response = "InvalidFormat";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(uploadNgomePayeeVM.FileName.InputStream))
                {
                    var currentSheet = package.Workbook.Worksheets;
                    var workSheet = currentSheet.First();
                    var noOfRows = workSheet.Dimension.End.Row;
                    var noOfCols = workSheet.Dimension.End.Column;

                    string payeeCode = "";
                    string payeeName = "";
                    string benAccount = "";
                    string unappliedAccount = "";
                    decimal amount = 0;
                    string endtoendId = "";
                    string bankBic = "";
                    string bankName = "";
                    string paymentDesc = "";
                    string unappliedReason = "";
                    string voteCode = "";
                    string voteName = "";
                    DateTime paymentDate;
                    DateTime unappliedAt;
                    List<Unapplied> newUnappliedList = new List<Unapplied>();
                    for (int i = 2; i <= noOfRows; i++)
                    {
                        //payeeCode = workSheet.Cells[i, 1].Value != null ? workSheet.Cells[i, 1].Value.ToString() : "";
                        payeeName = workSheet.Cells[i, 1].Value != null ? workSheet.Cells[i, 1].Value.ToString() : "";
                        benAccount = workSheet.Cells[i, 2].Value != null ? workSheet.Cells[i, 2].Value.ToString() : "";
                        unappliedAccount = workSheet.Cells[i, 3].Value != null ? workSheet.Cells[i, 3].Value.ToString() : "";
                        amount = workSheet.Cells[i, 4].Value != null ? Convert.ToDecimal(workSheet.Cells[i, 4].Value) : 0;
                        endtoendId = workSheet.Cells[i, 5].Value != null ? workSheet.Cells[i, 5].Value.ToString() : "";
                        bankBic = workSheet.Cells[i, 6].Value != null ? workSheet.Cells[i, 6].Value.ToString() : "";
                        bankName = workSheet.Cells[i, 7].Value != null ? workSheet.Cells[i, 7].Value.ToString() : "";
                        paymentDesc = workSheet.Cells[i, 8].Value != null ? workSheet.Cells[i, 8].Value.ToString() : "";
                        unappliedReason = workSheet.Cells[i, 9].Value != null ? workSheet.Cells[i, 9].Value.ToString() : "";
                        voteCode = workSheet.Cells[i, 10].Value != null ? workSheet.Cells[i, 10].Value.ToString() : "";
                        voteName = workSheet.Cells[i, 11].Value != null ? workSheet.Cells[i, 11].Value.ToString() : "";
                        paymentDate = workSheet.Cells[i, 12].Value != null ? Convert.ToDateTime(workSheet.Cells[i, 12].Value) : DateTime.Now;
                        unappliedAt = workSheet.Cells[i, 13].Value != null ? Convert.ToDateTime(workSheet.Cells[i, 13].Value) : DateTime.Now;

                        var insitution = db.Institution.Where(a => a.EpicorVoteCode == voteCode).FirstOrDefault();
                        //var insitution = db.Institution.Where(a => a.VoteCode == voteCode).FirstOrDefault();
                        Unapplied unapplied = new Unapplied();

                        unapplied.BenName = payeeName;
                        unapplied.BenAcct = benAccount;
                        unapplied.UnappliedAccount = unappliedAccount;
                        unapplied.TrxAmount = amount;
                        unapplied.EndToEndId = endtoendId;
                        unapplied.BenBic = bankBic;
                        unapplied.BankName = bankName;
                        unapplied.PaymentDesc = paymentDesc;
                        unapplied.BankingStatusDesc = unappliedReason;
                        unapplied.VoteCode = voteCode;
                        unapplied.VoteName = voteName;
                        unapplied.OverallStatus = "Waiting Confirmation";
                        unapplied.PaymentDate = DateTime.Now;
                        unapplied.CreatedAt = DateTime.Now;
                        unapplied.isPreviousYearUnapplied = true;
                        unapplied.UnappliedAt = unappliedAt;
                        unapplied.PaymentDate = paymentDate;
                        unapplied.InstitutionId = insitution.InstitutionId;
                        unapplied.InstitutionCode = insitution.InstitutionCode;
                        unapplied.InstitutionName = insitution.InstitutionName;
                        newUnappliedList.Add(unapplied);
                        //db.Unapplieds.Add(unapplied);
                        //db.SaveChanges();
                    }
                    /////Check for existing End to End
                    List<Unapplied> existingUnappliedList = db.Unapplieds.ToList();

                    List<string> existingEndToEndList = (from a in newUnappliedList
                                                         join b in existingUnappliedList on a.EndToEndId equals b.EndToEndId
                                                         select a.EndToEndId).Distinct().ToList();
                    /////check for duplicate end to end
                    //List<string> duplicateEndToEndList =newUnappliedList.Where(a=>a.EndToEndId.Count()>1).Select(a=>a.EndToEndId).ToList();

                    /////Check if payee exists in Payee
                    List<string> existingpayeeList = db.Payees.Select(a => a.PayeeCode).ToList();

                    //List<string> missingPayeeCodeList = (from a in newUnappliedList
                    //                                     where (!existingpayeeList.Contains(a.VendorCode))
                    //                                     select a.VendorCode).ToList();

                    response = "";
                    if (existingEndToEndList.Count > 0)
                    {
                        response = "Voucher number(s): " + string.Join(", ", existingEndToEndList) + "already exist and will be skipped. ";

                    }
                    //if (duplicateEndToEndList.Any())
                    //{
                    //    response += "Voucher number(s): " + string.Join(", ", duplicateEndToEndList) + " is duplicate. ";
                    //    //response= "Success";
                    //}

                    //if (missingPayeeCodeList.Count > 0)
                    //{
                    //    response += "Payee code(s) : " + string.Join(", ", missingPayeeCodeList) + " do not exist in MUSE and will be skipped";
                    //    //return Content(response);
                    //}

                    List<Unapplied> finalUnappliedList = new List<Unapplied>();

                    foreach (Unapplied unapplied in newUnappliedList)
                    {
                        if (existingEndToEndList.Contains(unapplied.EndToEndId) /*|| missingPayeeCodeList.Contains(unapplied.VendorCode)*/ /*/*|| duplicateEndToEndList.Contains(unapplied.EndToEndId)*/)
                        {
                            //skip
                        }
                        else
                        {
                            //db.Unapplieds.AddRange(finalUnappliedList);
                            finalUnappliedList.Add(unapplied);
                        }
                    }
                    if (finalUnappliedList.Count > 0)
                    {
                        db.Unapplieds.AddRange(finalUnappliedList);
                        db.SaveChanges();
                    }

                    if (response == "")
                    {
                        response = "Success";
                    }

                }
            }
            catch (Exception ex)
            {
                response = "An Error Occured while Processing Your Request,Please contacct System Administrator";
                return Content(ex.Message.ToString());
            }

            return Content(response);
        }

        public ActionResult AddUnappliedDetails(int? unappliedId, string payeeName)
        {
            var unapplied = db.Unapplieds.Find(unappliedId);

            AddUnappliedDetailsVM unappliedDetailsVM = new AddUnappliedDetailsVM
            {
                UnappliedId = unappliedId,
                PayeeCode = unapplied.VendorCode,
                PayeeName = unapplied.BenName,
            };

            return View(unappliedDetailsVM);
        }

        [HttpPost]
        public ActionResult AddUnappliedDetails(AddUnappliedDetailsVM unappliedDetailsVM)
        {
            var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            try
            {
                if (ModelState.IsValid)
                {

                    var unapplied = db.Unapplieds.Find(unappliedDetailsVM.UnappliedId);
                    unapplied.VendorCode = unappliedDetailsVM.PayeeCode;
                    db.SaveChanges();
                    return RedirectToAction("PreviousYearUnappliedList", "Unapplieds", new { id = unappliedDetailsVM.UnappliedId });
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return View(unappliedDetailsVM);
        }

        public JsonResult SearchPayeeCode(string term)
        {
            var userPayStation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());

            List<Select2DTOString> payeeList = new List<Select2DTOString>();

            try
            {

                var payees = (from a in db.Payees.Where(a => a.PayeeCode == term || a.PayeeName == term)
                              select new
                              {
                                  PayeeCode = a.PayeeCode,
                                  PayeeName = a.PayeeCode + " - " + a.PayeeName
                              }).ToList();

                foreach (var item in payees)
                {
                    payeeList.Add(new Select2DTOString(item.PayeeCode, item.PayeeName));
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return Json(new { payeeList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPayeeCode(int? id)
        {
            var unapplied = db.Unapplieds.Find(id);

            if (unapplied != null)
            {
                return Json(new
                {
                    VendorCode = unapplied.VendorCode,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {

            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ConfirmPYUnapplied(int? id)
        {
            string response = "Success";
            try
            {
                var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
                Unapplied unapplied = db.Unapplieds.Find(id);
                if (unapplied != null)
                {

                    unapplied.OverallStatus = "Pending";
                    db.SaveChanges();
                }
                else
                {
                    response = "Payee Not Found!";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            return Content(response);

        }

        public ActionResult MdaUnappliedList()
        {
            var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            var unapplied = db.Unapplieds.Where(a => a.OverallStatus == "Waiting Confirmation" && a.isPreviousYearUnapplied == true && a.InstitutionCode == userPaystation.InstitutionCode).ToList();
            return View(unapplied);
        }

        public ActionResult AllUnapplieds()
        {
            return View();
        }
        public JsonResult GetAllUnapplieds(DataTablesParams param)
        {
            var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            IQueryable<Unapplied> unapplied;
            List<AllUnappliedVM> unappliedVM = new List<AllUnappliedVM>();

            int pageNo = 1;
            int totalCount = 0;

            if (param.iDisplayStart >= param.iDisplayLength)
            {
                pageNo = (param.iDisplayStart / param.iDisplayLength) + 1;
            }

            //unapplied = db.Unapplieds.Where(a=>a.InstitutionCode==userPaystation.InstitutionCode);
            unapplied = db.Unapplieds.Where(a => a.isPreviousYearUnapplied == true || a.OverallStatus == "Pending");

            if (param.sSearch != null)
            {
                unapplied = unapplied.Where(a => a.VendorCode.Contains(param.sSearch) || a.BenName.Contains(param.sSearch)
                || a.NewBankAccount.Contains(param.sSearch) || a.EndToEndId.Contains(param.sSearch) || a.InstitutionCode.Contains(param.sSearch));
            }

            totalCount = unapplied.Count();

            unappliedVM = unapplied.OrderByDescending(a => a.CreatedAt).Skip((pageNo - 1) * param.iDisplayLength).Take(param.iDisplayLength)
                .Select(a => new AllUnappliedVM
                {
                    UnappliedId = a.UnappliedId,
                    VendorCode = a.VendorCode,
                    BenName = a.BenName,
                    BenAcct = a.BenAcct,
                    EndToEndId = a.EndToEndId,
                    TrxAmount = a.TrxAmount,
                    BankName = a.BankName,
                    BankingStatusDesc = a.BankingStatusDesc,
                    InstitutionCode = a.InstitutionCode,
                    OverallStatus = a.OverallStatus
                }).ToList();

            return Json(new
            {
                aaData = unappliedVM,
                sEcho = param.sEcho,
                iTotalDisplayRecords = totalCount,
                iTotalRecords = totalCount
            }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult CreateBulkUnapplied(int? id)
        {
            var createBulkUnappliedVM = new CreateBulkUnappliedVM();
            ViewBag.Banks = db.Banks.ToList();
            return View();
        }


        [HttpPost, Authorize(Roles = "Unapplied Confirmation")]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateBulkUnapplied(CreateBulkUnappliedVM createUnappliedVM)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var financialYear = serviceManager.GetFinancialYear(DateTime.Now);
                    var userPayStation = serviceManager.GetUserPayStation(User.Identity.GetUserId());
                    var institutionId = 0;
                    if (userPayStation != null)
                    {
                        institutionId = userPayStation.InstitutionId;
                    }

                    Unapplied unapplied = db.Unapplieds.Find(createUnappliedVM.UnappliedId);
                    if (unapplied.BulkPaymentStatus == null)
                    {
                        unapplied.NewBenName = createUnappliedVM.NewBenName;
                        unapplied.NewBankAccount = createUnappliedVM.NewBankAccount;
                        unapplied.NewBankName = createUnappliedVM.NewBankName;
                        unapplied.NewBIC = createUnappliedVM.NewBenBic;
                        unapplied.OverallStatus = "Edited";
                    }
                    else
                    {
                        unapplied.NewBenName = createUnappliedVM.NewBenNameb;
                        unapplied.NewBankAccount = createUnappliedVM.NewBankAccountb;
                        unapplied.NewBankName = createUnappliedVM.NewBankNameb;
                        unapplied.NewBIC = createUnappliedVM.NewBenBicb;
                        unapplied.OverallStatus = "Edited";
                    }

                    await db.SaveChangesAsync();
                    return RedirectToAction("UnappliedList");
                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }

                //var unapplied = new Unapplied
                //{
                //    VendorCode=createUnappliedVM.VendorCode,
                //    NewBenName=createUnappliedVM.NewBenName,
                //    NewBankAccount=createUnappliedVM.NewBankAccount,
                //    NewBankName=createUnappliedVM.NewBankName,
                //    NewBIC=createUnappliedVM.NewBIC,
                //    PaymentDesc=createUnappliedVM.PaymentDesc,

                //    TrxAmount=createUnappliedVM.TrxAmount,


                //};
                //db.Unapplieds.Add(unapplied);

            }

            ViewBag.Banks = db.Banks.ToList();

            return View(createUnappliedVM);
        }

        public JsonResult SearchBulkUnapplied(string term)
        {
            var userPayStation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
            List<Select2DTO> unappliedList = new List<Select2DTO>();

            try
            {

                var unapplieds = (from a in db.Unapplieds
                                  .Where(a => (a.VendorCode.Contains(term)
                                  || a.BenName.Contains(term)
                                  || a.EndToEndId.Contains(term))
                                  && a.InstitutionCode == userPayStation.InstitutionCode
                                  && (a.OverallStatus == "Pending" || a.OverallStatus == "Cancelled")
                                  )
                                  select new
                                  {
                                      UnappliedId = a.UnappliedId,
                                      VendorCode = a.VendorCode,
                                      VendorName = a.VendorCode + " - " + a.BenName + " - " + a.EndToEndId
                                  }).ToList();

                foreach (var item in unapplieds)
                {
                    unappliedList.Add(new Select2DTO((int)item.UnappliedId, item.VendorName));
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return Json(new { unappliedList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBulkUnappliedDetails(int? id)
        {
            string response = "Success";
            //string payeestatus = "Active";

            var unapplied = db.Unapplieds.Find(id);
            if (unapplied == null)
            {
                response = "";
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            if (unapplied.BulkPaymentStatus == null)
            {

                var payee = db.Payees
                .Where(a => a.PayeeCode == unapplied.VendorCode
                && a.OverallStatus == "ACTIVE")
                .FirstOrDefault();

                if (payee == null)
                {
                    response = "Error: This Payee Is not ACTIVE!";
                    return Json(new { response }, JsonRequestBehavior.AllowGet);
                }

                var payeeDetails = db.PayeeDetails
             .Where(a => a.PayeeId == payee.PayeeId
             && a.IsActive == true)
             .ToList();

                var accounts = (from u in payeeDetails
                                select new
                                {
                                    u.Accountnumber,
                                    AccountnumberAccountName = u.Accountnumber + " " + u.AccountName
                                }).ToList();

                return Json(new
                {
                    response,
                    unapplied.BenName,
                    unapplied.VendorCode,
                    unapplied.EndToEndId,
                    BankAccount = unapplied.BenAcct,
                    unapplied.BankName,
                    unapplied.BenBic,
                    unapplied.TrxAmount,
                    unapplied.UnappliedAccount,
                    unapplied.PaymentDesc,
                    unapplied.BankingStatusDesc,

                    Accounts = accounts,
                    unapplied.BulkPaymentStatus,

                }, JsonRequestBehavior.AllowGet);

            }
            else
            {

                return Json(new
                {
                    response,
                    unapplied.BenName,
                    unapplied.VendorCode,
                    unapplied.EndToEndId,
                    BankAccount = unapplied.BenAcct,
                    unapplied.BankName,
                    unapplied.BenBic,
                    unapplied.TrxAmount,
                    unapplied.UnappliedAccount,
                    unapplied.PaymentDesc,
                    unapplied.BankingStatusDesc,
                    unapplied.BulkPaymentStatus,
                }, JsonRequestBehavior.AllowGet);

            }
        }

        public JsonResult GetBankBic(string bankName)
        {
            string bankBic = db.Banks
                .Where(a => a.BankName == bankName)
                .Select(a => a.BIC)
                .FirstOrDefault();
            return Json(new { success = true, bankBic = bankBic });
        }

        public ActionResult AllUnappliedList()
        {
            var unappliedlist = db.Unapplieds.ToList();

            return View(unappliedlist);
        }

        [HttpPost]
        public ActionResult RejectWithdrawal(int? id)
        {
            string response = "Success";
            try
            {
                var userPaystation = serviceManager.GetDefaultUserPayStation(User.Identity.GetUserId());
                Unapplied unapplied = db.Unapplieds.Find(id);
                if (unapplied != null)
                {

                    unapplied.OverallStatus = "Rejected";
                    db.SaveChanges();
                }
                else
                {
                    response = "Payee Not Found!";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
            }

            return Content(response);

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

