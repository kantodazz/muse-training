using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class ContractController : Controller
    {
        // GET: IFMISTZ/Contract
        private IFMISTZDbContext db = new IFMISTZDbContext();
        [Authorize(Roles = "Contract Entry")]
        public ActionResult CreateContract()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
            .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
              && a.SubBudgetClass != null && a.SubBudgetClass != "101" && a.SubBudgetClass != "303")
            .OrderBy(a => a.SubBudgetClass)
             .Select(s => new
             {
                 SubBudgetClass = s.SubBudgetClass,
                 Description = s.SubBudgetClass + "-" + s.SubBudgetClassDesc
             }).ToList();

            var financialYear = ServiceManager.GetFinancialYear(db, DateTime.Now);
            ContractVM new_contract = new ContractVM()
            {
                VatPercentage = 18,
                SubBudgetClassList = new SelectList(subBudgetClassList, "SubBudgetClass", "Description"),
                ContractTypeList = new SelectList(db.ContractTypes, "ContractTypeName", "ContractTypeName"),
                FinancialYearsList = new SelectList(db.FinancialYears, "FinancialYearCode", "FinancialYearDesc", financialYear),
                ProcurementMethodList = new SelectList(db.ProcurementMethods, "Method", "Method"),
                ItemCategoriesList = new SelectList(db.ItemCategories, "CategoryName", "CategoryName"),
                ItemClassificationsList = new SelectList(db.ItemClassifications.Where(a => a.Status == "Active"), "ItemClassificationId", "ClassificationDesc"),
                PayeeTypesList = new SelectList(db.PayeeTypes.Where(a => (a.PayeeTypeCode == "Supplier" || a.PayeeTypeCode == "Utility" || a.PayeeTypeCode == "Contractor" || a.PayeeTypeCode == "Consultancy" || a.PayeeTypeCode == "Service Provider") && a.Status == "Active").ToList(), "PayeeTypeCode", "PayeeTypeCode"),
                UOMList = new SelectList(db.UOMs, "UomName", "UomName")
            };
            if (IsSubtresureOffice())
            {
                ViewBag.STPayments = "Yes";
            }
            return View(new_contract);


        }


        [Authorize(Roles = "Contract Entry")]
        public ActionResult ManageVariation(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

            var subBudgetClassList = db.CurrencyRateViews
        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
          && a.SubBudgetClass != null)
        .OrderBy(a => a.SubBudgetClass)
         .Select(s => new
         {
             SubBudgetClass = s.SubBudgetClass,
             Description = s.SubBudgetClass + "-" + s.SubBudgetClassDesc
         }).ToList();
            ProcurementController procurement = new ProcurementController();
            var contract = new ContractVM();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                 contract = (from p in db.Contracts
                                where institutionCodesArray.Contains(p.InstitutionCode) && p.ContractId == id
                                select new ContractVM
                                {
                                    ContractId = p.ContractId,
                                    ContractNumber = p.ContractNumber,
                                    ContractName = p.ContractName,
                                    ContractAmount = p.ContractAmount,
                                    OverallStatus = p.OverallStatus,
                                    ContractDescription = p.ContractDescription,
                                    ContractStartDate = p.ContractStartDate,
                                    ContractEndDate = p.ContractEndDate,
                                    Lotted = p.Lotted,
                                    ProcurementMethod = p.ProcurementMethod,
                                    LotNo = p.LotNo,
                                    LotDescription = p.LotDescription,
                                    PayeeDetailId = p.PayeeDetailId,
                                    ContractType = p.ContractType,
                                    Payeename = p.Payeename,
                                    PayeeType = p.PayeeType,
                                    VatPercentage = 18,
                                    SubBudgetClass = p.SubBudgetClass,
                                    VariationReason = p.VariationReason,
                                    FinancialYear = p.FinancialYear,
                                    OperationalCurrency = p.OperationalCurrency
                                }
                           ).FirstOrDefault();
            }
            else
            {
                 contract = (from p in db.Contracts
                                where p.InstitutionCode == userPaystation.InstitutionCode && p.ContractId == id
                                select new ContractVM
                                {
                                    ContractId = p.ContractId,
                                    ContractNumber = p.ContractNumber,
                                    ContractName = p.ContractName,
                                    ContractAmount = p.ContractAmount,
                                    OverallStatus = p.OverallStatus,
                                    ContractDescription = p.ContractDescription,
                                    ContractStartDate = p.ContractStartDate,
                                    ContractEndDate = p.ContractEndDate,
                                    Lotted = p.Lotted,
                                    ProcurementMethod = p.ProcurementMethod,
                                    LotNo = p.LotNo,
                                    LotDescription = p.LotDescription,
                                    PayeeDetailId = p.PayeeDetailId,
                                    ContractType = p.ContractType,
                                    Payeename = p.Payeename,
                                    PayeeType = p.PayeeType,
                                    VatPercentage = 18,
                                    SubBudgetClass = p.SubBudgetClass,
                                    VariationReason = p.VariationReason,
                                    FinancialYear = p.FinancialYear,
                                    OperationalCurrency = p.OperationalCurrency
                                }
                           ).FirstOrDefault();
            }
              

            contract.FinancialYearsList = new SelectList(db.FinancialYears, "FinancialYearCode", "FinancialYearDesc", contract.FinancialYear);
            contract.SubBudgetClassList = new SelectList(subBudgetClassList, "SubBudgetClass", "Description", contract.SubBudgetClass);
            contract.ContractTypeList = new SelectList(db.ContractTypes, "ContractTypeName", "ContractTypeName", contract.ContractType);
            contract.ItemCategoriesList = new SelectList(db.ItemCategories, "CategoryName", "CategoryName");
            contract.ItemClassificationsList = new SelectList(db.ItemClassifications.Where(a => a.Status == "Active"), "ItemClassificationId", "ClassificationDesc");
            contract.UOMList = new SelectList(db.UOMs, "UomName", "UomName");
            contract.ProcurementMethodList = new SelectList(db.ProcurementMethods, "Method", "Method", contract.ProcurementMethod);
            contract.PayeeTypesList = new SelectList(db.PayeeTypes.Where(a => (a.PayeeTypeCode == "Supplier" || a.PayeeTypeCode == "Utility" || a.PayeeTypeCode == "Contractor" || a.PayeeTypeCode == "Consultancy" || a.PayeeTypeCode == "Service Provider") && a.Status == "Active").ToList(), "PayeeTypeCode", "PayeeTypeCode", contract.PayeeType);
            contract.PaymentSchedules = (from p in db.PaymentSchedules
                                         where p.ContractId == contract.ContractId
                                         select new PaymentScheduleVM
                                         {
                                             PaymentScheduleId = p.PaymentScheduleId,
                                             Amount = p.Amount,
                                             Description = p.Description,
                                             Deliverable = p.Deliverable,
                                             FinancialYearDesc = db.FinancialYears.Where(a => a.FinancialYearCode == p.FinancialYear).Select(a => a.FinancialYearDesc).FirstOrDefault()
                                         }).ToList();

            contract.ItemsList = (from p in db.ContractDetails
                                  join q in db.ItemClassifications on p.ItemClassificationId equals q.ItemClassificationId
                                  join s in db.PaymentSchedules on p.PaymentScheduleId equals s.PaymentScheduleId
                                  where p.ContractId == contract.ContractId && p.Status != "Cancelled"
                                  select new { p, q, s } into r
                                  select new PurchaseOrderDetailVM
                                  {
                                      ContractDetailId = r.p.ContractDetailId,
                                      ItemClassificationId = (int)r.p.ItemClassificationId,
                                      PaymentScheduleId = r.s.PaymentScheduleId,
                                      PaymentScheduleDesc = r.s.Description,
                                      ItemCategory = r.q.ItemCategory,
                                      ItemDesc = r.p.ItemDesc,
                                      Quantity = r.p.Quantity,
                                      UOM = r.p.UOM,
                                      UnitPrice = r.p.UnitPrice,
                                      VAT = r.p.VAT,
                                      TotalAmount = (Decimal)r.p.TotalAmount
                                  }
                         ).OrderBy(a => a.PaymentScheduleId).ToList();

            return View(contract);


        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Contract Entry")]
        public JsonResult SaveContract(ContractSummaryVM contactSummaryVM)
        {
            string response = null;
            int? contract_id = 0;
            string currency = null;
            decimal schedule_balance = 0;
            decimal item_balance = 0;
            string contract_no = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                var currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == contactSummaryVM.SubBudgetClass && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                if (currencyRateDetail == null)
                {
                    response = "SetupProblem";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                var payeeInfo = db.PayeeBankViews.Where(a => a.PayeeDetailId == contactSummaryVM.PayeeDetailId && a.OverallStatus.ToUpper() == "APPROVED" && a.PayeeOverallStatus.ToUpper() == "ACTIVE").FirstOrDefault();
                decimal? baseAmount = contactSummaryVM.ContractAmount * currencyRateDetail.OperationalExchangeRate;

                if (contactSummaryVM.ContractId == null)
                {
                    Contract contract = new Contract()
                    {
                        PayeeDetailId = contactSummaryVM.PayeeDetailId,
                        PayeeCode = payeeInfo.PayeeCode,
                        Payeename = contactSummaryVM.Payeename,
                        PayeeBankAccount = payeeInfo.Accountnumber,
                        PayeeBankName = payeeInfo.BankName,
                        PayeeAccountName = payeeInfo.AccountName,
                        PayeeAddress = payeeInfo.Address1,
                        PayeeBIC = payeeInfo.BIC,
                        PayeeType = contactSummaryVM.PayeeType,
                        ContractNumber = contactSummaryVM.ContractNumber.Trim(),
                        ContractName = contactSummaryVM.ContractName.Trim(),
                        ContractDescription = contactSummaryVM.ContractDescription.Trim(),
                        ContractAmount = contactSummaryVM.ContractAmount,
                        BaseAmount = baseAmount,
                        ContractType = contactSummaryVM.ContractType,
                        ContractStartDate = contactSummaryVM.ContractStartDate,
                        ContractEndDate = contactSummaryVM.ContractEndDate,
                        Lotted = contactSummaryVM.Lotted,
                        ProcurementMethod = contactSummaryVM.ProcurementMethod,
                        SubBudgetClass = currencyRateDetail.SubBudgetClass,
                        LotNo = contactSummaryVM.LotNo,
                        LotDescription = contactSummaryVM.LotDescription,
                        CurrentExchangeRate = currencyRateDetail.OperationalExchangeRate,
                        ExchangeRateDate = currencyRateDetail.ExchangeRateDate,
                        OperationalCurrencyId = currencyRateDetail.OperationalCurrencyId,
                        BaseCurrency = "TZS",
                        OperationalCurrency = currencyRateDetail.OperationalCurrencyCode,
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now,
                        JournalTypeCode = "PO",
                        OverallStatus = "Pending",
                        GLStatus = "Pending",
                        InstitutionName = userPaystation.Institution.InstitutionName,
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionId = userPaystation.InstitutionId,
                        SubLevelCategory = userPaystation.SubLevelCategory,
                        SubLevelCode = userPaystation.SubLevelCode,
                        SubLevelDesc = userPaystation.SubLevelDesc,
                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                        PaystationId = userPaystation.InstitutionSubLevelId,
                        ContractVersion = 2
                    };

                    db.Contracts.Add(contract);
                    db.SaveChanges();
                    trans.Commit();
                        var savedContract = db.Contracts.Find(contract.ContractId);
                    savedContract.ContractNo = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "CO", contract.ContractId);
                    contract_no = savedContract.ContractNo;
                    contract_id = contract.ContractId;
                    currency = contract.OperationalCurrency;
                    if (contactSummaryVM.StPayment == "YES")
                    {
                        savedContract.StPaymentFlag = true;
                        savedContract.ParentInstitutionCode = contactSummaryVM.ParentInstitutionCode;
                        savedContract.ParentInstitutionName = db.InstitutionSubWarrantHolders.Where(a => a.ParentInstitutionCode == contactSummaryVM.ParentInstitutionCode).Select(a => a.ParentInstitutionName).FirstOrDefault();
                        savedContract.SubWarrantCode = contactSummaryVM.SubWarrantCode;
                        savedContract.SubWarrantDescription = db.InstitutionSubWarrantHolders.Where(a => a.SubWarrantCode == contactSummaryVM.SubWarrantCode).Select(a => a.SubWarrantDescription).FirstOrDefault();
                    }
                    db.Entry(savedContract).State = EntityState.Modified;
                    db.SaveChanges();
                    trans.Commit();

                     response = "Success";

                }
                else
                {
                    Contract contract = db.Contracts.Find(contactSummaryVM.ContractId);
                    decimal? previousExchangeRate = contract.CurrentExchangeRate;
                    contract.PayeeDetailId = contactSummaryVM.PayeeDetailId;
                    contract.Payeename = contactSummaryVM.Payeename;
                    contract.PayeeCode = payeeInfo.PayeeCode;
                    contract.PayeeBankAccount = payeeInfo.Accountnumber;
                    contract.PayeeBankName = payeeInfo.BankName;
                    contract.PayeeAccountName = payeeInfo.AccountName;
                    contract.PayeeAddress = payeeInfo.Address1;
                    contract.PayeeBIC = payeeInfo.BIC;
                    contract.PayeeType = contactSummaryVM.PayeeType;
                    contract.ContractNumber = contactSummaryVM.ContractNumber;
                    contract.ContractName = contactSummaryVM.ContractName;
                    contract.ContractDescription = contactSummaryVM.ContractDescription;
                    contract.ContractAmount = contactSummaryVM.ContractAmount;
                    contract.BaseAmount = baseAmount;
                    contract.ContractType = contactSummaryVM.ContractType;
                    if (contactSummaryVM.ContractStartDate != null)
                    {
                        contract.ContractStartDate = contactSummaryVM.ContractStartDate;
                    }
                    if (contactSummaryVM.ContractEndDate != null)
                    {
                        contract.ContractEndDate = contactSummaryVM.ContractEndDate;
                    }

                    contract.Lotted = contactSummaryVM.Lotted;
                    contract.LotNo = contactSummaryVM.LotNo;
                    contract.LotDescription = contactSummaryVM.LotDescription;
                    contract.ProcurementMethod = contactSummaryVM.ProcurementMethod;
                    contract.SubBudgetClass = currencyRateDetail.SubBudgetClass;
                    contract.CurrentExchangeRate = currencyRateDetail.OperationalExchangeRate;
                    contract.ExchangeRateDate = currencyRateDetail.ExchangeRateDate;
                    contract.OperationalCurrencyId = currencyRateDetail.OperationalCurrencyId;
                    contract.OperationalCurrency = currencyRateDetail.OperationalCurrencyCode;
                    if (contactSummaryVM.StPayment == "YES")
                    {
                        contract.StPaymentFlag = true;
                        contract.ParentInstitutionCode = contactSummaryVM.ParentInstitutionCode;
                        contract.ParentInstitutionName = db.InstitutionSubWarrantHolders.Where(a => a.ParentInstitutionCode == contactSummaryVM.ParentInstitutionCode).Select(a => a.ParentInstitutionName).FirstOrDefault();
                        contract.SubWarrantCode = contactSummaryVM.SubWarrantCode;
                        contract.SubWarrantDescription = db.InstitutionSubWarrantHolders.Where(a => a.SubWarrantCode == contactSummaryVM.SubWarrantCode).Select(a => a.SubWarrantDescription).FirstOrDefault();
                    }
                    else
                    {
                        contract.StPaymentFlag = false;
                        contract.ParentInstitutionCode = null;
                        contract.ParentInstitutionName = null;
                        contract.SubWarrantCode = null;
                        contract.SubWarrantDescription = null;
                    }

                    db.Entry(contract).State = EntityState.Modified;
                    db.SaveChanges();
                    trans.Commit();
                    contract_no = contract.ContractNo;
                    response = "Updated";
                    contract_id = contract.ContractId;
                    currency = contract.OperationalCurrency;
                    item_balance = contract.ContractAmount - (decimal)db.ContractDetails.Where(a => a.ContractId == contract.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                    schedule_balance = contract.ContractAmount - (decimal)db.PaymentSchedules.Where(a => a.ContractId == contract.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                    //Update items if Exchange rate altered
                    if (previousExchangeRate != contract.CurrentExchangeRate)
                    {
                        var paymentScheduleList = GetPaymentScheduleList(contract.ContractId);

                        var itemsList = GetAllItems(contract.ContractId);
                        if (itemsList.Count() > 0)
                        {
                            foreach (var item in itemsList)
                            {
                                ContractDetail contractDetail = db.ContractDetails.Find(item.ContractDetailId);
                                if (contractDetail != null)
                                {
                                    decimal? itemBaseAmount = contract.CurrentExchangeRate * contractDetail.TotalAmount;
                                    contractDetail.BaseAmount = itemBaseAmount;
                                    db.Entry(contractDetail).State = EntityState.Modified;
                                }
                            }

                        }
                        if (paymentScheduleList.Count() > 0)
                        {
                            foreach (var item in paymentScheduleList)
                            {
                                PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(item.PaymentScheduleId);
                                if (paymentSchedule != null)
                                {
                                    decimal? psBaseAmount = contract.CurrentExchangeRate * paymentSchedule.Amount;
                                    paymentSchedule.BaseAmount = psBaseAmount;
                                    db.Entry(paymentSchedule).State = EntityState.Modified;
                                }
                            }
                            db.SaveChanges();
                            trans.Commit();
                            }
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);

               trans.Rollback();
               response = "DbException";
            }
              }
            var result_data = new { response = response, id = contract_id, currency = currency, itembalance = item_balance, schdbalance = schedule_balance, contractno = contract_no };
            return Json(result_data, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Contract Entry")]
        public ActionResult EditContract(int? id)
        {

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var subBudgetClassList = db.CurrencyRateViews
        .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
          && a.SubBudgetClass != null && a.SubBudgetClass != "101" && a.SubBudgetClass != "303")
        .OrderBy(a => a.SubBudgetClass)
         .Select(s => new
         {
             SubBudgetClass = s.SubBudgetClass,
             Description = s.SubBudgetClass + "-" + s.SubBudgetClassDesc
         }).ToList();
            ProcurementController procurement = new ProcurementController();
            var contract = new ContractVM();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                contract = (from p in db.Contracts
                            where institutionCodesArray.Contains(p.InstitutionCode) && (p.OverallStatus == "Pending" || p.OverallStatus == "Rejected") && p.ContractId == id
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                OverallStatus = p.OverallStatus,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                Lotted = p.Lotted,
                                ProcurementMethod = p.ProcurementMethod,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                PayeeDetailId = p.PayeeDetailId,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                PayeeType = p.PayeeType,
                                VatPercentage = 18,
                                FinancialYear = p.FinancialYear,
                                SubBudgetClass = p.SubBudgetClass,
                                OperationalCurrency = p.OperationalCurrency,
                                IsStPayment = p.StPaymentFlag,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                SubWarrantCode = p.SubWarrantCode
                            }
                         ).FirstOrDefault();
            }
            else
            {
                contract = (from p in db.Contracts
                            where p.InstitutionCode == userPaystation.InstitutionCode && (p.OverallStatus == "Pending" || p.OverallStatus == "Rejected") && p.ContractId == id
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                OverallStatus = p.OverallStatus,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                Lotted = p.Lotted,
                                ProcurementMethod = p.ProcurementMethod,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                PayeeDetailId = p.PayeeDetailId,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                PayeeType = p.PayeeType,
                                VatPercentage = 18,
                                FinancialYear = p.FinancialYear,
                                SubBudgetClass = p.SubBudgetClass,
                                OperationalCurrency = p.OperationalCurrency,
                                IsStPayment = p.StPaymentFlag,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                SubWarrantCode = p.SubWarrantCode
                            }
                         ).FirstOrDefault();
            }
            
            if (contract == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                //End Only for Sub Treasure offices
                if (IsSubtresureOffice())
                {
                    ViewBag.STPayments = "Yes";

                    var parentInstitutionList = db.InstitutionSubWarrantHolders
                          .Where(a => a.StInstitutionCode == userPaystation.InstitutionCode)
                           .Select(s => new
                           {
                               ParentInstitutionCode = s.ParentInstitutionCode,
                               Description = s.ParentInstitutionCode + "-" + s.ParentInstitutionName
                           }).ToList();

                    if (contract.IsStPayment)
                    {
                        var subWarrantList = db.InstitutionSubWarrantHolders
                                            .Where(a => a.ParentInstitutionCode == contract.ParentInstitutionCode)
                                             .Select(s => new
                                             {
                                                 SubWarrantCode = s.SubWarrantCode,
                                                 Description = s.SubWarrantCode + "-" + s.SubWarrantDescription
                                             }).ToList();
                        contract.ParentInstitutionsList = new SelectList(parentInstitutionList, "ParentInstitutionCode", "Description", contract.ParentInstitutionCode);
                        contract.SubWarrantsList = new SelectList(subWarrantList, "SubWarrantCode", "Description", contract.SubWarrantCode);
                    }
                    else
                    {
                        contract.ParentInstitutionsList = new SelectList(parentInstitutionList, "ParentInstitutionCode", "Description");
                    }


                }
                //End Only for Sub Treasure offices

                contract.FinancialYearsList = new SelectList(db.FinancialYears, "FinancialYearCode", "FinancialYearDesc", contract.FinancialYear);
                contract.SubBudgetClassList = new SelectList(subBudgetClassList, "SubBudgetClass", "Description", contract.SubBudgetClass);
                contract.ContractTypeList = new SelectList(db.ContractTypes, "ContractTypeName", "ContractTypeName", contract.ContractType);
                contract.ItemCategoriesList = new SelectList(db.ItemCategories, "CategoryName", "CategoryName");
                contract.ItemClassificationsList = new SelectList(db.ItemClassifications.Where(a => a.Status == "Active"), "ItemClassificationId", "ClassificationDesc");
                contract.UOMList = new SelectList(db.UOMs, "UomName", "UomName");
                contract.ProcurementMethodList = new SelectList(db.ProcurementMethods, "Method", "Method", contract.ProcurementMethod);
                contract.PayeeTypesList = new SelectList(db.PayeeTypes.Where(a => (a.PayeeTypeCode == "Supplier" || a.PayeeTypeCode == "Utility" || a.PayeeTypeCode == "Contractor" || a.PayeeTypeCode == "Consultancy" || a.PayeeTypeCode == "Service Provider") && a.Status == "Active").ToList(), "PayeeTypeCode", "PayeeTypeCode", contract.PayeeType);
                contract.PaymentSchedules = (from p in db.PaymentSchedules
                                             where p.ContractId == contract.ContractId
                                             select new PaymentScheduleVM
                                             {
                                                 PaymentScheduleId = p.PaymentScheduleId,
                                                 Amount = p.Amount,
                                                 Description = p.Description,
                                                 Deliverable = p.Deliverable,
                                                 FinancialYearDesc = db.FinancialYears.Where(a => a.FinancialYearCode == p.FinancialYear).Select(a => a.FinancialYearDesc).FirstOrDefault()
                                             }).ToList();

                contract.ItemsList = (from p in db.ContractDetails
                                      join q in db.ItemClassifications on p.ItemClassificationId equals q.ItemClassificationId
                                      join s in db.PaymentSchedules on p.PaymentScheduleId equals s.PaymentScheduleId
                                      where p.ContractId == contract.ContractId && p.Status != "Cancelled"
                                      select new { p, q, s } into r
                                      select new PurchaseOrderDetailVM
                                      {
                                          ContractDetailId = r.p.ContractDetailId,
                                          ItemClassificationId = (int)r.p.ItemClassificationId,
                                          PaymentScheduleId = r.s.PaymentScheduleId,
                                          PaymentScheduleDesc = r.s.Description,
                                          ItemCategory = r.q.ItemCategory,
                                          ItemDesc = r.p.ItemDesc,
                                          Quantity = r.p.Quantity,
                                          UOM = r.p.UOM,
                                          VatStatus = r.p.VatStatus,
                                          OverheadPercentage = r.p.OverheadPercentage,
                                          UnitPrice = r.p.UnitPrice,
                                          VAT = r.p.VAT,
                                          TotalAmount = (Decimal)r.p.TotalAmount
                                      }
                             ).OrderBy(a => a.PaymentScheduleId).ToList();

                return View(contract);
            }


        }
        [Authorize(Roles = "Contract Entry,Contract Approval")]
        public JsonResult CancelContract(int? id)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {

                Contract contract = db.Contracts.Find(id);
                if (contract.OverallStatus == "Pending" || contract.OverallStatus == "Rejected")
                {
                    contract.OverallStatus = "Cancelled";
                    contract.ApprovalStatus = "Cancelled";
                    contract.CancelledBy = User.Identity.Name;
                    contract.CancelledAt = DateTime.Now;
                    db.Entry(contract).State = EntityState.Modified;
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";
                }
                else
                {
                    response = "This contract can not be cancelled since overall status is not pending";
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

        public List<ContractVM> GetContractEntries(string institutionCode, string[] StatusArray)
        {
            ProcurementController procurement = new ProcurementController();
            List<ContractVM> contractsList = new List<ContractVM>();
            if (procurement.IsTarura(institutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(institutionCode);
                contractsList = (from p in db.Contracts
                                where institutionCodesArray.Contains(p.InstitutionCode) && StatusArray.Contains(p.OverallStatus) && p.VariationStatus !="Pending"
                                select new ContractVM
                                {
                                    ContractId = p.ContractId,
                                    ContractNo = p.ContractNo,
                                    ContractNumber = p.ContractNumber,
                                    ContractAmount = p.ContractAmount,
                                    Currency = p.OperationalCurrency,
                                    OverallStatus = p.OverallStatus,
                                    PaymentScheduleAmount = db.PaymentSchedules.Where(a => a.ContractId == p.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum(),
                                    TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum(),
                                    CountItems = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.Quantity).DefaultIfEmpty(0).Sum(),
                                    Payeename = p.Payeename,
                                    ContractDescription = p.ContractDescription
                                }
                               ).OrderByDescending(a => a.ContractId).ToList();


            }
            else
            {

                contractsList = (from p in db.Contracts
                                 where p.InstitutionCode==institutionCode && StatusArray.Contains(p.OverallStatus) && p.VariationStatus != "Pending"
                                 select new ContractVM
                                 {
                                     ContractId = p.ContractId,
                                     ContractNo = p.ContractNo,
                                     ContractNumber = p.ContractNumber,
                                     ContractAmount = p.ContractAmount,
                                     Currency = p.OperationalCurrency,
                                     OverallStatus = p.OverallStatus,
                                     PaymentScheduleAmount = db.PaymentSchedules.Where(a => a.ContractId == p.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum(),
                                     TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum(),
                                     CountItems = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.Quantity).DefaultIfEmpty(0).Sum(),
                                     Payeename = p.Payeename,
                                     ContractDescription = p.ContractDescription
                                 }
                             ).OrderByDescending(a => a.ContractId).ToList();

            }
            return contractsList;
        }
        public List<ContractVM> GetContractEntry(string institutionCode, string[] StatusArray)
        {
            ProcurementController procurement = new ProcurementController();
            List<ContractVM> contractsList = new List<ContractVM>();
            if (procurement.IsTarura(institutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(institutionCode);
                contractsList = (from p in db.Contracts
                                 where institutionCodesArray.Contains(p.InstitutionCode) && StatusArray.Contains(p.OverallStatus) && p.VariationStatus == "Pending"
                                 select new ContractVM
                                 {
                                     ContractId = p.ContractId,
                                     ContractNo = p.ContractNo,
                                     ContractNumber = p.ContractNumber,
                                     ContractAmount = p.ContractAmount,
                                     Currency = p.OperationalCurrency,
                                     OverallStatus = p.OverallStatus,
                                     PaymentScheduleAmount = db.PaymentSchedules.Where(a => a.ContractId == p.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum(),
                                     TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum(),
                                     CountItems = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.Quantity).DefaultIfEmpty(0).Sum(),
                                     Payeename = p.Payeename,
                                     ContractDescription = p.ContractDescription
                                 }
                               ).OrderByDescending(a => a.ContractId).ToList();


            }
            else
            {

                contractsList = (from p in db.Contracts
                                 where p.InstitutionCode == institutionCode && StatusArray.Contains(p.OverallStatus) && p.VariationStatus == "Pending"
                                 select new ContractVM
                                 {
                                     ContractId = p.ContractId,
                                     ContractNo = p.ContractNo,
                                     ContractNumber = p.ContractNumber,
                                     ContractAmount = p.ContractAmount,
                                     Currency = p.OperationalCurrency,
                                     OverallStatus = p.OverallStatus,
                                     PaymentScheduleAmount = db.PaymentSchedules.Where(a => a.ContractId == p.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum(),
                                     TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum(),
                                     CountItems = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.Quantity).DefaultIfEmpty(0).Sum(),
                                     Payeename = p.Payeename,
                                     ContractDescription = p.ContractDescription
                                 }
                             ).OrderByDescending(a => a.ContractId).ToList();

            }
            return contractsList;
        }
        public List<ContractVM> GetContractData(string institutionCode, string[] StatusArray)
        {
            ProcurementController procurement = new ProcurementController();
            List<ContractVM> contractsList = new List<ContractVM>();
            if (procurement.IsTarura(institutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(institutionCode);
                contractsList = (from p in db.Contracts
                                 where institutionCodesArray.Contains(p.InstitutionCode) && StatusArray.Contains(p.OverallStatus)
                                 select new ContractVM
                                 {
                                     ContractId = p.ContractId,
                                     ContractNo = p.ContractNo,
                                     ContractNumber = p.ContractNumber,
                                     ContractAmount = p.ContractAmount,
                                     Currency = p.OperationalCurrency,
                                     OverallStatus = p.OverallStatus,
                                     TotalAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                     Payeename = p.Payeename
                                 }
                               ).OrderByDescending(a => a.ContractId).ToList();


            }
            else
            {

                contractsList = (from p in db.Contracts
                                 where p.InstitutionCode == institutionCode && StatusArray.Contains(p.OverallStatus)
                                 select new ContractVM
                                 {
                                     ContractId = p.ContractId,
                                     ContractNo = p.ContractNo,
                                     ContractNumber = p.ContractNumber,
                                     ContractAmount = p.ContractAmount,
                                     Currency = p.OperationalCurrency,
                                     OverallStatus = p.OverallStatus,
                                     TotalAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                     Payeename = p.Payeename
                                 }
                             ).OrderByDescending(a => a.ContractId).ToList();

            }
            return contractsList;
        }


        [Authorize(Roles = "Contract Entry")]
        public ActionResult PendingContract()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Pending", "Rejected" };
            var ContractList = GetContractEntries(userPaystation.InstitutionCode, StatusArray);
            ContractList = ContractList.Where(a => a.VariationStatus != "Pending").ToList();
            return View(ContractList);
        }
        [Authorize(Roles = "Contract Entry")]
        public ActionResult TerminateContract()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Approved", "Partial" };
            var ContractList = GetContractData(userPaystation.InstitutionCode, StatusArray);
            return View(ContractList);
        }
  
        [Authorize(Roles = "Contract Approval")]
        public ActionResult ApproveContract()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Confirmed" };
            var ContractList = GetContractEntries(userPaystation.InstitutionCode, StatusArray);
            return View(ContractList);
        }
        [Authorize(Roles = "Contract Approval")]
        public ActionResult TerminationApproval()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Request Void", "Request Close" };
            var ContractList = GetContractData(userPaystation.InstitutionCode, StatusArray);
            return View(ContractList);
        }
        [Authorize(Roles = "Contract Approval")]
        public ActionResult ApproveVariation()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Confirmed" };
            var ContractList = GetContractEntry(userPaystation.InstitutionCode, StatusArray);
            return View(ContractList);
        }
        [Authorize(Roles = "Contract Approval")]
        public ActionResult ApprovedContract()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string[] StatusArray = new string[] { "Approved" };
            var ContractList = GetContractEntries(userPaystation.InstitutionCode, StatusArray);

            //ViewBag.reportUrlName = ReportManager.GetReportUrl(db, "IFMISTZ");

            return View(ContractList);
        }

        [Authorize(Roles = "Contract Entry,Contract Examiner,Contract Approval,Inventory Info Entry,Inventory Info Approval,Inventory Issuing,Inventory Issuing Approval")]
        public ActionResult PreviewContract(int? id, string status)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            ProcurementController procurement = new ProcurementController();
            var contract = new ContractVM();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                contract = (from p in db.Contracts
                            where p.ContractId == id && institutionCodesArray.Contains(p.InstitutionCode)
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNo = p.ContractNo,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                SubBudgetClass = p.SubBudgetClass,
                                OverallStatus = p.OverallStatus,
                                Rejecter = p.Rejecter,
                                RejectionReason = p.RejectionReason,
                                RejectionSolution = p.RejectionSolution,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                SupplierName = p.Payeename,
                                PayeeCode = p.PayeeCode,
                                OperationalCurrency = p.OperationalCurrency,
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                VariationReason = p.VariationReason,
                                PreviousAmount = p.PreviousAmount,
                                PreviousStartDate = p.PreviousStartDate,
                                PreviousEndDate = p.PreviousEndDate,
                                ReducedBy = p.ReducedBy,
                                IncreasedBy = p.IncreasedBy,
                                VariationFileName = p.VariationFileName,
                                PerformanceBondFile = p.PerformanceBondFile,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                ParentInstitutionName = p.ParentInstitutionName,
                                SubWarrantCode = p.SubWarrantCode,
                                SubWarrantDescription = p.SubWarrantDescription
                            }
                              ).FirstOrDefault();
            }
            else
            {
                contract = (from p in db.Contracts
                            where p.ContractId == id && p.InstitutionCode == userPaystation.InstitutionCode
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNo = p.ContractNo,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                SubBudgetClass = p.SubBudgetClass,
                                OverallStatus = p.OverallStatus,
                                Rejecter = p.Rejecter,
                                RejectionReason = p.RejectionReason,
                                RejectionSolution = p.RejectionSolution,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                SupplierName = p.Payeename,
                                PayeeCode = p.PayeeCode,
                                OperationalCurrency = p.OperationalCurrency,
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                VariationReason = p.VariationReason,
                                PreviousAmount = p.PreviousAmount,
                                PreviousStartDate = p.PreviousStartDate,
                                PreviousEndDate = p.PreviousEndDate,
                                ReducedBy = p.ReducedBy,
                                IncreasedBy = p.IncreasedBy,
                                VariationFileName = p.VariationFileName,
                                PerformanceBondFile = p.PerformanceBondFile,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                ParentInstitutionName = p.ParentInstitutionName,
                                SubWarrantCode = p.SubWarrantCode,
                                SubWarrantDescription = p.SubWarrantDescription
                            }
                              ).FirstOrDefault();
            }
                

            if (contract == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                contract.Status = status;
                contract.ItemsList = (from m in db.ContractDetails
                                      join n in db.ItemClassifications on m.ItemClassificationId equals n.ItemClassificationId
                                      join p in db.PaymentSchedules on m.PaymentScheduleId equals p.PaymentScheduleId
                                      where m.ContractId == contract.ContractId && m.Status != "Cancelled"
                                      select new { m, n, p } into r
                                      select new PurchaseOrderDetailVM
                                      {
                                          PaymentScheduleId = r.p.PaymentScheduleId,
                                          PaymentScheduleDesc = r.p.Description,
                                          ItemCategory = r.n.ItemCategory,
                                          ContractDetailId = r.m.ContractDetailId,
                                          ItemDesc = r.m.ItemDesc,
                                          Quantity = r.m.Quantity,
                                          UOM = r.m.UOM,
                                          UnitPrice = r.m.UnitPrice,
                                          VAT = r.m.VAT,
                                          OverheadPercentage = r.m.OverheadPercentage,
                                          TotalAmount = (Decimal)r.m.TotalAmount
                                      }
                          ).OrderBy(a => a.PaymentScheduleId).ToList();
                contract.ContractDetails = db.ContractDetails.Where(a => a.ContractId == contract.ContractId && a.Status != "Cancelled").ToList();
                contract.ContractCoas = db.ContractCoas.Where(a => a.ContractId == contract.ContractId).ToList();
            }
            return View(contract);
        }

        [Authorize(Roles = "Contract Entry,Contract Examiner,Contract Approval")]
        public ActionResult ContractPreview(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            ProcurementController procurement = new ProcurementController();
            var contract = new ContractVM();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                contract = (from p in db.Contracts
                            where p.ContractId == id && institutionCodesArray.Contains(p.InstitutionCode)
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNo = p.ContractNo,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                SubBudgetClass = p.SubBudgetClass,
                                OverallStatus = p.OverallStatus,
                                Rejecter = p.Rejecter,
                                RejectionReason = p.RejectionReason,
                                RejectionSolution = p.RejectionSolution,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                PayeeCode = p.PayeeCode,
                                OperationalCurrency = p.OperationalCurrency,
                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                VariationReason = p.VariationReason,
                                PreviousAmount = p.PreviousAmount,
                                PreviousStartDate = p.PreviousStartDate,
                                PreviousEndDate = p.PreviousEndDate,
                                ReducedBy = p.ReducedBy,
                                IncreasedBy = p.IncreasedBy,
                                VariationFileName = p.VariationFileName,
                                PerformanceBondFile = p.PerformanceBondFile,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                ParentInstitutionName = p.ParentInstitutionName,
                                SubWarrantCode = p.SubWarrantCode,
                                SubWarrantDescription = p.SubWarrantDescription
                            }
                              ).FirstOrDefault();
            }
            else
            {
                contract = (from p in db.Contracts
                            where p.ContractId == id && p.InstitutionCode == userPaystation.InstitutionCode
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNo = p.ContractNo,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                SubBudgetClass = p.SubBudgetClass,
                                OverallStatus = p.OverallStatus,
                                Rejecter = p.Rejecter,
                                RejectionReason = p.RejectionReason,
                                RejectionSolution = p.RejectionSolution,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                PayeeCode = p.PayeeCode,
                                OperationalCurrency = p.OperationalCurrency,
                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                VariationReason = p.VariationReason,
                                PreviousAmount = p.PreviousAmount,
                                PreviousStartDate = p.PreviousStartDate,
                                PreviousEndDate = p.PreviousEndDate,
                                ReducedBy = p.ReducedBy,
                                IncreasedBy = p.IncreasedBy,
                                VariationFileName = p.VariationFileName,
                                PerformanceBondFile = p.PerformanceBondFile,
                                ParentInstitutionCode = p.ParentInstitutionCode,
                                ParentInstitutionName = p.ParentInstitutionName,
                                SubWarrantCode = p.SubWarrantCode,
                                SubWarrantDescription = p.SubWarrantDescription
                            }
                              ).FirstOrDefault();

            }
                

            if (contract == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                contract.ItemsList = (from m in db.ContractDetails
                                      join n in db.ItemClassifications on m.ItemClassificationId equals n.ItemClassificationId
                                      join p in db.PaymentSchedules on m.PaymentScheduleId equals p.PaymentScheduleId
                                      where m.ContractId == contract.ContractId && m.Status != "Cancelled"
                                      select new { m, n, p } into r
                                      select new PurchaseOrderDetailVM
                                      {
                                          PaymentScheduleId = r.p.PaymentScheduleId,
                                          PaymentScheduleDesc = r.p.Description,
                                          ItemCategory = r.n.ItemCategory,
                                          ContractDetailId = r.m.ContractDetailId,
                                          ItemDesc = r.m.ItemDesc,
                                          Quantity = r.m.Quantity,
                                          UOM = r.m.UOM,
                                          UnitPrice = r.m.UnitPrice,
                                          VAT = r.m.VAT,
                                          OverheadPercentage = r.m.OverheadPercentage,
                                          TotalAmount = (Decimal)r.m.TotalAmount
                                      }
                          ).OrderBy(a => a.PaymentScheduleId).ToList();
                contract.ContractDetails = db.ContractDetails.Where(a => a.ContractId == contract.ContractId && a.Status != "Cancelled").ToList();
                contract.ContractCoas = db.ContractCoas.Where(a => a.ContractId == contract.ContractId).ToList();
            }
            return View(contract);
        }
        [Authorize(Roles = "Contract Examiner")]
        public ActionResult ExamineContract(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var contract = (from p in db.Contracts
                            where p.ContractId == id && p.InstitutionCode == userPaystation.InstitutionCode
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                SubBudgetClass = p.SubBudgetClass,
                                OverallStatus = p.OverallStatus,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                OperationalCurrency = p.OperationalCurrency,
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription
                            }
                               ).FirstOrDefault();

            if (contract == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                contract.ItemsList = (from m in db.ContractDetails
                                      join n in db.ItemClassifications on m.ItemClassificationId equals n.ItemClassificationId
                                      join p in db.PaymentSchedules on m.PaymentScheduleId equals p.PaymentScheduleId
                                      where m.ContractId == contract.ContractId
                                      select new { m, n, p } into r
                                      select new PurchaseOrderDetailVM
                                      {
                                          PaymentScheduleId = r.p.PaymentScheduleId,
                                          PaymentScheduleDesc = r.p.Description,
                                          ItemCategory = r.n.ItemCategory,
                                          ContractDetailId = r.m.ContractDetailId,
                                          ItemDesc = r.m.ItemDesc,
                                          Quantity = r.m.Quantity,
                                          UOM = r.m.UOM,
                                          UnitPrice = r.m.UnitPrice,
                                          VAT = r.m.VAT,
                                          TotalAmount = (Decimal)r.m.TotalAmount
                                      }
                          ).OrderBy(a => a.PaymentScheduleId).ToList();
                contract.ContractCoas = db.ContractCoas.Where(a => a.ContractId == contract.ContractId).ToList();
            }
            return View(contract);
        }

        [Authorize(Roles = "Contract Entry")]
        public JsonResult InsertPaymentSchedule(PaymentSchedule paymentSchedule)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                Contract contract = db.Contracts.Find(paymentSchedule.ContractId);
                decimal prev_entered = db.PaymentSchedules.Where(a => a.ContractId == paymentSchedule.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                decimal Total = prev_entered + paymentSchedule.Amount;
                if (Total <= contract.ContractAmount)
                {
                    decimal? psBaseAmount = contract.CurrentExchangeRate * paymentSchedule.Amount;
                    paymentSchedule.Balance = paymentSchedule.Amount;
                    paymentSchedule.BaseAmount = psBaseAmount;
                    db.PaymentSchedules.Add(paymentSchedule);
                    db.SaveChanges();
                    trans.Commit();
                    var paymentScheduleList = GetPaymentSchedules(paymentSchedule.ContractId);
                    response = "Success";
                    var result_data = new { response = response, paymentScheduleList = paymentScheduleList };
                    return Json(result_data, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    response = "Exceed";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                trans.Rollback();
                return Json(response, JsonRequestBehavior.AllowGet);
            }
           }

        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult SaveContractItem(PurchaseOrderDetailVM model)
        {
            string response = null;

                using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                    decimal prev_amnt_entered = Convert.ToDecimal(db.ContractDetails.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum());
                    decimal total = prev_amnt_entered + model.TotalAmount;
                    if (total <= paymentSchedule.Amount)
                    {
                        decimal? vat_amount = 0;
                        string vat_status = null;
                        decimal amount = (Decimal)(model.Quantity * model.UnitPrice);
                        if (model.VatApplicable == "YES")
                        {
                            vat_status = "Applicable";
                            vat_amount = model.TotalAmount - model.Quantity * model.UnitPrice;
                        }
                        else
                        {
                            vat_status = "Excempted";
                            vat_amount = 0;

                        }
                        var class_id = db.ItemClassifications.Where(a => a.ItemClassificationId == model.ItemClassificationId).Select(a => a.ClassId).FirstOrDefault();
                        if (class_id >= 1 && class_id <= 3)
                        {
                            int search_item = db.Items.Where(a => a.ItemClassificationId == model.ItemClassificationId && a.ItemDescription.ToUpper() == model.ItemDesc.ToUpper()).Count();
                            if (search_item == 0)
                            {
                                Item item = new Item()
                                {
                                    ItemClassificationId = model.ItemClassificationId,
                                    ItemDescription = model.ItemDesc,
                                    Status = "Active"
                                };
                                db.Items.Add(item);
                            }
                        }
                        Contract contract = db.Contracts.Find(paymentSchedule.ContractId);
                        decimal? itemBaseAmount = contract.CurrentExchangeRate * model.TotalAmount;
                        ContractDetail details = new ContractDetail()
                        {
                            ContractId = model.ContractId,
                            ItemClassificationId = (int)model.ItemClassificationId,
                            ClassId = class_id,
                            Quantity = model.Quantity,
                            UnitPrice = model.UnitPrice,
                            OverheadPercentage = model.OverheadPercentage,
                            UOM = model.UOM,
                            ItemDesc = model.ItemDesc,
                            VAT = vat_amount,
                            VatStatus = vat_status,
                            ItemsCharged = "No",
                            TotalAmount = model.TotalAmount,
                            BaseAmount = itemBaseAmount,
                            PaymentScheduleId = model.PaymentScheduleId,

                        };


                        db.ContractDetails.Add(details);
                        var payment_schedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                        payment_schedule.Balance = paymentSchedule.Amount - total;
                        db.Entry(payment_schedule).State = EntityState.Modified;
                        db.SaveChanges();
                        trans.Commit();
                        var itemsList = GetAllItems(model.ContractId);
                        var paymentScheduleList = GetPaymentScheduleList(model.ContractId);
                        response = "Success";
                        var result_data = new { response = response, itemsList = itemsList, paymentScheduleList = paymentScheduleList };
                        return Json(result_data, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        response = "Exceed";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }


                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    trans.Rollback();
                    response = "DbException";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
            }
          
        }

        [Authorize(Roles = "Contract Entry")]
        public JsonResult UpdateItem(PurchaseOrderDetailVM model)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                PaymentSchedule schedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                decimal schedule_amount = db.PaymentSchedules.Where(a => a.PaymentScheduleId == model.PaymentScheduleId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                ContractDetail contractDetail = db.ContractDetails.Find(model.ContractDetailId);
                decimal? prev_amount = db.ContractDetails.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                decimal total = (Decimal)(prev_amount + model.TotalAmount - contractDetail.TotalAmount);
                decimal amount = (Decimal)contractDetail.TotalAmount;
                if (total <= schedule.Amount)
                {
                    decimal? vat_amount = 0;
                    string vat_status = null;
                    if (model.VatApplicable == "YES")
                    {
                        vat_status = "Applicable";
                        vat_amount = model.TotalAmount - model.Quantity * model.UnitPrice;
                    }
                    else
                    {
                        vat_status = "Excempted";
                        vat_amount = 0;

                    }

                    Contract contract = db.Contracts.Find(schedule.ContractId);
                    decimal? itemBaseAmount = contract.CurrentExchangeRate * model.TotalAmount;
                    contractDetail.ItemClassificationId = (int)model.ItemClassificationId;
                    contractDetail.ClassId = db.ItemClassifications.Where(a => a.ItemClassificationId == model.ItemClassificationId).Select(a => a.ClassId).FirstOrDefault();
                    contractDetail.Quantity = model.Quantity;
                    contractDetail.UnitPrice = model.UnitPrice;
                    contractDetail.OverheadPercentage = model.OverheadPercentage;
                    contractDetail.UOM = model.UOM;
                    contractDetail.ItemDesc = model.ItemDesc;
                    contractDetail.VAT = vat_amount;
                    contractDetail.VatStatus = vat_status;
                    contractDetail.ItemsCharged = "No";
                    contractDetail.TotalAmount = model.TotalAmount;
                    contractDetail.BaseAmount = itemBaseAmount;
                    db.Entry(contractDetail).State = EntityState.Modified;
                    schedule.Balance = schedule.Balance + amount - model.TotalAmount;
                    db.Entry(schedule).State = EntityState.Modified;
                    db.SaveChanges();
                   trans.Commit();
                   var itemsList = GetAllItems(contractDetail.ContractId);
                    var paymentScheduleList = GetPaymentScheduleList(contractDetail.ContractId);
                    response = "Success";
                    var result_data = new { response = response, itemsList = itemsList, paymentScheduleList = paymentScheduleList };
                    return Json(result_data, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    response = "Exceed";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    trans.Rollback();
                    response = "DbException";
            }
        }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Contract Entry")]
        public JsonResult DeselectGLAccount(int? id)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {

                ContractCoa coa = db.ContractCoas.Find(id);
                var idsList = db.ContractCoas.Where(a => a.ContractId == coa.ContractId).Select(a => a.ContractCoaId).ToList();
                foreach (var coa_id in idsList)
                {
                    var itemIdList = db.ItemContractCoas.Where(a => a.ContractCoaId == coa_id).Select(a => a.ContractDetailId).ToList();

                    //DELETE ContractDetailId &  CoId FROM ItemContractCoa
                    db.ItemContractCoas.RemoveRange(db.ItemContractCoas.Where(a => a.ContractCoaId == coa_id));
                    db.SaveChanges();
                    //SET ALL PREVIOUS CHARGED ITEM TO NOT CHARGED STATUS 
                    foreach (var item in itemIdList)
                    {
                        var item_detail = db.ContractDetails.Find(item);
                        item_detail.ItemsCharged = "No";
                        db.Entry(item_detail).State = EntityState.Modified;
                    }
                    Contract contract = db.Contracts.Find(coa.ContractId);
                    if (contract.GLStatus.ToUpper() == "COMPLETED")
                    {
                        contract.GLStatus = "Pending";
                        db.Entry(contract).State = EntityState.Modified;
                    }
                }
                db.ContractCoas.RemoveRange(db.ContractCoas.Where(a => a.ContractId == coa.ContractId));
                db.SaveChanges();
                trans.Commit();
                response = "Success";
                return Json(response, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                trans.Rollback();
               response = "DbException";
            }
        }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult DeleteItem(int? id)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {

                ContractDetail contractDetail = db.ContractDetails.Find(id);
                //Release amount in payment schedule
                var payment_schedule = db.PaymentSchedules.Find(contractDetail.PaymentScheduleId);
                payment_schedule.Balance = payment_schedule.Balance + contractDetail.TotalAmount;
                contractDetail.Status = "Cancelled";
                db.Entry(payment_schedule).State = EntityState.Modified;
                db.Entry(contractDetail).State = EntityState.Modified;
                db.SaveChanges();
                trans.Commit();
                var itemsList = GetAllItems(payment_schedule.ContractId);
                var paymentScheduleList = GetPaymentScheduleList(payment_schedule.ContractId);
                response = "Success";
                var result_data = new { response = response, itemsList = itemsList, paymentScheduleList = paymentScheduleList };
                return Json(result_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                trans.Rollback();
                response = "DbException";
            }
        }
            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult ItemsCharge(ReceivingCoaVM model)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                Contract contract = db.Contracts.Find(model.ContractId);
                ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(model.ReceivingSummaryId);
                string subBudgetClass = null;
                if (receivingSummary.SubBudgetClass != null)
                {
                    subBudgetClass = receivingSummary.SubBudgetClass;
                }
                else
                {
                    subBudgetClass = contract.SubBudgetClass;
                }
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                // Check for Fund Balance
                //if (receivingSummary.Accrual != "YES")
                //{
                //    foreach (ReceivingCoaCharge receivingCoa in model.ReceivingCoas)
                //    {
                //        db.Database.CommandTimeout = 120;

                //        BalanceResponse fundBalanceResponse = ServiceManager.GetFundBalance(db, userPaystation.InstitutionCode, subBudgetClass);
                //        if (fundBalanceResponse.overallStatus == "Error")
                //        {
                //            response = "Oops..! Something went wrong.";
                //            return Json(response, JsonRequestBehavior.AllowGet);
                //        }

                //        FundBalanceView fv = fundBalanceResponse.FundBalanceViewList
                //            .Where(a => a.FundingRefNo == receivingCoa.FundingReference
                //                && a.GlAccount == receivingCoa.GLAccount)
                //            .First();
                //        if (fv.FundBalance < receivingCoa.ExpenseAmount)
                //        {
                //            response = "Insufficient Fund Balance for GlAccount: "
                //          + receivingCoa.GLAccount + " Expense Amount: "
                //          + receivingCoa.ExpenseAmount + " Fund Balance: " + fv.FundBalance
                //          + " Fund Reference No: " + fv.FundingRefNo;
                //            return Json(response, JsonRequestBehavior.AllowGet);
                //        }

                //    }

                //}



                db.ReceivingCoas.RemoveRange(db.ReceivingCoas.Where(a => a.ReceivingSummaryId == model.ReceivingSummaryId));
                db.SaveChanges();

                decimal? previous_received = db.ReceivingCoas.Where(a => a.ContractId == contract.ContractId).Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();
                decimal new_amount = (decimal)model.ReceivingCoas.Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();

                if (previous_received == null || previous_received == 0)
                {
                    decimal total = new_amount;

                    if (receivingSummary.AdvancePayment > 0)
                    {
                        total = total + (decimal)receivingSummary.AdvancePayment;
                    }
                    if (total <= contract.ContractAmount)
                    {
                        List<ReceivingCoa> ReceivingCoas = new List<ReceivingCoa>();
                        foreach (var item in model.ReceivingCoas)
                        {
                            if (item.ExpenseAmount > 0)
                            {
                                ReceivingCoa receivingCoa = new ReceivingCoa
                                {
                                    ContractId = model.ContractId,
                                    ExpenseAmount = item.ExpenseAmount,
                                    ExpenseBaseAmount = item.ExpenseAmount * receivingSummary.ExchangeRate,
                                    Balance = 0,
                                    GLAccount = item.GLAccount,
                                    GLAccountDesc = item.GLAccountDesc,
                                    FundingReference = item.FundingReference,
                                    ReceivingSummaryId = model.ReceivingSummaryId,
                                    BaseAmount = item.ExpenseAmount * contract.CurrentExchangeRate
                                };

                                db.ReceivingCoas.Add(receivingCoa);
                            }
                        }
                        db.ReceivingCoas.AddRange(ReceivingCoas);
                        db.SaveChanges();
                         trans.Commit();
                        response = "Success";
                    }
                    else
                    {

                        response = "This contract previous received,this receiving cause to exceed contract Amount.Contact technical supporter";
                    }
                }
                else
                {
                    decimal totalToBe = (decimal)previous_received + new_amount;
                    //Calculate Advance payment
                    decimal totalAdvancePayment = 0;
                    decimal? total = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.AdvancePayment).DefaultIfEmpty(0).Sum();
                    if (total > 0)
                    {
                        totalAdvancePayment = (decimal)total;

                        if (receivingSummary.AdvancePayment > 0)
                        {
                            decimal current_received = (decimal)receivingSummary.AdvancePayment;
                            if (totalAdvancePayment > current_received)
                            {
                                totalAdvancePayment = totalAdvancePayment - current_received;
                            }
                        }
                    }
                    //End Calculate Advance Payment
                    if (contract.ContractVersion == 1)
                    {
                        totalToBe = totalToBe + totalAdvancePayment;
                    }
                    else
                    {
                        totalToBe = totalToBe - totalAdvancePayment;

                    }

                    if (totalToBe <= contract.ContractAmount)
                    {
                        List<ReceivingCoa> ReceivingCoas = new List<ReceivingCoa>();
                        foreach (var item in model.ReceivingCoas)
                        {
                            if (item.ExpenseAmount > 0)
                            {
                                ReceivingCoa receivingCoa = new ReceivingCoa
                                {
                                    ContractId = model.ContractId,
                                    ExpenseAmount = item.ExpenseAmount,
                                    Balance = 0,
                                    GLAccount = item.GLAccount,
                                    GLAccountDesc = item.GLAccountDesc,
                                    FundingReference = item.FundingReference,
                                    ReceivingSummaryId = model.ReceivingSummaryId,
                                    BaseAmount = item.ExpenseAmount * contract.CurrentExchangeRate
                                };

                                db.ReceivingCoas.Add(receivingCoa);
                            }
                        }
                        db.ReceivingCoas.AddRange(ReceivingCoas);
                        db.SaveChanges();
                        trans.Commit();
                        response = "Success";
                    }
                    else
                    {
                        response = "This contract previous received,this receiving cause to exceed contract Amount.Contact technical supporter";
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                    trans.Rollback();
                    response = "DbException,Please logout and login again then try again";
            }
          }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Contract Entry")]
        public JsonResult ConfirmContract(int? id)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {

                Contract contract = db.Contracts.Find(id);
                decimal? contract_amount = 0;
                decimal? payment_schedule = 0;
                decimal? items_amount = 0;
                decimal? totalPaymentSchedule = db.PaymentSchedules.Where(a => a.ContractId == id).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                decimal? totalItemValue = db.ContractDetails.Where(a => a.ContractId == id && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                if (contract.ContractAmount == totalPaymentSchedule)
                {
                    if (contract.ContractAmount == totalItemValue)
                    {
                        contract.OverallStatus = "Confirmed";
                        contract.ApprovalStatus = "Confirmed";
                        contract.ConfirmedBy = User.Identity.Name;
                        contract.ConfirmedAt = DateTime.Now;
                        db.Entry(contract).State = EntityState.Modified;
                        db.SaveChanges();
                        trans.Commit();
                        response = "Success";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }

                }
                if (contract.ContractAmount == totalPaymentSchedule)
                {
                    payment_schedule = totalPaymentSchedule;
                    items_amount = totalItemValue;
                    response = "TotalItems";
                    var data_result = new { response = response, schedule = payment_schedule, items_amount = items_amount };
                    return Json(data_result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    contract_amount = contract.ContractAmount;
                    payment_schedule = totalPaymentSchedule;
                    response = "PaymentSchedule";
                    var data_result = new { response = response, amount = contract_amount, schedule = payment_schedule };
                    return Json(data_result, JsonRequestBehavior.AllowGet);
                }


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                trans.Rollback();
              return Json(response, JsonRequestBehavior.AllowGet);
            }
          }

        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult ConfirmVariation(int? id)
        {
            string response = null;
            try
            {
                Contract contract = db.Contracts.Find(id);
                int count = checkAttach("ContractVariation", contract.ContractId);
                if (count > 0)
                {
                    decimal? contract_amount = 0;
                    decimal? payment_schedule = 0;
                    decimal? items_amount = 0;
                    decimal? totalPaymentSchedule = db.PaymentSchedules.Where(a => a.ContractId == id).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                    decimal? totalItemValue = db.ContractDetails.Where(a => a.ContractId == id && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                    if (contract.ContractAmount == totalPaymentSchedule)
                    {
                        if (contract.ContractAmount == totalItemValue)
                        {
                            contract.OverallStatus = "Confirmed";
                            contract.ApprovalStatus = "Confirmed";
                            contract.ConfirmedBy = User.Identity.Name;
                            contract.ConfirmedAt = DateTime.Now;
                            db.Entry(contract).State = EntityState.Modified;
                            db.SaveChanges();
                            response = "Success";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                    }
                    if (contract.ContractAmount == totalPaymentSchedule)
                    {
                        payment_schedule = totalPaymentSchedule;
                        items_amount = totalItemValue;
                        response = "TotalItems";
                        var data_result = new { response = response, schedule = payment_schedule, items_amount = items_amount };
                        return Json(data_result, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        contract_amount = contract.ContractAmount;
                        payment_schedule = totalPaymentSchedule;
                        response = "PaymentSchedule";
                        var data_result = new { response = response, amount = contract_amount, schedule = payment_schedule };
                        return Json(data_result, JsonRequestBehavior.AllowGet);
                    }

                }
                else
                {
                    response = "NoAttachment";
                }

                return Json(response, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }


        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult ConfirmRejVariation(StatusVM model)
        {
            string response = null;
            try
            {
                Contract contract = db.Contracts.Find(model.Id);
                int count = checkAttach("ContractVariation", contract.ContractId);
                if (count > 0)
                {
                    decimal? contract_amount = 0;
                    decimal? payment_schedule = 0;
                    decimal? items_amount = 0;
                    decimal? totalPaymentSchedule = db.PaymentSchedules.Where(a => a.ContractId == contract.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                    decimal? totalItemValue = db.ContractDetails.Where(a => a.ContractId == contract.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                    if (contract.ContractAmount == totalPaymentSchedule)
                    {
                        if (contract.ContractAmount == totalItemValue)
                        {
                            contract.RejectionSolution = model.Reason;
                            contract.OverallStatus = "Confirmed";
                            contract.ApprovalStatus = "Confirmed";
                            contract.ConfirmedBy = User.Identity.Name;
                            contract.ConfirmedAt = DateTime.Now;
                            db.Entry(contract).State = EntityState.Modified;
                            PurchaseRejection rejection = new PurchaseRejection()
                            {
                                RejectionReason = contract.RejectionReason,
                                RejectionSolution = model.Reason,
                                Rejecter = contract.Rejecter,
                                Type = "Contract Variation",
                                RejectedBy = contract.RejectedBy,
                                RejectedAt = contract.RejectedAt,
                                ResolvedBy = User.Identity.Name,
                                ResolvedAt = DateTime.Now,
                                Contractid = model.Id
                            };
                            db.PurchaseRejections.Add(rejection);
                            db.SaveChanges();
                            response = "Success";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                    }
                    if (contract.ContractAmount == totalPaymentSchedule)
                    {
                        payment_schedule = totalPaymentSchedule;
                        items_amount = totalItemValue;
                        response = "TotalItems";
                        var data_result = new { response = response, schedule = payment_schedule, items_amount = items_amount };
                        return Json(data_result, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        contract_amount = contract.ContractAmount;
                        payment_schedule = totalPaymentSchedule;
                        response = "PaymentSchedule";
                        var data_result = new { response = response, amount = contract_amount, schedule = payment_schedule };
                        return Json(data_result, JsonRequestBehavior.AllowGet);
                    }

                }
                else
                {
                    response = "NoAttachment";
                }

                return Json(response, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }


        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult UpdatePaymentSchedule(PaymentSchedule paymentSchedule)
        {
            string response = null;
            try
            {
                var payment_schedule = db.PaymentSchedules.Find(paymentSchedule.PaymentScheduleId);
                if (payment_schedule.Received == "Full")
                {
                    response = "Full";
                }
                else
                {

                    Contract contract = db.Contracts.Find(paymentSchedule.ContractId);
                    decimal prev_entered = db.PaymentSchedules.Where(a => a.ContractId == paymentSchedule.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();

                    decimal Total = prev_entered + paymentSchedule.Amount - payment_schedule.Amount;
                    if (Total <= contract.ContractAmount)
                    {
                        decimal? item_value = db.ContractDetails.Where(a => a.PaymentScheduleId == payment_schedule.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                        if (paymentSchedule.Amount < item_value)
                        {
                            response = "ItemExceed";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        decimal payment_schedule_amount = db.PaymentSchedules.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId).Select(a => a.Amount).FirstOrDefault();
                        if (paymentSchedule.Amount > payment_schedule_amount)
                        {
                            decimal prev_amnt_entered = Convert.ToDecimal(db.ContractDetails.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.ContractId == paymentSchedule.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum());
                            decimal? psBaseAmount = contract.CurrentExchangeRate * paymentSchedule.Amount;
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            payment_schedule.Amount = paymentSchedule.Amount;
                            payment_schedule.Balance = paymentSchedule.Amount - prev_amnt_entered;
                            payment_schedule.BaseAmount = psBaseAmount;
                            db.Entry(payment_schedule).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        else if (payment_schedule_amount == paymentSchedule.Amount)
                        {
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            db.Entry(payment_schedule).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        else
                        {
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            payment_schedule.Amount = paymentSchedule.Amount;
                            payment_schedule.Balance = paymentSchedule.Amount;
                            db.Entry(payment_schedule).State = EntityState.Modified;


                            db.SaveChanges();
                        }

                        var paymentScheduleList = GetPaymentSchedules(paymentSchedule.ContractId);
                        response = "Success";
                        var result_data = new { response = response, paymentScheduleList = paymentScheduleList };
                        return Json(result_data, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        response = "Exceed";

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";

            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult UpdatePayScheduleVariation(PaymentSchedule paymentSchedule)
        {
            string response = null;
            try
            {
                PaymentSchedule payment_schedule = db.PaymentSchedules.Find(paymentSchedule.PaymentScheduleId);
                if (payment_schedule.Amount >= 0)
                {

                    Contract contract = db.Contracts.Find(paymentSchedule.ContractId);
                    decimal prev_entered = db.PaymentSchedules.Where(a => a.ContractId == paymentSchedule.ContractId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();

                    decimal Total = prev_entered + paymentSchedule.Amount - payment_schedule.Amount;
                    if (Total <= contract.ContractAmount)
                    {
                        decimal? item_value = db.ContractDetails.Where(a => a.PaymentScheduleId == payment_schedule.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();
                        if (paymentSchedule.Amount < item_value)
                        {
                            response = "ItemExceed";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }
                        decimal payment_schedule_amount = db.PaymentSchedules.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId).Select(a => a.Amount).FirstOrDefault();
                        if (paymentSchedule.Amount > payment_schedule_amount)
                        {
                            decimal prev_amnt_entered = Convert.ToDecimal(db.ContractDetails.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.ContractId == paymentSchedule.ContractId&&a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum());
                            decimal? psBaseAmount = contract.CurrentExchangeRate * paymentSchedule.Amount;
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            payment_schedule.Amount = paymentSchedule.Amount;
                            payment_schedule.Balance = paymentSchedule.Amount - prev_amnt_entered;
                            payment_schedule.BaseAmount = psBaseAmount;
                            db.Entry(payment_schedule).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        else if (payment_schedule_amount == paymentSchedule.Amount)
                        {
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            db.Entry(payment_schedule).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        else
                        {
                            payment_schedule.FinancialYear = paymentSchedule.FinancialYear;
                            payment_schedule.Description = paymentSchedule.Description;
                            payment_schedule.Deliverable = paymentSchedule.Deliverable;
                            payment_schedule.Amount = paymentSchedule.Amount;
                            payment_schedule.Balance = paymentSchedule.Amount;
                            db.Entry(payment_schedule).State = EntityState.Modified;


                            db.SaveChanges();
                        }

                        var paymentScheduleList = GetPaymentSchedules(paymentSchedule.ContractId);
                        response = "Success";
                        var result_data = new { response = response, paymentScheduleList = paymentScheduleList };
                        return Json(result_data, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        response = "Exceed";

                    }
                }
               

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";

            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Entry")]
        public JsonResult DeletePaySchedule(int? id)
        {
            string response = null;
            try
            {
                //REmove all items attached to this payment schedule
                db.ContractDetails.RemoveRange(db.ContractDetails.Where(a => a.PaymentScheduleId == id));
                var payment = db.PaymentSchedules.Find(id);
                var contract_id = payment.ContractId;
                db.Entry(payment).State = EntityState.Deleted;
                db.SaveChanges();
                var itemsList = GetAllItems(contract_id);
                var paymentScheduleList = GetPaymentSchedules(contract_id);
                response = "Success";
                var result_data = new { response = response, paymentScheduleList = paymentScheduleList, itemsList = itemsList };
                return Json(result_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

        }

        [Authorize(Roles = "Contract Entry")]
        public JsonResult DeletePayScheduleVariation(int? id)
        {
            string response = null;
            try
            {
                var count = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == id).Count();
                if (count == 0)
                {
                    //REmove all items attached to this payment schedule
                    db.ContractDetails.RemoveRange(db.ContractDetails.Where(a => a.PaymentScheduleId == id));
                    var payment = db.PaymentSchedules.Find(id);
                    var contract_id = payment.ContractId;
                    db.Entry(payment).State = EntityState.Deleted;
                    db.SaveChanges();
                    var itemsList = GetAllItems(contract_id);
                    var paymentScheduleList = GetPaymentSchedules(contract_id);
                    response = "Success";
                    var result_data = new { response = response, paymentScheduleList = paymentScheduleList, itemsList = itemsList };
                    return Json(result_data, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    response = "Received";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

        }

        [Authorize(Roles = "Contract Approval")]
        public JsonResult ContractApproval(int? id)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(id);
                contract.OverallStatus = "Approved";
                contract.ApprovalStatus = "Approved";
                if (contract.VariationStatus == "Pending")
                {
                    contract.VariationStatus = "Approved";
                    decimal? totalReceived = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type == "Contract").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                    if (contract.ContractAmount != totalReceived)
                    {
                        contract.Received = "Partial";
                    }
                }
                contract.ApprovedBy = User.Identity.Name;
                contract.ApprovedAt = DateTime.Now;
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "Contract Approval")]
        public JsonResult ApproveTermination(int? id)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(id);
                if (contract.OverallStatus== "Request Void") {
                    contract.OverallStatus = "Voided";
                    contract.ApprovalStatus = "Voided";
                } else
                {
                    contract.OverallStatus = "Closed";
                    contract.ApprovalStatus = "Closed";
                }
           
                contract.ApprovedBy = User.Identity.Name;
                contract.ApprovedAt = DateTime.Now;
                db.Entry(contract).State = EntityState.Modified;
                db.ReceivingSummarys.Where(a => a.ContractId == id).ToList().ForEach(a => { a.OverallStatus = "Cancelled"; a.ApprovalStatus = "Cancelled";a.CancelledBy = User.Identity.Name;a.CancelledAt = DateTime.Now; });
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Examiner")]
        public JsonResult ContractVerification(int? id)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(id);
                contract.OverallStatus = "Examined";
                contract.ApprovalStatus = "Examined";
                contract.ExaminedBy = User.Identity.Name;
                contract.ExaminedAt = DateTime.Now;
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Examiner")]
        public JsonResult RejectVerification(StatusVM model)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(model.Id);
                contract.OverallStatus = "Rejected";
                contract.RejectionReason = model.Reason;
                contract.RejectedBy = User.Identity.Name;
                contract.RejectedAt = DateTime.Now;
                contract.Rejecter = "Examiner";
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //Confirm again Contract Previous was rejected
        [Authorize(Roles = "Contract Entry")]
        public JsonResult ConfirmRejContract(StatusVM model)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(model.Id);
                decimal? contract_amount = 0;
                decimal? payment_schedule = 0;
                decimal? items_amount = 0;
                decimal? totalPaymentSchedule = db.PaymentSchedules.Where(a => a.ContractId == model.Id).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                decimal? totalItemValue = db.ContractDetails.Where(a => a.ContractId == model.Id && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum();

                if (contract.ContractAmount == totalPaymentSchedule)
                {
                    if (contract.ContractAmount == totalItemValue)
                    {
                        contract.RejectionSolution = model.Reason;
                        contract.OverallStatus = "Confirmed";
                        contract.ConfirmedBy = User.Identity.Name;
                        contract.ConfirmedAt = DateTime.Now;
                        db.Entry(contract).State = EntityState.Modified;
                        PurchaseRejection rejection = new PurchaseRejection()
                        {
                            RejectionReason = contract.RejectionReason,
                            RejectionSolution = model.Reason,
                            Rejecter = contract.Rejecter,
                            Type = "Contract",
                            RejectedBy = contract.RejectedBy,
                            RejectedAt = contract.RejectedAt,
                            ResolvedBy = User.Identity.Name,
                            ResolvedAt = DateTime.Now,
                            Contractid = model.Id
                        };
                        db.PurchaseRejections.Add(rejection);
                        db.SaveChanges();
                        response = "Success";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }

                }
                if (contract.ContractAmount == totalPaymentSchedule)
                {
                    payment_schedule = totalPaymentSchedule;
                    items_amount = totalItemValue;
                    response = "TotalItems";
                    var data_result = new { response = response, schedule = payment_schedule, items_amount = items_amount };
                    return Json(data_result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    contract_amount = contract.ContractAmount;
                    payment_schedule = totalPaymentSchedule;
                    response = "PaymentSchedule";
                    var data_result = new { response = response, amount = contract_amount, schedule = payment_schedule };
                    return Json(data_result, JsonRequestBehavior.AllowGet);
                }


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //Reject contract
        [Authorize(Roles = "Contract Approval")]
        public JsonResult RejectApproval(StatusVM model)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(model.Id);
                contract.OverallStatus = "Rejected";
                contract.RejectionReason = model.Reason;
                contract.RejectedBy = User.Identity.Name;
                contract.RejectedAt = DateTime.Now;
                contract.Rejecter = "Approver";
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Contract Approval")]
        public JsonResult RejectTermination(StatusVM model)
        {
            string response = null;
            try
            {

                Contract contract = db.Contracts.Find(model.Id);
                if (contract.OverallStatus == "Request Void")
                {
                    contract.OverallStatus = "Approved";
                }
                else
                {
                    contract.OverallStatus = "Partial";
                }
                contract.RejectionReason = model.Reason;
                contract.RejectedBy = User.Identity.Name;
                contract.RejectedAt = DateTime.Now;
                contract.Rejecter = "Closed With Balance";
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetReceivingItems(string contractNo, int? id)
        {
            string response = null;
            try
            {
                var payment_schedule = db.PaymentSchedules.Find(id);
                if (payment_schedule.ReceiveType == "ByAmount")
                {
                    response = "Received";
                }
                else
                {
                    List<ContractDetail> contractDetails = new List<ContractDetail>();
                    var contract_items = db.ContractDetails.Where(a => a.PaymentScheduleId == id && a.Status != "Cancelled").ToList();
                    var received = db.Receivings.Where(a => a.PaymentScheduleId == id).ToList();
                    foreach (var item in contract_items)
                    {
                        ContractDetail contractDetail = new ContractDetail()
                        {
                            ContractDetailId = item.ContractDetailId,
                            ItemDesc = item.ItemDesc,
                            UOM = item.UOM,
                            UnitPrice = item.UnitPrice,
                            VatStatus = item.VatStatus,
                            Quantity = item.Quantity - (int)received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.ReceivedQuantity).DefaultIfEmpty(0).Sum(),
                            VAT = item.VAT - received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.Vat).DefaultIfEmpty(0).Sum(),
                            TotalAmount = item.TotalAmount - received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.Amount).DefaultIfEmpty(0).Sum()
                        };
                        contractDetails.Add(contractDetail);
                    }
                    var result_data = new { Result = contractDetails, schedule = payment_schedule };
                    return Json(result_data, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetReceivingSchedule(int? id)
        {
            string response = null;
            try
            {
                var payment_schedule = db.PaymentSchedules.Find(id);

                List<ContractDetail> contractDetails = new List<ContractDetail>();
                var contract_items = db.ContractDetails.Where(a => a.PaymentScheduleId == id && a.Status != "Cancelled").ToList();
                var received = db.Receivings.Where(a => a.PaymentScheduleId == id).ToList();
                foreach (var item in contract_items)
                {
                    ContractDetail contractDetail = new ContractDetail()
                    {
                        ContractDetailId = item.ContractDetailId,
                        ItemDesc = item.ItemDesc,
                        UOM = item.UOM,
                        UnitPrice = item.UnitPrice,
                        VatStatus = item.VatStatus,
                        Quantity = item.Quantity,
                        VAT = item.VAT,
                        TotalAmount = item.TotalAmount
                    };
                    contractDetails.Add(contractDetail);
                }
                var contract_payments = db.ContractPayments.Where(a => a.PaymentScheduleId == id && a.OverallStatus != "Cancelled").ToList();
                var result_data = new { Result = contractDetails, schedule = payment_schedule, contract_payments = contract_payments };
                return Json(result_data, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult EditCertificate(ReceiveByAmount receiveByAmountVM)
        {
            string response = null;
            try
            {
                ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                decimal scheduleAmount = db.PaymentSchedules.Where(a => a.PaymentScheduleId == contractPayment.PaymentScheduleId).Select(a => a.Amount).FirstOrDefault();
                decimal totalCertificateAmount = (decimal)db.ContractPayments.Where(a => a.PaymentScheduleId == contractPayment.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateAmount).DefaultIfEmpty(0).Sum();
                decimal newTotalCertificateAmount = (decimal)(totalCertificateAmount + receiveByAmountVM.CertificateAmount - contractPayment.CertificateAmount);
                if (totalCertificateAmount >= newTotalCertificateAmount)
                {

                    if (receiveByAmountVM.CertificateAmount < contractPayment.PaidAmount)
                    {
                        response = "Exceed";
                    }
                    else
                    {
                        contractPayment.CertificateNumber = receiveByAmountVM.CertificateNumber;

                        contractPayment.Balance = contractPayment.Balance + receiveByAmountVM.CertificateAmount - contractPayment.CertificateAmount;
                        contractPayment.CertificateAmount = receiveByAmountVM.CertificateAmount;
                        db.Entry(contractPayment).State = EntityState.Modified;
                        db.SaveChanges();
                        response = "Success";
                    }
                }
                else
                {
                    response = "ScheduleExceed";
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult EditReceivingAmount(int? id, int? receivingid, decimal? amount)
        {
            string response = null;
            string glAssigned = "No";
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                PaymentSchedule payment_schedule = db.PaymentSchedules.Find(id);
               InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                var totalReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == id && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                Contract contract = db.Contracts.Where(a => a.ContractId == payment_schedule.ContractId).FirstOrDefault();
                ReceivingSummary receiving_summary = db.ReceivingSummarys.Find(receivingid);
                decimal? previous_amount = receiving_summary.ReceivedAmount;
                decimal total = (Decimal)totalReceived - (Decimal)receiving_summary.ReceivedAmount + (Decimal)amount;
                ContractPayment contract_payment = db.ContractPayments.Find(receiving_summary.ContractPaymentId);
                if (contract_payment == null)
                {
                    response = "Technical";
                    var result_data1 = new { response = response, gl = glAssigned };
                    return Json(result_data1, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    decimal? certificatePaid = contract_payment.PaidAmount + amount - receiving_summary.ReceivedAmount;
                    if (certificatePaid <= contract_payment.CertificateAmount)
                    {
                        contract_payment.PaidAmount = certificatePaid;
                        contract_payment.Balance = contract_payment.CertificateAmount - certificatePaid;
                        db.Entry(contract_payment).State = EntityState.Modified;
                    }
                    else
                    {
                        response = "CertificateExceed";
                        var result_data1 = new { response = response, gl = glAssigned };
                        return Json(result_data1, JsonRequestBehavior.AllowGet);
                    }
                }
                if (contract.ContractVersion > 1)
                {
                    //CALCULATE TOTAL ADVANCE PAYMENT PAID TO THIS CONTRACT
                    decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    if (contractAdvancePayment > 0)
                    {
                        if (receiving_summary.AdvancePayment > 0)
                        {

                            ReceivingResponse receivingResponse = AdvancePaymentDeduction(contract, amount, receiving_summary.AdvancePayment, total);
                            if (receivingResponse.Response != "Success")
                            {
                                response = "AdvancePaymentProblem";
                                var result_data1 = new { response = response, gl = glAssigned };
                                return Json(result_data1, JsonRequestBehavior.AllowGet);
                            }
                        }

                    }

                }
                string sbc = null;
                if (receiving_summary.SubBudgetClass != null)
                {
                    sbc = receiving_summary.SubBudgetClass;
                }
                else
                {
                    sbc = contract.SubBudgetClass;
                }
                CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == sbc && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                if (currencyRateDetail == null)
                {
                    response = "SetupProblem";
                    var result_data1 = new { response = response, gl = glAssigned };
                    return Json(result_data1, JsonRequestBehavior.AllowGet);
                }
                decimal? total_deduction = 0;
                if (receiving_summary.AdvancePayment > 0)
                {
                    total_deduction = total_deduction + receiving_summary.AdvancePayment;
                }

                if (receiving_summary.RetentionPercentage > 0)
                {
                    total_deduction = total_deduction + receiving_summary.RetentionPercentage * receiving_summary.ReceivedAmount;
                }

                if (receiving_summary.LiquidatedDamageAmount > 0)
                {
                    total_deduction = total_deduction + receiving_summary.LiquidatedDamageAmount;
                }

                //Calculate WithHolding
                decimal withHoldingAmount = 0;

                if (contract.ContractType.ToUpper() == "WORKS")
                {
                    decimal payableAmount = (decimal)receiving_summary.ReceivedAmount;
                    if (receiving_summary.VAT > 0)
                    {
                        payableAmount = payableAmount - (decimal)receiving_summary.VAT;
                    }
                    decimal serviceAmount = 2 * payableAmount / 5;
                    decimal goodsAmount = 3 * payableAmount / 5;
                    withHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                }
                //End Calculate WithHolding
                if (withHoldingAmount > 0)
                {
                    total_deduction = total_deduction + withHoldingAmount;
                }


                //CALCULATE AMOUNT WILL REMAINS AFTER THIS RECEIVING
                decimal? amountRemainsInContract = (80 * contract.ContractAmount) / 100 - totalReceived + receiving_summary.ReceivedAmount - amount;

                if (total > payment_schedule.Amount)
                {
                    response = "Exceed";
                }
                else if (amount <= total_deduction)
                {
                    response = "ExceedDeduction";
                }
                else
                {

                    receiving_summary.ReceivedAmount = amount;
                    receiving_summary.RemainingAmount = receiving_summary.RemainingAmount + receiving_summary.ReceivedAmount - amount;

                    //Calculate Total VAT in this Payment Schedule                              
                    decimal? TotalVAT = db.ContractDetails.Where(a => a.PaymentScheduleId == payment_schedule.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.VAT).DefaultIfEmpty(0).FirstOrDefault();
                    if (TotalVAT > 0)
                    {
                        decimal? currentVAT = amount * TotalVAT / payment_schedule.Amount;
                        receiving_summary.VAT = currentVAT;
                    }
                    //End Calculate Total VAT in the this Payment Schedule
                    receiving_summary.BaseAmount = amount * currencyRateDetail.OperationalExchangeRate;
                    if (receiving_summary.AdvancePaymentBA != null)
                    {
                        receiving_summary.AdvancePaymentBA = receiving_summary.AdvancePaymentBA * currencyRateDetail.OperationalExchangeRate;
                    }
                    receiving_summary.ExchangeRate = currencyRateDetail.OperationalExchangeRate;
                    receiving_summary.ExchangeRateDate = currencyRateDetail.ExchangeRateDate;
                    db.Entry(receiving_summary).State = EntityState.Modified;
                    db.SaveChanges();

                    totalReceived = db.ReceivingSummarys.Where(a => a.ContractId == payment_schedule.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                    if (contract.ContractAmount == totalReceived)
                    {
                        contract.Received = "Full";
                        contract.OverallStatus = "FullReceived";
                    }
                    else
                    {
                        contract.Received = "Partial";
                        contract.OverallStatus = "Partial";
                    }
                    if (payment_schedule.Amount == total)
                    {
                        payment_schedule.Received = "Full";
                    }
                    else
                    {
                        payment_schedule.Received = "Partial";
                    }
                    payment_schedule.ReceivedAmount = payment_schedule.ReceivedAmount - previous_amount + amount;
                    payment_schedule.Balance = payment_schedule.Amount - payment_schedule.ReceivedAmount;
                    db.Entry(payment_schedule).State = EntityState.Modified;
                    db.Entry(contract).State = EntityState.Modified;
                    var receivingCoasList = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receiving_summary.ReceivingSummaryId).ToList();
                    if (receivingCoasList.Count() > 0)
                    {
                        glAssigned = "Yes";
                        db.ReceivingCoas.RemoveRange(db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receiving_summary.ReceivingSummaryId));
                    }
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";



                }


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                trans.Rollback();
                response = "DbException";
            }

        }
            var result_data = new { response = response, gl = glAssigned };
            return Json(result_data, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult PendingReceiving()
        {

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            List<ContractVM> receivingList = null;
            string[] StatusArray = new string[] { "Pending", "Rejected" };
            string contractType = null;
            ProcurementController procurement = new ProcurementController();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                if (User.IsInRole("Works Only Receiving Entry"))
                {
                    contractType = "Works Only";
                    receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                    foreach (var item in receivingList)
                    {
                        if (item.SubContractId > 0)
                        {
                            SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                            item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                            item.RemainingAmount = subContractPaymentSchedule.Balance;
                            item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                        }
                    }
                }
                else 
                {
                    contractType = "Except Works Only";
                    receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                    foreach (var item in receivingList)
                    {
                        if (item.SubContractId > 0)
                        {
                            SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                            item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                            item.RemainingAmount = subContractPaymentSchedule.Balance;
                            item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                        }
                    }
                }
            }
            else
            {
                contractType = "All Contract Type";
                receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                foreach (var item in receivingList)
                {
                    if (item.SubContractId > 0)
                    {
                        SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                        item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                        item.RemainingAmount = subContractPaymentSchedule.Balance;
                        item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                    }
                }

            }
              
         

            return View(receivingList);
        }
        [Authorize(Roles = "Contract Examiner")]
        public ActionResult ExamineReceiving()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

            string[] StatusArray = new string[] { "Confirmed" };
            string contractType = "All Contract Type";
            var receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
            foreach (var item in receivingList)
            {
                if (item.SubContractId > 0)
                {
                    SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                    item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                    item.RemainingAmount = subContractPaymentSchedule.Balance;
                    item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                }
            }

            return View(receivingList);
        }
        [Authorize(Roles = "Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval")]
        public ActionResult ReceivingApproval()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string response = CheckSpeciaReceiving(userPaystation);
            if (response == "NotAuthorizes")
            {
                ViewBag.Response = response;
                return View();
            }
            string[] StatusArray = new string[] { "Examined", "RejectedByPO" };
            string contractType = null;
            List<ContractVM> receivingList = null;
            ProcurementController procurement = new ProcurementController();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                if (User.IsInRole("Works Only Receiving Approval"))
                {
                    contractType = "Works Only";
                    receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                    foreach (var item in receivingList)
                    {
                        if (item.SubContractId > 0)
                        {
                            SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                            item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                            item.RemainingAmount = subContractPaymentSchedule.Balance;
                            item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                        }
                    }
                }
                else 
                {
                    contractType = "Except Works Only";
                    receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                    foreach (var item in receivingList)
                    {
                        if (item.SubContractId > 0)
                        {
                            SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                            item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                            item.RemainingAmount = subContractPaymentSchedule.Balance;
                            item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                        }
                    }
                }
            }
            else
            {
                contractType = "All Contract Type";
                receivingList = ReceivingEntries(userPaystation.InstitutionCode, StatusArray, contractType);
                foreach (var item in receivingList)
                {
                    if (item.SubContractId > 0)
                    {
                        SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.SubContractId == item.SubContractId && a.OverallStatus == "Active").FirstOrDefault();
                        item.PaymentScheduleAmount = subContractPaymentSchedule.Amount;
                        item.RemainingAmount = subContractPaymentSchedule.Balance;
                        item.PreviousReceived = subContractPaymentSchedule.Amount - subContractPaymentSchedule.Balance - item.ReceivedAmount;
                    }
                }
            }
           
           


            return View(receivingList);
        }

        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult AddInformation(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            Institution institution = db.Institution.Find(userPaystation.InstitutionId);
            if (institution.InstitutionCategory== "Ministry") {
                ViewBag.MDAReceiving = "Yes";
            }
        
            if (IsSubtresureOffice())
            {
                ViewBag.MDAReceiving = "Yes";
            }

                var receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             where p.ReceivingSummaryId == id && p.InstitutionCode == userPaystation.InstitutionCode
                             select new { p, q } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 ContractNumber = r.q.ContractNumber,
                                 ContractNo = r.q.ContractNo,
                                 ContractId = r.q.ContractId,
                                 SubContractId = r.p.SubContractId,
                                 ContractAmount = r.q.ContractAmount,
                                 ContractDescription = r.q.ContractDescription,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ReceivedAmount = r.p.ReceivedAmount,
                                 AmountReceived = r.p.ReceivedAmount,
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 SupplierName = r.q.Payeename,
                                 PayeeCode = r.q.PayeeCode,
                                 OverallStatus = r.p.OverallStatus,
                                 Currency = r.q.OperationalCurrency,
                                 OperationCurrency = r.p.OperationalCurrency,
                                 SubBudgetClass = r.p.SubBudgetClass,
                                 SubBudgetClass2 = r.q.SubBudgetClass,
                                 ContractType = r.q.ContractType,
                                 HasRetention = r.p.HasRetention,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 RetentionBy = r.p.RetentionBy,
                                 HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 BankAccountTo = r.p.BankAccountTo,
                                 BankAccountToDisp = r.p.BankAccountTo,
                                 AccountName = r.p.AccountName,
                                 InvoiceNumber = r.p.InvoiceNo,
                                 DeliveryNote = r.p.DeliveryNote,
                                 InspectionReportNo = r.p.InspectionReportNo,
                                 InvoiceDate = r.p.InvoiceDate,
                                 DeliveryDate = r.p.ReceivedDate,
                                 InspectionReportDate = r.p.InspectionReportDate,
                                 InvoiceFileName = r.p.InvoiceFileName,
                                 DeliveryFileName = r.p.DeliveryFileName,
                                 InspReportFileName = r.p.InspReportFileName,
                                 SubBudgetClassTo = r.p.SubBudgetClassTo,
                                 AdvancePayment = r.p.AdvancePayment,
                                 EditAttachment = r.p.EditAttachment,
                                 Accrual = r.p.Accrual,
                                 LiquidatedNumberOfDays = r.p.LiquidatedNumberOfDays,
                                 LiquidatedSBCTo = r.p.LiquidatedSBCTo,
                                 ClosingStatus = r.q.ClosingStatus,
                                 ParentInstitutionCode = r.p.ParentInstitutionCode,
                                 ParentInstitutionName = r.p.ParentInstitutionName,
                                 SubWarrantCode = r.p.SubWarrantCode,
                                 SubWarrantDescription = r.p.SubWarrantDescription,
                                 ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                             }).FirstOrDefault();
            
   
            if (receiving == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            decimal? receivingAmount = receiving.ReceivedAmount;
            if (receiving.AdvancePayment != null)
            {
                receiving.ReceivedAmount = receivingAmount - receiving.AdvancePayment;

            }

            if (institution.InstitutionCategory == "Ministry")
            {
                if (receiving.RetentionBy == "Accrual")
                {
                    if (receiving.RetentionPercentage > 0)
                    {
                        decimal? retentionAmount = receivingAmount * receiving.RetentionPercentage /100;
                        receiving.ReceivedAmount = receiving.ReceivedAmount - retentionAmount;
                    }
                }
              
            }


            if (receiving.SubContractId > 0)
            {
                SubContract subContract = db.SubContracts.Find(receiving.SubContractId);
                receiving.SupplierName = subContract.PayeeName;
                receiving.PayeeCode = subContract.PayeeCode;
            }
            if (receiving.ClosingStatus == "ClosedWithBalance")
            {
                receiving.Receiving = "YES";
            }
            else
            {
                receiving.Receiving = "NO";
            }
            if (receiving.OperationCurrency != null)
            {
                receiving.Currency = receiving.OperationCurrency;

            }
            if (receiving.SubBudgetClass == null)
            {
                receiving.SubBudgetClass = receiving.SubBudgetClass2;
            }

            var subBudgetClassList = db.CurrencyRateViews
           .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
             && a.SubBudgetClass != null && a.SubBudgetClass != "101" && a.SubBudgetClass != "303")
           .OrderBy(a => a.SubBudgetClass)
            .Select(s => new
            {
                SubBudgetClass = s.SubBudgetClass,
                Description = s.SubBudgetClass + "-" + s.SubBudgetClassDesc
            }).ToList();
            receiving.SBCList = new SelectList(subBudgetClassList, "SubBudgetClass", "Description");
            List<RetentionType> retentionTypeList = new List<RetentionType>();
            RetentionType retention_type = new RetentionType
            {
                RetentionBy = "BankTransfer",
                Description = "Bank Transfer"
            };
            RetentionType retention_type1 = new RetentionType
            {
                RetentionBy = "InternalTransfer",
                Description = "Internal Transfer"
            };

            retentionTypeList.Add(retention_type);
            retentionTypeList.Add(retention_type1);


            var subBudgetClassToList = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.SubBudgetClass == "301"
                && a.OverallStatus != "Cancelled")
                .Select(s => new
                {
                    SubBudgetClass = s.SubBudgetClass,
                }).ToList();

            var bankAccountsList = (from u in db.InstitutionAccounts
                                    where u.InstitutionCode == userPaystation.InstitutionCode
                                    select new InstitutionAccountVM
                                    {
                                        AccountNumber = u.AccountNumber,
                                        AccountName = u.AccountNumber + " - " + u.AccountName
                                    }).DistinctBy(a => a.AccountNumber).ToList();
            receiving.RetentionByList = new SelectList(retentionTypeList, "RetentionBy", "Description", receiving.RetentionBy);
            receiving.SubBudgetClassList = new SelectList(subBudgetClassToList, "SubBudgetClass", "SubBudgetClass", receiving.SubBudgetClassTo);
            receiving.SubBudgetClassLDList = new SelectList(subBudgetClassToList, "SubBudgetClass", "SubBudgetClass", receiving.LiquidatedSBCTo);
            receiving.AccountToList = new SelectList(bankAccountsList, "AccountNumber", "AccountName", receiving.BankAccountTo);
            return View(receiving);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult SaveAttachments(ReceivingAttachmentVM receivingAttachmentVM)
        {
            string response = null;
            try
            {

                ReceivingSummary receiving = db.ReceivingSummarys.Find(receivingAttachmentVM.ReceivingSummaryId);
                Contract contract = db.Contracts.Find(receiving.ContractId);
                if (receivingAttachmentVM.DocumentType == "Invoice")
                {
                    receiving.InvoiceNo = receivingAttachmentVM.CertificateNumber;
                    receiving.InvoiceDate = receivingAttachmentVM.AttachmentDate;
                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = contract.ContractNo + "_Invoice_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Invoices"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Invoices/") + fileName);
                        //Save file to server folder  
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.InvoiceFileName = fileName;
                    }
                    if (receiving.ContractPaymentId > 0)
                    {
                        SaveInvoiceAttachment(receiving);
                    }

                }
                else if (receivingAttachmentVM.DocumentType == "Delivery Note")
                {
                    receiving.DeliveryNote = receivingAttachmentVM.CertificateNumber;
                    receiving.ReceivedDate = receivingAttachmentVM.AttachmentDate;

                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = contract.ContractNo + "_Delivery_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Deliveries"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Deliveries/") + fileName);
                        //Save file to server folder  
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.DeliveryFileName = fileName;
                    }
                    if (receiving.ContractPaymentId > 0)
                    {
                        SaveDeliveryAttachment(receiving);
                    }

                }
                else
                {
                    receiving.InspectionReportNo = receivingAttachmentVM.CertificateNumber;
                    receiving.InspectionReportDate = receivingAttachmentVM.AttachmentDate;

                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = contract.ContractNo + "_Inspection_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Inspections"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Inspections/") + fileName);
                        //Save file to server folder  
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.InspReportFileName = fileName;
                    }
                    if (receiving.ContractPaymentId > 0)
                    {
                        SaveInspectionReportAttachment(receiving);
                    }

                }




                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);


        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult EditInformation(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            Institution institution = db.Institution.Find(userPaystation.InstitutionId);
            if (institution.InstitutionCategory == "Ministry")
            {
                ViewBag.MDAReceiving = "Yes";
            }

            if (IsSubtresureOffice())
            {
                ViewBag.MDAReceiving = "Yes";
            }
              var  receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             where p.ReceivingSummaryId == id && p.InstitutionCode == userPaystation.InstitutionCode
                             select new { p, q } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 ContractNumber = r.q.ContractNumber,
                                 ContractNo = r.q.ContractNo,
                                 ContractId = r.q.ContractId,
                                 SubContractId = r.p.SubContractId,
                                 ContractDescription = r.q.ContractDescription,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ReceivedAmount = r.p.ReceivedAmount,
                                 AmountReceived = r.p.ReceivedAmount,
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 SupplierName = r.q.Payeename,
                                 PayeeCode = r.q.PayeeCode,
                                 OverallStatus = r.p.OverallStatus,
                                 InvoiceNumber = r.p.InvoiceNo,
                                 InvoiceDate = r.p.InvoiceDate,
                                 DeliveryNote = r.p.DeliveryNote,
                                 DeliveryDate = r.p.ReceivedDate,
                                 InspectionReportNo = r.p.InspectionReportNo,
                                 InspectionReportDate = r.p.InspectionReportDate,
                                 SubBudgetClass = r.p.SubBudgetClass,
                                 SubBudgetClass2 = r.q.SubBudgetClass,
                                 ContractType = r.q.ContractType,
                                 HasRetention = r.p.HasRetention,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 RetentionBy = r.p.RetentionBy,
                                 HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 Currency = r.q.OperationalCurrency,
                                 BankAccountTo = r.p.BankAccountTo,
                                 BankAccountToDisp = r.p.BankAccountTo,
                                 AccountName = r.p.AccountName,
                                 InvoiceFileName = r.p.InvoiceFileName,
                                 DeliveryFileName = r.p.DeliveryFileName,
                                 InspReportFileName = r.p.InspReportFileName,
                                 SubBudgetClassTo = r.p.SubBudgetClassTo,
                                 AdvancePayment = r.p.AdvancePayment,
                                 EditAttachment = r.p.EditAttachment,
                                 Accrual = r.p.Accrual,
                                 LiquidatedNumberOfDays = r.p.LiquidatedNumberOfDays,
                                 LiquidatedSBCTo = r.p.LiquidatedSBCTo,
                                 OperationCurrency = r.p.OperationalCurrency,
                                 ClosingStatus = r.q.ClosingStatus,
                                 ParentInstitutionCode = r.p.ParentInstitutionCode,
                                 ParentInstitutionName = r.p.ParentInstitutionName,
                                 SubWarrantCode = r.p.SubWarrantCode,
                                 SubWarrantDescription = r.p.SubWarrantDescription,
                                 ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                             }).FirstOrDefault();

            
                
            if (receiving == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            decimal? receivingAmount = receiving.ReceivedAmount;
            if (receiving.AdvancePayment != null)
            {
                receiving.ReceivedAmount = receivingAmount - receiving.AdvancePayment;

            }

            if (institution.InstitutionCategory == "Ministry")
            {
                if (receiving.RetentionBy == "Accrual")
                {
                    if (receiving.RetentionPercentage > 0)
                    {
                        decimal? retentionAmount = receivingAmount * receiving.RetentionPercentage / 100;
                        receiving.ReceivedAmount = receiving.ReceivedAmount - retentionAmount;
                    }
                }

            }

            if (receiving.SubContractId > 0)
            {
                SubContract subContract = db.SubContracts.Find(receiving.SubContractId);
                receiving.SupplierName = subContract.PayeeName;
                receiving.PayeeCode = subContract.PayeeCode;
            }
            if (receiving.ClosingStatus == "ClosedWithBalance")
            {
                receiving.Receiving = "YES";
            }
            else
            {
                receiving.Receiving = "NO";
            }
            if (receiving.OperationCurrency != null)
            {
                receiving.Currency = receiving.OperationCurrency;

            }
            if (receiving.SubBudgetClass == null)
            {
                receiving.SubBudgetClass = receiving.SubBudgetClass2;
            }
            var subBudgetClassList = db.CurrencyRateViews
           .Where(a => a.InstitutionCode == userPaystation.InstitutionCode
             && a.SubBudgetClass != null && a.SubBudgetClass != "101" && a.SubBudgetClass != "303")
           .OrderBy(a => a.SubBudgetClass)
            .Select(s => new
            {
                SubBudgetClass = s.SubBudgetClass,
                Description = s.SubBudgetClass + "-" + s.SubBudgetClassDesc
            }).ToList();
            receiving.SBCList = new SelectList(subBudgetClassList, "SubBudgetClass", "Description");
            List<RetentionType> retentionTypeList = new List<RetentionType>();
            RetentionType retention_type = new RetentionType
            {
                RetentionBy = "BankTransfer",
                Description = "Bank Transfer"
            };
            RetentionType retention_type1 = new RetentionType
            {
                RetentionBy = "InternalTransfer",
                Description = "Internal Transfer"
            };

            if (receiving.ClosingStatus == "ClosedWithBalance")
            {
                receiving.Receiving = "YES";
            }
            else
            {
                receiving.Receiving = "NO";
            }
            retentionTypeList.Add(retention_type);
            retentionTypeList.Add(retention_type1);
            var subBudgetClassToList = db.InstitutionAccounts
                .Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.SubBudgetClass == "301"
                && a.OverallStatus != "Cancelled")
                .Select(s => new
                {
                    SubBudgetClass = s.SubBudgetClass,
                }).ToList();
            var bankAccountsList = (from u in db.InstitutionAccounts
                                    where u.InstitutionCode == userPaystation.InstitutionCode
                                    select new InstitutionAccountVM
                                    {
                                        AccountNumber = u.AccountNumber,
                                        AccountName = u.AccountNumber + " - " + u.AccountName
                                    }).DistinctBy(a => a.AccountNumber).ToList();
            receiving.RetentionByList = new SelectList(retentionTypeList, "RetentionBy", "Description", receiving.RetentionBy);

            receiving.SubBudgetClassList = new SelectList(subBudgetClassToList, "SubBudgetClass", "SubBudgetClass", receiving.SubBudgetClassTo);
            receiving.SubBudgetClassLDList = new SelectList(subBudgetClassToList, "SubBudgetClass", "SubBudgetClass", receiving.LiquidatedSBCTo);
            receiving.AccountToList = new SelectList(bankAccountsList, "AccountNumber", "AccountName", receiving.BankAccountTo);
            return View(receiving);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult EditAttachments(ReceivingAttachmentVM receivingAttachmentVM)
        {
            string response = null;
            try
            {

                ReceivingSummary receiving = db.ReceivingSummarys.Find(receivingAttachmentVM.ReceivingSummaryId);
                if (receivingAttachmentVM.DocumentType == "Invoice")
                {
                    receiving.InvoiceNo = receivingAttachmentVM.CertificateNumber;

                    if (receivingAttachmentVM.AttachmentDate != null)
                    {
                        receiving.InvoiceDate = receivingAttachmentVM.AttachmentDate;
                    }
                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = "Invoice_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Invoices"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Invoices/") + fileName);
                        //Delete Previous File
                        string path = Path.Combine(Server.MapPath("~/Media/Contract/Invoices/"), fileName);

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        //Save new file to server folder  
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.InvoiceFileName = fileName;

                    }
                }
                else if (receivingAttachmentVM.DocumentType == "Delivery Note")
                {
                    receiving.DeliveryNote = receivingAttachmentVM.CertificateNumber;

                    if (receivingAttachmentVM.AttachmentDate != null)
                    {
                        receiving.ReceivedDate = receivingAttachmentVM.AttachmentDate;
                    }
                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = "Delivery_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Deliveries"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Deliveries/") + fileName);
                        //Delete Previous File
                        string path = Path.Combine(Server.MapPath("~/Media/Contract/Deliveries/"), fileName);

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        //Save new file to server folder   
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.DeliveryFileName = fileName;

                    }
                }
                else
                {
                    receiving.InspectionReportNo = receivingAttachmentVM.CertificateNumber;

                    if (receivingAttachmentVM.AttachmentDate != null)
                    {
                        receiving.InspectionReportDate = receivingAttachmentVM.AttachmentDate;
                    }
                    //Checking file is available to save.  
                    if (receivingAttachmentVM.AttachentFile != null)
                    {
                        string fileExtension = Path.GetExtension(receivingAttachmentVM.AttachentFile.FileName);
                        string fileName = "Inspection_" + receivingAttachmentVM.ReceivingSummaryId;
                        fileName = fileName + fileExtension;
                        Directory.CreateDirectory(Server.MapPath("~/Media/Contract/Inspections"));
                        var ServerSavePath = Path.Combine(Server.MapPath("~/Media/Contract/Inspections/") + fileName);
                        //Delete Previous File
                        string path = Path.Combine(Server.MapPath("~/Media/Contract/Inspections/"), fileName);

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        //Save new file to server folder  
                        receivingAttachmentVM.AttachentFile.SaveAs(ServerSavePath);
                        receiving.InspReportFileName = fileName;

                    }
                }

                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);


        }

        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Voucher Entry,Voucher Approval")]
        public FileResult PerformanceQuarantee(string Filename)
        {
            var FileVirtualPath = "~/Media/Contract/PerformanceBond/" + Filename;
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Voucher Entry,Voucher Approval")]
        public FileResult InvoiceAttachment(string Filename)
        {
            var FileVirtualPath = "~/Media/Contract/Invoices/" + Filename;
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Voucher Entry,Voucher Approval")]
        public FileResult DeliveryAttachment(string Filename)
        {
            var FileVirtualPath = "~/Media/Contract/Deliveries/" + Filename;
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Voucher Entry,Voucher Approval")]
        public FileResult InspectionAttachment(string Filename)
        {
            var FileVirtualPath = "~/Media/Contract/Inspections/" + Filename;
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Voucher Entry,Voucher Approval")]
        public FileResult VariationAttachment(string Filename)
        {
            var FileVirtualPath = "~/Media/Contract/Variation/" + Filename;
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult ConfirmReceiving(int? id)
        {
            string response = null;
            try
            {
                ReceivingSummary receiving = db.ReceivingSummarys.Find(id);
                receiving.OverallStatus = "Confirmed";
                receiving.ApprovalStatus = "Confirmed";
                receiving.ConfirmedBy = User.Identity.Name;
                receiving.ConfirmedAt = DateTime.Now;
                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Examiner")]
        public JsonResult ReceivingExamine(int? id)
        {
            string response = null;
            try
            {

                ReceivingSummary receiving = db.ReceivingSummarys.Find(id);
                receiving.OverallStatus = "Examined";
                receiving.ApprovalStatus = "Examined";
                receiving.ConfirmedBy = User.Identity.Name;
                receiving.ConfirmedAt = DateTime.Now;
                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval")]
        public JsonResult ApproveReceiving(int id)
        {
            string response = null;
            try
            {
                //UpdateReservedRetention(id);

                ProcessResponse voucherStatus = ServiceManager.GeneratePaymentVoucher(db, "CO", id, User);

                if (voucherStatus.OverallStatus == "Success")
                {
                    ReceivingSummary receiving = db.ReceivingSummarys.Find(id);
                    receiving.OverallStatus = "Approved";
                    receiving.ApprovalStatus = "Approved";
                    receiving.ApprovedBy = User.Identity.Name;
                    receiving.ApprovedAt = DateTime.Now;
                    receiving.PVNo = voucherStatus.StringReturnValue;
                    db.Entry(receiving).State = EntityState.Modified;
                    Contract contract = db.Contracts.Find(receiving.ContractId);
                    if (contract.OverallStatus == "FullReceived")
                    {
                        contract.OverallStatus = "Closed";
                        db.Entry(contract).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    //Run GLQueue
                    string journalTypeCode = "PO";
                    var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalTypeCode) };
                    db.Database.ExecuteSqlCommand("dbo.sp_UpdateGLQueue @JournalTypeCode", parameters);
                    if (receiving.Type != "AdvancePayment")
                    {
                        //update multiple at once
                        db.ReceivedAssets.Where(a => a.ReceivingSummaryId == id).ToList().ForEach(a => a.OverallStatus = "Incomplete");
                        db.AssetDetails.Where(a => a.ReceivingSummaryId == id).ToList().ForEach(a => a.OverallStatus = "Incomplete");
                        db.InventoryDetails.Where(a => a.ReceivingSummaryId == id).ToList().ForEach(a => a.OverallStatus = "Incomplete");
                    }
                    if (receiving.RetentionBy == "Accrual")
                    {
                        RetentionPayment retentionPayment = db.RetentionPayments.Where(a => a.ReceivingSummaryId == id && a.OverallStatus == "Incomplete").FirstOrDefault();
                        if (retentionPayment != null)
                        {
                            retentionPayment.OverallStatus = "Pending";
                            db.Entry(retentionPayment).State = EntityState.Modified;
                        }
                     }
                    db.SaveChanges();
                    response = "Success";

                }
                else
                {
                    response = voucherStatus.OverallStatusDescription;
                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult CancelReceiving(int id)
        {
            string response = null;
            try
            {
                ReceivingSummary receiving = db.ReceivingSummarys.Find(id);
                receiving.OverallStatus = "Cancelled";
                receiving.ApprovalStatus = "Cancelled";
                receiving.CancelledBy = User.Identity.Name;
                receiving.ConfirmedAt = DateTime.Now;
                db.Entry(receiving).State = EntityState.Modified;
                RemoveRetention(receiving.ReceivingSummaryId);
                Contract contract = db.Contracts.Find(receiving.ContractId);
                if (contract.ContractVersion == 1)
                {
                    //Release amount due to advance payments
                    if (receiving.AdvancePayment > 0)
                    {
                        db.AdvancePayments.RemoveRange(db.AdvancePayments.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                    }
                    //End release amount due to advance payments
                }

                //Cancel MDA Accrual Retention
                if (receiving.RetentionBy == "Accrual")
                {
                    RetentionPayment retentionPayment = db.RetentionPayments.Where(a => a.ReceivingSummaryId == id &&(a.OverallStatus == "Incomplete" || a.OverallStatus == "Pending" || a.OverallStatus == "Confirmed" || a.OverallStatus == "Examined")).FirstOrDefault();
                    if (retentionPayment != null)
                    {
                        retentionPayment.OverallStatus = "Cancelled";
                        db.Entry(retentionPayment).State = EntityState.Modified;
                    }
                }
                //End Cancel MDA Accrual Retention
                db.SaveChanges();



                int countReceived = db.ReceivingSummarys.Where(a => a.ContractId == receiving.ContractId && a.OverallStatus != "Cancelled").Count();

                PaymentSchedule schedule = db.PaymentSchedules.Find(receiving.PaymentScheduleId);
                schedule.ReceivedAmount = schedule.ReceivedAmount - receiving.ReceivedAmount;
                schedule.Balance = schedule.Balance + receiving.ReceivedAmount;
                if (schedule.SubContractAmount > 0)
                {
                    schedule.SubContractBalance = schedule.SubContractBalance + receiving.ReceivedAmount;
                }
                if (schedule.ReceivedAmount == 0 || schedule.ReceiveType == "ByQuantity")
                {
                    db.Receivings.RemoveRange(db.Receivings.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                    db.ReceivedAssets.RemoveRange(db.ReceivedAssets.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                    db.AssetDetails.RemoveRange(db.AssetDetails.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                    db.InventoryDetails.RemoveRange(db.InventoryDetails.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));

                }
                if (receiving.SubContractId > 0)
                {
                    SubContractPaymentSchedule contractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.SubContractId == receiving.SubContractId && a.PaymentScheduleId == schedule.PaymentScheduleId && a.OverallStatus == "Active").FirstOrDefault();
                    if (contractPaymentSchedule != null)
                    {
                        contractPaymentSchedule.Balance = contractPaymentSchedule.Balance + receiving.ReceivedAmount;
                        db.Entry(contractPaymentSchedule).State = EntityState.Modified;
                    }
                }
                if (schedule.ReceivedAmount == 0)
                {
                    if (contract.Rejecter != "Closed With Balance")
                    {
                        contract.Received = null;
                        schedule.Received = null;
                        schedule.ReceiveType = null;
                    }

                }
                else
                {
                    if (contract.Rejecter != "Closed With Balance")
                    {
                    contract.Received = "Partial";
                    schedule.Received = "Partial";
                    }
                }
                ContractPayment contract_payment = db.ContractPayments.Find(receiving.ContractPaymentId);
                if (contract_payment != null)
                {
                    contract_payment.PaidAmount = contract_payment.PaidAmount - receiving.ReceivedAmount;
                    if (contract_payment.PaidAmount == 0)
                    {
                        contract_payment.PaidAmount = 0;
                        contract_payment.Balance = contract_payment.CertificateAmount;
                        contract_payment.OverallStatus = "Cancelled";
                        db.Entry(contract_payment).State = EntityState.Modified;
                    }
                    else
                    {
                        contract_payment.Balance = contract_payment.Balance + receiving.ReceivedAmount;
                        db.Entry(contract_payment).State = EntityState.Modified;
                    }
                }

                db.Entry(schedule).State = EntityState.Modified;
                var receivingCoasList = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId).ToList();
                if (receivingCoasList != null)
                {
                    db.ReceivingCoas.RemoveRange(db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                }
                if (countReceived == 0)
                {
                    if (contract.Rejecter != "Closed With Balance")
                    {
                        contract.OverallStatus = "Approved";
                    }
                    var paymentSchedules = db.PaymentSchedules.Where(a => a.ContractId == receiving.ContractId).ToList();
                    foreach (var item in paymentSchedules)
                    {

                        PaymentSchedule schedule1 = db.PaymentSchedules.Find(item.PaymentScheduleId);
                        schedule1.Balance = schedule1.Amount;
                        schedule1.ReceivedAmount = null;
                        schedule1.Received = null;
                        schedule1.ReceiveType = null;
                        db.Entry(schedule1).State = EntityState.Modified;
                    }

                }
                else
                {
                    if (contract.Rejecter != "Closed With Balance")
                    {
                        contract.OverallStatus = "Partial";
                     }
                }
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Contract Examiner,Voucher Entry,Voucher Approval")]
        public ActionResult PreviewReceiving(int? id)
        {

            ReceivingSummaryVM receiving = new ReceivingSummaryVM();

            if (User.IsInRole("Payment Office Submission")
            || User.IsInRole("Payment Office Approval")
            || User.IsInRole("Payment Office Verification")
            || User.IsInRole("Payment Office Reports")
            )
            {
                receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             where p.ReceivingSummaryId == id
                             select new { p, q } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 PaymentScheduleId = r.p.PaymentScheduleId,
                                 ContractNumber = r.q.ContractNumber,
                                 ContractNo = r.q.ContractNo,
                                 ContractId = r.q.ContractId,
                                 SubContractId = r.p.SubContractId,
                                 ContractDescription = r.q.ContractDescription,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ReceivedAmount = r.p.ReceivedAmount,
                                 CertificateNumber = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateNumber).FirstOrDefault(),
                                 CertificateAmount = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateAmount).FirstOrDefault(),
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 OverallStatus = r.p.OverallStatus,
                                 SupplierName = r.q.Payeename,
                                 SubBudgetClass = r.p.SubBudgetClass,
                                 Rejecter = r.p.Rejecter,
                                 RejectionReason = r.p.RejectionReason,
                                 RejectionSolution = r.p.RejectionSolution,
                                 InvoiceNumber = r.p.InvoiceNo,
                                 InvoiceDate = r.p.InvoiceDate,
                                 DeliveryNote = r.p.DeliveryNote,
                                 DeliveryDate = r.p.ReceivedDate,
                                 InspectionReportNo = r.p.InspectionReportNo,
                                 InspectionReportDate = r.p.InspectionReportDate,
                                 InvoiceFileName = r.p.InvoiceFileName,
                                 DeliveryFileName = r.p.DeliveryFileName,
                                 InspReportFileName = r.p.InspReportFileName,
                                 Currency = r.q.OperationalCurrency,
                                 ContractType = r.q.ContractType,
                                 HasRetention = r.p.HasRetention,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 RetentionBy = r.p.RetentionBy,
                                 HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 PerformanceBondFile = r.q.PerformanceBondFile,
                                 BankAccountTo = r.p.BankAccountTo,
                                 BankAccountToDisp = r.p.BankAccountTo,
                                 AccountName = r.p.AccountName,
                                 SubBudgetClassTo = r.p.SubBudgetClassTo,
                                 AdvancePayment = r.p.AdvancePayment,
                                 ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                             }).FirstOrDefault();
            }
            else
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             where p.ReceivingSummaryId == id && p.InstitutionCode == userPaystation.InstitutionCode
                             select new { p, q } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 PaymentScheduleId = r.p.PaymentScheduleId,
                                 ContractNumber = r.q.ContractNumber,
                                 ContractNo = r.q.ContractNo,
                                 ContractId = r.q.ContractId,
                                 SubContractId = r.p.SubContractId,
                                 ContractDescription = r.q.ContractDescription,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ReceivedAmount = r.p.ReceivedAmount,
                                 CertificateNumber = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateNumber).FirstOrDefault(),
                                 CertificateAmount = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateAmount).FirstOrDefault(),
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 OverallStatus = r.p.OverallStatus,
                                 SupplierName = r.q.Payeename,
                                 SubBudgetClass = r.p.SubBudgetClass,
                                 Rejecter = r.p.Rejecter,
                                 RejectionReason = r.p.RejectionReason,
                                 RejectionSolution = r.p.RejectionSolution,
                                 InvoiceNumber = r.p.InvoiceNo,
                                 InvoiceDate = r.p.InvoiceDate,
                                 DeliveryNote = r.p.DeliveryNote,
                                 DeliveryDate = r.p.ReceivedDate,
                                 InspectionReportNo = r.p.InspectionReportNo,
                                 InspectionReportDate = r.p.InspectionReportDate,
                                 InvoiceFileName = r.p.InvoiceFileName,
                                 DeliveryFileName = r.p.DeliveryFileName,
                                 InspReportFileName = r.p.InspReportFileName,
                                 Currency = r.q.OperationalCurrency,
                                 ContractType = r.q.ContractType,
                                 HasRetention = r.p.HasRetention,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 RetentionBy = r.p.RetentionBy,
                                 HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 PerformanceBondFile = r.q.PerformanceBondFile,
                                 BankAccountTo = r.p.BankAccountTo,
                                 BankAccountToDisp = r.p.BankAccountTo,
                                 AccountName = r.p.AccountName,
                                 SubBudgetClassTo = r.p.SubBudgetClassTo,
                                 AdvancePayment = r.p.AdvancePayment,
                                 ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                             }).FirstOrDefault();
            }
            if (receiving == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {

                receiving.ReceivingList = db.Receivings.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId).ToList();

            }

            return View(receiving);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval,Contract Examiner,Voucher Entry,Voucher Approval")]
        public ActionResult Preview(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            ProcurementController procurement = new ProcurementController();
            var receiving = new ReceivingSummaryVM();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                //Only For Tarura 
                receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             where p.ReceivingSummaryId == id && institutionCodesArray.Contains(p.InstitutionCode)
                             select new { p, q } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ContractNumber = r.q.ContractNumber,
                                 ContractNo = r.q.ContractNo,
                                 SubContractId = r.p.SubContractId,
                                 ContractAmount = r.q.ContractAmount,
                                 ContractName = r.q.ContractName,
                                 ContractDescription = r.q.ContractDescription,
                                 ReceivedAmount = r.p.ReceivedAmount,
                                 SupplierName = r.q.Payeename,
                                 PayeeCode = r.q.PayeeCode,
                                 SubBudgetClass = r.p.SubBudgetClass,
                                 Currency = r.q.OperationalCurrency,
                                 OperationCurrency = r.p.OperationalCurrency,
                                 OverallStatus = r.p.OverallStatus,
                                 ContractId = r.q.ContractId,
                                 CertificateNumber = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateNumber).FirstOrDefault(),
                                 CertificateAmount = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateAmount).FirstOrDefault(),
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 Rejecter = r.p.Rejecter,
                                 RejectionReason = r.p.RejectionReason,
                                 RejectionSolution = r.p.RejectionSolution,
                                 InvoiceNumber = r.p.InvoiceNo,
                                 InvoiceDate = r.p.InvoiceDate,
                                 DeliveryNote = r.p.DeliveryNote,
                                 DeliveryDate = r.p.ReceivedDate,
                                 InspectionReportNo = r.p.InspectionReportNo,
                                 InspectionReportDate = r.p.InspectionReportDate,
                                 InvoiceFileName = r.p.InvoiceFileName,
                                 DeliveryFileName = r.p.DeliveryFileName,
                                 InspReportFileName = r.p.InspReportFileName,
                                 ContractType = r.q.ContractType,
                                 HasRetention = r.p.HasRetention,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 RetentionBy = r.p.RetentionBy,
                                 HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 PerformanceBondFile = r.q.PerformanceBondFile,
                                 BankAccountTo = r.p.BankAccountTo,
                                 BankAccountToDisp = r.p.BankAccountTo,
                                 AccountName = r.p.AccountName,
                                 SubBudgetClassTo = r.p.SubBudgetClassTo,
                                 SubBudgetClass2 = r.p.SubBudgetClass,
                                 AdvancePayment = r.p.AdvancePayment,
                                 Accrual = r.p.Accrual,
                                 ClosingStatus = r.q.ClosingStatus,
                                 ParentInstitutionCode = r.p.ParentInstitutionCode,
                                 ParentInstitutionName = r.p.ParentInstitutionName,
                                 SubWarrantCode = r.p.SubWarrantCode,
                                 SubWarrantDescription = r.p.SubWarrantDescription,
                                 PVNo = r.p.PVNo,
                                 ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                             }).FirstOrDefault();
            }
            else
            {
                 receiving = (from p in db.ReceivingSummarys
                                 join q in db.Contracts on p.ContractId equals q.ContractId
                                 where p.ReceivingSummaryId == id && p.InstitutionCode == userPaystation.InstitutionCode
                                 select new { p, q } into r
                                 select new ReceivingSummaryVM
                                 {
                                     ReceivingSummaryId = r.p.ReceivingSummaryId,
                                     ReceivingNumber = r.p.ReceivingNumber,
                                     ContractNumber = r.q.ContractNumber,
                                     ContractNo = r.q.ContractNo,
                                     SubContractId = r.p.SubContractId,
                                     ContractAmount = r.q.ContractAmount,
                                     ContractName = r.q.ContractName,
                                     ContractDescription = r.q.ContractDescription,
                                     ReceivedAmount = r.p.ReceivedAmount,
                                     SupplierName = r.q.Payeename,
                                     PayeeCode = r.q.PayeeCode,
                                     SubBudgetClass = r.p.SubBudgetClass,
                                     Currency = r.q.OperationalCurrency,
                                     OperationCurrency = r.p.OperationalCurrency,
                                     OverallStatus = r.p.OverallStatus,
                                     ContractId = r.q.ContractId,
                                     CertificateNumber = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateNumber).FirstOrDefault(),
                                     CertificateAmount = db.ContractPayments.Where(a => a.PaymentScheduleId == r.p.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.CertificateAmount).FirstOrDefault(),
                                     ContractStartDate = r.q.ContractStartDate,
                                     ContractEndDate = r.q.ContractEndDate,
                                     Rejecter = r.p.Rejecter,
                                     RejectionReason = r.p.RejectionReason,
                                     RejectionSolution = r.p.RejectionSolution,
                                     InvoiceNumber = r.p.InvoiceNo,
                                     InvoiceDate = r.p.InvoiceDate,
                                     DeliveryNote = r.p.DeliveryNote,
                                     DeliveryDate = r.p.ReceivedDate,
                                     InspectionReportNo = r.p.InspectionReportNo,
                                     InspectionReportDate = r.p.InspectionReportDate,
                                     InvoiceFileName = r.p.InvoiceFileName,
                                     DeliveryFileName = r.p.DeliveryFileName,
                                     InspReportFileName = r.p.InspReportFileName,
                                     ContractType = r.q.ContractType,
                                     HasRetention = r.p.HasRetention,
                                     RetentionPercentage = r.p.RetentionPercentage,
                                     RetentionBy = r.p.RetentionBy,
                                     HasLiquidatedDamage = r.p.HasLiquidatedDamage,
                                     LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                     PerformanceBondFile = r.q.PerformanceBondFile,
                                     BankAccountTo = r.p.BankAccountTo,
                                     BankAccountToDisp = r.p.BankAccountTo,
                                     AccountName = r.p.AccountName,
                                     SubBudgetClassTo = r.p.SubBudgetClassTo,
                                     SubBudgetClass2 = r.p.SubBudgetClass,
                                     AdvancePayment = r.p.AdvancePayment,
                                     Accrual = r.p.Accrual,
                                     ClosingStatus = r.q.ClosingStatus,
                                     ParentInstitutionCode = r.p.ParentInstitutionCode,
                                     ParentInstitutionName = r.p.ParentInstitutionName,
                                     SubWarrantCode = r.p.SubWarrantCode,
                                     SubWarrantDescription = r.p.SubWarrantDescription,
                                     PVNo = r.p.PVNo,
                                     ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                 }).FirstOrDefault();
            }
            if (receiving == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                if (receiving.ClosingStatus == "ClosedWithBalance")
                {
                    receiving.Receiving = "YES";
                }

                if (receiving.OperationCurrency != null)
                {
                    receiving.Currency = receiving.OperationCurrency;

                }
                if (receiving.SubBudgetClass2 != null)
                {
                    receiving.SubBudgetClass = receiving.SubBudgetClass2;

                }
                receiving.ReceivingList = db.Receivings.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId).ToList();
                if (receiving.SubContractId > 0)
                {
                    SubContract subContract = db.SubContracts.Find(receiving.SubContractId);
                    receiving.SupplierName = subContract.PayeeName;
                    receiving.PayeeCode = subContract.PayeeCode;
                }
                if (receiving.PVNo != null)
                {
                    string paymentVoucherStatus = db.PaymentVouchers.Where(a => a.PVNo == receiving.PVNo).Select(a => a.OverallStatus).FirstOrDefault();
                    if (paymentVoucherStatus != null)
                    {
                        receiving.VoucherStatus = paymentVoucherStatus;
                    }
                }
            }
            return PartialView("_PreviewReceiving", receiving);
        }

        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult EditReceiving(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

            var receiving = (from p in db.ReceivingSummarys
                             join q in db.Contracts on p.ContractId equals q.ContractId
                             join s in db.PaymentSchedules on p.PaymentScheduleId equals s.PaymentScheduleId
                             where p.ReceivingSummaryId == id && p.InstitutionCode == userPaystation.InstitutionCode
                             select new { p, q, s } into r
                             select new ReceivingSummaryVM
                             {
                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                 PaymentScheduleId = r.p.PaymentScheduleId,
                                 PaymentSchedule = r.s.Description,
                                 ReceiveType = r.s.ReceiveType,
                                 ContractId = r.p.ContractId,
                                 ContractNo = r.q.ContractNo,
                                 ReceivingNumber = r.p.ReceivingNumber,
                                 ContractDescription = r.q.ContractDescription,
                                 ContractAmount = r.q.ContractAmount,
                                 ReceivingItemsValue = r.p.ReceivedAmount,
                                 AdvancePayment = r.p.AdvancePayment,
                                 LiquidatedDamageAmount = r.p.LiquidatedDamageAmount,
                                 RetentionPercentage = r.p.RetentionPercentage,
                                 SupplierName = r.q.Payeename,
                                 ContractStartDate = r.q.ContractStartDate,
                                 ContractEndDate = r.q.ContractEndDate,
                                 Currency = r.q.OperationalCurrency,

                             }).FirstOrDefault();
            if (receiving == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                List<PurchaseOrderDetailVM> receivingList = new List<PurchaseOrderDetailVM>();
                var ItemsList = db.ContractDetails.Where(a => a.ContractId == receiving.ContractId && a.Status != "Cancelled").ToList();
                var ReceivingList = db.Receivings.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId).ToList();
                foreach (var item in ReceivingList)
                {
                    PurchaseOrderDetailVM lpoItems = new PurchaseOrderDetailVM
                    {
                        ContractDetailId = (int)item.ContractDetailId,
                        ItemDesc = item.ItemDesc,
                        Quantity = ItemsList.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.Quantity).FirstOrDefault() - db.Receivings.Where(a => a.ReceivingSummaryId != receiving.ReceivingSummaryId && a.ContractDetailId == item.ContractDetailId).Select(a => a.ReceivedQuantity).DefaultIfEmpty(0).Sum(),
                        ReceivedQuantity = ReceivingList.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.ReceivedQuantity).FirstOrDefault(),
                        TotalAmount = (Decimal)item.Amount,
                        UnitPrice = item.UnitPrice,
                        VAT = item.Vat,
                        UOM = item.UOM
                    };
                    receivingList.Add(lpoItems);
                }
                receiving.ReceivedItemsList = receivingList;
            }
            return View(receiving);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult EditReceiving(ReceivingSummaryVM model)
        {
            string response = null;
            string glAssigned = "No";
            using (var trans = db.Database.BeginTransaction())
            {
                try
            {
                //Test if Receiving quantity is greater than total deduction
                decimal? receiving_amount = 0;
                   foreach (var item in model.ReceivedItemsList)
                {
                    if (item.ReceivedQuantity <= item.Quantity)
                    {
                        if (item.ReceivedQuantity > 0)
                        {
                            var contractDetail = db.ContractDetails.Where(a => a.ContractDetailId == item.ContractDetailId && a.Status != "Cancelled").FirstOrDefault();

                            Double VAT = 0;
                            var VatRate = 0.18;
                            Double AssetValue = (Double)contractDetail.UnitPrice;
                            Double total_asset_value = (Double)(item.ReceivedQuantity * contractDetail.UnitPrice);
                            if (contractDetail.VAT != 0)
                            {
                                VAT = ((Double)(item.ReceivedQuantity * contractDetail.UnitPrice)) * VatRate;
                            }
                            receiving_amount = receiving_amount + item.ReceivedQuantity * contractDetail.UnitPrice + (Decimal)VAT;
                        }
                    }
                }
                ReceivingSummary receivingsummary = db.ReceivingSummarys.Find(model.ReceivingSummaryId);
                decimal? previous_amount = receivingsummary.ReceivedAmount;
                Contract contract = db.Contracts.Find(receivingsummary.ContractId);
                if (contract.ContractVersion > 1)
                {
                    decimal? totalReceived = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    decimal total = (Decimal)totalReceived - (Decimal)receivingsummary.ReceivedAmount + (Decimal)receiving_amount;
                    //CALCULATE TOTAL ADVANCE PAYMENT PAID TO THIS CONTRACT
                    decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    if (contractAdvancePayment > 0)
                    {
                        if (receivingsummary.AdvancePayment > 0)
                        {
                            ReceivingResponse receivingResponse = AdvancePaymentDeduction(contract, receiving_amount, receivingsummary.AdvancePayment, total);
                            if (receivingResponse.Response != "Success")
                            {
                                response = "AdvancePaymentProblem";
                                var result_data1 = new { response = response, gl = glAssigned };
                                return Json(result_data1, JsonRequestBehavior.AllowGet);
                            }
                        }

                    }

                }
                string sbc = null;
                if (receivingsummary.SubBudgetClass != null)
                {
                    sbc = receivingsummary.SubBudgetClass;
                }
                else
                {
                    sbc = contract.SubBudgetClass;
                }
                    InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

                    CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == sbc && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                if (currencyRateDetail == null)
                {
                    response = "SetupProblem";
                    var result_data1 = new { response = response, gl = glAssigned };
                    return Json(result_data1, JsonRequestBehavior.AllowGet);
                }

                decimal? total_deduction = 0;
                if (receivingsummary.AdvancePayment > 0)
                {
                    total_deduction = total_deduction + receivingsummary.AdvancePayment;
                }

                if (receivingsummary.RetentionPercentage > 0)
                {
                    total_deduction = total_deduction + receivingsummary.RetentionPercentage * receivingsummary.ReceivedAmount;
                }

                if (receivingsummary.LiquidatedDamageAmount > 0)
                {
                    total_deduction = total_deduction + receivingsummary.LiquidatedDamageAmount;
                }

                //Calculate WithHolding
                decimal withHoldingAmount = 0;

                if (contract.ContractType.ToUpper() == "WORKS")
                {
                    decimal payableAmount = (decimal)receivingsummary.ReceivedAmount;
                    if (receivingsummary.VAT > 0)
                    {
                        payableAmount = payableAmount - (decimal)receivingsummary.VAT;
                    }
                    decimal serviceAmount = 2 * payableAmount / 5;
                    decimal goodsAmount = 3 * payableAmount / 5;
                    withHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                }
                //End Calculate WithHolding
                if (withHoldingAmount > 0)
                {
                    total_deduction = total_deduction + withHoldingAmount;
                }
                //End Test if Receiving quantity is greater than total deduction

                if (receiving_amount <= total_deduction)
                {
                    response = "ExceedDeduction";
                }
                else
                {
                    //Remove previous received               
                    receivingsummary.CreatedBy = User.Identity.Name;
                    receivingsummary.CreatedAt = DateTime.Now;
                    contract.Received = null;
                    PaymentSchedule schedule = db.PaymentSchedules.Find(receivingsummary.PaymentScheduleId);
                    schedule.Received = null;
                    db.Receivings.RemoveRange(db.Receivings.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId));
                    db.ReceivedAssets.RemoveRange(db.ReceivedAssets.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId));
                    db.AssetDetails.RemoveRange(db.AssetDetails.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId));
                    db.InventoryDetails.RemoveRange(db.InventoryDetails.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId));
                    db.SaveChanges();
                    //End Remove previous receiving
      
                    decimal? sum = 0;
                    var ReceivingSummaryId = receivingsummary.ReceivingSummaryId;
                    string receivingNumber = receivingsummary.ReceivingNumber;
                    //summary.ReceivingNumber = receivingNumber;
                    //db.Entry(summary).State = EntityState.Modified;
                    //db.SaveChanges();

                    List<Receiving> receivings = new List<Receiving>();
                    List<AssetDetail> assetDetails = new List<AssetDetail>();
                    Double Total_VAT = 0;
                    foreach (var item in model.ReceivedItemsList)
                    {
                        if (item.ReceivedQuantity <= item.Quantity)
                        {
                            if (item.ReceivedQuantity > 0)
                            {
                                var contractDetail = db.ContractDetails.Where(a => a.ContractDetailId == item.ContractDetailId && a.Status != "Cancelled").FirstOrDefault();

                                Double VAT = 0;
                                var VatRate = 0.18;
                                Double AssetValue = (Double)contractDetail.UnitPrice;
                                Double total_asset_value = (Double)(item.ReceivedQuantity * contractDetail.UnitPrice);
                                if (item.VatStatus == "Applicable")
                                {
                                    if (contractDetail.VAT != 0)
                                    {
                                        VAT = ((Double)(item.ReceivedQuantity * contractDetail.UnitPrice)) * VatRate;
                                        Total_VAT = Total_VAT + VAT;
                                        AssetValue = AssetValue + VatRate * AssetValue;
                                        total_asset_value = total_asset_value + VAT;
                                    }
                                }
                                sum = sum + item.ReceivedQuantity * contractDetail.UnitPrice + (Decimal)VAT;
                                if (contractDetail.ItemDesc == "Retention")
                                {
                                    receivingsummary.HasRetention = true;
                                    db.Entry(receivingsummary).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                                Receiving receiving = new Receiving()
                                {
                                    ItemDesc = contractDetail.ItemDesc,
                                    Amount = item.ReceivedQuantity * contractDetail.UnitPrice + (Decimal)VAT,
                                    ReceivedQuantity = item.ReceivedQuantity,
                                    ContractId = model.ContractId,
                                    PaymentScheduleId = model.PaymentScheduleId,
                                    ContractDetailId = item.ContractDetailId,
                                    ReceiveDate = DateTime.Now,
                                    Vat = (decimal)VAT,
                                    UOM = contractDetail.UOM,
                                    UnitPrice = contractDetail.UnitPrice,
                                    ReceivingSummaryId = ReceivingSummaryId,
                                    SubLevelCategory = userPaystation.SubLevelCategory,
                                    ClassId = contractDetail.ClassId,
                                    ItemClassificationId = contractDetail.ItemClassificationId
                                };
                                receivings.Add(receiving);

                                if (contractDetail.ClassId == 2)
                                {
                                    decimal quantity = (Decimal)(item.ReceivedQuantity);
                                    decimal item_quantity = Math.Round(quantity);
                                    int NumberOfAssets = (int)item_quantity;

                                    ReceivedAssets summaryReceived = new ReceivedAssets()
                                    {
                                        AssetName = contractDetail.ItemDesc,
                                        ContractDetailId = item.ContractDetailId,
                                        SubLevelCategory = contract.SubLevelCategory,
                                        SubLevelCode = contract.SubLevelCode,
                                        SubLevelDesc = contract.SubLevelDesc,
                                        AssetsValue = (Decimal)total_asset_value,
                                        Quantity = NumberOfAssets,
                                        ReceivingNumber = receivingNumber,
                                        ReceivingSummaryId = ReceivingSummaryId,
                                        PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                                        ReceivingDetailId = ReceivingSummaryId,
                                        InstitutionCode = userPaystation.InstitutionCode,
                                        InstitutionId = contract.InstitutionId,
                                        OverallStatus = "NotReceived",
                                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                                        OperationCurrency = contract.OperationalCurrency,
                                        JournalCode = "PP",
                                        SourceModule = "Contract",
                                        CreatedBy = User.Identity.Name,
                                        CreatedAt = DateTime.Now
                                    };

                                    db.ReceivedAssets.Add(summaryReceived);
                                    db.SaveChanges();
                                    //Generate and Save Legal number
                                    var currentId = summaryReceived.ReceivedAssetsId;
                                    summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "AS", currentId);
                                    db.Entry(summaryReceived).State = EntityState.Modified;
                                    db.SaveChanges();

                                    int i = 0;
                                    while (i < NumberOfAssets)
                                    {
                                        AssetDetail assetDetail = new AssetDetail()
                                        {

                                            AssetName = contractDetail.ItemDesc,
                                            ContractDetailId = item.ContractDetailId,
                                            Currency = contract.OperationalCurrency,
                                            AssetValue = (Decimal)AssetValue,
                                            ReceivingNumber = receivingNumber,
                                            PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                                            ReceivingSummaryId = ReceivingSummaryId,
                                            InstitutionCode = userPaystation.InstitutionCode,
                                            InstitutionId = contract.InstitutionId,
                                            OverallStatus = "NotReceived",
                                            ReceivedAssetsId = currentId
                                        };

                                        assetDetails.Add(assetDetail);
                                        i++;
                                    }

                                }
                                else if (contractDetail.ClassId == 3)
                                {
                                    if (item.ReceivedQuantity > 0)
                                    {

                                        InventoryDetail inventoryDetail = new InventoryDetail()
                                        {
                                            ItemName = contractDetail.ItemDesc,
                                            ContractDetailId = item.ContractDetailId,
                                            SubLevelCategory = contract.SubLevelCategory,
                                            SubLevelCode = contract.SubLevelCode,
                                            SubLevelDesc = contract.SubLevelDesc,
                                            UnitPrice = contractDetail.UnitPrice,
                                            InventoryValue = item.ReceivedQuantity * contractDetail.UnitPrice + (Decimal)VAT,
                                            UOM = contractDetail.UOM,
                                            Quantity = item.ReceivedQuantity,
                                            ReceivingNumber = receivingNumber,
                                            PurchaseOrderDetailId = item.PurchaseOrderDetailId,
                                            ReceivingSummaryId = ReceivingSummaryId,
                                            InstitutionCode = userPaystation.InstitutionCode,
                                            InstitutionId = contract.InstitutionId,
                                            OverallStatus = "NotReceived",
                                            FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                                            OperationCurrency = contract.OperationalCurrency,
                                            JournalCode = "IV",
                                            SourceModule = "Contract",
                                            CreatedBy = User.Identity.Name,
                                            CreatedAt = DateTime.Now
                                        };

                                        db.InventoryDetails.Add(inventoryDetail);
                                        db.SaveChanges();
                                        //Generate and save Legal number
                                        var currentId = inventoryDetail.InventoryDetailId;
                                        inventoryDetail.InventoryCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "IV", currentId);
                                        db.Entry(inventoryDetail).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                    db.Receivings.AddRange(receivings);
                    db.AssetDetails.AddRange(assetDetails);
                    receivingsummary.ReceivedAmount = sum;
                    receivingsummary.BaseAmount = sum * currencyRateDetail.OperationalExchangeRate;
                    if (receivingsummary.AdvancePaymentBA != null)
                    {
                        receivingsummary.AdvancePaymentBA = receivingsummary.AdvancePaymentBA * currencyRateDetail.OperationalExchangeRate;
                    }
                    receivingsummary.ExchangeRate = currencyRateDetail.OperationalExchangeRate;
                    receivingsummary.ExchangeRateDate = currencyRateDetail.ExchangeRateDate;
                    db.Entry(receivingsummary).State = EntityState.Modified;
                    db.SaveChanges();
                    var total_received = db.ReceivingSummarys.Where(a => a.ContractId == model.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                    var payment_schedule = db.PaymentSchedules.Where(a => a.PaymentScheduleId == model.PaymentScheduleId).FirstOrDefault();
                    var received_in_schedule = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                    receivingsummary.RemainingAmount = payment_schedule.Amount - received_in_schedule;
                    db.Entry(receivingsummary).State = EntityState.Modified;


                    if (contract.ContractAmount == total_received)
                    {
                        contract.Received = "Full";
                        contract.OverallStatus = "FullReceived";
                    }
                    else
                    {
                        contract.Received = "Partial";
                        contract.OverallStatus = "Partial";
                    }
                    if (payment_schedule.Amount == received_in_schedule)
                    {
                        payment_schedule.Received = "Full";
                    }
                    else
                    {
                        payment_schedule.Received = "Partial";
                    }
                    receivingsummary.VAT = (decimal)Total_VAT;
                    db.Entry(receivingsummary).State = EntityState.Modified;
                    var receivingCoasList = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId).ToList();
                    if (receivingCoasList.Count() > 0)
                    {
                        glAssigned = "Yes";
                        db.ReceivingCoas.RemoveRange(db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receivingsummary.ReceivingSummaryId));
                    }
                    payment_schedule.ReceivedAmount = payment_schedule.ReceivedAmount - previous_amount + receiving_amount;
                    payment_schedule.Balance = payment_schedule.Amount - payment_schedule.ReceivedAmount;
                    db.Entry(payment_schedule).State = EntityState.Modified;
                    db.Entry(contract).State = EntityState.Modified;
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";

                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                trans.Rollback();
                response = "DbException";
            }

        }
            var result_data = new { response = response, gl = glAssigned };
            return Json(result_data, JsonRequestBehavior.AllowGet);

        }
        [Authorize(Roles = "Contract Examiner")]
        public JsonResult RejectExamination(StatusVM model)
        {
            string response = null;
            try
            {

                ReceivingSummary receiving = db.ReceivingSummarys.Find(model.Id);
                receiving.OverallStatus = "Rejected";
                receiving.Rejecter = "Examiner";
                receiving.RejectionReason = model.Reason;
                receiving.RejectedBy = User.Identity.Name;
                receiving.RejectedAt = DateTime.Now;
                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval")]
        public JsonResult RejectReceiving(StatusVM model)
        {
            string response = null;
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    ReceivingSummary receiving = db.ReceivingSummarys.Find(model.Id);
                    var paidVouchersList = db.PaymentVouchers.Where(a => a.OtherSourceId == receiving.ReceivingSummaryId && a.SourceModule == "Contract" && (a.OverallStatus.ToUpper() == "UNAPPLIED" || a.OverallStatus == "Sent to BOT" || a.OverallStatus.ToUpper() == "ACCEPTED" || a.OverallStatus.ToUpper() == "PROCESSED" || a.OverallStatus.ToUpper() == "SETTLED")).ToList();
                    if (paidVouchersList.Count() > 0)
                    {
                        response = "Paid";
                        receiving.OverallStatus = "Approved";
                        db.Entry(receiving).State = EntityState.Modified;
                        db.SaveChanges();
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }
                    receiving.OverallStatus = "Rejected";
                    receiving.Rejecter = "Approver";
                    receiving.RejectionReason = model.Reason;
                    receiving.RejectedBy = User.Identity.Name;
                    receiving.RejectedAt = DateTime.Now;
                    db.Entry(receiving).State = EntityState.Modified;
                    var vouchersList = db.PaymentVouchers.Where(a => a.OtherSourceId == receiving.ReceivingSummaryId && a.SourceModule == "Contract" && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected By Examiner" || a.OverallStatus == "Confirmed" || a.OverallStatus == "Rejected" || a.OverallStatus == "Examined" || a.OverallStatus == "Approved")).ToList();
                    if (vouchersList.Count() > 0)
                    {
                        foreach (var paymentVoucher in vouchersList)
                        {
                            paymentVoucher.OverallStatus = "Cancelled";
                            paymentVoucher.CancelledAt = DateTime.Now;
                            paymentVoucher.CancelledBy = User.Identity.GetUserName();
                            db.Entry(paymentVoucher).State = EntityState.Modified;
                            var parameters = new SqlParameter[] { new SqlParameter("@PVNo", paymentVoucher.PVNo) };
                            db.Database.ExecuteSqlCommand("dbo.reverse_ungenerated_payment_gl_p @PVNo", parameters);
                        }

                    }
                    db.SaveChanges();
                    trans.Commit();
                    response = "Success";

                }
                catch (Exception ex)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                    trans.Rollback();
                    response = "DbException";
                }
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        //Confirm again Receiving Previous was rejected
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult ConfirmRejReceiving(StatusVM model)
        {
            string response = null;
            try
            {

                ReceivingSummary receiving = db.ReceivingSummarys.Find(model.Id);
                receiving.RejectionSolution = model.Reason;
                receiving.OverallStatus = "Confirmed";
                receiving.ConfirmedBy = User.Identity.Name;
                receiving.ConfirmedAt = DateTime.Now;
             
                PurchaseRejection rejection = new PurchaseRejection()
                {
                    RejectionReason = receiving.RejectionReason,
                    RejectionSolution = model.Reason,
                    Rejecter = receiving.Rejecter,
                    Type = "Contract receiving",
                    RejectedBy = receiving.RejectedBy,
                    RejectedAt = receiving.RejectedAt,
                    ResolvedBy = User.Identity.Name,
                    ResolvedAt = DateTime.Now,
                    Contractid = model.Id
                };
                receiving.Rejecter = null;
                db.Entry(receiving).State = EntityState.Modified;
                db.PurchaseRejections.Add(rejection);
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Contract Entry,Contract Examiner,Contract Approval,Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval")]
        public ActionResult TrackContract(Search search)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            List<ContractVM> ContractList = new List<ContractVM>();
            DateTime start_date, end_date;
            ProcurementController procurement = new ProcurementController();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                if (search.StartDate != null && search.EndDate != null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    end_date = ((DateTime)search.EndDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
            ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                            ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                }
                else if (search.StartDate != null && search.EndDate == null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }

                }
                else if (search.StartDate == null && search.EndDate != null)
                {
                    end_date = ((DateTime)search.EndDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                                   ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }
                }
                else
                {
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                 ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                  ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }

                }
            }
            else
            {
                if (search.StartDate != null && search.EndDate != null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    end_date = ((DateTime)search.EndDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
            ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                            ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                }
                else if (search.StartDate != null && search.EndDate == null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }

                }
                else if (search.StartDate == null && search.EndDate != null)
                {
                    end_date = ((DateTime)search.EndDate).Date;
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                                   ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }
                }
                else
                {
                    if (search.ContractStatus.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus != "Cancelled" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus != "Cancelled"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                 ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper() && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                  ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus.ToUpper() == search.ContractStatus.ToString().ToUpper()
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == p.ContractId && a.Type == "Contract" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }


                    }

                }
            }
            if (ContractList.Count == 0)
            {
                if (search.ContractStatus != null || search.StartDate != null || search.EndDate != null || !string.IsNullOrEmpty(search.Keyword))
                {
                    ViewBag.Message = "NoMatching";
                }
            }
            ViewBag.ContractList = ContractList;
            Search vm = new Search();
            if (search.StartDate != null)
            {
                vm.StartDate = search.StartDate;
            }
            if (search.EndDate != null)
            {
                vm.EndDate = search.EndDate;
            }
            if (search.Keyword != null)
            {
                vm.Keyword = search.Keyword;
            }
            return View(vm);
        }
        [Authorize(Roles = "Contract Entry")]

        public JsonResult VoidContractEntry(StatusVM model)
        {
            string response = null;
            try
            {
                if (db.ReceivingSummarys.Any(a => a.ContractId == model.Id && a.OverallStatus == "APPROVED" && a.Type == "AdvancePayment"))
                {
                    response = "Failed to void ,This Contract arleady paid Advance Payment";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                    var contract = db.Contracts.Find(model.Id);
                    contract.RejectionReason = model.Reason;
                    contract.OverallStatus = "Request Void";
                    contract.VoidedBy = User.Identity.Name;
                    contract.VoidedAt = DateTime.Now;
                    db.Entry(contract).State = EntityState.Modified;                 
                    db.SaveChanges();
                    response = "Success";
              
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = ex.Message.ToString();
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CloseContractEntry(StatusVM model)
        {
            string response = null;
            try
            {
                if (!db.ReceivingSummarys.Any(a => a.ContractId == model.Id && (a.OverallStatus == "Pending" || a.OverallStatus == "Confirmed") && a.Type != "AdvancePayment"))
                {
                    if (db.ReceivingSummarys.Any(a => a.ContractId == model.Id && a.OverallStatus == "APPROVED" && a.Type == "AdvancePayment"))
                    {
                        response = "Failed to void ,This Contract arleady paid Advance Payment";
                        return Json(response, JsonRequestBehavior.AllowGet);
                    }        
                    var contract = db.Contracts.Find(model.Id);
                    contract.RejectionReason = model.Reason;
                    contract.OverallStatus = "Request Close";
                    contract.VoidedBy = User.Identity.Name;
                    contract.VoidedAt = DateTime.Now;
                    db.Entry(contract).State = EntityState.Modified;
                    db.SaveChanges();
                    response = "Success";
               
                }
                else
                {
                    response = "To close this Contract,Please cancel or approve its existing receiving first";
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = ex.Message.ToString();
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry,Contract Examiner,Works Only Receiving Approval,Except Works Receiving Approval,All Contract Type Receiving Approval")]
        public ActionResult ReceivingTracker(Search search)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            List<ContractVM> receivingList = new List<ContractVM>();
            DateTime start_date, end_date;
            ProcurementController procurement = new ProcurementController();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                if (search.StartDate != null && search.EndDate != null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    end_date = ((DateTime)search.EndDate).Date;

                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }
                }
                else if (search.StartDate != null && search.EndDate == null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }

                }
                else if (search.StartDate == null && search.EndDate != null)
                {
                    end_date = ((DateTime)search.EndDate).Date;

                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();


                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }

                    }
                }
                else
                {
                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();


                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where institutionCodesArray.Contains(p.InstitutionCode) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }

                }
            }
            else
            {
                if (search.StartDate != null && search.EndDate != null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    end_date = ((DateTime)search.EndDate).Date;

                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }



                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }
                }
                else if (search.StartDate != null && search.EndDate == null)
                {
                    start_date = ((DateTime)search.StartDate).Date;
                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }

                }
                else if (search.StartDate == null && search.EndDate != null)
                {
                    end_date = ((DateTime)search.EndDate).Date;

                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();


                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }

                    }
                }
                else
                {
                    if (search.ReceivingStatus2.ToString().ToUpper() == "ALL")
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && p.ReceivingNumber.Contains(receiving_number) && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus != "Cancelled" && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();


                        }


                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string receiving_number = search.Keyword.Trim();
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.ReceivingNumber.Contains(receiving_number) && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }
                        else
                        {
                            receivingList = (from p in db.ReceivingSummarys
                                             join q in db.Contracts on p.ContractId equals q.ContractId
                                             join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                             where p.InstitutionCode == userPaystation.InstitutionCode && p.OverallStatus.ToUpper() == search.ReceivingStatus2.ToString().ToUpper() && p.Type == "Contract"
                                             select new { p, q, m } into r
                                             select new ContractVM
                                             {
                                                 ReceivingSummaryId = r.p.ReceivingSummaryId,
                                                 ContractNo = r.q.ContractNo,
                                                 ContractNumber = r.q.ContractNumber,
                                                 ReceivingNumber = r.p.ReceivingNumber,
                                                 ReceivedAmount = r.p.ReceivedAmount,
                                                 PaymentScheduleAmount = r.m.Amount,
                                                 DeliverySchedule = r.m.Description,
                                                 OverallStatus = r.p.OverallStatus,
                                                 Lotted = r.q.Lotted,
                                                 LotNo = r.q.LotNo,
                                                 InvoiceNo = r.p.InvoiceNo,
                                                 SupplierName = r.q.Payeename,
                                                 CreatedAt = r.q.CreatedAt,
                                                 Currency = r.q.OperationalCurrency
                                             }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                        }

                    }

                }
            }
            if (receivingList.Count == 0)
            {
                if (search.ReceivingStatus2 != null || search.StartDate != null || search.EndDate != null || !string.IsNullOrEmpty(search.Keyword))
                {

                    ViewBag.Message = "NoMatching";
                }
            }
            ViewBag.receivingList = receivingList;
            Search vm = new Search();
            if (search.StartDate != null)
            {
                vm.StartDate = search.StartDate;
            }
            if (search.EndDate != null)
            {
                vm.EndDate = search.EndDate;
            }
            if (search.Keyword != null)
            {
                vm.Keyword = search.Keyword;
            }
            return View(vm);
        }
        [Authorize(Roles = "Contract Entry")]
        public ActionResult ContractVariation(Search search, string status)
        {
            if (status == "Pending")
            {
                ViewBag.Pending = "in active";
            }
            else
            {
                ViewBag.Search = "in active";
            }

            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            ProcurementController procurement = new ProcurementController();
            var receiving = new ReceivingSummaryVM();
            List<ContractVM> ContractList = new List<ContractVM>();
            if (procurement.IsTarura(userPaystation.InstitutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                //Only For Tarura 
                var VariationList = (from p in db.Contracts
                                     where institutionCodesArray.Contains(p.InstitutionCode) && (p.OverallStatus == "Pending" || p.OverallStatus == "Rejected") && p.VariationStatus == "Pending"
                                     select new ContractVM
                                     {
                                         ContractId = p.ContractId,
                                         ContractNo = p.ContractNo,
                                         ContractNumber = p.ContractNumber,
                                         ContractAmount = p.ContractAmount,
                                         VariationType = p.VariationType,
                                         PreviousAmount = p.PreviousAmount,
                                         Currency = p.OperationalCurrency,
                                         OverallStatus = p.OverallStatus,
                                         Payeename = p.Payeename,
                                         GLStatus = p.GLStatus,
                                         Lotted = p.Lotted,
                                         LotNo = p.LotNo,
                                         CreatedAt = p.CreatedAt,
                                         ContractEndDate = p.ContractEndDate,
                                         ContractDescription = p.ContractDescription,
                                         TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum()
                                     }
      ).OrderByDescending(a => a.ContractId).ToList();
                ViewBag.VariationList = VariationList;

                DateTime start_date, end_date;
                ViewBag.count_pending = VariationList.Count();
                if (search.VariationStatus != null)
                {
                    if (search.StartDate != null && search.EndDate != null)
                    {
                        start_date = ((DateTime)search.StartDate).Date;
                        end_date = ((DateTime)search.EndDate).Date;

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                            ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                    else if (search.StartDate != null && search.EndDate == null)
                    {
                        start_date = ((DateTime)search.StartDate).Date;
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                    else if (search.StartDate == null && search.EndDate != null)
                    {
                        end_date = ((DateTime)search.EndDate).Date;

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                  ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where institutionCodesArray.Contains(p.InstitutionCode) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                }
            }
            else
            {
                var VariationList = (from p in db.Contracts
                                     where p.InstitutionCode == userPaystation.InstitutionCode && (p.OverallStatus == "Pending" || p.OverallStatus == "Rejected") && p.VariationStatus == "Pending"
                                     select new ContractVM
                                     {
                                         ContractId = p.ContractId,
                                         ContractNo = p.ContractNo,
                                         ContractNumber = p.ContractNumber,
                                         ContractAmount = p.ContractAmount,
                                         VariationType = p.VariationType,
                                         PreviousAmount = p.PreviousAmount,
                                         Currency = p.OperationalCurrency,
                                         OverallStatus = p.OverallStatus,
                                         Payeename = p.Payeename,
                                         GLStatus = p.GLStatus,
                                         Lotted = p.Lotted,
                                         LotNo = p.LotNo,
                                         CreatedAt = p.CreatedAt,
                                         ContractEndDate = p.ContractEndDate,
                                         ContractDescription = p.ContractDescription,
                                         TotalAmount = db.ContractDetails.Where(a => a.ContractId == p.ContractId && a.Status != "Cancelled").Select(a => a.TotalAmount).DefaultIfEmpty(0).Sum()
                                     }
           ).OrderByDescending(a => a.ContractId).ToList();
                ViewBag.VariationList = VariationList;
            
                DateTime start_date, end_date;
                ViewBag.count_pending = db.Contracts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected") && a.VariationStatus == "Pending").Count();
                if (search.VariationStatus != null)
                {
                    if (search.StartDate != null && search.EndDate != null)
                    {
                        start_date = ((DateTime)search.StartDate).Date;
                        end_date = ((DateTime)search.EndDate).Date;

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                    ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                                            ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                    else if (search.StartDate != null && search.EndDate == null)
                    {
                        start_date = ((DateTime)search.StartDate).Date;
                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) >= DbFunctions.TruncateTime(start_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                           ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                    else if (search.StartDate == null && search.EndDate != null)
                    {
                        end_date = ((DateTime)search.EndDate).Date;

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && DbFunctions.TruncateTime(p.CreatedAt) <= DbFunctions.TruncateTime(end_date) && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                        ).OrderByDescending(a => a.ContractId).ToList();
                        }



                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(search.Keyword))
                        {
                            string contract_number = search.Keyword.Trim();
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending" && p.ContractNo.Contains(contract_number)
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                  ).OrderByDescending(a => a.ContractId).ToList();
                        }
                        else
                        {
                            ContractList = (from p in db.Contracts
                                            where p.InstitutionCode == userPaystation.InstitutionCode && (p.OverallStatus == "Approved" || p.OverallStatus == "Partial" || p.OverallStatus == "FullReceived" || p.OverallStatus == "Closed") && p.VariationStatus != "Pending"
                                            select new ContractVM
                                            {
                                                ContractId = p.ContractId,
                                                ContractNo = p.ContractNo,
                                                ContractNumber = p.ContractNumber,
                                                ContractAmount = p.ContractAmount,
                                                Currency = p.OperationalCurrency,
                                                OverallStatus = p.OverallStatus,
                                                Payeename = p.Payeename,
                                                GLStatus = p.GLStatus,
                                                Lotted = p.Lotted,
                                                LotNo = p.LotNo,
                                                CreatedAt = p.CreatedAt,
                                                ContractDescription = p.ContractDescription
                                            }
                             ).OrderByDescending(a => a.ContractId).ToList();
                        }




                    }
                }

            }
             


            if (ContractList.Count == 0)
            {
                if (search.VariationStatus != null || search.StartDate != null || search.EndDate != null || !string.IsNullOrEmpty(search.Keyword))
                {
                    ViewBag.Message = "NoMatching";
                }
            }
            ViewBag.ContractList = ContractList;
            ViewBag.count_search = ContractList.Count();
            Search vm = new Search();
            if (search.StartDate != null)
            {
                vm.StartDate = search.StartDate;
            }
            if (search.EndDate != null)
            {
                vm.EndDate = search.EndDate;
            }
            if (search.Keyword != null)
            {
                vm.Keyword = search.Keyword;
            }
            return View(vm);
        }
        [Authorize(Roles = "Contract Entry")]
        public ActionResult Variation(int? id)
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var contract = (from p in db.Contracts
                            where p.ContractId == id && p.InstitutionCode == userPaystation.InstitutionCode
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNo = p.ContractNo,
                                ContractNumber = p.ContractNumber,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                ContractDescription = p.ContractDescription,
                                ContractStartDate = p.ContractStartDate,
                                ContractEndDate = p.ContractEndDate,
                                ProcurementMethod = p.ProcurementMethod,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                OperationalCurrency = p.OperationalCurrency,
                                Lotted = p.Lotted,
                                LotNo = p.LotNo,
                                LotDescription = p.LotDescription,
                                SubBudgetClass = p.SubBudgetClass,

                            }
                               ).FirstOrDefault();

            if (contract == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View(contract);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult SaveLiquidedDamage(LiquidatedDamageVM liquidatedDamageVM)
        {
            string response = null;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                //Validate deducted COA for given SBC
                var exist = (from p in db.PaymentVoucherDeductionTypes
                             join q in db.COAs on p.DeductionGfsCode equals q.GfsCode
                             where p.DeductionTypeName == "Retention" && q.SubBudgetClass == "301" && q.InstitutionId == userPaystation.InstitutionId
                             select new
                             {
                                 SubBudgetClass = q.SubBudgetClass
                             }).FirstOrDefault();
                if (exist == null)
                {
                    response = "Setup";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                ReceivingSummary summary = db.ReceivingSummarys.Find(liquidatedDamageVM.ReceivingSummaryId);
                Contract contract = db.Contracts.Find(summary.ContractId);
                decimal? amount = contract.ContractAmount * (decimal)(liquidatedDamageVM.NumberOfDays * 0.001);

                decimal? retention_ptg = summary.RetentionPercentage;
                decimal received_amount = (decimal)summary.ReceivedAmount;
                decimal? advance_payment = summary.AdvancePayment;
                if (retention_ptg != null)
                {
                    decimal ptg = (decimal)retention_ptg;
                    decimal retention_amount = ptg * (decimal)received_amount / 100;
                    received_amount = received_amount - retention_amount;
                }
                if (advance_payment > 0)
                {
                    received_amount = received_amount - (decimal)advance_payment;
                }


                //Calculate WithHolding
                decimal withHoldingAmount = 0;

                if (contract.ContractType.ToUpper() == "WORKS")
                {
                    decimal payableAmount = (decimal)summary.ReceivedAmount;
                    if (summary.VAT > 0)
                    {
                        payableAmount = payableAmount - (decimal)summary.VAT;
                    }
                    decimal serviceAmount = 2 * payableAmount / 5;
                    decimal goodsAmount = 3 * payableAmount / 5;
                    withHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                }
                //End Calculate WithHolding
                if (withHoldingAmount > 0)
                {
                    received_amount = received_amount - withHoldingAmount;
                }

                if (amount >= received_amount)
                {
                    response = "Exceed";
                }
                else
                {
                    summary.HasLiquidatedDamage = true;
                    summary.LiquidatedDamageAmount = amount;
                    summary.LiquidatedNumberOfDays = liquidatedDamageVM.NumberOfDays;
                    summary.LiquidatedSBCTo = liquidatedDamageVM.SubBudgetClass;
                    summary.LiquidedDamageBA = amount * summary.ExchangeRate;

                    //Internal Transfer data for liquidated damage

                    var bankAccounts = ServiceManager.GetBankAccountByInstitution(db, userPaystation.InstitutionCode);
                    var bank_account = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass && a.OverallStatus != "Cancelled").Select(a => a.AccountNumber).FirstOrDefault();
                    summary.BankAccountTo = bank_account;
                    //summary.AccountName = bankAccounts.Where(a => a.AccountNumber == bank_account).Select(a => a.AccountName).FirstOrDefault();
                    summary.SubBudgetClassTo = liquidatedDamageVM.SubBudgetClass;
                    summary.InstitutionCodeTo = userPaystation.InstitutionCode;
                    summary.InstitutionIdTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionId).FirstOrDefault();
                    summary.InstitutionNameTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionName).FirstOrDefault();

                    db.Entry(summary).State = EntityState.Modified;
                    db.SaveChanges();
                    response = "Success";

                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult SaveRetention(Retention retention)
        {
            string response = null;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                //Validate deducted COA for given SBC
                var exist = (from p in db.PaymentVoucherDeductionTypes
                             join q in db.COAs on p.DeductionGfsCode equals q.GfsCode
                             where p.DeductionTypeName == "Retention" && q.SubBudgetClass == "301" && q.InstitutionId == userPaystation.InstitutionId
                             select new
                             {
                                 SubBudgetClass = q.SubBudgetClass
                             }).FirstOrDefault();
                if (exist == null)
                {
                    response = "Setup";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                ReceivingSummary summary = db.ReceivingSummarys.Find(retention.id);
                Contract contract = db.Contracts.Find(summary.ContractId);
                decimal? liquidated_damage = summary.LiquidatedDamageAmount;
                decimal liq_damage_value = 0;
                if (liquidated_damage > 0)
                {
                    liq_damage_value = (decimal)liquidated_damage;
                }
                decimal received_amount = (decimal)summary.ReceivedAmount;
                decimal? retention_percentage = retention.percentage;

                decimal? retention_amount = retention_percentage * received_amount / 100;

                decimal? advance_payment = summary.AdvancePayment;
                if (advance_payment > 0)
                {
                    received_amount = received_amount - (decimal)advance_payment;
                }

                received_amount = received_amount - liq_damage_value;

                //Calculate WithHolding
                decimal withHoldingAmount = 0;

                if (contract.ContractType.ToUpper() == "WORKS")
                {
                    decimal payableAmount = (decimal)summary.ReceivedAmount;
                    if (summary.VAT > 0)
                    {
                        payableAmount = payableAmount - (decimal)summary.VAT;
                    }
                    decimal serviceAmount = 2 * payableAmount / 5;
                    decimal goodsAmount = 3 * payableAmount / 5;
                    withHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                }
                //End Calculate WithHolding
                if (withHoldingAmount > 0)
                {
                    received_amount = received_amount - withHoldingAmount;
                }

                if (retention_amount >= received_amount)
                {
                    response = "Exceed";
                }
                else
                {
                    summary.HasRetention = true;
                    summary.RetentionPercentage = retention.percentage;
                    summary.RetentionBy = retention.retentionBy;
                    if (retention.retentionBy == "BankTransfer")
                    {
                        var bankAccounts = ServiceManager.GetBankAccountByInstitution(db, userPaystation.InstitutionCode);
                        summary.BankAccountTo = retention.bankAccount;
                        summary.AccountName = bankAccounts.Where(a => a.AccountNumber == retention.bankAccount && a.OverallStatus != "Cancelled").Select(a => a.AccountName).FirstOrDefault();
                        summary.SubBudgetClassTo = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.AccountNumber == retention.bankAccount && a.OverallStatus != "Cancelled").Select(a => a.SubBudgetClass).FirstOrDefault();
                        summary.InstitutionCodeTo = userPaystation.InstitutionCode;
                        summary.InstitutionIdTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionId).FirstOrDefault();
                        summary.InstitutionNameTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionName).FirstOrDefault();

                    }
                    else
                    {
                        var bankAccounts = ServiceManager.GetBankAccountByInstitution(db, userPaystation.InstitutionCode);
                        var bank_account = db.InstitutionAccounts.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass && a.OverallStatus != "Cancelled").Select(a => a.AccountNumber).FirstOrDefault();
                        summary.BankAccountTo = bank_account;
                        //summary.AccountName = bankAccounts.Where(a => a.AccountNumber == bank_account).Select(a => a.AccountName).FirstOrDefault();
                        summary.SubBudgetClassTo = retention.SubBudgetClass;
                        summary.InstitutionCodeTo = userPaystation.InstitutionCode;
                        summary.InstitutionIdTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionId).FirstOrDefault();
                        summary.InstitutionNameTo = db.Institution.Where(a => a.InstitutionCode == userPaystation.InstitutionCode && a.OverallStatus != "Cancelled").Select(a => a.InstitutionName).FirstOrDefault();

                    }
                    RetentionDeducted retentionDeducted1 = db.RetentionDeductions.Where(a => a.ReceivingSummaryId == summary.ReceivingSummaryId).FirstOrDefault();
                    if (retentionDeducted1 != null)
                    {
                        retentionDeducted1.BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate;
                        retentionDeducted1.OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount;
                        db.Entry(retentionDeducted1).State = EntityState.Modified;
                    }
                    else
                    {
                        //Save Retention deducted to be used in paying back to Payee
                        if (summary.SubContractId > 0)
                        {
                            SubContract subContract = db.SubContracts.Find(summary.SubContractId);
                            RetentionDeducted retentionDeducted = new RetentionDeducted()
                            {
                                BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate / 100,
                                OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount / 100,
                                SubBudgetClass = summary.SubBudgetClass,
                                ExchangeRate = summary.ExchangeRate,
                                ExchangeRateAt = summary.ExchangeRateDate,
                                OperationCurrency = summary.OperationalCurrency,
                                BaseCurrency = summary.BaseCurrency,
                                ContractType = contract.ContractType,
                                PayeeDetailId = subContract.PayeeDetailId,
                                PayeeCode = subContract.PayeeCode,
                                PayeeName = subContract.PayeeName,
                                PayeeBankAccount = subContract.PayeeBankAccount,
                                PayeeBankName = subContract.PayeeBankName,
                                PayeeAccountName = subContract.PayeeAccountName,
                                PayeeAddress = subContract.PayeeAddress,
                                PayeeBIC = subContract.PayeeBIC,
                                OverallStatus = "Pending",
                                ReceivingSummaryId = summary.ReceivingSummaryId,
                                CreatedBy = User.Identity.Name,
                                CreatedAt = DateTime.Now,
                                InstitutionCode = userPaystation.InstitutionCode,
                                SubLevelCode = contract.SubLevelCode,
                                InstitutionId = contract.InstitutionId,
                                PaystationId = contract.PaystationId,
                                SubLevelDesc = contract.SubLevelDesc,
                                SubLevelCategory = contract.SubLevelCategory,
                                PayeeType = contract.PayeeType,
                                InstitutionName = contract.InstitutionName
                            };
                            db.RetentionDeductions.Add(retentionDeducted);

                        }
                        else
                        {
                            RetentionDeducted retentionDeducted = new RetentionDeducted()
                            {
                                BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate / 100,
                                OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount / 100,
                                SubBudgetClass = summary.SubBudgetClass,
                                ExchangeRate = summary.ExchangeRate,
                                ExchangeRateAt = summary.ExchangeRateDate,
                                OperationCurrency = summary.OperationalCurrency,
                                BaseCurrency = summary.BaseCurrency,
                                ContractType = contract.ContractType,
                                PayeeDetailId = contract.PayeeDetailId,
                                PayeeCode = contract.PayeeCode,
                                PayeeName = contract.Payeename,
                                PayeeBankAccount = contract.PayeeBankAccount,
                                PayeeBankName = contract.PayeeBankName,
                                PayeeAccountName = contract.PayeeAccountName,
                                PayeeAddress = contract.PayeeAddress,
                                PayeeBIC = contract.PayeeBIC,
                                OverallStatus = "Pending",
                                ReceivingSummaryId = summary.ReceivingSummaryId,
                                CreatedBy = User.Identity.Name,
                                CreatedAt = DateTime.Now,
                                InstitutionCode = userPaystation.InstitutionCode,
                                SubLevelCode = contract.SubLevelCode,
                                InstitutionId = contract.InstitutionId,
                                PaystationId = contract.PaystationId,
                                SubLevelDesc = contract.SubLevelDesc,
                                SubLevelCategory = contract.SubLevelCategory,
                                PayeeType = contract.PayeeType,
                                InstitutionName = contract.InstitutionName
                            };
                            db.RetentionDeductions.Add(retentionDeducted);
                        }
                    }
                    db.Entry(summary).State = EntityState.Modified;
                    db.SaveChanges();
                    response = "Success";

                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult SaveAccrualRetention(Retention retention)
        {
            string response = null;
            try
            {
                ReceivingSummary summary = db.ReceivingSummarys.Find(retention.id);
                Contract contract = db.Contracts.Find(summary.ContractId);
                decimal? liquidated_damage = summary.LiquidatedDamageAmount;
                decimal liq_damage_value = 0;
                if (liquidated_damage > 0)
                {
                    liq_damage_value = (decimal)liquidated_damage;
                }
                decimal received_amount = (decimal)summary.ReceivedAmount;
                decimal? retention_percentage = retention.percentage;

                decimal? retention_amount = retention_percentage * received_amount / 100;

                decimal? advance_payment = summary.AdvancePayment;
                if (advance_payment > 0)
                {
                    received_amount = received_amount - (decimal)advance_payment;
                }

                received_amount = received_amount - liq_damage_value;

                //Calculate WithHolding
                decimal withHoldingAmount = 0;

                if (contract.ContractType.ToUpper() == "WORKS")
                {
                    decimal payableAmount = (decimal)summary.ReceivedAmount;
                    if (summary.VAT > 0)
                    {
                        payableAmount = payableAmount - (decimal)summary.VAT;
                    }
                    decimal serviceAmount = 2 * payableAmount / 5;
                    decimal goodsAmount = 3 * payableAmount / 5;
                    withHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                }
                //End Calculate WithHolding to receiving
                if (withHoldingAmount > 0)
                {
                    received_amount = received_amount - withHoldingAmount;
                }

                if (retention_amount >= received_amount)
                {
                    response = "Exceed";
                }
                else
                {
                    summary.HasRetention = true;
                    summary.RetentionPercentage = retention.percentage;
                    summary.RetentionBy = "Accrual";
                    RetentionPayment retentionPayment1 = db.RetentionPayments.Where(a => a.ReceivingSummaryId == summary.ReceivingSummaryId).FirstOrDefault();
                    if (retentionPayment1 != null)
                    {
                        retentionPayment1.BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate;
                        retentionPayment1.OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount;
                        db.Entry(retentionPayment1).State = EntityState.Modified;
                    }
                    else
                    {

                        //Save Retention deducted to be used in paying back to Payee
                        if (summary.SubContractId > 0)
                        {
                            SubContract subContract = db.SubContracts.Find(summary.SubContractId);
                            RetentionPayment retentionPayment = new RetentionPayment()
                            {
                                PaymentDescription = "Contract Retention Payment",
                                BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate / 100,
                                OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount / 100,
                                SubBudgetClass = summary.SubBudgetClass,
                                ExchangeRate = summary.ExchangeRate,
                                ExchangeRateAt = summary.ExchangeRateDate,
                                OperationCurrency = summary.OperationalCurrency,
                                BaseCurrency = summary.BaseCurrency,
                                ContractType = contract.ContractType,
                                PayeeDetailId = subContract.PayeeDetailId,
                                PayeeCode = subContract.PayeeCode,
                                PayeeName = subContract.PayeeName,
                                PayeeBankAccount = subContract.PayeeBankAccount,
                                PayeeBankName = subContract.PayeeBankName,
                                PayeeAccountName = subContract.PayeeAccountName,
                                PayeeAddress = subContract.PayeeAddress,
                                PayeeBIC = subContract.PayeeBIC,
                                OverallStatus = "Incomplete",
                                Accrual = "Yes",
                                ReceivingSummaryId = summary.ReceivingSummaryId,
                                CreatedBy = User.Identity.Name,
                                CreatedAt = DateTime.Now,
                                SubLevelCode = contract.SubLevelCode,
                                InstitutionCode = summary.InstitutionCode,
                                InstitutionId = contract.InstitutionId,
                                PaystationId = contract.PaystationId,
                                SubLevelDesc = contract.SubLevelDesc,
                                SubLevelCategory = contract.SubLevelCategory,
                                PayeeType = contract.PayeeType,
                                InstitutionName = contract.InstitutionName
                            };
                            if (summary.StPaymentFlag)
                            {
                                retentionPayment.StPaymentFlag = true;
                                retentionPayment.ParentInstitutionCode = summary.ParentInstitutionCode;
                                retentionPayment.ParentInstitutionName = summary.ParentInstitutionName;
                                retentionPayment.SubWarrantCode = summary.SubWarrantCode;
                                retentionPayment.SubWarrantDescription = summary.SubWarrantDescription;
                            }
                            db.RetentionPayments.Add(retentionPayment);

                            db.SaveChanges();
                            retentionPayment.LegalNumber = ServiceManager.GetLegalNumber(db, summary.InstitutionCode, "RT", retentionPayment.RetentionPaymentId);
                            db.Entry(retentionPayment).State = EntityState.Modified;
                        }
                        else
                        {
                            RetentionPayment retentionPayment = new RetentionPayment()
                            {
                                PaymentDescription = "Contract Retention Payment",
                                BaseAmount = summary.RetentionPercentage * summary.ReceivedAmount * summary.ExchangeRate / 100,
                                OperationAmount = summary.RetentionPercentage * summary.ReceivedAmount / 100,
                                SubBudgetClass = summary.SubBudgetClass,
                                ExchangeRate = summary.ExchangeRate,
                                ExchangeRateAt = summary.ExchangeRateDate,
                                OperationCurrency = summary.OperationalCurrency,
                                BaseCurrency = summary.BaseCurrency,
                                ContractType = contract.ContractType,
                                PayeeDetailId = contract.PayeeDetailId,
                                PayeeCode = contract.PayeeCode,
                                PayeeName = contract.Payeename,
                                PayeeBankAccount = contract.PayeeBankAccount,
                                PayeeBankName = contract.PayeeBankName,
                                PayeeAccountName = contract.PayeeAccountName,
                                PayeeAddress = contract.PayeeAddress,
                                PayeeBIC = contract.PayeeBIC,
                                OverallStatus = "Incomplete",
                                Accrual = "Yes",
                                ReceivingSummaryId = summary.ReceivingSummaryId,
                                CreatedBy = User.Identity.Name,
                                CreatedAt = DateTime.Now,
                                InstitutionCode = summary.InstitutionCode,
                                SubLevelCode = contract.SubLevelCode,
                                InstitutionId = contract.InstitutionId,
                                PaystationId = contract.PaystationId,
                                SubLevelDesc = contract.SubLevelDesc,
                                SubLevelCategory = contract.SubLevelCategory,
                                PayeeType = contract.PayeeType,
                                InstitutionName = contract.InstitutionName
                            };
                            if (summary.StPaymentFlag)
                            {
                                retentionPayment.StPaymentFlag = true;
                                retentionPayment.ParentInstitutionCode = summary.ParentInstitutionCode;
                                retentionPayment.ParentInstitutionName = summary.ParentInstitutionName;
                                retentionPayment.SubWarrantCode = summary.SubWarrantCode;
                                retentionPayment.SubWarrantDescription = summary.SubWarrantDescription;
                            }
                            db.RetentionPayments.Add(retentionPayment);
                            db.SaveChanges();
                            retentionPayment.LegalNumber = ServiceManager.GetLegalNumber(db, summary.InstitutionCode, "RT", retentionPayment.RetentionPaymentId);
                            db.Entry(retentionPayment).State = EntityState.Modified;

                        }
                    }
                    db.Entry(summary).State = EntityState.Modified;
                    db.SaveChanges();

                    response = "Success";

                }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult DeleteLiqDamage(int? id)
        {
            string response = null;
            try
            {
                ReceivingSummary summary = db.ReceivingSummarys.Find(id);
                summary.HasLiquidatedDamage = false;
                summary.LiquidatedDamageAmount = null;
                summary.LiquidedDamageBA = null;
                summary.LiquidatedNumberOfDays = null;
                summary.LiquidatedSBCTo = null;
                if (summary.RetentionPercentage == null)
                {
                    summary.InstitutionCodeTo = null;
                    summary.InstitutionIdTo = null;
                    summary.InstitutionNameTo = null;
                }

                db.Entry(summary).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult DeleteRetention(Retention retention)
        {
            string response = null;
            try
            {
                ReceivingSummary summary = db.ReceivingSummarys.Find(retention.id);
                summary.HasRetention = false;
                summary.RetentionBy = null;
                summary.RetentionPercentage = null;
                summary.AccountName = null;
                summary.BankAccountTo = null;
                summary.SubBudgetClassTo = null;

                if (summary.LiquidatedDamageAmount == null)
                {
                    summary.InstitutionCodeTo = null;
                    summary.InstitutionIdTo = null;
                    summary.InstitutionNameTo = null;
                }
                RemoveRetention(summary.ReceivingSummaryId);
                db.Entry(summary).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

      
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetVouchers(string PayeeCode)
        {
            //db.Database.CommandTimeout = 120;
            List<AdvancePaymentVM> advancePaymentsList = new List<AdvancePaymentVM>();
            string response = null;
            try
            {
                var previousPaidList = db.AdvancePayments.Where(a => a.PayeeCode == PayeeCode).ToList();
                var paidVouchersList = db.PaymentVouchers.Where(a => a.PayeeCode == PayeeCode && a.SourceModule == "Normal Voucher" && (a.OverallStatus.ToUpper() == "APPROVED" || a.OverallStatus == "Sent to BOT" || a.OverallStatus.ToUpper() == "ACCEPTED" || a.OverallStatus.ToUpper() == "PROCESSED" || a.OverallStatus.ToUpper() == "SETTLED" || a.OverallStatus == "UNAPPLIED")).ToList();
                if (paidVouchersList.Count() == 0)
                {
                    response = "NoVourcher";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    foreach (var item in paidVouchersList)
                    {
                        AdvancePaymentVM advancePaymentVM = new AdvancePaymentVM();
                        advancePaymentVM.PaymentVoucherId = item.PaymentVoucherId;
                        advancePaymentVM.Narration = item.Narration;
                        advancePaymentVM.PVNo = item.PVNo;
                        advancePaymentVM.Amount = item.OperationalAmount;
                        advancePaymentVM.Currency = item.OperationalCurrency;

                        decimal? advancePayment = previousPaidList.Where(a => a.PaymentVoucherId == item.PaymentVoucherId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                        if (advancePayment > 0)
                        {
                            advancePaymentVM.Balance = item.OperationalAmount - advancePayment;
                        }
                        else
                        {
                            advancePaymentVM.Balance = item.OperationalAmount;
                        }
                        if (advancePaymentVM.Balance > 0)
                        {
                            advancePaymentsList.Add(advancePaymentVM);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data = advancePaymentsList }, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetAdvPaymentVouchers(int? id)
        {
            //db.Database.CommandTimeout = 120;
            List<AdvancePaymentVM> advancePaymentsList = new List<AdvancePaymentVM>();
            string response = null;
            try
            {
                var previousPaidList = db.AdvancePayments.Where(a => a.ContractId == id).ToList();
                var receivingIds = db.ReceivingSummarys.Where(a => a.ContractId == id && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type == "AdvancePayment").Select(a => a.ReceivingSummaryId).ToList();

                int count = 0;
                foreach (var item in receivingIds)
                {
                    PaymentVoucher paymentVoucher = db.PaymentVouchers.Where(a => a.OtherSourceId == item && a.SourceModule == "Advance Payment" && a.OverallStatus.ToUpper() != "CANCELLED").FirstOrDefault();
                    if (paymentVoucher != null)
                    {
                        AdvancePaymentVM advancePaymentVM = new AdvancePaymentVM();
                        advancePaymentVM.PaymentVoucherId = paymentVoucher.PaymentVoucherId;
                        advancePaymentVM.Narration = paymentVoucher.Narration;
                        advancePaymentVM.PVNo = paymentVoucher.PVNo;
                        advancePaymentVM.Amount = paymentVoucher.OperationalAmount;
                        advancePaymentVM.Currency = paymentVoucher.OperationalCurrency;
                        count = count + 1;
                        decimal? advancePayment = previousPaidList.Where(a => a.PaymentVoucherId == paymentVoucher.PaymentVoucherId).Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                        if (advancePayment > 0)
                        {
                            advancePaymentVM.Balance = paymentVoucher.OperationalAmount - advancePayment;
                        }
                        else
                        {
                            advancePaymentVM.Balance = paymentVoucher.OperationalAmount;
                        }
                        if (advancePaymentVM.Balance > 0)
                        {
                            advancePaymentsList.Add(advancePaymentVM);
                        }
                    }
                }
                if (count == 0)
                {
                    response = "NoVourcher";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }


            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data = advancePaymentsList }, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult ReceivingEntry()
        {

            return View();

        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetContracts()
        {
            List<AdvancePaymentVM> advancePaymentsList = new List<AdvancePaymentVM>();
            string response = null;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                response = CheckSpeciaReceiving(userPaystation);
                if (response == "NotAuthorizes")
                {
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                ProcurementController procurement = new ProcurementController();
                List<ContractVM> contractList = null;

                if (procurement.IsTarura(userPaystation.InstitutionCode))
                {
                    string[] institutionCodesArray = procurement.getInstutionCodes(userPaystation.InstitutionCode);
                    if (User.IsInRole("Works Only Receiving Entry"))
                    {
                        contractList = (from p in db.Contracts
                                        where institutionCodesArray.Contains(p.InstitutionCode) && p.ContractType == "Works" && (p.OverallStatus.ToUpper() == "APPROVED" || p.OverallStatus.ToUpper() == "PARTIAL") && p.HasSubContract != "Pending" 
                                        select new ContractVM
                                        {
                                            ContractId = p.ContractId,
                                            ContractNo = p.ContractNo,
                                            ContractNumber = p.ContractNumber,
                                            ContractName = p.ContractName,
                                            ContractDescription = p.ContractDescription,
                                            ContractAmount = p.ContractAmount,
                                            ContractType = p.ContractType,
                                            ContractVersion = p.ContractVersion,
                                            SubBudgetClass = p.SubBudgetClass,
                                            Payeename = p.Payeename,
                                            Currency = p.OperationalCurrency,
                                            PayeeCode = p.PayeeCode,
                                            HasSubContract = p.HasSubContract
                                        }).OrderByDescending(a => a.ContractId).ToList();

                    }
                    else
                    {
                        contractList = (from p in db.Contracts
                                        where institutionCodesArray.Contains(p.InstitutionCode) && p.ContractType != "Works" && (p.OverallStatus.ToUpper() == "APPROVED" || p.OverallStatus.ToUpper() == "PARTIAL") && p.HasSubContract != "Pending"
                                        select new ContractVM
                                        {
                                            ContractId = p.ContractId,
                                            ContractNo = p.ContractNo,
                                            ContractNumber = p.ContractNumber,
                                            ContractName = p.ContractName,
                                            ContractDescription = p.ContractDescription,
                                            ContractAmount = p.ContractAmount,
                                            ContractType = p.ContractType,
                                            ContractVersion = p.ContractVersion,
                                            SubBudgetClass = p.SubBudgetClass,
                                            Payeename = p.Payeename,
                                            Currency = p.OperationalCurrency,
                                            PayeeCode = p.PayeeCode,
                                            HasSubContract = p.HasSubContract
                                        }).OrderByDescending(a => a.ContractId).ToList();
                    }

                }
                else
                {

                    contractList = (from p in db.Contracts
                                    where p.InstitutionCode == userPaystation.InstitutionCode && (p.OverallStatus.ToUpper() == "APPROVED" || p.OverallStatus.ToUpper() == "PARTIAL") && p.HasSubContract != "Pending"
                                    select new ContractVM
                                    {
                                        ContractId = p.ContractId,
                                        ContractNo = p.ContractNo,
                                        ContractNumber = p.ContractNumber,
                                        ContractName = p.ContractName,
                                        ContractAmount = p.ContractAmount,
                                        ContractType = p.ContractType,
                                        ContractVersion = p.ContractVersion,
                                        SubBudgetClass = p.SubBudgetClass,
                                        Payeename = p.Payeename,
                                        Currency = p.OperationalCurrency,
                                        PayeeCode = p.PayeeCode,
                                        HasSubContract = p.HasSubContract
                                    }).OrderByDescending(a => a.ContractId).ToList();

                }

                if (contractList.Count == 0)
                {
                    response = "NoContract";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                foreach (var item in contractList)
                {
                    decimal? paidAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == item.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.AdvancePayment).DefaultIfEmpty(0).Sum();
                    if (paidAdvancePayment > 0)
                    {
                        item.AdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == item.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - paidAdvancePayment;

                    }
                    else
                    {
                        item.AdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == item.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    }
                    item.ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == item.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                    item.RemainingAmount = item.ContractAmount - item.ReceivedAmount;
                }

                return Json(new { data = contractList }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult ReceiveByAmount(ReceiveByAmount receiveByAmountVM)
        {
            string response = null;
            ReceivingResponse receivingResponse = new ReceivingResponse();
            using (var trans = db.Database.BeginTransaction())
            {
       
            try
            {
                int? paymentScheduleId;
                    InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                    if (receiveByAmountVM.EntryType == "SubContract")
                {
                    SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Find(receiveByAmountVM.PaymentScheduleId);
                    paymentScheduleId = subContractPaymentSchedule.PaymentScheduleId;
                }
                else
                {
                    paymentScheduleId = receiveByAmountVM.PaymentScheduleId;
                }

                PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(paymentScheduleId);
                Contract contract = db.Contracts.Where(a => a.ContractId == paymentSchedule.ContractId).FirstOrDefault();
                decimal? totalReceived;
                if (receiveByAmountVM.Amount == 0)
                {

                    decimal? receivingAmount;
                    ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                    if (contractPayment == null)
                    {
                        receivingAmount = receiveByAmountVM.CertificateAmount;
                    }
                    else
                    {
                        receivingAmount = contractPayment.Balance;
                    }
                    totalReceived = receivingAmount + db.ReceivingSummarys.Where(a => a.ContractId == paymentSchedule.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                }
                else
                {

                    totalReceived = receiveByAmountVM.Amount + db.ReceivingSummarys.Where(a => a.ContractId == paymentSchedule.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                }

                if (contract.ContractVersion > 1)
                {
                    //CALCULATE TOTAL ADVANCE PAYMENT PAID TO THIS CONTRACT
                    decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    if (contractAdvancePayment > 0)
                    {
                        receivingResponse = VersionTwoWithAdvancePayment(receiveByAmountVM, contract, paymentSchedule, totalReceived, userPaystation.InstitutionCode);
                        //Receave Accrual(Receave Amount remains in Certificate) Contract without Advance Payment
                        if (receivingResponse.id > 0)
                        {
                            if (receiveByAmountVM.Amount >= 0)
                            {
                                string respond = ReceaveAccrualWithAdvancePayment(receivingResponse.id, contractAdvancePayment);
                            }

                        }
                    }
                    else
                    {
                        receivingResponse = WithoutAdvancePayment(receiveByAmountVM, contract, paymentSchedule, totalReceived, userPaystation.InstitutionCode);
                        //Receave Accrual(Receave Amount remains in Certificate) Contract without Advance Payment
                        if (receivingResponse.id > 0)
                        {
                            if (receiveByAmountVM.Amount >= 0)
                            {
                                string respond = ReceaveAccrualWithoutAdvancePayment(receivingResponse.id);
                            }

                        }
                    }


                }
                else
                {
                    if (receiveByAmountVM.AdvPaymentReceivingVMs != null)
                    {
                        decimal advancePayments = (decimal)receiveByAmountVM.AdvPaymentReceivingVMs.Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();
                        receivingResponse = VersionOneWithAdvancePayment(receiveByAmountVM, contract, paymentSchedule, totalReceived, userPaystation.InstitutionCode);
                    }
                    else
                    {
                        receivingResponse = WithoutAdvancePayment(receiveByAmountVM, contract, paymentSchedule, totalReceived, userPaystation.InstitutionCode);
                        //Receave Accrual(Receave Amount remains in Certificate) Contract without Advance Payment
                        if (receivingResponse.id > 0)
                        {
                            if (receiveByAmountVM.Amount == 0)
                            {
                                string respond = ReceaveAccrualWithoutAdvancePayment(receivingResponse.id);
                            }

                        }
                    }
                }

                    trans.Commit();
                }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                trans.Rollback();
                var result_data2 = new { response = response };
                return Json(result_data2, JsonRequestBehavior.AllowGet);
            }
        }
            var result_data = new { response = receivingResponse.Response, amount = receivingResponse.AdvancePayment };
            return Json(result_data, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult ReceiveItems(ReceivingVM model)
        {
            using (var trans = db.Database.BeginTransaction())
            {
                string response = null;
            ReceivingResponse receivingResponse = new ReceivingResponse();
            try
            {
                    InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                    decimal sum = model.ItemsReceived.Select(a => a.Amount).DefaultIfEmpty(0).Sum();
                Contract contract = db.Contracts.Where(a => a.ContractId == model.ContractId).FirstOrDefault();
                decimal? totalReceived = sum + db.ReceivingSummarys.Where(a => a.ContractId == model.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                if (contract.ContractVersion > 1)
                {
                    decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                    if (contractAdvancePayment > 0)
                    {
                        PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                        decimal? received_in_schedule = sum + db.ReceivingSummarys.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                        receivingResponse = ByItemsVersionTwoWithAdvancePayment(contract, paymentSchedule, model, sum, totalReceived, received_in_schedule, userPaystation.InstitutionCode);


                    }
                    else
                    {
                        PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                        decimal? received_in_schedule = sum + db.ReceivingSummarys.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                        receivingResponse = ByItemsWithoutAdvancePayment(contract, paymentSchedule, model, sum, totalReceived, received_in_schedule, userPaystation.InstitutionCode);
                    }
                }
                else
                {


                    if (model.AdvPaymentReceivingVMs != null)
                    {
                        decimal advancePayments = (decimal)model.AdvPaymentReceivingVMs.Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();
                        if (advancePayments >= sum)
                        {
                            receivingResponse.Response = "AdvanceExceed";
                        }
                        else
                        {
                            PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                            decimal? received_in_schedule = sum + db.ReceivingSummarys.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                            receivingResponse = ByItemsVersionOneWithAdvancePayment(contract, paymentSchedule, model, sum, totalReceived, received_in_schedule, userPaystation.InstitutionCode);
                        }
                    }
                    else
                    {

                        PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(model.PaymentScheduleId);
                        decimal? received_in_schedule = sum + db.ReceivingSummarys.Where(a => a.PaymentScheduleId == model.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

                        receivingResponse = ByItemsWithoutAdvancePayment(contract, paymentSchedule, model, sum, totalReceived, received_in_schedule, userPaystation.InstitutionCode);


                    }
                }

                    trans.Commit();
             }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
                trans.Rollback();
                 var result_data2 = new { response = response};
                return Json(result_data2, JsonRequestBehavior.AllowGet);
            }

            var result_data = new { response = receivingResponse.Response, amount = receivingResponse.AdvancePayment };
            return Json(result_data, JsonRequestBehavior.AllowGet);
            }
        }

        public void SaveAssetsAndInventories(Contract contract, ReceivingSummary receivingSummary, PaymentSchedule paymentSchedule)
        {

            List<Receiving> receivings = new List<Receiving>();
            List<AssetDetail> assetDetails = new List<AssetDetail>();
            Decimal Total_VAT = 0;
            var itemsInSchedule = db.ContractDetails.Where(a => a.ContractId == paymentSchedule.ContractId && a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.Status != "Cancelled").ToList();
            foreach (var item in itemsInSchedule)
            {
                Decimal VAT = 0;
                var VatRate = 0.18;
                Decimal AssetValue = (Decimal)item.UnitPrice;
                decimal total_asset_value = (decimal)item.TotalAmount;
                if (item.VatStatus == "Applicable")
                {
                    if (item.VAT > 0)
                    {
                        VAT = (Decimal)item.VAT;
                        Total_VAT = Total_VAT + VAT;
                        AssetValue = AssetValue + (decimal)VatRate * AssetValue;

                    }
                }
                Receiving receiving = new Receiving()
                {
                    ItemDesc = item.ItemDesc,
                    Amount = item.TotalAmount,
                    ReceivedQuantity = item.Quantity,
                    ContractId = paymentSchedule.ContractId,
                    PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                    ContractDetailId = item.ContractDetailId,
                    ReceiveDate = DateTime.Now,
                    Vat = (decimal)VAT,
                    UOM = item.UOM,
                    UnitPrice = item.UnitPrice,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                    SubLevelCategory = null,
                    ClassId = item.ClassId,
                    ItemClassificationId = item.ItemClassificationId
                };
                receivings.Add(receiving);

                if (item.ClassId == 2)
                {
                    decimal quantity = (Decimal)(item.Quantity);
                    decimal item_quantity = Math.Round(quantity);
                    int NumberOfAssets = (int)item_quantity;

                    ReceivedAssets summaryReceived = new ReceivedAssets()
                    {
                        AssetName = item.ItemDesc,
                        ContractDetailId = item.ContractDetailId,
                        SubLevelCategory = contract.SubLevelCategory,
                        SubLevelCode = contract.SubLevelCode,
                        SubLevelDesc = contract.SubLevelDesc,
                        AssetsValue = (Decimal)total_asset_value,
                        Quantity = NumberOfAssets,
                        ReceivingNumber = receivingSummary.ReceivingNumber,
                        ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                        ReceivingDetailId = receivingSummary.ReceivingSummaryId,
                        InstitutionCode = receivingSummary.InstitutionCode,
                        InstitutionId = contract.InstitutionId,
                        OverallStatus = "NotReceived",
                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                        OperationCurrency = contract.OperationalCurrency,
                        JournalCode = "PP",
                        SourceModule = "Contract",
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now
                    };

                    db.ReceivedAssets.Add(summaryReceived);
                    db.SaveChanges();
                    //Generate and Save Legal number
                    var currentId = summaryReceived.ReceivedAssetsId;
                    summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "AS", currentId);
                    db.Entry(summaryReceived).State = EntityState.Modified;
                    db.SaveChanges();

                    int i = 0;
                    while (i < NumberOfAssets)
                    {
                        AssetDetail assetDetail = new AssetDetail()
                        {

                            AssetName = item.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            Currency = contract.OperationalCurrency,
                            AssetValue = (Decimal)AssetValue,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = receivingSummary.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            ReceivedAssetsId = currentId
                        };

                        assetDetails.Add(assetDetail);
                        i++;
                    }

                }
                else if (item.ClassId == 3)
                {
                    if (item.Quantity > 0)
                    {

                        InventoryDetail inventoryDetail = new InventoryDetail()
                        {
                            ItemName = item.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            SubLevelCategory = contract.SubLevelCategory,
                            SubLevelCode = contract.SubLevelCode,
                            SubLevelDesc = contract.SubLevelDesc,
                            UnitPrice = item.UnitPrice,
                            InventoryValue = item.TotalAmount,
                            UOM = item.UOM,
                            Quantity = item.Quantity,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = receivingSummary.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            OperationCurrency = contract.OperationalCurrency,
                            JournalCode = "IV",
                            SourceModule = "Contract",
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now
                        };

                        db.InventoryDetails.Add(inventoryDetail);
                        db.SaveChanges();
                        //Generate and save Legal number
                        var currentId = inventoryDetail.InventoryDetailId;
                        inventoryDetail.InventoryCode = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "IV", currentId);
                        db.Entry(inventoryDetail).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

            }
            db.Receivings.AddRange(receivings);
            db.AssetDetails.AddRange(assetDetails);

        }
        public void SaveAssetsAndInventoriesTwo(Contract contract, ReceivingSummary receivingSummary, PaymentSchedule paymentSchedule)
        {


            List<Receiving> receivings = new List<Receiving>();
            List<AssetDetail> assetDetails = new List<AssetDetail>();
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            Decimal Total_VAT = 0;

            //List of Items Remains
            List<ContractDetail> contractDetails = new List<ContractDetail>();
            var contract_items = db.ContractDetails.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.Status != "Cancelled").ToList();
            var received = db.Receivings.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId).ToList();
            foreach (var item in contract_items)
            {
                ContractDetail contractDetail = new ContractDetail()
                {
                    ContractDetailId = item.ContractDetailId,
                    ItemClassificationId = item.ItemClassificationId,
                    ClassId = item.ClassId,
                    ItemDesc = item.ItemDesc,
                    UOM = item.UOM,
                    UnitPrice = item.UnitPrice,
                    VatStatus = item.VatStatus,
                    Quantity = item.Quantity - (int)received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.ReceivedQuantity).DefaultIfEmpty(0).Sum(),
                    VAT = item.VAT - received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.Vat).DefaultIfEmpty(0).Sum(),
                    TotalAmount = item.TotalAmount - received.Where(a => a.ContractDetailId == item.ContractDetailId).Select(a => a.Amount).DefaultIfEmpty(0).Sum()
                };
                contractDetails.Add(contractDetail);
            }
            //End of Item remains

            foreach (var item in contractDetails)
            {
                Decimal VAT = 0;
                var VatRate = 0.18;
                Decimal AssetValue = (Decimal)item.UnitPrice;
                decimal total_asset_value = (decimal)item.TotalAmount;
                if (item.VatStatus == "Applicable")
                {
                    if (item.VAT > 0)
                    {
                        VAT = (Decimal)item.VAT;
                        Total_VAT = Total_VAT + VAT;
                        AssetValue = AssetValue + (decimal)VatRate * AssetValue;

                    }
                }

                Receiving receiving = new Receiving()
                {
                    ItemDesc = item.ItemDesc,
                    Amount = item.TotalAmount,
                    ReceivedQuantity = item.Quantity,
                    ContractId = paymentSchedule.ContractId,
                    PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                    ContractDetailId = item.ContractDetailId,
                    ReceiveDate = DateTime.Now,
                    Vat = (decimal)VAT,
                    UOM = item.UOM,
                    UnitPrice = item.UnitPrice,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                    SubLevelCategory = null,
                    ClassId = item.ClassId,
                    ItemClassificationId = item.ItemClassificationId
                };
                receivings.Add(receiving);

                if (item.ClassId == 2)
                {
                    decimal quantity = (Decimal)(item.Quantity);
                    decimal item_quantity = Math.Round(quantity);
                    int NumberOfAssets = (int)item_quantity;

                    ReceivedAssets summaryReceived = new ReceivedAssets()
                    {
                        AssetName = item.ItemDesc,
                        ContractDetailId = item.ContractDetailId,
                        SubLevelCategory = contract.SubLevelCategory,
                        SubLevelCode = contract.SubLevelCode,
                        SubLevelDesc = contract.SubLevelDesc,
                        AssetsValue = (Decimal)total_asset_value,
                        Quantity = NumberOfAssets,
                        ReceivingNumber = receivingSummary.ReceivingNumber,
                        ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                        ReceivingDetailId = receivingSummary.ReceivingSummaryId,
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionId = contract.InstitutionId,
                        OverallStatus = "NotReceived",
                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                        OperationCurrency = contract.OperationalCurrency,
                        JournalCode = "PP",
                        SourceModule = "Contract",
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now
                    };

                    db.ReceivedAssets.Add(summaryReceived);
                    db.SaveChanges();
                    //Generate and Save Legal number
                    var currentId = summaryReceived.ReceivedAssetsId;
                    summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "AS", currentId);
                    db.Entry(summaryReceived).State = EntityState.Modified;
                    db.SaveChanges();

                    int i = 0;
                    while (i < NumberOfAssets)
                    {
                        AssetDetail assetDetail = new AssetDetail()
                        {

                            AssetName = item.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            Currency = contract.OperationalCurrency,
                            AssetValue = (Decimal)AssetValue,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = userPaystation.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            ReceivedAssetsId = currentId
                        };

                        assetDetails.Add(assetDetail);
                        i++;
                    }

                }
                else if (item.ClassId == 3)
                {
                    if (item.Quantity > 0)
                    {

                        InventoryDetail inventoryDetail = new InventoryDetail()
                        {
                            ItemName = item.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            SubLevelCategory = contract.SubLevelCategory,
                            SubLevelCode = contract.SubLevelCode,
                            SubLevelDesc = contract.SubLevelDesc,
                            UnitPrice = item.UnitPrice,
                            InventoryValue = item.TotalAmount,
                            UOM = item.UOM,
                            Quantity = item.Quantity,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = userPaystation.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            OperationCurrency = contract.OperationalCurrency,
                            JournalCode = "IV",
                            SourceModule = "Contract",
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now
                        };

                        db.InventoryDetails.Add(inventoryDetail);
                        db.SaveChanges();
                        //Generate and save Legal number
                        var currentId = inventoryDetail.InventoryDetailId;
                        inventoryDetail.InventoryCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "IV", currentId);
                        db.Entry(inventoryDetail).State = EntityState.Modified;
                        db.SaveChanges();

                    }
                }

            }
            db.Receivings.AddRange(receivings);
            db.AssetDetails.AddRange(assetDetails);

        }
        public Double SaveAssetsAndInventoriesThree(Contract contract, ReceivingSummary receivingSummary, PaymentSchedule paymentSchedule, IEnumerable<ItemReceived> ItemsReceived)
        {

            List<Receiving> receivings = new List<Receiving>();
            List<AssetDetail> assetDetails = new List<AssetDetail>();
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            Double Total_VAT = 0;
            foreach (var item in ItemsReceived)
            {
                var contractDetail = db.ContractDetails.Where(a => a.ContractDetailId == item.ContractDetailId && a.Status != "Cancelled").FirstOrDefault();

                Double VAT = 0;
                var VatRate = 0.18;
                Double AssetValue = (Double)contractDetail.UnitPrice;
                Double total_asset_value = (Double)(item.Quantity * contractDetail.UnitPrice);
                if (contractDetail.VatStatus == "Applicable")
                {
                    if (item.Quantity * contractDetail.UnitPrice != item.Amount)
                    {
                        VAT = (Double)item.Amount * VatRate;
                        Total_VAT = Total_VAT + VAT;
                        AssetValue = AssetValue + VatRate * AssetValue;
                        total_asset_value = total_asset_value + VAT;
                    }
                }
                Receiving receiving = new Receiving()
                {
                    ItemDesc = contractDetail.ItemDesc,
                    Amount = item.Amount,
                    ReceivedQuantity = item.Quantity,
                    ContractId = contract.ContractId,
                    PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                    ContractDetailId = item.ContractDetailId,
                    ReceiveDate = DateTime.Now,
                    Vat = (decimal)VAT,
                    UOM = contractDetail.UOM,
                    UnitPrice = contractDetail.UnitPrice,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                    SubLevelCategory = null,
                    ClassId = contractDetail.ClassId,
                    ItemClassificationId = contractDetail.ItemClassificationId
                };
                receivings.Add(receiving);

                if (contractDetail.ClassId == 2)
                {
                    decimal quantity = (Decimal)(item.Quantity);
                    decimal item_quantity = Math.Round(quantity);
                    int NumberOfAssets = (int)item_quantity;
                    ReceivedAssets summaryReceived = new ReceivedAssets()
                    {
                        AssetName = contractDetail.ItemDesc,
                        ContractDetailId = item.ContractDetailId,
                        SubLevelCategory = contract.SubLevelCategory,
                        SubLevelCode = contract.SubLevelCode,
                        SubLevelDesc = contract.SubLevelDesc,
                        AssetsValue = (Decimal)total_asset_value,
                        Quantity = NumberOfAssets,
                        ReceivingNumber = receivingSummary.ReceivingNumber,
                        ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                        ReceivingDetailId = receivingSummary.ReceivingSummaryId,
                        InstitutionCode = userPaystation.InstitutionCode,
                        InstitutionId = contract.InstitutionId,
                        OverallStatus = "NotReceived",
                        FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                        OperationCurrency = contract.OperationalCurrency,
                        JournalCode = "PP",
                        SourceModule = "Contract",
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now
                    };

                    db.ReceivedAssets.Add(summaryReceived);
                    db.SaveChanges();
                    //Generate and Save Legal number
                    var currentId = summaryReceived.ReceivedAssetsId;
                    summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "AS", currentId);
                    db.Entry(summaryReceived).State = EntityState.Modified;
                    db.SaveChanges();

                    int i = 0;
                    while (i < NumberOfAssets)
                    {
                        AssetDetail assetDetail = new AssetDetail()
                        {

                            AssetName = contractDetail.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            Currency = contract.OperationalCurrency,
                            AssetValue = (Decimal)AssetValue,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = userPaystation.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            ReceivedAssetsId = currentId
                        };

                        assetDetails.Add(assetDetail);
                        i++;
                    }

                }
                else if (contractDetail.ClassId == 3)
                {
                    if (item.Quantity > 0)
                    {

                        InventoryDetail inventoryDetail = new InventoryDetail()
                        {
                            ItemName = contractDetail.ItemDesc,
                            ContractDetailId = item.ContractDetailId,
                            SubLevelCategory = contract.SubLevelCategory,
                            SubLevelCode = contract.SubLevelCode,
                            SubLevelDesc = contract.SubLevelDesc,
                            UnitPrice = contractDetail.UnitPrice,
                            InventoryValue = item.Amount,
                            UOM = contractDetail.UOM,
                            Quantity = item.Quantity,
                            ReceivingNumber = receivingSummary.ReceivingNumber,
                            ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                            InstitutionCode = userPaystation.InstitutionCode,
                            InstitutionId = contract.InstitutionId,
                            OverallStatus = "NotReceived",
                            FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            OperationCurrency = contract.OperationalCurrency,
                            JournalCode = "IV",
                            SourceModule = "Contract",
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now
                        };

                        db.InventoryDetails.Add(inventoryDetail);
                        db.SaveChanges();
                        //Generate and save Legal number
                        var currentId = inventoryDetail.InventoryDetailId;
                        inventoryDetail.InventoryCode = ServiceManager.GetLegalNumber(db, userPaystation.InstitutionCode, "IV", currentId);
                        db.Entry(inventoryDetail).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

            }
            db.Receivings.AddRange(receivings);
            db.AssetDetails.AddRange(assetDetails);
            return Total_VAT;
        }
        public ReceivingResponse VersionOneWithAdvancePayment(ReceiveByAmount receiveByAmountVM, Contract contract, PaymentSchedule paymentSchedule, decimal? totalReceived,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            decimal advancePayments = (decimal)receiveByAmountVM.AdvPaymentReceivingVMs.Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();

            if (advancePayments >= receiveByAmountVM.Amount)
            {
                receivingResponse.Response = "AdvanceExceed";
            }
            else
            {


                List<AdvancePayment> advancePaymentList = new List<AdvancePayment>();


                //Receive with Advance Payment

                decimal? previous_received = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == receiveByAmountVM.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
                //decimal? previous = payment_schedule.ReceivedAmount;
                var received_status = paymentSchedule.Received;
                decimal? total = 0;
                decimal? receivingAmount; ;
                if (receiveByAmountVM.Amount == 0)
                {

                    ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                    if (contractPayment == null)
                    {
                        receivingAmount = receiveByAmountVM.CertificateAmount;
                    }
                    else
                    {
                        receivingAmount = contractPayment.Balance;
                    }
                }
                else
                {
                    receivingAmount = receiveByAmountVM.Amount;
                }
                if (previous_received == null)
                {
                    total = (decimal)receiveByAmountVM.Amount;
                }
                else
                {
                    total = previous_received + receivingAmount;
                }


                if (total > paymentSchedule.Amount)
                {
                    receivingResponse.Response = "Exceed";
                }
                else
                {
                    ReceivingResponse responseReceiving = SaveReceivingSummaryTwo(contract, paymentSchedule, receivingAmount, totalReceived, advancePayments, institutionCode);
                    if (responseReceiving.Response == "Success")
                    {
                        ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                        if (receiveByAmountVM.Amount > 0)
                        {
                            receivingSummary.Accrual = receiveByAmountVM.Accrual;
                        }
                        if (received_status == null)
                        {
                            receivingResponse.id = receivingSummary.ReceivingSummaryId;
                            SaveAdvancePaymentDetails(receiveByAmountVM.AdvPaymentReceivingVMs, receivingSummary);
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                            ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                            SetReceivingNumber(contract, receivingSummary);
                            receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            SaveAssetsAndInventories(contract, receivingSummary, paymentSchedule);
                            receivingResponse.Response = SetReceivingStatus(contract, paymentSchedule, receivingSummary, totalReceived, total);

                        }
                        else if (received_status == "Partial")
                        {
                            receivingResponse.id = receivingSummary.ReceivingSummaryId;
                            SetReceivingNumber(contract, receivingSummary);
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                            SaveAssetsAndInventoriesTwo(contract, receivingSummary, paymentSchedule);

                            ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                            if (contract_payment == null)
                            {

                                ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);

                                receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            }
                            else
                            {
                                contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                                contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                                db.Entry(contract_payment).State = EntityState.Modified;
                                receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;

                            }

                            receivingResponse.Response = SetReceivingStatusTwo(contract, paymentSchedule, receivingSummary, totalReceived, total);

                        }
                        else
                        {
                            receivingResponse.id = receivingSummary.ReceivingSummaryId;
                            SetReceivingNumber(contract, receivingSummary);
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);

                            ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                            if (contract_payment == null)
                            {
                                ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                                receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            }
                            else
                            {
                                contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                                contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                                db.Entry(contract_payment).State = EntityState.Modified;
                                receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;
                            }
                            receivingResponse.Response = SetReceivingStatusThree(contract, paymentSchedule, receivingSummary, totalReceived, total);



                        }

                        db.Entry(receivingSummary).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        receivingResponse = responseReceiving;
                    }

                }
                //End Advance Payment

            }

            return receivingResponse;
        }
        public ReceivingResponse WithoutAdvancePayment(ReceiveByAmount receiveByAmountVM, Contract contract, PaymentSchedule paymentSchedule, decimal? totalReceived,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            decimal? previous_received = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == receiveByAmountVM.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            //decimal? previous = payment_schedule.ReceivedAmount;
            var received_status = paymentSchedule.Received;
            decimal? total = 0;
            decimal? receivingAmount; ;
            if (receiveByAmountVM.Amount == 0)
            {

                ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                if (contractPayment == null)
                {
                    receivingAmount = receiveByAmountVM.CertificateAmount;
                }
                else
                {
                    receivingAmount = contractPayment.Balance;
                }
            }
            else
            {
                receivingAmount = receiveByAmountVM.Amount;
            }
            if (previous_received == null)
            {
                total = receivingAmount;
            }
            else
            {
                total = previous_received + receivingAmount;
            }


            if (total > paymentSchedule.Amount)
            {
                receivingResponse.Response = "Exceed";
            }
            else
            {
                ReceivingResponse responseReceiving = SaveReceivingSummary(contract, receiveByAmountVM, totalReceived, institutionCode);
                if (responseReceiving.Response == "Success")
                {
                    ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                    if (receiveByAmountVM.Amount > 0)
                    {
                        receivingSummary.Accrual = receiveByAmountVM.Accrual;
                    }

                    if (received_status == null)
                    {
                        receivingResponse.id = receivingSummary.ReceivingSummaryId;
                        ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                        SetReceivingNumber(contract, receivingSummary);
                        receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                        CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                        SaveAssetsAndInventories(contract, receivingSummary, paymentSchedule);
                        receivingResponse.Response = SetReceivingStatus(contract, paymentSchedule, receivingSummary, totalReceived, total);

                    }
                    else if (received_status == "Partial")
                    {
                        receivingResponse.id = receivingSummary.ReceivingSummaryId;
                        SetReceivingNumber(contract, receivingSummary);
                        CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                        SaveAssetsAndInventoriesTwo(contract, receivingSummary, paymentSchedule);
                        ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                        if (contract_payment == null)
                        {
                            ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);

                            receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                        }
                        else
                        {
                            contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                            contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                            db.Entry(contract_payment).State = EntityState.Modified;
                            receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;

                        }
                        receivingResponse.Response = SetReceivingStatusTwo(contract, paymentSchedule, receivingSummary, totalReceived, total);

                    }
                    else
                    {

                        receivingResponse.id = receivingSummary.ReceivingSummaryId;
                        SetReceivingNumber(contract, receivingSummary);
                        CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);

                        ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                        if (contract_payment == null)
                        {
                            ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                            receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                        }
                        else
                        {
                            contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                            contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                            db.Entry(contract_payment).State = EntityState.Modified;
                            receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;

                        }
                        receivingResponse.Response = SetReceivingStatusThree(contract, paymentSchedule, receivingSummary, totalReceived, total);

                    }

                    db.Entry(receivingSummary).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    receivingResponse = responseReceiving;
                }

            }
            return receivingResponse;
        }
        public ReceivingResponse VersionTwoWithAdvancePayment(ReceiveByAmount receiveByAmountVM, Contract contract, PaymentSchedule paymentSchedule, decimal? totalReceived,string institutionCode)
        {
            ReceivingResponse receivingResponse1 = new ReceivingResponse();
            decimal? previous_received = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == receiveByAmountVM.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            //decimal? previous = payment_schedule.ReceivedAmount;
            var received_status = paymentSchedule.Received;

            decimal? receivingAmount; ;
            if (receiveByAmountVM.Amount == 0)
            {
                ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                if (contractPayment == null)
                {
                    receivingAmount = receiveByAmountVM.CertificateAmount;
                }
                else
                {
                    receivingAmount = contractPayment.Balance;
                }
            }
            else
            {
                receivingAmount = (decimal)receiveByAmountVM.Amount;
            }

            decimal? total = 0;
            if (previous_received == null)
            {

                total = receivingAmount;


            }
            else
            {

                total = previous_received + receivingAmount;
            }


            if (total > paymentSchedule.Amount)
            {
                receivingResponse1.Response = "Exceed";
            }
            else
            {

                ReceivingResponse receivingResponse = AdvancePaymentDeduction(contract, receivingAmount, receiveByAmountVM.AdvancePayment, totalReceived);


                if (receivingResponse.Response == "Success")
                {

                    ReceivingResponse responseReceiving = SaveReceivingSummaryTwo(contract, receiveByAmountVM, totalReceived, receivingResponse.AdvancePayment, institutionCode);
                    if (responseReceiving.Response == "Success")
                    {
                        ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                        if (receiveByAmountVM.Amount > 0)
                        {
                            receivingSummary.Accrual = receiveByAmountVM.Accrual;
                        }
                        if (received_status == null)
                        {
                            receivingResponse1.id = receivingSummary.ReceivingSummaryId;
                            ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                            SetReceivingNumber(contract, receivingSummary);
                            receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                            SaveAssetsAndInventories(contract, receivingSummary, paymentSchedule);
                            receivingResponse1.Response = SetReceivingStatus(contract, paymentSchedule, receivingSummary, totalReceived, total);

                        }
                        else if (received_status == "Partial")
                        {
                            SetReceivingNumber(contract, receivingSummary);
                            receivingResponse1.id = receivingSummary.ReceivingSummaryId;
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);
                            SaveAssetsAndInventoriesTwo(contract, receivingSummary, paymentSchedule);
                            ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                            if (contract_payment == null)
                            {
                                ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);

                                receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            }
                            else
                            {

                                contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                                contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                                db.Entry(contract_payment).State = EntityState.Modified;
                                receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;

                            }
                            receivingResponse1.Response = SetReceivingStatusTwo(contract, paymentSchedule, receivingSummary, totalReceived, total);

                        }
                        else
                        {
                            receivingResponse1.id = receivingSummary.ReceivingSummaryId;
                            SetReceivingNumber(contract, receivingSummary);
                            CalculateVATReceiveByAmount(paymentSchedule, receivingSummary);

                            ContractPayment contract_payment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                            if (contract_payment == null)
                            {
                                ContractPayment contractPayment = SaveCertificatePayment(paymentSchedule, receiveByAmountVM);
                                receivingSummary.ContractPaymentId = contractPayment.ContractPaymentId;
                            }
                            else
                            {
                                contract_payment.PaidAmount = contract_payment.PaidAmount + receivingAmount;
                                contract_payment.Balance = contract_payment.CertificateAmount - contract_payment.PaidAmount;
                                db.Entry(contract_payment).State = EntityState.Modified;
                                receivingSummary.ContractPaymentId = contract_payment.ContractPaymentId;

                            }
                            receivingResponse1.Response = SetReceivingStatusThree(contract, paymentSchedule, receivingSummary, totalReceived, total);

                        }

                        db.Entry(receivingSummary).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        receivingResponse1 = responseReceiving;
                    }

                }
                else
                {
                    receivingResponse1 = receivingResponse;
                }
            }
            return receivingResponse1;
        }
        public ReceivingResponse ByItemsVersionOneWithAdvancePayment(Contract contract, PaymentSchedule paymentSchedule, ReceivingVM model, decimal? receivingAmount, decimal? totalReceived, decimal? received_in_schedule,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            decimal advancePayments = (decimal)model.AdvPaymentReceivingVMs.Select(a => a.ExpenseAmount).DefaultIfEmpty(0).Sum();
            if (advancePayments >= receivingAmount)
            {
                receivingResponse.Response = "AdvanceExceed";
            }
            else
            {
                ReceivingResponse responseReceiving = SaveReceivingSummaryTwo(contract, paymentSchedule, receivingAmount, totalReceived, advancePayments, institutionCode);
                if (responseReceiving.Response == "Success")
                {
                    ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                    receivingSummary.Accrual = model.Accrual;
                    receivingResponse.id = receivingSummary.ReceivingSummaryId;
                    SaveAdvancePaymentDetails(model.AdvPaymentReceivingVMs, receivingSummary);
                    SetReceivingNumber(contract, receivingSummary);
                    Double Total_VAT = 0;
                    Total_VAT = SaveAssetsAndInventoriesThree(contract, receivingSummary, paymentSchedule, model.ItemsReceived);
                    receivingResponse.Response = SetReceivingStatusFour(contract, paymentSchedule, receivingSummary, totalReceived, received_in_schedule, Total_VAT);
                }
                else
                {
                    receivingResponse = responseReceiving;
                }

            }

            return receivingResponse;
        }
        public ReceivingResponse ByItemsWithoutAdvancePayment(Contract contract, PaymentSchedule paymentSchedule, ReceivingVM model, decimal? receivingAmount, decimal? totalReceived, decimal? received_in_schedule,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            ReceivingResponse responseReceiving = SaveReceivingSummary(contract, paymentSchedule, receivingAmount, totalReceived, institutionCode);
            if (responseReceiving.Response == "Success")
            {
                ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                receivingSummary.Accrual = model.Accrual;
                receivingResponse.id = receivingSummary.ReceivingSummaryId;
                SetReceivingNumber(contract, receivingSummary);
                Double Total_VAT = 0;
                Total_VAT = SaveAssetsAndInventoriesThree(contract, receivingSummary, paymentSchedule, model.ItemsReceived);
                receivingResponse.Response = SetReceivingStatusFour(contract, paymentSchedule, receivingSummary, totalReceived, received_in_schedule, Total_VAT);
            }
            else
            {
                receivingResponse = responseReceiving;
            }
            return receivingResponse;
        }
        public ReceivingResponse ByItemsVersionTwoWithAdvancePayment(Contract contract, PaymentSchedule paymentSchedule, ReceivingVM model, decimal? receivingAmount, decimal? totalReceived, decimal? received_in_schedule,string institutionCode)
        {
            ReceivingResponse receivingResponse1 = new ReceivingResponse();

            decimal? previous_received = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.OverallStatus != "Cancelled").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            ReceivingResponse receivingResponse = AdvancePaymentDeduction(contract, receivingAmount, model.AdvancePayment, totalReceived);
            if (receivingResponse.Response == "Success")
            {
                ReceivingResponse responseReceiving = SaveReceivingSummaryTwo(contract, paymentSchedule, receivingAmount, totalReceived, receivingResponse.AdvancePayment, institutionCode);
                if (responseReceiving.Response == "Success")
                {
                    ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(responseReceiving.id);
                    receivingSummary.Accrual = model.Accrual;
                    receivingResponse1.id = receivingSummary.ReceivingSummaryId;
                    SetReceivingNumber(contract, receivingSummary);
                    Double Total_VAT = 0;
                    Total_VAT = SaveAssetsAndInventoriesThree(contract, receivingSummary, paymentSchedule, model.ItemsReceived);
                    receivingResponse1.Response = SetReceivingStatusFour(contract, paymentSchedule, receivingSummary, totalReceived, received_in_schedule, Total_VAT);
                }
                else
                {
                    receivingResponse1 = responseReceiving;
                }

            }
            else
            {
                receivingResponse1 = receivingResponse;
            }



            return receivingResponse1;
        }
        public ReceivingResponse SaveReceivingSummary(Contract contract, ReceiveByAmount receiveByAmountVM, decimal? totalReceived,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == institutionCode).FirstOrDefault();
            if (currencyRateDetail == null)
            {
                receivingResponse.Response = "SetupProblem";
            }
            decimal? receivingAmount;
            string accrual;
            if (receiveByAmountVM.Amount == 0)
            {

                ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                if (contractPayment == null)
                {
                    receivingAmount = receiveByAmountVM.CertificateAmount;
                }
                else
                {
                    receivingAmount = contractPayment.Balance;
                }
                accrual = "YES";
            }
            else
            {
                receivingAmount = receiveByAmountVM.Amount;
                accrual = null;
            }
            decimal? baseAmount = receivingAmount * currencyRateDetail.OperationalExchangeRate;
            int? paymentScheduleId;
            int? subContractId = null;
            if (receiveByAmountVM.EntryType == "SubContract")
            {
                SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Find(receiveByAmountVM.PaymentScheduleId);
                paymentScheduleId = subContractPaymentSchedule.PaymentScheduleId;
                subContractId = subContractPaymentSchedule.SubContractId;
            }
            else
            {
                paymentScheduleId = receiveByAmountVM.PaymentScheduleId;
            }

            ReceivingSummary summary = new ReceivingSummary()
            {
                ReceivedAmount = receivingAmount,
                BaseAmount = baseAmount,
                ExchangeRate = currencyRateDetail.OperationalExchangeRate,
                ExchangeRateDate = currencyRateDetail.ExchangeRateDate,
                RemainingAmount = contract.ContractAmount - totalReceived,
                FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                OverallStatus = "Pending",
                Type = "Contract",
                InstitutionCode = institutionCode,
                ContractId = contract.ContractId,
                JournalCode = "PO",
                PaymentScheduleId = paymentScheduleId,
                HasLiquidatedDamage = false,
                HasRetention = false,
                IssueStatus = "Pending",
                Accrual = accrual,
                SubBudgetClass = contract.SubBudgetClass,
                SubContractId = subContractId
            };
            if (contract.StPaymentFlag)
            {
                summary.StPaymentFlag = contract.StPaymentFlag;
                summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                summary.ParentInstitutionName = contract.ParentInstitutionName;
                summary.SubWarrantCode = contract.SubWarrantCode;
                summary.SubWarrantDescription = contract.SubWarrantDescription;
            }
            db.ReceivingSummarys.Add(summary);
            if (receiveByAmountVM.EntryType == "MainContract")
            {
                PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(paymentScheduleId);
                paymentSchedule.SubContractBalance = paymentSchedule.SubContractBalance - receivingAmount;
                if (paymentSchedule.SubContractBalance == 0)
                {
                    paymentSchedule.SubReceivedStatus = "Full";
                }
                else
                {
                    paymentSchedule.SubReceivedStatus = "Partial";
                }
                db.Entry(paymentSchedule).State = EntityState.Modified;
            }
            else if (receiveByAmountVM.EntryType == "SubContract")
            {
                SubContractPaymentSchedule subContractPayment = db.SubContractPaymentSchedules.Find(receiveByAmountVM.PaymentScheduleId);
                subContractPayment.Balance = subContractPayment.Balance - receivingAmount;
                if (subContractPayment.Balance == 0)
                {
                    subContractPayment.Received = "Full";
                }
                else
                {
                    subContractPayment.Received = "Partial";
                }
                db.Entry(subContractPayment).State = EntityState.Modified;
            }
            db.SaveChanges();
            receivingResponse.Response = "Success";
            receivingResponse.id = summary.ReceivingSummaryId;
            return receivingResponse;
        }
        public ReceivingResponse SaveReceivingSummaryTwo(Contract contract, ReceiveByAmount receiveByAmountVM, decimal? totalReceived, decimal? advancePayments,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == institutionCode).FirstOrDefault();
            if (currencyRateDetail == null)
            {
                receivingResponse.Response = "SetupProblem";
            }
            decimal? receivingAmount;
            string accrual;
            if (receiveByAmountVM.Amount == 0)
            {
                ContractPayment contractPayment = db.ContractPayments.Find(receiveByAmountVM.ContractPaymentId);
                if (contractPayment == null)
                {
                    receivingAmount = receiveByAmountVM.CertificateAmount;
                }
                else
                {
                    receivingAmount = contractPayment.Balance;
                }
                accrual = "YES";
            }
            else
            {
                receivingAmount = receiveByAmountVM.Amount;
                accrual = null;
            }
            decimal? baseAmount = receivingAmount * currencyRateDetail.OperationalExchangeRate;
            decimal? advancePaymentsBA = advancePayments * currencyRateDetail.OperationalExchangeRate;
            int? paymentScheduleId;
            int? subContractId = null;
            if (receiveByAmountVM.EntryType == "SubContract")
            {
                SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Find(receiveByAmountVM.PaymentScheduleId);
                paymentScheduleId = subContractPaymentSchedule.PaymentScheduleId;
                subContractId = subContractPaymentSchedule.SubContractId;
            }
            else
            {
                paymentScheduleId = receiveByAmountVM.PaymentScheduleId;
            }

            ReceivingSummary summary = new ReceivingSummary()
            {
                ReceivedAmount = receivingAmount,
                BaseAmount = baseAmount,
                ExchangeRate = currencyRateDetail.OperationalExchangeRate,
                ExchangeRateDate = currencyRateDetail.ExchangeRateDate,
                RemainingAmount = contract.ContractAmount - totalReceived,
                FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                OverallStatus = "Pending",
                Type = "Contract",
                InstitutionCode = institutionCode,
                ContractId = contract.ContractId,
                JournalCode = "PO",
                PaymentScheduleId = paymentScheduleId,
                HasLiquidatedDamage = false,
                HasRetention = false,
                IssueStatus = "Pending",
                AdvancePayment = advancePayments,
                AdvancePaymentBA = advancePaymentsBA,
                Accrual = accrual,
                SubContractId = subContractId,
                SubBudgetClass = contract.SubBudgetClass,
            };
            if (contract.StPaymentFlag)
            {
                summary.StPaymentFlag = contract.StPaymentFlag;
                summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                summary.ParentInstitutionName = contract.ParentInstitutionName;
                summary.SubWarrantCode = contract.SubWarrantCode;
                summary.SubWarrantDescription = contract.SubWarrantDescription;
            }
            db.ReceivingSummarys.Add(summary);
            if (receiveByAmountVM.EntryType == "MainContract")
            {
                PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(paymentScheduleId);
                paymentSchedule.SubContractBalance = paymentSchedule.SubContractBalance - receivingAmount;
                if (paymentSchedule.SubContractBalance == 0)
                {
                    paymentSchedule.SubReceivedStatus = "Full";
                }
                else
                {
                    paymentSchedule.SubReceivedStatus = "Partial";
                }
                db.Entry(paymentSchedule).State = EntityState.Modified;
            }
            else if (receiveByAmountVM.EntryType == "SubContract")
            {
                SubContractPaymentSchedule subContractPayment = db.SubContractPaymentSchedules.Find(receiveByAmountVM.PaymentScheduleId);
                subContractPayment.Balance = subContractPayment.Balance - receivingAmount;
                if (subContractPayment.Balance == 0)
                {
                    subContractPayment.Received = "Full";
                }
                else
                {
                    subContractPayment.Received = "Partial";
                }
                db.Entry(subContractPayment).State = EntityState.Modified;
            }
            db.SaveChanges();
            receivingResponse.Response = "Success";
            receivingResponse.id = summary.ReceivingSummaryId;
            return receivingResponse;
        }
        public ReceivingResponse SaveReceivingSummary(Contract contract, PaymentSchedule paymentSchedule, decimal? ReceivingAmount, decimal? totalReceived,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == institutionCode).FirstOrDefault();
            if (currencyRateDetail == null)
            {
                receivingResponse.Response = "SetupProblem";
            }
            decimal? baseAmount = ReceivingAmount * currencyRateDetail.OperationalExchangeRate;
            ReceivingSummary summary = new ReceivingSummary()
            {
                ReceivedAmount = ReceivingAmount,
                BaseAmount = baseAmount,
                ExchangeRate = currencyRateDetail.OperationalExchangeRate,
                ExchangeRateDate = currencyRateDetail.ExchangeRateDate,
                RemainingAmount = contract.ContractAmount - totalReceived,
                FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                OverallStatus = "Pending",
                Type = "Contract",
                InstitutionCode = institutionCode,
                ContractId = paymentSchedule.ContractId,
                JournalCode = "PO",
                PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                HasLiquidatedDamage = false,
                HasRetention = false,
                SubBudgetClass = contract.SubBudgetClass,
                IssueStatus = "Pending"
            };
            if (contract.StPaymentFlag)
            {
                summary.StPaymentFlag = contract.StPaymentFlag;
                summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                summary.ParentInstitutionName = contract.ParentInstitutionName;
                summary.SubWarrantCode = contract.SubWarrantCode;
                summary.SubWarrantDescription = contract.SubWarrantDescription;
            }
            db.ReceivingSummarys.Add(summary);
            db.SaveChanges();
            receivingResponse.Response = "Success";
            receivingResponse.id = summary.ReceivingSummaryId;
            return receivingResponse;
        }
        public ReceivingResponse SaveReceivingSummaryTwo(Contract contract, PaymentSchedule paymentSchedule, decimal? ReceivingAmount, decimal? totalReceived, decimal? advancePayments,string institutionCode)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == institutionCode).FirstOrDefault();
            if (currencyRateDetail == null)
            {
                receivingResponse.Response = "SetupProblem";
            }
            decimal? baseAmount = ReceivingAmount * currencyRateDetail.OperationalExchangeRate;
            decimal? advancePaymentsBA = advancePayments * currencyRateDetail.OperationalExchangeRate;
            ReceivingSummary summary = new ReceivingSummary()
            {
                ReceivedAmount = ReceivingAmount,
                BaseAmount = baseAmount,
                ExchangeRate = currencyRateDetail.OperationalExchangeRate,
                ExchangeRateDate = currencyRateDetail.ExchangeRateDate,
                RemainingAmount = contract.ContractAmount - totalReceived,
                FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                OverallStatus = "Pending",
                Type = "Contract",
                InstitutionCode = institutionCode,
                ContractId = paymentSchedule.ContractId,
                JournalCode = "PO",
                PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                HasLiquidatedDamage = false,
                HasRetention = false,
                IssueStatus = "Pending",
                SubBudgetClass = contract.SubBudgetClass,
                AdvancePayment = advancePayments,
                AdvancePaymentBA = advancePaymentsBA
            };
            if (contract.StPaymentFlag)
            {
                summary.StPaymentFlag = contract.StPaymentFlag;
                summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                summary.ParentInstitutionName = contract.ParentInstitutionName;
                summary.SubWarrantCode = contract.SubWarrantCode;
                summary.SubWarrantDescription = contract.SubWarrantDescription;
            }
            db.ReceivingSummarys.Add(summary);
            db.SaveChanges();
            receivingResponse.Response = "Success";
            receivingResponse.id = summary.ReceivingSummaryId;
            return receivingResponse;
        }
        public ContractPayment SaveCertificatePayment(PaymentSchedule paymentSchedule, ReceiveByAmount receiveByAmountVM)
        {
            if (receiveByAmountVM.Amount == 0)
            {

                ContractPayment payment = new ContractPayment
                {
                    PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                    CertificateNumber = receiveByAmountVM.CertificateNumber,
                    CertificateAmount = receiveByAmountVM.CertificateAmount,
                    PaidAmount = receiveByAmountVM.CertificateAmount,
                    Balance = 0,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    OverallStatus = "Äctive"
                };
                db.ContractPayments.Add(payment);
                db.SaveChanges();
                return payment;
            }
            else
            {
                ContractPayment payment = new ContractPayment
                {
                    PaymentScheduleId = paymentSchedule.PaymentScheduleId,
                    CertificateNumber = receiveByAmountVM.CertificateNumber,
                    CertificateAmount = receiveByAmountVM.CertificateAmount,
                    PaidAmount = receiveByAmountVM.Amount,
                    Balance = receiveByAmountVM.CertificateAmount - receiveByAmountVM.Amount,
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    OverallStatus = "Äctive"
                };
                db.ContractPayments.Add(payment);
                db.SaveChanges();
                return payment;
            }


        }

        public void CalculateVATReceiveByAmount(PaymentSchedule paymentSchedule, ReceivingSummary receivingSummary)
        {
            decimal? TotalVAT = db.ContractDetails.Where(a => a.PaymentScheduleId == paymentSchedule.PaymentScheduleId && a.Status != "Cancelled").Select(a => a.VAT).DefaultIfEmpty(0).FirstOrDefault();
            if (TotalVAT > 0)
            {
                decimal? currentVAT = receivingSummary.ReceivedAmount * TotalVAT / paymentSchedule.Amount;
                receivingSummary.VAT = currentVAT;
                //End Calculate Total VAT in this Payment Schedule
                db.Entry(receivingSummary).State = EntityState.Modified;
                db.SaveChanges();
            }


        }
        public void SetReceivingNumber(Contract contract, ReceivingSummary receivingSummary)
        {
            string receivingNumber = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "RC", receivingSummary.ReceivingSummaryId);
            receivingSummary.ReceivingNumber = receivingNumber;
        }
        public string SetReceivingStatus(Contract contract, PaymentSchedule paymentSchedule, ReceivingSummary receivingSummary, decimal? totalReceived, decimal? totalReceivedInPs)
        {
            //totalReceivedInPs total Received in Payment Schedule
            string response = null;
            if (contract.ContractAmount == totalReceived)
            {
                contract.Received = "Full";
                contract.OverallStatus = "FullReceived";
            }
            else
            {
                contract.Received = "Partial";
                contract.OverallStatus = "Partial";
            }
            if (paymentSchedule.Amount == totalReceivedInPs)
            {
                paymentSchedule.Received = "Full";
            }
            else
            {
                paymentSchedule.Received = "Partial";
            }
            paymentSchedule.ReceiveType = "ByAmount";
            paymentSchedule.ReceivedAmount = receivingSummary.ReceivedAmount;
            paymentSchedule.Balance = paymentSchedule.Amount - paymentSchedule.ReceivedAmount;
            db.Entry(receivingSummary).State = EntityState.Modified;
            db.Entry(contract).State = EntityState.Modified;
            db.Entry(paymentSchedule).State = EntityState.Modified;
            db.SaveChanges();
            response = "Success";
            return response;
        }
        public string SetReceivingStatusTwo(Contract contract, PaymentSchedule paymentSchedule, ReceivingSummary receivingSummary, decimal? totalReceived, decimal? totalReceivedInPs)
        {
            //totalReceivedInPs total Received in Payment Schedule
            string response = null;
            if (contract.ContractAmount == totalReceived)
            {
                contract.Received = "Full";
                contract.OverallStatus = "FullReceived";
            }
            else
            {
                contract.Received = "Partial";
                contract.OverallStatus = "Partial";
            }
            if (paymentSchedule.Amount == totalReceivedInPs)
            {
                paymentSchedule.Received = "Full";
            }
            else
            {
                paymentSchedule.Received = "Partial";
            }
            paymentSchedule.ReceiveType = "ByAmount";
            paymentSchedule.ReceivedAmount = paymentSchedule.ReceivedAmount + receivingSummary.ReceivedAmount;
            paymentSchedule.Balance = paymentSchedule.Amount - paymentSchedule.ReceivedAmount;
            db.Entry(receivingSummary).State = EntityState.Modified;
            db.Entry(contract).State = EntityState.Modified;
            db.Entry(paymentSchedule).State = EntityState.Modified;
            db.SaveChanges();
            response = "Success";
            return response;
        }
        public string SetReceivingStatusThree(Contract contract, PaymentSchedule paymentSchedule, ReceivingSummary receivingSummary, decimal? totalReceived, decimal? totalReceivedInPs)
        {
            //totalReceivedInPs total Received in Payment Schedule
            string response = null;
            if (contract.ContractAmount == totalReceived)
            {
                contract.Received = "Full";
                contract.OverallStatus = "FullReceived";
            }
            else
            {
                contract.Received = "Partial";
                contract.OverallStatus = "Partial";
            }
            if (paymentSchedule.Amount == totalReceivedInPs)
            {
                paymentSchedule.Received = "Full";
            }
            else
            {
                paymentSchedule.Received = "Partial";
            }
            if (paymentSchedule.ReceivedAmount == null)
            {
                paymentSchedule.ReceivedAmount = receivingSummary.ReceivedAmount;
            }
            else
            {
                paymentSchedule.ReceivedAmount = paymentSchedule.ReceivedAmount + receivingSummary.ReceivedAmount;
            }

            paymentSchedule.Balance = paymentSchedule.Amount - paymentSchedule.ReceivedAmount;
            db.Entry(receivingSummary).State = EntityState.Modified;
            db.Entry(contract).State = EntityState.Modified;
            db.Entry(paymentSchedule).State = EntityState.Modified;
            db.SaveChanges();
            response = "Success";
            return response;
        }
        public string SetReceivingStatusFour(Contract contract, PaymentSchedule paymentSchedule, ReceivingSummary receivingSummary, decimal? totalReceived, decimal? totalReceivedInPs, Double Total_VAT)
        {
            //totalReceivedInPs total Received in Payment Schedule
            string response = null;
            if (contract.ContractAmount == totalReceived)
            {
                contract.Received = "Full";
                contract.OverallStatus = "FullReceived";
            }
            else
            {
                contract.Received = "Partial";
                contract.OverallStatus = "Partial";
            }
            if (paymentSchedule.Amount == totalReceivedInPs)
            {
                paymentSchedule.Received = "Full";
            }
            else
            {
                paymentSchedule.Received = "Partial";
            }
            paymentSchedule.ReceiveType = "ByQuantity";
            if (paymentSchedule.ReceivedAmount == null)
            {
                paymentSchedule.ReceivedAmount = receivingSummary.ReceivedAmount;
                paymentSchedule.Balance = paymentSchedule.Amount - receivingSummary.ReceivedAmount;
            }
            else
            {
                paymentSchedule.ReceivedAmount = paymentSchedule.ReceivedAmount + receivingSummary.ReceivedAmount;
                paymentSchedule.Balance = paymentSchedule.Amount - paymentSchedule.ReceivedAmount;
            }
            receivingSummary.VAT = (decimal)Total_VAT;
            db.Entry(receivingSummary).State = EntityState.Modified;
            db.Entry(contract).State = EntityState.Modified;
            db.Entry(paymentSchedule).State = EntityState.Modified;
            db.SaveChanges();
            response = "Success";
            return response;
        }

        public void SaveAdvancePaymentDetails(IEnumerable<AdvPaymentReceivingVM> AdvPaymentReceivingVMs, ReceivingSummary receivingSummary)
        {
            List<AdvancePayment> advancePaymentList = new List<AdvancePayment>();
            foreach (var item in AdvPaymentReceivingVMs)
            {
                PaymentVoucher paymentVoucher = db.PaymentVouchers.Where(a => a.PVNo == item.Vourcher).FirstOrDefault();
                AdvancePayment advancePayment = new AdvancePayment()
                {
                    Amount = item.ExpenseAmount,
                    PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                    ContractId = receivingSummary.ContractId,
                    PayeeCode = paymentVoucher.PayeeCode,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId
                };
                advancePaymentList.Add(advancePayment);
            }
            db.AdvancePayments.AddRange(advancePaymentList);
        }
        public ReceivingResponse AdvancePaymentDeduction(Contract contract, decimal? receivingAmount, decimal? advancePayment, decimal? totalReceived)
        {
            ReceivingResponse receivingResponse = new ReceivingResponse();
            //CALCULATE TOTAL ADVANCE PAYMENT PAID TO THIS CONTRACT
            decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            decimal? currentAdvancePayment = 0;
            //CALCULATE TWENTY PERCENT OF ADVANCE PAYMENT
            decimal? advancePaymentTwentyPtg = (contractAdvancePayment * 2) / 10;
            //CALCULATE TOTAL ADVANCE PAYMENT DEDUCTED PREVIOUS TO THIS CONTRACT
            decimal? totalAdvancePaymentPaid = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.AdvancePayment).DefaultIfEmpty(0).Sum();
            if (totalAdvancePaymentPaid == null)
            {
                totalAdvancePaymentPaid = 0;
            }
            decimal? advancePaymentRemains = contractAdvancePayment - totalAdvancePaymentPaid;
            if (advancePaymentRemains > 0)
            {

                if (contractAdvancePayment > 0)
                {

                    if (totalAdvancePaymentPaid <= contractAdvancePayment)
                    {

                        //CALCULATE DEDUCT ADVANCE PAYMENT
                        decimal contractEightyPtg = (contract.ContractAmount * 8) / 10;

                        if (totalReceived < contractEightyPtg)
                        {
                            decimal? balance = contractAdvancePayment - advancePayment;
                            if (totalAdvancePaymentPaid == 0 || balance == 0)
                            {
                                currentAdvancePayment = (receivingAmount * contractAdvancePayment) / contractEightyPtg;
                                if (currentAdvancePayment < advancePaymentTwentyPtg)
                                {
                                    currentAdvancePayment = advancePaymentTwentyPtg;
                                }

                            }
                            else
                            {
                                if (advancePaymentRemains <= advancePaymentTwentyPtg)
                                {
                                    currentAdvancePayment = advancePaymentRemains;
                                }
                                else
                                {
                                    decimal contractSeventyPtg = (contract.ContractAmount * 7) / 10;
                                    if (totalReceived < contractSeventyPtg)
                                    {
                                        currentAdvancePayment = advancePaymentTwentyPtg;
                                    }
                                    else
                                    {
                                        currentAdvancePayment = advancePaymentRemains;
                                    }
                                }
                            }
                        }
                        else
                        {

                            currentAdvancePayment = advancePaymentRemains;

                        }
                        //END CALCULATE DEDUCT ADVANCE PAYMENT
                    }
                }
            }

            if (currentAdvancePayment > advancePaymentRemains)
            {
                currentAdvancePayment = advancePaymentRemains;
            }

            if (currentAdvancePayment == 0)
            {
                receivingResponse.AdvancePayment = null;
                receivingResponse.Response = "Success";
            }
            else
            {
                decimal? totalAdvancePaymentPaidToBe = totalAdvancePaymentPaid + advancePayment;
                if (advancePayment >= currentAdvancePayment && totalAdvancePaymentPaidToBe <= contractAdvancePayment)
                {
                    receivingResponse.AdvancePayment = advancePayment;
                    receivingResponse.Response = "Success";
                }
                else if (totalAdvancePaymentPaidToBe > contractAdvancePayment)
                {
                    receivingResponse.AdvancePayment = contractAdvancePayment - totalAdvancePaymentPaid;
                    receivingResponse.Response = "ExeedAdvancePayment";
                }
                else
                {
                    receivingResponse.AdvancePayment = currentAdvancePayment;
                    receivingResponse.Response = "LessAdvancePayment";
                }
            }

            return receivingResponse;
        }

        public decimal GetAdvancePaymentDeduction(Contract contract, decimal? receivingAmount, decimal? advancePayment, decimal? totalReceived)
        {
            decimal amount = 0;
            ReceivingResponse receivingResponse = new ReceivingResponse();
            //CALCULATE TOTAL ADVANCE PAYMENT PAID TO THIS CONTRACT
            decimal? contractAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            decimal? currentAdvancePayment = 0;
            //CALCULATE TWENTY PERCENT OF ADVANCE PAYMENT
            decimal? advancePaymentTwentyPtg = (contractAdvancePayment * 2) / 10;
            //CALCULATE TOTAL ADVANCE PAYMENT DEDUCTED PREVIOUS TO THIS CONTRACT
            decimal? totalAdvancePaymentPaid = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.AdvancePayment).DefaultIfEmpty(0).Sum();
            if (totalAdvancePaymentPaid == null)
            {
                totalAdvancePaymentPaid = 0;
            }
            decimal? advancePaymentRemains = contractAdvancePayment - totalAdvancePaymentPaid;
            if (advancePaymentRemains > 0)
            {

                if (contractAdvancePayment > 0)
                {

                    if (totalAdvancePaymentPaid <= contractAdvancePayment)
                    {

                        //CALCULATE DEDUCT ADVANCE PAYMENT
                        decimal contractEightyPtg = (contract.ContractAmount * 8) / 10;

                        if (totalReceived < contractEightyPtg)
                        {
                            decimal? balance = contractAdvancePayment - advancePayment;
                            if (totalAdvancePaymentPaid == 0 || balance == 0)
                            {
                                currentAdvancePayment = (receivingAmount * contractAdvancePayment) / contractEightyPtg;
                                if (currentAdvancePayment < advancePaymentTwentyPtg)
                                {
                                    currentAdvancePayment = advancePaymentTwentyPtg;
                                }

                            }
                            else
                            {
                                if (advancePaymentRemains <= advancePaymentTwentyPtg)
                                {
                                    currentAdvancePayment = advancePaymentRemains;
                                }
                                else
                                {
                                    decimal contractSeventyPtg = (contract.ContractAmount * 7) / 10;
                                    if (totalReceived < contractSeventyPtg)
                                    {
                                        currentAdvancePayment = advancePaymentTwentyPtg;
                                    }
                                    else
                                    {
                                        currentAdvancePayment = advancePaymentRemains;
                                    }
                                }
                            }
                        }
                        else
                        {

                            currentAdvancePayment = advancePaymentRemains;

                        }
                        //END CALCULATE DEDUCT ADVANCE PAYMENT
                    }
                }
            }

            if (currentAdvancePayment > advancePaymentRemains)
            {
                currentAdvancePayment = advancePaymentRemains;
            }

            if (currentAdvancePayment > 0)
            {
                amount = (Decimal)currentAdvancePayment;
            }

            return amount;
        }
        public void SaveCertificateAttachments(int? id, ReceivingSummary receivingSummary)
        {
            ReceivingSummary receivingSummary1 = db.ReceivingSummarys.Where(a => a.ContractPaymentId == id).FirstOrDefault();
            if (receivingSummary1 != null)
            {
                receivingSummary.InvoiceNo = receivingSummary1.InvoiceNo;
                receivingSummary.InvoiceDate = receivingSummary1.InvoiceDate;
                receivingSummary.InvoiceFileName = receivingSummary1.InvoiceFileName;
                receivingSummary.DeliveryNote = receivingSummary1.DeliveryNote;
                receivingSummary.ReceivedDate = receivingSummary1.ReceivedDate;
                receivingSummary.DeliveryFileName = receivingSummary1.DeliveryFileName;
                receivingSummary.InspectionReportNo = receivingSummary1.InspectionReportNo;
                receivingSummary.InspectionReportDate = receivingSummary1.InspectionReportDate;
                receivingSummary.InspReportFileName = receivingSummary1.InspReportFileName;
                receivingSummary.EditAttachment = "No";
            }
        }
        public void SaveInvoiceAttachment(ReceivingSummary receivingSummary)
        {
            var receivingSummaries = db.ReceivingSummarys.Where(a => a.ContractPaymentId == receivingSummary.ContractPaymentId && a.OverallStatus.ToUpper() != "CANCELLED" && a.OverallStatus.ToUpper() == "PENDING").ToList();
            foreach (var receiving in receivingSummaries)
            {
                if (receiving.ReceivingSummaryId != receivingSummary.ReceivingSummaryId)
                {
                    if (receiving != null)
                    {
                        receiving.InvoiceNo = receivingSummary.InvoiceNo;
                        receiving.InvoiceDate = receivingSummary.InvoiceDate;
                        receiving.InvoiceFileName = receivingSummary.InvoiceFileName;
                        receiving.EditAttachment = "No";
                        db.Entry(receiving).State = EntityState.Modified;
                    }
                }
            }

        }
        public void SaveDeliveryAttachment(ReceivingSummary receivingSummary)
        {
            var receivingSummaries = db.ReceivingSummarys.Where(a => a.ContractPaymentId == receivingSummary.ContractPaymentId && a.OverallStatus.ToUpper() != "CANCELLED" && a.OverallStatus.ToUpper() == "PENDING").ToList();
            foreach (var receiving in receivingSummaries)
            {
                if (receiving.ReceivingSummaryId != receivingSummary.ReceivingSummaryId)
                {
                    if (receiving != null)
                    {
                        receiving.DeliveryNote = receivingSummary.DeliveryNote;
                        receiving.ReceivedDate = receivingSummary.ReceivedDate;
                        receiving.DeliveryFileName = receivingSummary.DeliveryFileName;
                        receiving.EditAttachment = "No";
                        db.Entry(receiving).State = EntityState.Modified;
                    }
                }
            }

        }
        public void SaveInspectionReportAttachment(ReceivingSummary receivingSummary)
        {
            var receivingSummaries = db.ReceivingSummarys.Where(a => a.ContractPaymentId == receivingSummary.ContractPaymentId && a.OverallStatus.ToUpper() != "CANCELLED" && a.OverallStatus.ToUpper() == "PENDING").ToList();
            foreach (var receiving in receivingSummaries)
            {
                if (receiving.ReceivingSummaryId != receivingSummary.ReceivingSummaryId)
                {
                    if (receiving != null)
                    {
                        receiving.InspectionReportNo = receivingSummary.InspectionReportNo;
                        receiving.InspectionReportDate = receivingSummary.InspectionReportDate;
                        receiving.InspReportFileName = receivingSummary.InspReportFileName;
                        receiving.EditAttachment = "No";
                        db.Entry(receiving).State = EntityState.Modified;
                    }
                }
            }

        }
        public List<PaymentScheduleVM> GetPaymentSchedules(int? id)
        {
            var paymentScheduleList = (from p in db.PaymentSchedules
                                       where p.ContractId == id
                                       select new PaymentScheduleVM
                                       {
                                           PaymentScheduleId = p.PaymentScheduleId,
                                           Amount = p.Amount,
                                           Balance = p.Balance,
                                           Description = p.Description,
                                           Deliverable = p.Deliverable,
                                           FinancialYearDesc = db.FinancialYears.Where(a => a.FinancialYearCode == p.FinancialYear).Select(a => a.FinancialYearDesc).FirstOrDefault()
                                       }).ToList();
            return paymentScheduleList;
        }
        [Authorize(Roles = "Contract Entry")]
        public ActionResult GetPaymentSchedule(int? id)
        {

            var paymentScheduleList = db.PaymentSchedules.Where(a => a.ContractId == id && a.Balance > 0).ToList();

            return Json(paymentScheduleList, JsonRequestBehavior.AllowGet);
        }
        public List<PaymentSchedule> GetPaymentScheduleList(int? id)
        {

            var paymentScheduleList = db.PaymentSchedules.Where(a => a.ContractId == id && a.Balance > 0).ToList();

            return paymentScheduleList;
        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public JsonResult GetPaymentSchedulesList(int? id)
        {
            try
            {
                var paymentScheduleList = db.PaymentSchedules.Where(a => a.ContractId == id && (a.Received == null || a.Received == "Partial")).ToList();
                return Json(paymentScheduleList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                string response = "DBException";
                return Json(response, JsonRequestBehavior.AllowGet);
            }

        }
        public List<PurchaseOrderDetailVM> GetAllItems(int? id)
        {
            var itemsList = (from p in db.ContractDetails
                             join q in db.ItemClassifications on p.ItemClassificationId equals q.ItemClassificationId
                             join s in db.PaymentSchedules on p.PaymentScheduleId equals s.PaymentScheduleId
                             where p.ContractId == id && p.Status != "Cancelled"
                             select new { p, q, s } into r
                             select new PurchaseOrderDetailVM
                             {
                                 ContractDetailId = r.p.ContractDetailId,
                                 ItemClassificationId = (int)r.p.ItemClassificationId,
                                 PaymentScheduleId = r.s.PaymentScheduleId,
                                 PaymentScheduleDesc = r.s.Description,
                                 ItemCategory = r.q.ItemCategory,
                                 ItemDesc = r.p.ItemDesc,
                                 Quantity = r.p.Quantity,
                                 UOM = r.p.UOM,
                                 UnitPrice = r.p.UnitPrice,
                                 OverheadPercentage = r.p.OverheadPercentage,
                                 VatStatus = r.p.VatStatus,
                                 VAT = r.p.VAT,
                                 TotalAmount = (Decimal)r.p.TotalAmount
                             }
                            ).OrderBy(a => a.PaymentScheduleId).ToList();
            return itemsList;
        }
        public int checkAttach(string sourceModule, int id)
        {
            int count = db.PaymentVoucherAttachments.Where(a => a.GroupId == id && a.SourceModule == sourceModule).Count();
            return count;
        }
        public string ReceaveAccrualWithoutAdvancePayment(int? id)
        {
            string response = null;
            ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(id);
            Contract contract = db.Contracts.Find(receivingSummary.ContractId);
            ContractPayment contract_payment = db.ContractPayments.Find(receivingSummary.ContractPaymentId);
            if (contract_payment != null)
            {
                if (contract_payment.Balance > 0)
                {
                    if (receivingSummary.SubContractId > 0)
                    {
                        //For Sub Contract Receiving
                        ReceivingSummary summary = new ReceivingSummary()
                        {
                            ReceivedAmount = contract_payment.Balance,
                            BaseAmount = contract_payment.Balance * receivingSummary.ExchangeRate,
                            ExchangeRate = receivingSummary.ExchangeRate,
                            ExchangeRateDate = receivingSummary.ExchangeRateDate,
                            RemainingAmount = receivingSummary.RemainingAmount - contract_payment.Balance,
                            FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            OverallStatus = "Pending",
                            Type = "Contract",
                            InstitutionCode = receivingSummary.InstitutionCode,
                            ContractId = receivingSummary.ContractId,
                            JournalCode = "PO",
                            PaymentScheduleId = receivingSummary.PaymentScheduleId,
                            HasLiquidatedDamage = false,
                            HasRetention = false,
                            Accrual = "YES",
                            ContractPaymentId = contract_payment.ContractPaymentId,
                            IssueStatus = "Pending",
                            SubBudgetClass = receivingSummary.SubBudgetClass,
                            SubContractId = receivingSummary.SubContractId
                        };
                        if (contract.StPaymentFlag)
                        {
                            summary.StPaymentFlag = contract.StPaymentFlag;
                            summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                            summary.ParentInstitutionName = contract.ParentInstitutionName;
                            summary.SubWarrantCode = contract.SubWarrantCode;
                            summary.SubWarrantDescription = contract.SubWarrantDescription;
                        }
                        db.ReceivingSummarys.Add(summary);

                        //Update Certificate Information
                        contract_payment.PaidAmount = contract_payment.CertificateAmount;

                        //End Update Certificate Information

                        //Update Sub contract Information
                        SubContractPaymentSchedule subContractPayment = db.SubContractPaymentSchedules.Where(a => a.SubContractId == receivingSummary.SubContractId && a.PaymentScheduleId == receivingSummary.PaymentScheduleId && a.OverallStatus == "Active").FirstOrDefault();
                        subContractPayment.Balance = subContractPayment.Balance - contract_payment.Balance;
                        if (subContractPayment.Balance == 0)
                        {
                            subContractPayment.Received = "Full";
                        }
                        else
                        {
                            subContractPayment.Received = "Partial";
                        }
                        db.Entry(subContractPayment).State = EntityState.Modified;
                        //End Update Sub contract Information
                        contract_payment.Balance = 0;
                        db.Entry(contract_payment).State = EntityState.Modified;
                        db.SaveChanges();
                        string receivingNumber = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "RC", receivingSummary.ReceivingSummaryId);
                        summary.ReceivingNumber = receivingNumber;
                        db.Entry(summary).State = EntityState.Modified;
                        db.SaveChanges();
                        response = "Success";
                    }
                    else
                    {
                        //For Main Contract Receiving
                        ReceivingSummary summary = new ReceivingSummary()
                        {
                            ReceivedAmount = contract_payment.Balance,
                            BaseAmount = contract_payment.Balance * receivingSummary.ExchangeRate,
                            ExchangeRate = receivingSummary.ExchangeRate,
                            ExchangeRateDate = receivingSummary.ExchangeRateDate,
                            RemainingAmount = receivingSummary.RemainingAmount - contract_payment.Balance,
                            FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            OverallStatus = "Pending",
                            Type = "Contract",
                            InstitutionCode = receivingSummary.InstitutionCode,
                            ContractId = receivingSummary.ContractId,
                            JournalCode = "PO",
                            PaymentScheduleId = receivingSummary.PaymentScheduleId,
                            HasLiquidatedDamage = false,
                            HasRetention = false,
                            SubBudgetClass = receivingSummary.SubBudgetClass,
                            Accrual = "YES",
                            ContractPaymentId = contract_payment.ContractPaymentId,
                            IssueStatus = "Pending"
                        };
                        if (contract.StPaymentFlag)
                        {
                            summary.StPaymentFlag = contract.StPaymentFlag;
                            summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                            summary.ParentInstitutionName = contract.ParentInstitutionName;
                            summary.SubWarrantCode = contract.SubWarrantCode;
                            summary.SubWarrantDescription = contract.SubWarrantDescription;
                        }
                        db.ReceivingSummarys.Add(summary);
                        contract_payment.PaidAmount = contract_payment.CertificateAmount;

                        //Update Main Contract Information
                        PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(receivingSummary.PaymentScheduleId);
                        if (paymentSchedule.SubContractAmount > 0)
                        {
                            paymentSchedule.SubContractBalance = paymentSchedule.SubContractBalance - contract_payment.Balance;
                            if (paymentSchedule.SubContractBalance == 0)
                            {
                                paymentSchedule.SubReceivedStatus = "Full";
                            }
                            else
                            {
                                paymentSchedule.SubReceivedStatus = "Partial";
                            }
                            db.Entry(paymentSchedule).State = EntityState.Modified;

                        }
                        else
                        {
                            paymentSchedule.ReceivedAmount = paymentSchedule.ReceivedAmount + contract_payment.Balance;
                            paymentSchedule.Balance = paymentSchedule.Balance - contract_payment.Balance;
                            if (paymentSchedule.Balance == 0)
                            {
                                paymentSchedule.Received = "Full";
                            }
                            else
                            {
                                paymentSchedule.Received = "Partial";
                            }
                            db.Entry(paymentSchedule).State = EntityState.Modified;
                        }
                        //End Update Main Contract Information
                        contract_payment.Balance = 0;
                        db.Entry(contract_payment).State = EntityState.Modified;
                        db.SaveChanges();
                        string receivingNumber = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "RC", receivingSummary.ReceivingSummaryId);
                        summary.ReceivingNumber = receivingNumber;
                        db.Entry(summary).State = EntityState.Modified;

                        db.SaveChanges();
                        response = "Success";

                    }

                }
                else
                {
                    response = "FullReceived";
                }
            }
            return response;
        }
        public string ReceaveAccrualWithAdvancePayment(int? id, decimal? advancePayment)
        {
            string response = null;
            ReceivingSummary receivingSummary = db.ReceivingSummarys.Find(id);
            ContractPayment contract_payment = db.ContractPayments.Find(receivingSummary.ContractPaymentId);
            if (contract_payment != null)
            {
                if (contract_payment.Balance > 0)
                {
                    Contract contract = db.Contracts.Find(receivingSummary.ContractId);
                    decimal? totalReceived = 0;
                    decimal advancePaymentDeduction = GetAdvancePaymentDeduction(contract, contract_payment.Balance, advancePayment, totalReceived);

                    if (receivingSummary.SubContractId > 0)
                    {
                        //For Sub Contract Receiving
                        ReceivingSummary summary = new ReceivingSummary()
                        {
                            ReceivedAmount = contract_payment.Balance,
                            BaseAmount = contract_payment.Balance * receivingSummary.ExchangeRate,
                            ExchangeRate = receivingSummary.ExchangeRate,
                            ExchangeRateDate = receivingSummary.ExchangeRateDate,
                            RemainingAmount = receivingSummary.RemainingAmount - contract_payment.Balance,
                            FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            OverallStatus = "Pending",
                            Type = "Contract",
                            InstitutionCode = receivingSummary.InstitutionCode,
                            ContractId = receivingSummary.ContractId,
                            JournalCode = "PO",
                            PaymentScheduleId = receivingSummary.PaymentScheduleId,
                            HasLiquidatedDamage = false,
                            HasRetention = false,
                            Accrual = "YES",
                            ContractPaymentId = contract_payment.ContractPaymentId,
                            IssueStatus = "Pending",
                            AdvancePayment = advancePaymentDeduction,
                            AdvancePaymentBA = advancePaymentDeduction * receivingSummary.ExchangeRate,
                            SubBudgetClass = receivingSummary.SubBudgetClass,
                            SubContractId = receivingSummary.SubContractId
                        };
                        if (contract.StPaymentFlag)
                        {
                            summary.StPaymentFlag = contract.StPaymentFlag;
                            summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                            summary.ParentInstitutionName = contract.ParentInstitutionName;
                            summary.SubWarrantCode = contract.SubWarrantCode;
                            summary.SubWarrantDescription = contract.SubWarrantDescription;
                        }
                        db.ReceivingSummarys.Add(summary);
                        contract_payment.PaidAmount = contract_payment.CertificateAmount;
                        //Update Sub contract Information
                        SubContractPaymentSchedule subContractPayment = db.SubContractPaymentSchedules.Where(a => a.SubContractId == receivingSummary.SubContractId && a.PaymentScheduleId == receivingSummary.PaymentScheduleId && a.OverallStatus == "Active").FirstOrDefault();
                        subContractPayment.Balance = subContractPayment.Balance - contract_payment.Balance;
                        if (subContractPayment.Balance == 0)
                        {
                            subContractPayment.Received = "Full";
                        }
                        else
                        {
                            subContractPayment.Received = "Partial";
                        }
                        db.Entry(subContractPayment).State = EntityState.Modified;
                        //End Update Sub contract Information
                        contract_payment.Balance = 0;
                        db.Entry(contract_payment).State = EntityState.Modified;

                        db.SaveChanges();
                        string receivingNumber = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "RC", receivingSummary.ReceivingSummaryId);
                        summary.ReceivingNumber = receivingNumber;
                        db.Entry(summary).State = EntityState.Modified;

                        db.SaveChanges();
                        response = "Success";

                    }
                    else
                    {
                        //For Main Contract Receiving
                        ReceivingSummary summary = new ReceivingSummary()
                        {
                            ReceivedAmount = contract_payment.Balance,
                            BaseAmount = contract_payment.Balance * receivingSummary.ExchangeRate,
                            ExchangeRate = receivingSummary.ExchangeRate,
                            ExchangeRateDate = receivingSummary.ExchangeRateDate,
                            RemainingAmount = receivingSummary.RemainingAmount - contract_payment.Balance,
                            FinanciYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity.Name,
                            OverallStatus = "Pending",
                            Type = "Contract",
                            InstitutionCode = receivingSummary.InstitutionCode,
                            ContractId = receivingSummary.ContractId,
                            JournalCode = "PO",
                            PaymentScheduleId = receivingSummary.PaymentScheduleId,
                            HasLiquidatedDamage = false,
                            HasRetention = false,
                            Accrual = "YES",
                            ContractPaymentId = contract_payment.ContractPaymentId,
                            IssueStatus = "Pending",
                            SubBudgetClass = receivingSummary.SubBudgetClass,
                            AdvancePayment = advancePaymentDeduction,
                            AdvancePaymentBA = advancePaymentDeduction * receivingSummary.ExchangeRate
                        };
                        if (contract.StPaymentFlag)
                        {
                            summary.StPaymentFlag = contract.StPaymentFlag;
                            summary.ParentInstitutionCode = contract.ParentInstitutionCode;
                            summary.ParentInstitutionName = contract.ParentInstitutionName;
                            summary.SubWarrantCode = contract.SubWarrantCode;
                            summary.SubWarrantDescription = contract.SubWarrantDescription;
                        }
                        db.ReceivingSummarys.Add(summary);
                        contract_payment.PaidAmount = contract_payment.CertificateAmount;


                        //Update Main Contract Information
                        PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(receivingSummary.PaymentScheduleId);
                        if (paymentSchedule.SubContractAmount > 0)
                        {
                            paymentSchedule.SubContractBalance = paymentSchedule.SubContractBalance - contract_payment.Balance;
                            if (paymentSchedule.SubContractBalance == 0)
                            {
                                paymentSchedule.SubReceivedStatus = "Full";
                            }
                            else
                            {
                                paymentSchedule.SubReceivedStatus = "Partial";
                            }
                            db.Entry(paymentSchedule).State = EntityState.Modified;

                        }
                        else
                        {
                            paymentSchedule.ReceivedAmount = paymentSchedule.ReceivedAmount + contract_payment.Balance;
                            paymentSchedule.Balance = paymentSchedule.Balance - contract_payment.Balance;
                            if (paymentSchedule.Balance == 0)
                            {
                                paymentSchedule.Received = "Full";
                            }
                            else
                            {
                                paymentSchedule.Received = "Partial";
                            }
                            db.Entry(paymentSchedule).State = EntityState.Modified;
                        }
                        //End Update Main Contract Information
                        contract_payment.Balance = 0;
                        db.Entry(contract_payment).State = EntityState.Modified;

                        db.SaveChanges();
                        string receivingNumber = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "RC", receivingSummary.ReceivingSummaryId);
                        summary.ReceivingNumber = receivingNumber;
                        db.Entry(summary).State = EntityState.Modified;
                        db.SaveChanges();
                        response = "Success";
                    }

                }
                else
                {
                    response = "FullReceived";
                }
            }
            return response;
        }
        public string CheckSpeciaReceiving(InstitutionSubLevel userPaystation)
        {
            string response = null;
            bool institutionConfig = db.InstitutionConfigs.Where(a => a.InstitutionId == userPaystation.InstitutionId).Select(a => a.ConfigFlag).FirstOrDefault();
            if (institutionConfig)
            {
                if (User.IsInRole("All Contract Type Receiving Entry") || User.IsInRole("All Contract Type Receiving Approval"))
                {

                    response = "NotAuthorizes";
                }

            }
            return response;

        }
        public void SaveToReceivingTable(PurchaseOrderDetailVM purchaseOrderDetailVM, ReceivingSummary receivingSummary)
        {
            Receiving receiving = new Receiving()
            {
                ItemDesc = purchaseOrderDetailVM.ItemDesc,
                Amount = purchaseOrderDetailVM.TotalAmount,
                ReceivedQuantity = purchaseOrderDetailVM.Quantity,
                ContractId = purchaseOrderDetailVM.ContractId,
                PaymentScheduleId = purchaseOrderDetailVM.PaymentScheduleId,
                ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                ReceiveDate = DateTime.Now,
                Vat = purchaseOrderDetailVM.VAT,
                UOM = purchaseOrderDetailVM.UOM,
                UnitPrice = purchaseOrderDetailVM.UnitPrice,
                ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                ItemClassificationId = purchaseOrderDetailVM.ItemClassificationId
            };
            db.Receivings.Add(receiving);
        }
        public void SaveInventory(PurchaseOrderDetailVM purchaseOrderDetailVM, ReceivingSummary receivingSummary)
        {
            Contract contract = db.Contracts.Find(receivingSummary.ContractId);
            InventoryDetail inventoryDetail = new InventoryDetail()
            {
                ItemName = purchaseOrderDetailVM.ItemDesc,
                ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                SubLevelCategory = contract.SubLevelCategory,
                SubLevelCode = contract.SubLevelCode,
                SubLevelDesc = contract.SubLevelDesc,
                UnitPrice = purchaseOrderDetailVM.UnitPrice,
                InventoryValue = purchaseOrderDetailVM.TotalAmount,
                UOM = purchaseOrderDetailVM.UOM,
                Quantity = purchaseOrderDetailVM.Quantity,
                ReceivingNumber = receivingSummary.ReceivingNumber,
                ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                InstitutionCode = receivingSummary.InstitutionCode,
                InstitutionId = contract.InstitutionId,
                OverallStatus = "Incomplete",
                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                OperationCurrency = contract.OperationalCurrency,
                JournalCode = "IV",
                SourceModule = "Contract",
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            };

            db.InventoryDetails.Add(inventoryDetail);
        }
        public void SaveInventoryNew(PurchaseOrderDetailVM purchaseOrderDetailVM, ReceivingSummary receivingSummary, decimal? number)
        {
            Contract contract = db.Contracts.Find(receivingSummary.ContractId);
            InventoryDetail inventoryDetail = new InventoryDetail()
            {
                ItemName = purchaseOrderDetailVM.ItemDesc,
                ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                SubLevelCategory = contract.SubLevelCategory,
                SubLevelCode = contract.SubLevelCode,
                SubLevelDesc = contract.SubLevelDesc,
                UnitPrice = purchaseOrderDetailVM.UnitPrice,
                InventoryValue = purchaseOrderDetailVM.TotalAmount,
                UOM = purchaseOrderDetailVM.UOM,
                Quantity = number,
                ReceivingNumber = receivingSummary.ReceivingNumber,
                ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                InstitutionCode = receivingSummary.InstitutionCode,
                InstitutionId = contract.InstitutionId,
                OverallStatus = "Incomplete",
                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                OperationCurrency = contract.OperationalCurrency,
                JournalCode = "IV",
                SourceModule = "Contract",
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                AddedBy = "Variation"
            };

            db.InventoryDetails.Add(inventoryDetail);
        }
        public void SaveAsset(PurchaseOrderDetailVM purchaseOrderDetailVM, ReceivingSummary receivingSummary)
        {
            List<AssetDetail> assetDetails = new List<AssetDetail>();
            Contract contract = db.Contracts.Find(receivingSummary.ContractId);
            decimal quantity = (Decimal)(purchaseOrderDetailVM.Quantity);
            decimal item_quantity = Math.Round(quantity);
            int NumberOfAssets = (int)item_quantity;
            ReceivedAssets summaryReceived = new ReceivedAssets()
            {
                AssetName = purchaseOrderDetailVM.ItemDesc,
                ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                SubLevelCategory = contract.SubLevelCategory,
                SubLevelCode = contract.SubLevelCode,
                SubLevelDesc = contract.SubLevelDesc,
                AssetsValue = purchaseOrderDetailVM.TotalAmount,
                Quantity = NumberOfAssets,
                ReceivingNumber = receivingSummary.ReceivingNumber,
                ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                ReceivingDetailId = receivingSummary.ReceivingSummaryId,
                InstitutionCode = receivingSummary.InstitutionCode,
                InstitutionId = contract.InstitutionId,
                OverallStatus = "Incomplete",
                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                OperationCurrency = contract.OperationalCurrency,
                JournalCode = "PP",
                SourceModule = "Contract",
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now
            };

            db.ReceivedAssets.Add(summaryReceived);
            db.SaveChanges();
            decimal? assetValue = purchaseOrderDetailVM.TotalAmount / NumberOfAssets;
            //Generate and Save Legal number
            var currentId = summaryReceived.ReceivedAssetsId;
            summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "AS", currentId);
            db.Entry(summaryReceived).State = EntityState.Modified;
            db.SaveChanges();

            int i = 0;
            while (i < NumberOfAssets)
            {
                AssetDetail assetDetail = new AssetDetail()
                {

                    AssetName = purchaseOrderDetailVM.ItemDesc,
                    ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                    Currency = contract.OperationalCurrency,
                    AssetValue = (Decimal)assetValue,
                    ReceivingNumber = receivingSummary.ReceivingNumber,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                    InstitutionCode = receivingSummary.InstitutionCode,
                    InstitutionId = contract.InstitutionId,
                    OverallStatus = "Incomplete",
                    ReceivedAssetsId = currentId
                };

                assetDetails.Add(assetDetail);
                i++;
            }

        }
        public void SaveAssetNew(PurchaseOrderDetailVM purchaseOrderDetailVM, ReceivingSummary receivingSummary, int number)
        {
            List<AssetDetail> assetDetails = new List<AssetDetail>();
            Contract contract = db.Contracts.Find(receivingSummary.ContractId);
            int NumberOfAssets = number;
            ReceivedAssets summaryReceived = new ReceivedAssets()
            {
                AssetName = purchaseOrderDetailVM.ItemDesc,
                ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                SubLevelCategory = contract.SubLevelCategory,
                SubLevelCode = contract.SubLevelCode,
                SubLevelDesc = contract.SubLevelDesc,
                AssetsValue = purchaseOrderDetailVM.TotalAmount,
                Quantity = NumberOfAssets,
                ReceivingNumber = receivingSummary.ReceivingNumber,
                ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                ReceivingDetailId = receivingSummary.ReceivingSummaryId,
                InstitutionCode = receivingSummary.InstitutionCode,
                InstitutionId = contract.InstitutionId,
                OverallStatus = "Incomplete",
                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                OperationCurrency = contract.OperationalCurrency,
                JournalCode = "PP",
                SourceModule = "Contract",
                CreatedBy = User.Identity.Name,
                CreatedAt = DateTime.Now,
                AddedBy = "Variation"
            };

            db.ReceivedAssets.Add(summaryReceived);
            db.SaveChanges();
            decimal? assetValue = purchaseOrderDetailVM.TotalAmount / NumberOfAssets;
            //Generate and Save Legal number
            var currentId = summaryReceived.ReceivedAssetsId;
            summaryReceived.AssetsCode = ServiceManager.GetLegalNumber(db, receivingSummary.InstitutionCode, "AS", currentId);
            db.Entry(summaryReceived).State = EntityState.Modified;
            db.SaveChanges();

            int i = 0;
            while (i < NumberOfAssets)
            {
                AssetDetail assetDetail = new AssetDetail()
                {

                    AssetName = purchaseOrderDetailVM.ItemDesc,
                    ContractDetailId = purchaseOrderDetailVM.ContractDetailId,
                    Currency = contract.OperationalCurrency,
                    AssetValue = (Decimal)assetValue,
                    ReceivingNumber = receivingSummary.ReceivingNumber,
                    ReceivingSummaryId = receivingSummary.ReceivingSummaryId,
                    InstitutionCode = receivingSummary.InstitutionCode,
                    InstitutionId = contract.InstitutionId,
                    OverallStatus = "Incomplete",
                    ReceivedAssetsId = currentId
                };

                assetDetails.Add(assetDetail);
                i++;
            }

        }
        public void DeleteInventory(InventoryDetail inventoryDetail, decimal? number)
        {
            inventoryDetail.Quantity = inventoryDetail.Quantity - number;
            var issuingList = db.InventoryIssuings.Where(a => a.InventoryDetailId == inventoryDetail.InventoryDetailId).ToList();
            if (issuingList.Count() > 0)
            {
                foreach (var item in issuingList)
                {
                    if (number > 0)
                    {
                        if (item.IssuedQuantity > number)
                        {
                            item.IssuedQuantity = number;
                            number = 0;
                            db.Entry(item).State = EntityState.Modified;
                        }
                        else
                        {
                            number = number - item.IssuedQuantity;
                            db.Entry(item).State = EntityState.Deleted;
                        }
                    }

                }
            }
            db.Entry(inventoryDetail).State = EntityState.Modified;
        }
        public void DeleteAsset(ReceivedAssets receivedAssets, int number)
        {
            receivedAssets.Quantity = receivedAssets.Quantity - number;
            db.Entry(receivedAssets).State = EntityState.Modified;
            var assetDetailList = db.AssetDetails.Where(a => a.ReceivedAssetsId == receivedAssets.ReceivedAssetsId).OrderByDescending(a => a.AssetDetailId).ToList();
            if (assetDetailList.Count() > 0)
            {
                foreach (var item in assetDetailList)
                {
                    if (number > 0)
                    {
                        db.Entry(item).State = EntityState.Deleted;
                        number = number - 1;
                    }
                }
            }

        }
        [Authorize(Roles = "Works Only Receiving Entry,Except Works Receiving Entry,All Contract Type Receiving Entry")]
        public ActionResult SubContractReceiving(int? id)
        {
            var contract = (from p in db.Contracts
                            where p.ContractId == id
                            select new ContractVM
                            {
                                ContractId = p.ContractId,
                                ContractNumber = p.ContractNumber,
                                ContractNo = p.ContractNo,
                                ContractName = p.ContractName,
                                ContractAmount = p.ContractAmount,
                                ContractDescription = p.ContractDescription,
                                ContractType = p.ContractType,
                                Payeename = p.Payeename,
                                ContractVersion = p.ContractVersion,
                                Currency = p.OperationalCurrency
                            }
                 ).FirstOrDefault();
            decimal? paidAdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.AdvancePayment).DefaultIfEmpty(0).Sum();
            if (paidAdvancePayment > 0)
            {
                contract.AdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - paidAdvancePayment;

            }
            else
            {
                contract.AdvancePayment = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.Type == "AdvancePayment" && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();

            }
            contract.ReceivedAmount = db.ReceivingSummarys.Where(a => a.ContractId == contract.ContractId && a.OverallStatus.ToUpper() != "CANCELLED" && a.Type != "AdvancePayment").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum();
            contract.RemainingAmount = contract.ContractAmount - contract.ReceivedAmount;
            var paymentSheduleList = db.PaymentSchedules.Where(a => a.ContractId == contract.ContractId && a.Received != "Full").ToList();
            List<PaymentScheduleVM> paymentScheduleVMs = new List<PaymentScheduleVM>();
            foreach (var item in paymentSheduleList)
            {
                SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == item.PaymentScheduleId && a.OverallStatus == "Active").FirstOrDefault();

                if (subContractPaymentSchedule != null)
                {
                    if (item.SubContractBalance > 0)
                    {
                        PaymentScheduleVM paymentScheduleVM = new PaymentScheduleVM
                        {
                            PaymentScheduleId = item.PaymentScheduleId,
                            Description = item.Description
                        };
                        paymentScheduleVMs.Add(paymentScheduleVM);
                    }
                }
                else
                {
                    PaymentScheduleVM paymentScheduleVM = new PaymentScheduleVM
                    {
                        PaymentScheduleId = item.PaymentScheduleId,
                        Description = item.Description
                    };
                    paymentScheduleVMs.Add(paymentScheduleVM);
                }
            }
            contract.PaymentScheduleList = new SelectList(paymentScheduleVMs, "PaymentScheduleId", "Description");
            contract.SubContractList = new SelectList(db.SubContracts.Where(a => a.ContractId == contract.ContractId && a.OverallStatus == "Approved"), "SubContractId", "SubContractDesc");
            return View(contract);
        }
        public JsonResult GetSubContract(int? id)
        {
            string response = null;
            try
            {
                SubContract subContract = db.SubContracts.Find(id);
                response = "Success";
                var paymentSchedules = db.SubContractPaymentSchedules.Where(a => a.SubContractId == id && a.OverallStatus == "Active" && a.Balance > 0).ToList();
                var form_data = new { Response = response, PayeeName = subContract.PayeeName, PayeeCode = subContract.PayeeCode, PaymentSchedules = paymentSchedules };

                return Json(form_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPaymentScheduleInfo(int? id)
        {
            string response = null;
            try
            {
                PaymentSchedule paymentSchedule = db.PaymentSchedules.Find(id);
                SubContractPaymentSchedule subContractPaymentSchedule = db.SubContractPaymentSchedules.Where(a => a.PaymentScheduleId == id && a.OverallStatus == "Active").FirstOrDefault();
                decimal? balance, amount;
                if (subContractPaymentSchedule != null)
                {
                    balance = paymentSchedule.SubContractBalance;
                    amount = paymentSchedule.SubContractAmount;
                }
                else
                {
                    balance = paymentSchedule.Amount;
                    amount = paymentSchedule.Balance;
                }
                response = "Success";
                var form_data = new { Response = response, Description = paymentSchedule.Description, Amount = amount, Balance = balance };

                return Json(form_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetSubPaymentScheduleInfo(int? id)
        {
            string response = null;
            try
            {
                SubContractPaymentSchedule paymentSchedule = db.SubContractPaymentSchedules.Find(id);
                response = "Success";
                var form_data = new { Response = response, Description = paymentSchedule.Description, Amount = paymentSchedule.Amount, Balance = paymentSchedule.Balance };

                return Json(form_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult LastReceiving(int? id, string selection)
        {
            string response = null;
            try
            {
                Contract contract = db.Contracts.Find(id);
                if (selection == "YES")
                {
                    if (contract.OverallStatus == "FullReceived")
                    {
                        contract.ClosingStatus = "ClosedWithoutBalance";
                    }
                    else
                    {
                        contract.ClosingStatus = "ClosedWithBalance";
                    }
                    contract.Received = "Full";
                    contract.OverallStatus = "Closed";
                }
                else
                {
                    if (contract.ClosingStatus == "ClosedWithBalance")
                    {
                        contract.Received = "Partial";
                        contract.OverallStatus = "Partial";
                    }
                    else if (contract.ClosingStatus == "ClosedWithoutBalance")
                    {
                        contract.Received = "Full";
                        contract.OverallStatus = "FullReceived";
                    }
                    contract.ClosingStatus = null;
                }
                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ChangeSBC(int? id, string sbc)
        {
            string response = null;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

                CurrencyRateView currencyRateDetail = db.CurrencyRateViews.Where(a => a.SubBudgetClass == sbc && a.InstitutionCode == userPaystation.InstitutionCode).FirstOrDefault();
                if (currencyRateDetail == null)
                {
                    response = "SetupProblem";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                ReceivingSummary receiving = db.ReceivingSummarys.Find(id);
                Contract contract = db.Contracts.Find(receiving.ContractId);
                receiving.OperationalCurrency = currencyRateDetail.OperationalCurrencyCode;
                receiving.CurrentExchangeRate = currencyRateDetail.OperationalExchangeRate;
                receiving.ExchangeRateDate = currencyRateDetail.ExchangeRateDate;
                receiving.BaseCurrency = currencyRateDetail.BaseCurrencyCode;
                receiving.SubBudgetClass = currencyRateDetail.SubBudgetClass;
                receiving.OperationalCurrencyId = currencyRateDetail.OperationalCurrencyId;
                receiving.BaseAmount = receiving.ReceivedAmount * currencyRateDetail.OperationalExchangeRate;
                if (receiving.AdvancePaymentBA > 0)
                {
                    receiving.AdvancePaymentBA = receiving.AdvancePaymentBA * currencyRateDetail.OperationalExchangeRate;
                }
                if (receiving.LiquidedDamageBA > 0)
                {
                    receiving.LiquidedDamageBA = receiving.LiquidedDamageBA * currencyRateDetail.OperationalExchangeRate;
                }

                var count = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == id).Count();
                if (count > 1)
                {
                    response = "GLRemoved";
                    db.ReceivingCoas.RemoveRange(db.ReceivingCoas.Where(a => a.ReceivingSummaryId == receiving.ReceivingSummaryId));
                }
                else
                {
                    response = "Success";
                }

                db.Entry(receiving).State = EntityState.Modified;
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public void UpdateReservedRetention(int? id)
        {
            RetentionDeducted retentionDeducted = db.RetentionDeductions.Where(a => a.ReceivingSummaryId == id && a.OverallStatus == "Pending").FirstOrDefault();

            if (retentionDeducted != null)
            {
                ReservedRetention reservedRetention = db.ReservedRetentions.Where(a => a.PayeeCode == retentionDeducted.PayeeCode && a.InstitutionCode == retentionDeducted.InstitutionCode).FirstOrDefault();
                if (reservedRetention == null)
                {
                    ReservedRetention reservedRetention1 = new ReservedRetention()
                    {
                        PayeeCode = retentionDeducted.PayeeCode,
                        ReservedAmount = retentionDeducted.BaseAmount,
                        TotalPaid = 0,
                        Balance = retentionDeducted.BaseAmount,
                        InstitutionCode = retentionDeducted.InstitutionCode,
                        SubLevelCode = retentionDeducted.SubLevelCode,
                        CreatedBy = User.Identity.Name,
                        CreatedAt = DateTime.Now,
                    };
                    db.ReservedRetentions.Add(reservedRetention1);
                    retentionDeducted.ReservedRetentionId = reservedRetention1.ReservedRetentionId;

                }
                else
                {
                    reservedRetention.ReservedAmount = reservedRetention.ReservedAmount + retentionDeducted.BaseAmount;
                    reservedRetention.Balance = reservedRetention.Balance + retentionDeducted.BaseAmount;
                    db.Entry(reservedRetention).State = EntityState.Modified;
                    retentionDeducted.ReservedRetentionId = reservedRetention.ReservedRetentionId;
                }
                retentionDeducted.OverallStatus = "Approved";
                db.Entry(retentionDeducted).State = EntityState.Modified;
                db.SaveChanges();

            }
        }
        public void RemoveRetention(int? id)
        {
            RetentionDeducted retentionDeducted = db.RetentionDeductions.Where(a => a.ReceivingSummaryId == id).FirstOrDefault();
            if (retentionDeducted != null)
            {
                if (retentionDeducted.OverallStatus == "Approved")
                {
                    ReservedRetention reservedRetention = db.ReservedRetentions.Where(a => a.PayeeCode == retentionDeducted.PayeeCode && a.InstitutionCode == retentionDeducted.InstitutionCode).FirstOrDefault();

                    if (reservedRetention != null)
                    {
                        reservedRetention.ReservedAmount = reservedRetention.ReservedAmount - retentionDeducted.BaseAmount;
                        reservedRetention.Balance = reservedRetention.Balance - retentionDeducted.BaseAmount;
                        db.Entry(reservedRetention).State = EntityState.Modified;
                    }
                }
                retentionDeducted.OverallStatus = "Cancelled";
                db.Entry(retentionDeducted).State = EntityState.Modified;
                db.SaveChanges();

            }
        }
        public bool IsSubtresureOffice()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            string institutionCategory = db.Institution.Where(a => a.InstitutionId == userPaystation.InstitutionId).Select(a => a.InstitutionCategory).FirstOrDefault();
            if (institutionCategory == "Sub Treasury Offices")
            {
                return true;
            }
            else
            {
                return false;
            }


        }
        public List<ContractVM> ReceivingEntries(string institutionCode, string[] statusArray,string ContractType)
        {
            ProcurementController procurement= new ProcurementController();
            List<ContractVM> receivingList = new List<ContractVM>();
            if (procurement.IsTarura(institutionCode))
            {
                string[] institutionCodesArray = procurement.getInstutionCodes(institutionCode);
                if (ContractType=="Works Only") {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where institutionCodesArray.Contains(p.InstitutionCode) && q.ContractType == "Works" && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();

                }
                else if(ContractType == "Except Works Only")
                {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where institutionCodesArray.Contains(p.InstitutionCode) && q.ContractType != "Works" && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                }
                else
                {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where institutionCodesArray.Contains(p.InstitutionCode) && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                }

            



            }
            else
            {

                if (ContractType == "Works Only")
                {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where p.InstitutionCode == institutionCode && q.ContractType == "Works" && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                }
                else if (ContractType == "Except Works Only")
                {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where p.InstitutionCode == institutionCode && q.ContractType != "Works" && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                }
                else
                {
                    receivingList = (from p in db.ReceivingSummarys
                                     join q in db.Contracts on p.ContractId equals q.ContractId
                                     join m in db.PaymentSchedules on p.PaymentScheduleId equals m.PaymentScheduleId
                                     where p.InstitutionCode == institutionCode && p.Type == "Contract" && statusArray.Contains(p.OverallStatus)
                                     select new { p, q, m } into r
                                     select new ContractVM
                                     {
                                         ReceivingSummaryId = r.p.ReceivingSummaryId,
                                         PaymentScheduleId = r.p.PaymentScheduleId,
                                         ContractNo = r.q.ContractNo,
                                         ContractNumber = r.q.ContractNumber,
                                         SubContractId = r.p.SubContractId,
                                         ReceivingNumber = r.p.ReceivingNumber,
                                         ReceivedAmount = r.p.ReceivedAmount,
                                         PaymentScheduleAmount = r.m.Amount,
                                         RemainingAmount = r.m.Amount - db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum(),
                                         PreviousReceived = db.ReceivingSummarys.Where(a => a.PaymentScheduleId == r.m.PaymentScheduleId && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.ReceivedAmount).DefaultIfEmpty(0).Sum() - r.p.ReceivedAmount,
                                         OverallStatus = r.p.OverallStatus,
                                         InvoiceNo = r.p.InvoiceNo,
                                         DeliveryNote = r.p.DeliveryNote,
                                         InspectionReportNo = r.p.InspectionReportNo,
                                         ContractType = r.q.ContractType,
                                         Accrual = r.p.Accrual,
                                         Currency = db.Currencies.Where(a => a.CurrencyId == r.q.OperationalCurrencyId).Select(a => a.CurrencyCode).FirstOrDefault(),
                                         ReceivingCoas = db.ReceivingCoas.Where(a => a.ReceivingSummaryId == r.p.ReceivingSummaryId).ToList()
                                     }).OrderByDescending(a => a.ReceivingSummaryId).ToList();
                }

             
            }
            return receivingList;
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