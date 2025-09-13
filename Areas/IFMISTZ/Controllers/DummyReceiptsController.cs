using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class DummyReceiptsController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();

        // GET: IFMISTZ/Receipts
        public async Task<ActionResult> PendingDummyReceipts()
        {
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            return View(await db.Receipts.Where(a => (a.OverallStatus == "Pending" || a.OverallStatus == "REJECTED") && a.InstitutionId == userPayStation.InstitutionId && (a.TransactionType == "Deposit Receipt" || a.TransactionType == "Unapplied")).OrderByDescending(a => a.ReceiptId).ToListAsync());
        }

        public async Task<ActionResult> ConfirmedDummyReceipts()
        {
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            return View(await db.Receipts.Where(a => a.OverallStatus == "Confirmed" && a.InstitutionId == userPayStation.InstitutionId && (a.TransactionType == "Deposit Receipt" || a.TransactionType == "Unapplied")).OrderByDescending(a => a.ConfirmedAt).ToListAsync());
        }

        public async Task<ActionResult> ApprovedDummyReceipts()
        {
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            return View(await db.Receipts.Where(a => a.OverallStatus == "Approved" && a.InstitutionId == userPayStation.InstitutionId && (a.TransactionType == "Deposit Receipt" || a.TransactionType == "Unapplied")).OrderByDescending(a => a.ApprovedAt).ToListAsync());
        }

        public async Task<ActionResult> PostedDummyReceipts()
        {
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            return View(await db.Receipts.Where(a => a.OverallStatus == "Posted" && a.InstitutionId == userPayStation.InstitutionId && (a.TransactionType == "Deposit Receipt" || a.TransactionType == "Unapplied")).ToListAsync());
        }

        // GET: IFMISTZ/Receipts/Details/5
        public async Task<ActionResult> DummyReceiptDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Receipt receipt = await db.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return HttpNotFound();
            }
            return PartialView("_DummyReceiptDetails", receipt);
        }

        // GET: IFMISTZ/Receipts/Create
        public ActionResult CreateDummyReceipt()
        {
            var createReceiptVM = new CreateDummyReceiptVM();
            createReceiptVM.ReceiptTypes = new SelectList(db.ReceiptTypes, "ReceiptTypeDesc", "ReceiptTypeDesc");

            return View(createReceiptVM);
        }

        // POST: IFMISTZ/Receipts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDummyReceipt(CreateDummyReceiptVM createDummyReceiptVM)
        {
            if (ModelState.IsValid)
            {
                var financialYear = ServiceManager.GetFinancialYear(db, (DateTime)createDummyReceiptVM.ReceiptDate);
                var userPayStation = ServiceManager.GetDefaultUserPayStation(db, User.Identity.GetUserId());
                var institutionId = 0;
                var institutionCode = "";
                var institutionName = "";
                decimal? baseAmount = 0;
                decimal? currentExchangeRate = 0;
                if (userPayStation != null)
                {
                    institutionId = userPayStation.InstitutionId;
                    institutionCode = userPayStation.InstitutionCode;
                    institutionName = userPayStation.InstitutionName;
                }

                var currencyRateView = db.CurrencyRateViews.Where(a => a.InstitutionCode == institutionCode && a.SubBudgetClass == createDummyReceiptVM.SubBudgetClass && a.AccountNumber == createDummyReceiptVM.ReceivingBankAccountNo).FirstOrDefault();
                if (currencyRateView != null)
                {
                    baseAmount = createDummyReceiptVM.OperationalAmount * currencyRateView.OperationalExchangeRate;
                    currentExchangeRate = currencyRateView.OperationalExchangeRate;
                }

                var receipt = new Receipt
                {
                    ReferenceNo = createDummyReceiptVM.ReferenceNo,
                    BaseAmount = baseAmount,
                    OperationalAmount = createDummyReceiptVM.OperationalAmount,
                    OperationalCurrency = createDummyReceiptVM.OperationalCurrency,
                    CurrentExchangeRate = currentExchangeRate,
                    ReceivingBankName = createDummyReceiptVM.ReceivingBankName,
                    ReceivingBankBIC = createDummyReceiptVM.ReceivingBankBic,
                    ReceivingBankAccountNo = createDummyReceiptVM.ReceivingBankAccountNo,
                    ReceivingBankAccountName = createDummyReceiptVM.ReceivingBankAccountName,
                    CustomerName = createDummyReceiptVM.CustomerName,
                    ReceiptDate = createDummyReceiptVM.ReceiptDate,
                    ReceiptType = createDummyReceiptVM.ReceiptType,
                    Remarks = createDummyReceiptVM.Remarks,
                    JournalTypeCode = "DR",
                    FinancialYear = financialYear,
                    DrGlAccount = createDummyReceiptVM.DrGlAccount,
                    DrGlAccountDesc = createDummyReceiptVM.DrGlAccountDesc,
                    ReversalFlag = false,
                    InstitutionId = institutionId,
                    InstitutionCode = institutionCode,
                    InstitutionName = institutionName,
                    FundType = createDummyReceiptVM.FundType,
                    SubBudgetClass = createDummyReceiptVM.SubBudgetClass,
                    SubBudgetClassDesc = createDummyReceiptVM.SubBudgetClassDesc,
                    PvId = createDummyReceiptVM.PvId,
                    PvNo = createDummyReceiptVM.PvNo,
                    OverallStatus = "Pending",
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    TransactionType = "Deposit Receipt",
                    IsAvailableForDistribution = createDummyReceiptVM.ReceiptType == "Normal Receipt" ? "Yes" : "No",
                };

                var receiptId = db.Receipts.Max(a => (int?)a.ReceiptId) ?? 0;
                receiptId = ++receiptId;

                var legalNo = ServiceManager.GetLegalNumber(db, institutionCode, "DR", receiptId);
                receipt.LegalNo = legalNo;

                db.Receipts.Add(receipt);

                if (createDummyReceiptVM.ReceiptDetails != null)
                {
                    List<ReceiptDetail> receiptDetails = new List<ReceiptDetail>();
                    foreach (var item in createDummyReceiptVM.ReceiptDetails)
                    {
                        var receiptDetail = new ReceiptDetail
                        {
                            ReceiptId = receipt.ReceiptId,
                            CrGlAccount = item.GlAccount,
                            CrGlAccountDesc = item.GlAccountDesc,
                            ExpensedAmount = item.ExpensedAmount,
                            ReturnedAmount = item.ReturnedAmount
                        };

                        receiptDetails.Add(receiptDetail);
                    }

                    db.ReceiptDetails.AddRange(receiptDetails);
                }

                await db.SaveChangesAsync();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }

            return Json("An error occurred while processing your request. Contact system support.", JsonRequestBehavior.AllowGet);
        }

        // GET: IFMISTZ/Receipts/Edit/5
        public async Task<ActionResult> EditDummyReceipt(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Receipt receipt = await db.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return HttpNotFound();
            }

            var editReceiptVM = new EditReceiptVM
            {
                ReceiptId = receipt.ReceiptId,
                ReferenceNo = receipt.ReferenceNo,
                OperationalAmount = receipt.OperationalAmount,
                ReceiptDate = receipt.ReceiptDate,
                CustomerName = receipt.CustomerName,
                CustomerAddress = receipt.CustomerAddress,
                ReceivingBankAccountNo = receipt.ReceivingBankAccountNo,
                ReceivingBankAccountName = receipt.ReceivingBankAccountName,
                ReceivingBankName = receipt.ReceivingBankName,
                ReceivingBankBIC = receipt.ReceivingBankBIC,
                FundingSource = receipt.DFundSourceCode,
                FundingSourceDesc = receipt.DFundSourceDesc,
                DrGlAccount = receipt.DrGlAccount,
                DrGlAccountDesc = receipt.DrGlAccountDesc,
                CrGlAccountId = receipt.CoaId,
                CrGlAccount = receipt.CrGlAccount,
                CrGlAccountDesc = receipt.CrGlAccountDesc,
                Remarks = receipt.Remarks,
                IsAvailableForDistribution = (EnumsList.YesNo)Enum.Parse(typeof(EnumsList.YesNo), receipt.IsAvailableForDistribution)
            };

            return View("EditReceipt", editReceiptVM);
        }

        // POST: IFMISTZ/Receipts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDummyReceipt(EditReceiptVM editReceiptVM)
        {
            if (ModelState.IsValid)
            {
                Receipt receipt = await db.Receipts.FindAsync(editReceiptVM.ReceiptId);

                receipt.ReferenceNo = editReceiptVM.ReferenceNo;
                receipt.OperationalAmount = editReceiptVM.OperationalAmount;
                receipt.ReceiptDate = editReceiptVM.ReceiptDate;
                receipt.CustomerName = editReceiptVM.CustomerName;
                receipt.CustomerAddress = editReceiptVM.CustomerAddress;
                receipt.ReceivingBankAccountNo = editReceiptVM.ReceivingBankAccountNo;
                receipt.ReceivingBankAccountName = editReceiptVM.ReceivingBankAccountName;
                receipt.ReceivingBankName = editReceiptVM.ReceivingBankName;
                receipt.ReceivingBankBIC = editReceiptVM.ReceivingBankBIC;
                receipt.DrGlAccount = editReceiptVM.DrGlAccount;
                receipt.DrGlAccountDesc = editReceiptVM.DrGlAccountDesc;
                receipt.CoaId = editReceiptVM.CrGlAccountId;
                receipt.CrGlAccount = editReceiptVM.CrGlAccount;
                receipt.CrGlAccountDesc = editReceiptVM.CrGlAccountDesc;
                receipt.Remarks = editReceiptVM.Remarks;
                receipt.IsAvailableForDistribution = editReceiptVM.IsAvailableForDistribution.ToString();

                await db.SaveChangesAsync();

                return RedirectToAction("PendingReceipts");
            }

            return View(editReceiptVM);
        }

        // GET: IFMISTZ/Receipts/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Receipt receipt = await db.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return HttpNotFound();
            }
            return View(receipt);
        }

        // POST: IFMISTZ/Receipts/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            string response = "";

            try
            {
                Receipt receipt = await db.Receipts.FindAsync(id);
                db.Receipts.Remove(receipt);

                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Content(response);
        }

        public JsonResult GetReferenceNos(string term)
        {
            List<Select2DTOString> refs = new List<Select2DTOString>();

            var userPayStation = ServiceManager.GetDefaultUserPayStation(db, User.Identity.GetUserId());

            if (userPayStation != null)
            {
                var institutionAccounts = db.InstitutionAccounts.Where(a => a.InstitutionId == userPayStation.InstitutionId).Select(a => a.AccountNumber).ToArray();

                var bankStatements = from u in db.BankStatementSummarys
                                     join v in db.BankStatementDetails on u.BankStatementSummaryId equals v.BankStatementSummaryId
                                     where v.TransactionRef == term
                                     where v.TransactionType == "CR"
                                     where institutionAccounts.Contains(u.BankAccountNumber)
                                     select v;

                foreach (var item in bankStatements)
                {
                    refs.Add(new Select2DTOString(item.TransactionRef, item.TransactionRef));
                }
            }

            return Json(new { refs }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetReferenceNo(string id)
        {
            var refNo = (from u in db.Receipts
                         where u.ReferenceNo == id
                         select new
                         {
                             id = u.ReferenceNo,
                             text = u.ReferenceNo
                         }).FirstOrDefault();

            return Json(new
            {
                id = refNo.id,
                text = refNo.text
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSelectedFundingSource(string id)
        {
            var fundingSource = (from u in db.CoaSegments
                                 where u.SegmentCode == id
                                 select new
                                 {
                                     id = u.SegmentCode,
                                     text = u.SegmentDesc
                                 }).FirstOrDefault();

            return Json(new
            {
                id = fundingSource.id,
                text = fundingSource.text
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetBankStatementDetail(string id)
        {
            var institutionId = 0;
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }          

            var institutionAccounts = db.InstitutionAccounts.Where(a => a.InstitutionId == userPayStation.InstitutionId).Select(a => a.AccountNumber).ToArray();

            var bankStatement = (from u in db.BankStatementSummarys
                                 join v in db.BankStatementDetails on u.BankStatementSummaryId equals v.BankStatementSummaryId
                                 where v.TransactionRef == id
                                 where v.TransactionType == "CR"
                                 where institutionAccounts.Contains(u.BankAccountNumber)
                                 select new
                                 {
                                     ReferenceNo = v.TransactionRef,
                                     OperationalAmount = v.TransactionAmount,
                                     OperationalCurrency = u.CurrencyCode,
                                     ReceivingBankName = u.BankName,
                                     ReceivingBankBic = u.BankBic,
                                     ReceivingBankAccountNo = u.BankAccountNumber,
                                     ReceivingBankAccountName = u.BankAccountName,
                                     ReceiptDate = u.StatementDate,
                                 }).FirstOrDefault();

            if (bankStatement != null)
            {
                var countRefNo = ServiceManager.ValidateCashReceiptt(id, bankStatement.ReceivingBankAccountNo);

                if (countRefNo != null)
                {
                    return Json(new
                    {
                        duplicate = true
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    bankStatement.ReferenceNo,
                    bankStatement.OperationalAmount,
                    bankStatement.OperationalCurrency,
                    bankStatement.ReceivingBankName,
                    bankStatement.ReceivingBankBic,
                    bankStatement.ReceivingBankAccountNo,
                    bankStatement.ReceivingBankAccountName,
                    bankStatement.ReceiptDate,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = false
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetFundingSources(string term)
        {
            List<Select2DTOString> fundingSources = new List<Select2DTOString>();

            var coaSegments = db.CoaSegments.Where(a => a.SegmentNo == 13 && a.SegmentDesc.Contains(term)).ToList();

            foreach (var item in coaSegments)
            {
                fundingSources.Add(new Select2DTOString(item.SegmentCode, item.SegmentDesc));
            }

            return Json(new { fundingSources }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCoas(string id, string term)
        {
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var institutionId = 0;
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }

            List<Select2DTO> coas = new List<Select2DTO>();

            var models = db.COAs.Where(a => a.FundingSource == id && a.InstitutionId == institutionId && (a.GlAccount.Contains(term) || a.GlAccountDesc.Contains(term))).ToList();

            foreach (var item in models)
            {
                coas.Add(new Select2DTO(item.CoaId, item.GlAccount + " - " + item.GlAccountDesc));
            }

            return Json(new { coas }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCoa(int? id)
        {
            var coa = db.COAs.Find(id);

            if (coa != null)
            {
                return Json(new
                {
                    success = true,
                    CrGlAccount = coa.GlAccount,
                    CrGlAccountDesc = coa.GlAccountDesc,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = false
            });
        }

        public JsonResult ConfirmReceipt(int? id)
        {
            var response = "";

            try
            {
                var receipt = db.Receipts.Find(id);

                receipt.OverallStatus = "Confirmed";
                receipt.ConfirmedBy = User.Identity.Name;
                receipt.ConfirmedAt = DateTime.Now;

                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Error";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ApproveReceipt(int? id)
        {
            var response = "";

            try
            {
                var receipt = db.Receipts.Find(id);

                receipt.OverallStatus = "Approved";
                receipt.ApprovedBy = User.Identity.Name;
                receipt.ApprovedAt = DateTime.Now;

                if (receipt.TransactionType == "Unapplied")
                {
                    var unapplieds = db.Unapplieds.Where(a => a.UnappliedRef == receipt.ReferenceNo).ToList();
                    foreach (var item in unapplieds)
                    {
                        item.OverallStatus = "Approved";
                        item.ApprovedBy = User.Identity.Name;
                        item.ApprovedAt = DateTime.Now;
                    }
                }

                if (receipt.IsAvailableForDistribution.ToString() == EnumsList.YesNo.Yes.ToString())
                {
                    var fundReceiving = new FundReceiving
                    {
                        FundingSource = "Revenue Grant",
                        FundingRefNo = receipt.ReferenceNo,
                        OperationalAmount = receipt.OperationalAmount,
                        BaseAmount = receipt.OperationalAmount,
                        BaseCurrency = receipt.OperationalCurrency,
                        OperationalCurrency = receipt.OperationalCurrency,
                        Book = "Grant",
                        ComponentDesc = receipt.Remarks,
                        ComponentName = receipt.Remarks,
                        FundingDate = receipt.ReceiptDate,
                        SourceBankAccountNo = receipt.ReceivingBankAccountNo,
                        SourceBankName = receipt.ReceivingBankName,
                        ReceivingBankAccountNo = receipt.ReceivingBankAccountNo,
                        ReceivingBankName = receipt.ReceivingBankName,
                        DrGlAccount = receipt.DrGlAccount,
                        CrGlAccount = receipt.CrGlAccount,
                        JournalTypeCode = receipt.JournalTypeCode,
                        JournalTypeDetailId = 18,
                        InstitutionId = receipt.InstitutionId,
                        InstitutionCode = receipt.InstitutionCode,
                        InstitutionName = receipt.InstitutionName,
                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                        OverallStatus = "Approved",
                        CreatedAt = DateTime.Now,
                        CreatedBy = User.Identity.Name,
                        FundType = receipt.FundType,
                        SubBudgetClass = receipt.SubBudgetClass,
                        BudgetClass = receipt.SubBudgetClassDesc
                    };

                    db.FundReceivings.Add(fundReceiving);
                }

                db.SaveChanges();

                //Insert data into journal
                var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", receipt.JournalTypeCode) };
                db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);

                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Error";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSelectedCoa(int id)
        {
            var coa = (from u in db.COAs
                       where u.CoaId == id
                       select new
                       {
                           id = u.CoaId,
                           text = u.GlAccountDesc
                       }).FirstOrDefault();

            return Json(new
            {
                id = coa.id,
                text = coa.text
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TrackDummyReceipt()
        {
            var statue = ServiceManager.GetStatue();
            var statueSelectList = ServiceManager.GetSelectListItems(statue);
            var tracker = new TrackerVM
            {
                OverallStatue = new SelectList(statueSelectList, "Value", "Text")
            };

            return View(tracker);
        }

        public ActionResult GetReceipts(TrackerVM trackerVM)
        {
            IQueryable<Receipt> receipts;

            var institutionId = 0;
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }

            receipts = db.Receipts.Where(a => a.InstitutionId == institutionId);

            if (trackerVM.SearchKeyword != null)
            {
                receipts = receipts.Where(a => a.ReferenceNo.Contains(trackerVM.SearchKeyword) || a.CustomerName.Contains(trackerVM.SearchKeyword));
            }

            switch (trackerVM.OverallStatus)
            {
                case "Pending":
                    receipts = receipts.Where(a => a.OverallStatus == "Pending" && (DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "Confirmed":
                    receipts = receipts.Where(a => a.OverallStatus == "Confirmed" && (DbFunctions.TruncateTime(a.ConfirmedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.ConfirmedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "Approved":
                    receipts = receipts.Where(a => a.OverallStatus == "Approved" && (DbFunctions.TruncateTime(a.ApprovedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.ApprovedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "Posted":
                    receipts = receipts.Where(a => a.OverallStatus == "Posted" && (DbFunctions.TruncateTime(a.PostedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.PostedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate)));
                    break;
                case "All":
                    receipts = receipts.Where(a => DbFunctions.TruncateTime(a.CreatedAt) >= DbFunctions.TruncateTime(trackerVM.StartDate) && DbFunctions.TruncateTime(a.CreatedAt) <= DbFunctions.TruncateTime(trackerVM.EndDate));
                    break;
                default:
                    break;
            }

            return PartialView("_DummyReceipts", receipts);
        }

        public JsonResult GetSubBudgetClasses(string term, string accountNo)
        {
            List<Select2DTOString> subBudgetClasses = new List<Select2DTOString>();

            var sbcs = db.InstitutionAccounts.Where(a => a.AccountNumber == accountNo).Select(a => a.SubBudgetClass).ToArray();

            var coaSegments = db.CoaSegments.Where(a => a.SegmentNo == 7 && (a.SegmentCode.Contains(term) || a.SegmentDesc.Contains(term))).Where(a => sbcs.Contains(a.SegmentCode)).ToList();

            foreach (var item in coaSegments)
            {
                subBudgetClasses.Add(new Select2DTOString(item.SegmentCode, item.SegmentCodeSegmentDesc));
            }

            return Json(new { subBudgetClasses }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSelectedSubBudgetClass(string id)
        {
            var subBudgetClass = (from u in db.CoaSegments
                                  where u.SegmentCode == id
                                  select new
                                  {
                                      id = u.SegmentCode,
                                      text = u.SegmentDesc
                                  }).FirstOrDefault();

            return Json(new
            {
                id = subBudgetClass.id,
                text = subBudgetClass.text
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSubBudgetClass(string id)
        {
            var institutionId = 0;
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation == null)
            {
                return Json(new
                {
                    success = false,
                    response = "User has no paystation setup, give a pay station to a user before continue with a form"
                }, JsonRequestBehavior.AllowGet);
            }

            institutionId = userPayStation.InstitutionId;

            var budgetClassObj = ServiceManager.GetSubBudgetClassDescBySubBudgetClassCode(id);

            if (budgetClassObj == null)
            {
                return Json(new
                {
                    success = false,
                    response = "Budget class setup for sub budget class " + id + " is not complete, resolve the issue then continue with the form"
                }, JsonRequestBehavior.AllowGet);
            }

            var drGlAccount = "";
            var drGlAccountDesc = "";
            var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == id && a.InstitutionId == institutionId && a.OverallStatus != "Cancelled").FirstOrDefault();

            if (institutionAccount == null)
            {
                return Json(new
                {
                    success = false,
                    response = "Institution account setup for sub budget class " + id + " is not complete, resolve the issue before saving a receipt"
                }, JsonRequestBehavior.AllowGet);
            }

            drGlAccount = institutionAccount.GlAccount;
            drGlAccountDesc = institutionAccount.GlAccountDesc;

            return Json(new
            {
                success = true,
                DrGlAccount = drGlAccount,
                DrGlAccountDesc = drGlAccountDesc,
                FundType = budgetClassObj.FundType,
                SubBudgetClassDesc = budgetClassObj.SubBudgetClassDesc,
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPvNos(string term)
        {
            var institutionId = 0;
            var userPayStation = ServiceManager.GetDefaultUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation != null)
            {
                institutionId = userPayStation.InstitutionId;
            }

            List<Select2DTO> pvNos = new List<Select2DTO>();

            var paymentVouchers = db.PaymentVouchers.Where(a => a.PVNo == term && a.InstitutionId == institutionId).ToList();

            foreach (var item in paymentVouchers)
            {
                pvNos.Add(new Select2DTO(item.PaymentVoucherId, item.PVNo));
            }

            return Json(new { pvNos }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSelectedPvNo(int id)
        {
            var pvNo = (from u in db.PaymentVouchers
                        where u.PaymentVoucherId == id
                        select new
                        {
                            id = u.PVNo,
                            text = u.PVNo
                        }).FirstOrDefault();

            return Json(new
            {
                id = pvNo.id,
                text = pvNo.text
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPv(int id)
        {
            var institutionId = 0;
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation == null)
            {
                return Json(new
                {
                    success = false,
                    response = "User has no paystation setup, give a pay station to a user before continue with a form"
                }, JsonRequestBehavior.AllowGet);
            }

            institutionId = userPayStation.InstitutionId;

            var pv = db.PaymentVouchers.Find(id);

            if (pv == null)
            {
                return Json(new
                {
                    success = false,
                    response = "PV does not exist, resolve the issue then continue with the form"
                }, JsonRequestBehavior.AllowGet);
            }

            var budgetClassObj = ServiceManager.GetSubBudgetClassDescBySubBudgetClassCode(pv.SubBudgetClass);

            if (budgetClassObj == null)
            {
                return Json(new
                {
                    success = false,
                    response = "Budget class setup for sub budget class " + id + " is not complete, resolve the issue then continue with the form"
                }, JsonRequestBehavior.AllowGet);
            }

            var drGlAccount = "";
            var drGlAccountDesc = "";
            var institutionAccount = db.InstitutionAccounts.Where(a => a.SubBudgetClass == pv.SubBudgetClass && a.InstitutionId == institutionId && a.OverallStatus != "Cancelled").FirstOrDefault();

            if (institutionAccount == null)
            {
                return Json(new
                {
                    success = false,
                    response = "Institution account setup for sub budget class " + pv.SubBudgetClass + " is not complete, resolve the issue before saving a receipt"
                }, JsonRequestBehavior.AllowGet);
            }

            drGlAccount = institutionAccount.GlAccount;
            drGlAccountDesc = institutionAccount.GlAccountDesc;

            return Json(new
            {
                success = true,
                DrGlAccount = drGlAccount,
                DrGlAccountDesc = drGlAccountDesc,
                budgetClassObj.FundType,
                budgetClassObj.SubBudgetClassDesc,
                PvNo = pv.PVNo,
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetBudgetCoaStrings(string receiptType, string subBudgetClass, int? pvId, string term)
        {
            List<Select2DTOString> coas = new List<Select2DTOString>();

            var institutionCode = "";
            var userPayStation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            if (userPayStation != null)
            {
                institutionCode = userPayStation.InstitutionCode;
            }

            IQueryable<JournalTypeView> journalTypeViews;
            IQueryable<VoucherDetail> voucherDetails;

            if (receiptType == "Normal Receipt")
            {
                journalTypeViews = db.JournalTypeViews.Where(a => a.SubBudgetClass == subBudgetClass && a.InstitutionCode == institutionCode && a.JournalTypeCode == "DR" && a.TrxType == "CR" && (a.CrCoa.Contains(term) || a.CrCoaDesc.Contains(term)));
                foreach (var item in journalTypeViews)
                {
                    coas.Add(new Select2DTOString(item.CrCoa, item.CrCoa + " - " + item.CrCoaDesc));
                }
            }
            else if (receiptType == "Return Receipt")
            {
                voucherDetails = db.VoucherDetails.Where(a => a.PaymentVoucherId == pvId);
                foreach (var item in voucherDetails)
                {
                    coas.Add(new Select2DTOString(item.DrGlAccount, item.DrGlAccount + " - " + item.DrGlAccountDesc));
                }
            }

            return Json(new { coas }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSelectedBudgetCoa(long? id)
        {
            var coa = db.BudgetDetailViews.Find(id);

            if (coa != null)
            {
                return Json(new
                {
                    id = coa.ID,
                    text = coa.GlAccount,
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = false
            });
        }

        public JsonResult GetGlAccount(string glAccount, string receiptType, int? pvId)
        {
            if (receiptType == "Normal Receipt")
            {
                var glAccountDesc = db.JournalTypeViews.Where(a => a.CrCoa == glAccount).Select(a => a.CrCoaDesc).FirstOrDefault();
                return Json(new
                {
                    success = true,
                    GlAccountDesc = glAccountDesc,
                    ExpensedAmount = 0
                }, JsonRequestBehavior.AllowGet);
            }
            else if (receiptType == "Return Receipt")
            {
                var voucherDetail = db.VoucherDetails.Where(a => a.PaymentVoucherId == pvId && a.DrGlAccount == glAccount).FirstOrDefault();
                if (voucherDetail != null)
                {
                    return Json(new
                    {
                        success = true,
                        GlAccountDesc = voucherDetail.DrGlAccountDesc,
                        ExpensedAmount = voucherDetail.OperationalAmount
                    }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new
            {
                success = false,
                GlAccountDesc = "",
                ExpensedAmount = 0
            }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> RejectDummyReceipt(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Receipt receipt = await db.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return HttpNotFound();
            }

            var rejectReasonVM = new RejectReasonVM
            {
                Id = receipt.ReceiptId,
                Remark = receipt.RejectionReason,
                OverallStatus = "REJECTED"
            };

            return PartialView("_RejectReason", rejectReasonVM);
        }

        [HttpPost]
        public JsonResult RejectDummyReceipt(RejectReasonVM rejectReasonVM)
        {
            string response;
            try
            {
                var receipt = db.Receipts.Find(rejectReasonVM.Id);
                receipt.OverallStatus = rejectReasonVM.OverallStatus;
                receipt.RejectionReason = rejectReasonVM.Remark;
                receipt.RejectedBy = User.Identity.Name;
                receipt.RejectedAt = DateTime.Now;

                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "Error";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
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
