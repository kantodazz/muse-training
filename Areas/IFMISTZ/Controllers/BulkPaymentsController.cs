using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class BulkPaymentsController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();

        public ActionResult UploadPayments(int? id)
        {
            UploadPaymentsVM vm = new UploadPaymentsVM();

            var paymentBatchesList = ServiceManager.GetPaymentBatchList(db);
            ViewBag.PaymentBatchID = new SelectList(db.PaymentBatches.Where(a => a.PaymentBatchID == id), "PaymentBatchID", "BatchNo");
            vm.PaymentBatchId = (int)id;
            return View(vm);
        }

        [HttpPost]
        public ActionResult UploadPaymentData(UploadPaymentsVM uploadPaymentsVM)
        {
            string response = "";
            string fileExtension = "";
            int bankId = 0;
            string bankName = "";
            string institutionCode = "";
            var schemaPath = "";

            try
            {
                fileExtension = System.IO.Path.GetExtension(Request.Files["FileName"].FileName);

                if (!(fileExtension == ".xls" || fileExtension == ".xlsx"))
                {
                    response = "Invalid file format, file format should be MS Excel!";
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                using (var package = new ExcelPackage(uploadPaymentsVM.FileName.InputStream))
                {
                    var currentSheet = package.Workbook.Worksheets;
                    var workSheet = currentSheet.First();
                    var noOfRows = workSheet.Dimension.End.Row;
                    var noOfCols = workSheet.Dimension.End.Column;

                    PaymentBatch paymentBatches = db.PaymentBatches
                        .Where(a => a.PaymentBatchID == uploadPaymentsVM.PaymentBatchId
                        && a.OverallStatus == "Pending")
                        .FirstOrDefault();

                    List<BulkPayment> existingList = db.BulkPayments
                      .Where(a => a.PaymentBatchID == uploadPaymentsVM.PaymentBatchId
                      && a.OverallStatus == "Pending").ToList();

                    var payments = new List<BulkPayment>();

                    for (int i = 2; i <= noOfRows; i++)
                    {
                        string beneficiaryCode = "";
                        if (workSheet.Cells[i, 1].Value != null)
                        {
                            beneficiaryCode = workSheet.Cells[i, 1].Value.ToString().Replace(" ", "").Replace(" ", "").Replace("-", "").Replace("/", "").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "").Replace(" ", "").Replace("-", "");
                        }
                        else
                        {
                            response = "Beneficiary Code is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string beneficiaryName = "";
                        if (workSheet.Cells[i, 2].Value != null)
                        {
                            beneficiaryName = workSheet.Cells[i, 2].Value.ToString().Replace("'", "").Replace(",", "").Replace("’", "").Replace("`", "").Replace(".", "").Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "");
                        }
                        else
                        {
                            response = "Beneficiary Name is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string bankBic = "";
                        if (workSheet.Cells[i, 3].Value != null)
                        {
                            bankBic = workSheet.Cells[i, 3].Value.ToString();
                            //bankId = (bankBic != "" && db.Banks.Where(a => a.BIC == bankBic).Select(a => a.BankId).Single() > 0) ? db.Banks.Where(a => a.BIC == bankBic).Select(a => a.BankId).Single() : 0;
                            bankId = db.Banks.Where(a => a.BIC == bankBic).Select(a => a.BankId).FirstOrDefault();
                            if (bankId != 0)
                            {
                                bankName = db.Banks.Where(a => a.BankId == bankId).Select(a => a.BankName).FirstOrDefault();
                            }
                            else
                            {
                                //response = "Correct Bank BIC is required!";
                                response = "Bank BIC '" + bankBic + "' does not exist!";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }

                        }
                        else
                        {
                            response = "Bank BIC is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string bankName1 = "";
                        if (workSheet.Cells[i, 4].Value != null)
                        {
                            bankName1 = workSheet.Cells[i, 4].Value.ToString();
                        }

                        string accountNumber = "";
                        if (workSheet.Cells[i, 5].Value != null)
                        {
                            accountNumber = workSheet.Cells[i, 5].Value.ToString().Trim().Replace("-", "").Replace(" ", "").Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "");
                        }
                        else
                        {
                            response = "Account Number is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        decimal amount = 0;
                        if (workSheet.Cells[i, 6].Value != null)
                        {
                            amount = Convert.ToDecimal(workSheet.Cells[i, 6].Value.ToString());
                        }
                        else
                        {
                            response = "Amount is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string description = "";
                        if (workSheet.Cells[i, 7].Value != null)
                        {
                            description = workSheet.Cells[i, 7].Value.ToString().Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "");
                        }
                        else
                        {
                            response = "Payment Description is required!";
                            return Json(response, JsonRequestBehavior.AllowGet);
                        }

                        string Title = "";
                        if (workSheet.Cells[i, 8].Value != null)
                        {
                            Title = workSheet.Cells[i, 8].Value.ToString();
                        }

                        decimal rate = 0;
                        int numberOfDays;
                        if (noOfCols == 10)
                        {
                            if (workSheet.Cells[i, 9].Value != null)
                            {
                                rate = Convert.ToDecimal(workSheet.Cells[i, 9].Value.ToString());
                            }
                            else
                            {
                                response = "Rate is required!";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }

                            if (workSheet.Cells[i, 10].Value != null)
                            {
                                numberOfDays = Convert.ToInt32(workSheet.Cells[i, 10].Value.ToString());
                            }
                            else
                            {
                                response = "Number of Day is required!";
                                return Json(response, JsonRequestBehavior.AllowGet);
                            }

                        }
                        else
                        {
                            rate = 0;
                            numberOfDays = 0;
                        }


                        institutionCode = paymentBatches.InstitutionCode;

                        var payment = new BulkPayment
                        {
                            PaymentBatchID = uploadPaymentsVM.PaymentBatchId,
                            InstitutionId = paymentBatches.InstitutionId,
                            BeneficiaryCode = beneficiaryCode,
                            BeneficiaryName = beneficiaryName,
                            BankID = bankId,
                            BankBic = bankBic,
                            BankName = bankName,
                            BeneficiaryAccountNo = accountNumber,
                            Amount = amount,
                            PaymentDescription = description,
                            PaymentCategory = paymentBatches.PaymentCategory,
                            BatchNo = paymentBatches.BatchNo,
                            BatchDesc = paymentBatches.BatchDesc,
                            Rate = rate,
                            NumberOfDays = numberOfDays,
                            OverallStatus = "Pending",
                            CreatedBy = User.Identity.Name,
                            CreatedAt = DateTime.Now
                        };

                        var existingListCount = existingList
                            .Where(a => a.BeneficiaryAccountNo == payment.BeneficiaryAccountNo
                            && a.Amount == payment.Amount).Count();

                        if (existingListCount == 0)
                        {
                            payments.Add(payment);
                        }

                    }

                    var noofEntry = payments.Count();
                    if (noofEntry < 10)
                    {
                        response = "The bulk payment process require at least ten transactions";
                        return Content(response);
                        //return Json(response, JsonRequestBehavior.AllowGet);
                    }

                    var data = GetData(payments, paymentBatches);
                    XDocument xmlData = XDocument.Parse(data);
                    schemaPath = db.SystemConfigs.Where(a => a.ConfigName == "XMLSchemaPath").Select(a => a.ConfigValue).FirstOrDefault();
                    schemaPath = schemaPath + "schema_block_payment.xsd";

                    var isSchemaValid = XMLTools.ValidateXml(xmlData, schemaPath);
                    if (!isSchemaValid.ValidationStatus)
                    {
                        response = isSchemaValid.ValidationDesc;
                        return Content(response);
                        //return Json(response, JsonRequestBehavior.AllowGet);
                    }


                    db.BulkPayments.AddRange(payments);
                    db.SaveChanges();

                    List<BulkPayment> paymentList = db.BulkPayments
                        .Where(a => a.PaymentBatchID == uploadPaymentsVM.PaymentBatchId
                        && a.OverallStatus == "Pending")
                        .ToList();

                    var totalAmount = paymentList.Sum(a => a.Amount);
                    var noOftransaction = paymentList.Count();

                    foreach (var item in paymentList)
                    {
                        //string endToEndId = institutionCode + "E" + item.BulkPaymentID.ToString().PadLeft(6, '0');
                        string endToEndId = paymentBatches.InstitutionId.ToString() + "E" + item.BulkPaymentID.ToString().PadLeft(11, '0');
                        item.EndtoEndID = endToEndId;
                    }

                    paymentBatches.NoTrx = noOftransaction;
                    paymentBatches.TotalAmount = totalAmount;
                    paymentBatches.UploadStatus = "Yes";

                    db.SaveChanges();
                    response = "Success";

                }
            }

            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = ex.Message.ToString();
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        private string GetData(List<BulkPayment> payments, PaymentBatch paymentBatches)
        {
            var xml_text = new StringBuilder();
            ProcessResponse eftStatus = new ProcessResponse();
            eftStatus.OverallStatus = "Pending";
            string xml_line = "";
            try
            {
                int maxValue = db.BulkPayments
                   .Select(a => a.BulkPaymentID)
                   .DefaultIfEmpty(0)
                   .Max();

                int NbOfTxs = (int)payments.Count();
                decimal TotalAmount = (decimal)payments.Sum(a => a.Amount);
                //MessageId
                int financialyear = ServiceManager.GetFinancialYear(db, DateTime.Now);
                string MsgId = paymentBatches.MsgID;
                eftStatus.StrReturnId = MsgId;

                xml_line = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>";
                xml_line += "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_block_payment.xsd\" >";
                xml_line += "<Header>";

                xml_line += "<Sender>MOFPTZTZ</Sender>";
                xml_line += "<Receiver>" + "TANZTZTX" + "</Receiver>";

                xml_line += "<MsgId>" + MsgId + "</MsgId>";
                xml_line += "<PaymentType>" + "P120" + "</PaymentType>";
                xml_line += "<MessageType>Payment</MessageType>";
                xml_line += "</Header>";

                /*** Begin Payment Block ****/
                xml_line += "<BlockPayment>";

                xml_line += "<MsgSummary>";
                xml_line += "<TransferRef>" + paymentBatches.BatchNo + "</TransferRef>";
                xml_line += "<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</CreDtTm>";
                xml_line += "<NbOfTxs>" + NbOfTxs + "</NbOfTxs>";
                xml_line += "<Currency>TZS</Currency>";
                xml_line += "<TotalAmount>" + TotalAmount + "</TotalAmount>";
                xml_line += "<PayerName>" + "PayerBankName" + "</PayerName>";
                xml_line += "<PayerAcct>" + "9921159101" + "</PayerAcct>";
                xml_line += "<RegionCode>" + "TZDO" + "</RegionCode>";
                xml_line += "</MsgSummary>";


                xml_text.Append(xml_line);
                string priority = "0";
                string endToEndId = "";
                string disbNum = "";
                foreach (var bulkpayment in payments)
                {
                    maxValue = ++maxValue;
                    endToEndId = paymentBatches.InstitutionId + "E" + maxValue.ToString().PadLeft(6, '0');
                    disbNum = maxValue.ToString().PadLeft(6, '0');
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
                    xml_line += "<UnappliedAccount>" + "9921159101" + "</UnappliedAccount>";
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



        public ActionResult Create(int? id)
        {
            BulkPaymentVM vm = new BulkPaymentVM();
            var bankList = ServiceManager.GetBankList(db).Select(a => new { a.BankId, a.BankName }).Distinct();
            vm.bankNameList = new SelectList(bankList, "BankName", "BankName");
            ViewBag.PaymentBatchID = new SelectList(db.PaymentBatches.Where(a => a.PaymentBatchID == id), "PaymentBatchID", "BatchNo");
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BulkPaymentVM bulkPaymentvm)
        {
            if (ModelState.IsValid)
            {
                int bankId = 0;
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                int institutionId = db.PaymentBatches.Find(bulkPaymentvm.PaymentBatchID).InstitutionId;

                if (bulkPaymentvm.BankName != null)
                {
                    bankId = db.Banks
                        .Where(a => a.BankName == bulkPaymentvm.BankName)
                        .Select(a => a.BankId)
                        .FirstOrDefault();
                }

                BulkPayment bulkPayment = new BulkPayment()
                {
                    PaymentBatchID = bulkPaymentvm.PaymentBatchID,
                    InstitutionId = institutionId,
                    BeneficiaryCode = bulkPaymentvm.BeneficiaryCode,
                    BeneficiaryName = bulkPaymentvm.BeneficiaryName,
                    BankID = bankId,
                    BankBic = bulkPaymentvm.BankBic,
                    BankName = bulkPaymentvm.BankName,
                    BeneficiaryAccountNo = bulkPaymentvm.BeneficiaryAccountNo,
                    NumberOfDays = bulkPaymentvm.NumberOfDays,
                    Rate = bulkPaymentvm.Rate,
                    Amount = bulkPaymentvm.Amount,
                    PaymentDescription = bulkPaymentvm.PaymentDescription,
                    OverallStatus = "Pending",
                    CreatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                };
                db.BulkPayments.Add(bulkPayment);
                db.SaveChanges();
                int id = bulkPayment.BulkPaymentID;

                //string endToEndId = userPaystation.InstitutionCode + "E" + id.ToString().PadLeft(6, '0');
                string endToEndId = userPaystation.InstitutionId.ToString() + "E" + id.ToString().PadLeft(11, '0');
                bulkPayment.EndtoEndID = endToEndId;

                List<BulkPayment> paymentList = db.BulkPayments
                    .Where(a => a.PaymentBatchID == bulkPaymentvm.PaymentBatchID
                    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                    .ToList();

                var totalAmount = paymentList.Sum(a => a.Amount);
                var noOftransaction = paymentList.Count();

                PaymentBatch paymentBatches = db.PaymentBatches
                    .Where(a => a.PaymentBatchID == bulkPaymentvm.PaymentBatchID
                    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                    .FirstOrDefault();

                paymentBatches.NoTrx = noOftransaction;
                paymentBatches.TotalAmount = totalAmount;
                paymentBatches.UploadStatus = "Yes";

                db.SaveChanges();

                return RedirectToAction("PaymentConfirmationDetails", "PaymentBatches", new { @id = bulkPaymentvm.PaymentBatchID });
            }
            ViewBag.PaymentBatchID = new SelectList(db.PaymentBatches, "PaymentBatchID", "BatchNo", bulkPaymentvm.PaymentBatchID);
            var bankList = ServiceManager.GetBankList(db).Select(a => new { a.BankId, a.BankName }).Distinct();
            bulkPaymentvm.bankNameList = new SelectList(bankList, "BankName", "BankName", bulkPaymentvm.BankID);
            return View(bulkPaymentvm);
        }

        public ActionResult PaymentAdjustment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BulkPayment bulkPayment = db.BulkPayments.Find(id);
            if (bulkPayment == null)
            {
                return HttpNotFound();
            }
            ViewBag.PaymentBatchID = new SelectList(db.PaymentBatches, "PaymentBatchID", "BatchNo", bulkPayment.PaymentBatchID);

            var data = new BulkPaymentVM()
            {
                BulkPaymentID = (int)id,
                PaymentBatchID = bulkPayment.PaymentBatchID,
                BeneficiaryCode = bulkPayment.BeneficiaryCode,
                BeneficiaryName = bulkPayment.BeneficiaryName,
                BankID = bulkPayment.BankID,
                BankName = bulkPayment.BankName,
                BankBic = bulkPayment.BankBic,
                BeneficiaryAccountNo = bulkPayment.BeneficiaryAccountNo,
                NumberOfDays = bulkPayment.NumberOfDays,
                Rate = bulkPayment.Rate,
                Amount = bulkPayment.Amount,
                PaymentDescription = bulkPayment.PaymentDescription,
                OverallStatus = bulkPayment.OverallStatus,
                EndtoEndID = bulkPayment.EndtoEndID,
                RejectedReason = bulkPayment.RejectedReason,
                InstitutionId = bulkPayment.InstitutionId,
            };

            var bankList = ServiceManager.GetBankList(db).Select(a => new { a.BankId, a.BankName }).Distinct();
            data.bankNameList = new SelectList(bankList, "BankName", "BankName");

            return View(data);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PaymentAdjustment(BulkPaymentVM bulkPaymentvm)
        {

            int bankId = 0;

            if (bulkPaymentvm.BankName != null)
            {
                bankId = db.Banks.Where(a => a.BankName == bulkPaymentvm.BankName).
                    Select(a => a.BankId).FirstOrDefault();
            }

            BulkPayment bulkPayment = db.BulkPayments.Find(bulkPaymentvm.BulkPaymentID);
            bulkPayment.BulkPaymentID = (int)bulkPaymentvm.BulkPaymentID;
            bulkPayment.PaymentBatchID = bulkPaymentvm.PaymentBatchID;
            bulkPayment.InstitutionId = bulkPaymentvm.InstitutionId;
            bulkPayment.BeneficiaryCode = bulkPaymentvm.BeneficiaryCode;
            bulkPayment.BeneficiaryName = bulkPaymentvm.BeneficiaryName;
            bulkPayment.BankID = bankId;
            bulkPayment.BankBic = bulkPaymentvm.BankBic;
            bulkPayment.BankName = bulkPaymentvm.BankName;
            bulkPayment.BeneficiaryAccountNo = bulkPaymentvm.BeneficiaryAccountNo;
            bulkPayment.NumberOfDays = bulkPaymentvm.NumberOfDays;
            bulkPayment.Rate = bulkPaymentvm.Rate;
            bulkPayment.Amount = bulkPaymentvm.Amount;
            bulkPayment.PaymentDescription = bulkPaymentvm.PaymentDescription;
            bulkPayment.OverallStatus = "Pending";
            bulkPayment.CreatedBy = User.Identity.Name;
            bulkPayment.CreatedAt = DateTime.Now;
            db.SaveChanges();

            var batchpaymentid = db.BulkPayments
                .Where(a => a.BulkPaymentID == bulkPaymentvm.BulkPaymentID)
                .Select(a => a.PaymentBatchID)
                .FirstOrDefault();


            List<BulkPayment> paymentList = db.BulkPayments
                    .Where(a => a.PaymentBatchID == batchpaymentid
                    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                    .ToList();

            var totalAmount = paymentList.Sum(a => a.Amount);
            var noOftransaction = paymentList.Count();

            PaymentBatch paymentBatches = db.PaymentBatches
                .Where(a => a.PaymentBatchID == batchpaymentid
                && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                .FirstOrDefault();

            paymentBatches.NoTrx = noOftransaction;
            paymentBatches.TotalAmount = totalAmount;
            paymentBatches.UploadStatus = "Yes";

            db.SaveChanges();

            return RedirectToAction("PaymentConfirmationDetails", "PaymentBatches", new { @id = batchpaymentid });
        }



        [HttpPost]
        public ActionResult DeleteConfirmed(int id)
        {
            string response = "";
            try
            {
                BulkPayment bulkpayment = db.BulkPayments.Find(id);
                bulkpayment.OverallStatus = "Cancelled";
                db.SaveChanges();

                var batchpaymentid = db.BulkPayments
                    .Where(a => a.BulkPaymentID == id)
                    .Select(a => a.PaymentBatchID)
                    .FirstOrDefault();


                List<BulkPayment> paymentList = db.BulkPayments
                   .Where(a => a.PaymentBatchID == batchpaymentid
                   && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                   .ToList();

                var totalAmount = paymentList.Sum(a => a.Amount);
                var noOftransaction = paymentList.Count();

                PaymentBatch paymentBatches = db.PaymentBatches
                    .Where(a => a.PaymentBatchID == batchpaymentid
                    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                    .FirstOrDefault();

                paymentBatches.NoTrx = noOftransaction;
                paymentBatches.TotalAmount = totalAmount;
                paymentBatches.UploadStatus = "Yes";

                //var totalAmount = db.BulkPayments
                //    .Where(a => a.PaymentBatchID == batchpaymentid 
                //    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                //    .Sum(a => a.Amount);

                //var noOftransaction = db.BulkPayments
                //    .Where(a => a.PaymentBatchID == batchpaymentid 
                //    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                //    .Select(a => a.Amount).Count();

                //var paymentbatchList = db.PaymentBatches
                //    .Where(a => a.PaymentBatchID == batchpaymentid 
                //    && (a.OverallStatus == "Pending" || a.OverallStatus == "Rejected in Verification"))
                //    .ToList();

                //foreach (var item in paymentbatchList)
                //{
                //    item.NoTrx = noOftransaction;
                //    item.TotalAmount = totalAmount;
                //}


                string orgEndToEndid = bulkpayment.OrgEndtoEndID;
                var unappliedList = db.Unapplieds
                    .Where(a => a.EndToEndId == orgEndToEndid
                    && a.BulkPaymentStatus == "Add")
                    .FirstOrDefault();

                if (unappliedList != null)
                {
                    unappliedList.BulkPaymentStatus = "Pending";
                }

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
        public JsonResult GetBankBic(string bankName)
        {
            string bankBic = db.Banks
                .Where(a => a.BankName == bankName)
                .Select(a => a.BIC)
                .FirstOrDefault();

            return Json(new
            {
                success = true,
                bankBic = bankBic
            });
        }




        [HttpPost, Authorize()]
        public ActionResult AddControlNumber(BulkPaymentVM vm)
        {
            string response;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                BulkPayment bulkPayment = db.BulkPayments
                     .Where(a => a.BulkPaymentID == vm.BulkPaymentID).FirstOrDefault();
                bulkPayment.ControlNumber = vm.ControlNumber;
                db.SaveChanges();

                response = "Success";
            }
            catch (Exception ex)
            {
                response = ex.InnerException.ToString();
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
